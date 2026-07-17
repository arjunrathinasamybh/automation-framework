# Developer Guide

How to work in this codebase day to day. For the shape of the system see [architecture.md](architecture.md);
for exhaustive contracts and config keys see [technical-reference.md](technical-reference.md).

- [Setting up](#setting-up)
- [Running things](#running-things)
- [Writing a scenario](#writing-a-scenario)
- [Adding a page object](#adding-a-page-object)
- [Adding a sign-in screen](#adding-a-sign-in-screen)
- [Adding a browser](#adding-a-browser)
- [When Microsoft changes their UI](#when-microsoft-changes-their-ui)
- [Debugging a failure](#debugging-a-failure)
- [Conventions](#conventions)
- [CI](#ci)
- [Pitfalls](#pitfalls)

---

## Setting up

Needs the **.NET 10 SDK** and Chrome, Edge or Firefox installed. Browser *drivers* are handled automatically
by Selenium Manager — do not download chromedriver.

```bash
git clone <repo> && cd Office365Automation
dotnet build
dotnet test tests/Automation.UnitTests     # should pass immediately, no setup
```

### Point it at a test account

Only needed for the `@e2e` scenarios. Without this they **skip**, and the build stays green.

```bash
cd tests/Automation.Specs
copy Configuration\credentials.template.json Configuration\credentials.json
```

Set `Username` in the new file. Then keep the secrets **out of every file**:

```bash
dotnet user-secrets set "Credentials:Password" "<password>"

# Optional — omit it and MFA becomes interactive (see below).
dotnet user-secrets set "Credentials:TotpSecret" "<base32-secret>"
```

> **Never put the password or TOTP secret in a committed file.** `credentials.json` is git-ignored;
> `credentials.template.json` is committed and must stay empty. If you find yourself editing the template
> with a real value, stop — you are one `git add` from leaking it.

### MFA without a secret

If you cannot extract the account's TOTP secret, don't. Leave `TotpSecret` empty and make sure
`Browser:Headless` is `false`. The run will **pause at the MFA screen and hand you the browser**:

```text
==================================================================
  ACTION REQUIRED — multi-factor authentication
  Account : svc.automation@contoso.com
  Approve the sign-in request and enter 47 in your Authenticator app.
  Waiting up to 0:05:00 for you to finish.
==================================================================
```

Complete it however the tenant asks — Authenticator, push approval, SMS, hardware key — and the run
continues by itself. Only unattended/CI runs actually need the secret.

---

## Running things

```bash
# No credentials needed
dotnet test tests/Automation.UnitTests        # framework internals, ~150ms
dotnet test --filter "TestCategory=smoke"          # real browser → live sign-in page

# Credentials needed (skips cleanly without them)
dotnet test --filter "TestCategory=e2e"

# Everything
dotnet test
```

### Choosing a browser

Scenarios never name a browser — it is environment, not behaviour. Change `Configuration/browser.json`, or
override per run:

```bash
Browser__Type=Chrome Browser__Mode=Normal Browser__Headless=true dotnet test
```

On Windows PowerShell:

```powershell
$env:Browser__Type="Chrome"; $env:Browser__Headless="true"; dotnet test
```

Any config key works this way — `__` separates sections.

---

## Writing a scenario

Features live in `tests/Automation.Specs/Features/`.

```gherkin
@navigation
Feature: Microsoft 365 sidebar navigation

  Background:
    Given I am signed in to Microsoft 365

  @e2e
  Scenario Outline: Selecting an app from the navigation rail
    When I select "<item>" from the navigation rail
    Then "<item>" is the active navigation item

    Examples:
      | item    |
      | Outlook |
      | Teams   |
```

Steps go in `Steps/`, and take their dependencies through the constructor — the container resolves them
from the scenario's scope:

```csharp
[Binding]
public sealed class NavigationSteps
{
    private readonly M365HomePage _homePage;

    public NavigationSteps(M365HomePage homePage) => _homePage = homePage;

    [When("I select {string} from the navigation rail")]
    public void WhenISelect(string item) => _homePage.Sidebar.NavigateTo(item);
}
```

**Keep steps thin.** A step translates a sentence into a page-object call and asserts. No waits, no
selectors, no retries, no `Thread.Sleep`. If a step is growing logic, that logic belongs in a page object or
in Core — that is what keeps the framework usable outside Reqnroll and the specs readable by someone who
will never open the code.

**Tag `@e2e` on anything that signs in**, so it skips cleanly when no account is configured.

**Don't mention browsers in Gherkin.** A scenario describes behaviour; the browser is configuration.

---

## Adding a page object

```csharp
public sealed class TeamsPage : PageBase
{
    public TeamsPage(IWebDriver driver, IWaitService wait, IElementInteractor interactor,
                     ILogger<TeamsPage> logger)
        : base(driver, wait, interactor, logger) { }

    protected override By PageIdentifier => By.CssSelector("[data-tid='app-bar']");

    public void OpenChat() => Click(By.CssSelector("[data-tid='chat-button']"));
}
```

Register it in `AddO365Pages()`:

```csharp
services.AddScoped<TeamsPage>();
```

It can now be injected into any step. `PageBase` gives you `Click`, `Type`, `TextOf`, `IsDisplayed` and
`WaitUntilLoaded` — a page object should contain **only locators and domain-meaningful actions**.

---

## Adding a sign-in screen

This is the extension point you are most likely to need, and it is deliberately cheap: **one class, one
registration, nothing else changes.**

Say a tenant starts showing a terms-of-use consent page.

**1.** Add its locators to `MicrosoftLoginLocators` — a stable id first, then fallbacks:

```csharp
internal static readonly IReadOnlyList<By> TermsOfUsePrompt =
[
    By.Id("tou_accept"),
    By.XPath("//*[contains(text(), 'Terms of Use')]")
];
```

**2.** Add an order constant in `LoginStepOrder` (lower runs first when several match).

**3.** Write the handler:

```csharp
public sealed class TermsOfUseStepHandler : LoginStepHandlerBase
{
    public TermsOfUseStepHandler(IWaitService wait, IElementInteractor interactor,
                                 ILogger<TermsOfUseStepHandler> logger)
        : base(wait, interactor, logger) { }

    public override string Name  => "TermsOfUse";
    public override int    Order => LoginStepOrder.TermsOfUse;

    public override bool IsCurrentScreen(LoginContext context) =>
        AnyDisplayed(MicrosoftLoginLocators.TermsOfUsePrompt);

    public override void Execute(LoginContext context)
    {
        ClickFirst(MicrosoftLoginLocators.TermsOfUsePrompt, "Accept button");
        WaitForScreenToAdvance(MicrosoftLoginLocators.TermsOfUsePrompt);
    }
}
```

**4.** Register it in `AddO365Pages()`.

That is the whole change. `LoginFlowEngine` is untouched — this is exactly how the SSO account-picker
screen was added after it was discovered against a real tenant.

Rules for handlers:

- **`IsCurrentScreen` must never block.** Use `AnyDisplayed` (wait-free). The engine calls it on every
  handler on every poll; a per-locator wait there costs minutes per sign-in.
- **Finish with `WaitForScreenToAdvance`.** It waits for your screen's markers to clear, and deliberately
  *swallows* a timeout — if the screen doesn't advance, the cause is usually a rejected credential, and the
  higher-priority error handler will report Microsoft's actual message on the next poll. Throwing here would
  replace a precise diagnosis with a generic timeout.

---

## Adding a browser

Implement `IBrowserDriverProvider`. For anything Chromium-based, subclass — you supply only three things:

```csharp
public sealed class BraveDriverProvider : ChromiumDriverProviderBase<ChromeOptions>
{
    public BraveDriverProvider(ILogger<BraveDriverProvider> logger) : base(logger) { }

    public override BrowserType Browser => BrowserType.Brave;      // add to the enum
    protected override string PrivateBrowsingArgument => "--incognito";
    protected override IWebDriver CreateDriver(ChromeOptions options) => new ChromeDriver(options);
}
```

Register it as a singleton `IBrowserDriverProvider`. `WebDriverFactory` picks it up automatically — it
resolves providers by `BrowserType` and has no knowledge of any specific browser.

---

## When Microsoft changes their UI

It will. The failure looks like:

```
The 'Password' step could not find the password field. Microsoft may have changed the sign-in
markup — update the candidate locators in MicrosoftLoginLocators.
Tried: By.Id: i0118 | By.CssSelector: input[name='passwd'] | By.CssSelector: input[type='password']
```

**Only the locator files should ever need editing — one per surface:**

- `src/Automation.Pages/Login/MicrosoftLoginLocators.cs`
- `src/Automation.Pages/M365/SidebarLocators.cs`
- `src/Automation.Pages/Admin/AdminCenterLocators.cs`
- `src/Automation.Pages/Admin/ActiveUsersLocators.cs`

Open the DOM dump in `TestArtifacts/`, find the new markup, and **add** a locator to the front of the list —
don't replace the old ones unless you are certain they're dead. Locators are ordered lists precisely so
several tenant variants can coexist.

Prefer the **accessibility contract** — `role`, `aria-label`, `title` — over CSS classes. M365's class names
are generated and change constantly; the ARIA attributes are what screen readers depend on and are the most
stable thing on the page.

---

## Debugging a failure

**1. Look at the artifacts first.** Every failed scenario writes them automatically:

```
TestArtifacts/Selecting_an_app_20260714-101530-221.png
TestArtifacts/Selecting_an_app_20260714-101530-338.html
```

The screenshot usually answers it outright. Both real bugs found while building this framework were
diagnosed from these — a marketing landing page where a login page was expected, and an SSO account picker
offering the machine's own Windows identity.

**2. Watch it happen.** Set `Headless: false` in `browser.json` and re-run the single scenario.

**3. Read the message.** The exceptions are written to distinguish causes:

- *"rejected by Microsoft: …"* → your credentials are wrong; the quote is Microsoft's own wording.
- *"stalled: the 'X' screen was handled 4 times"* → a screen never advanced. Usually a bad credential with
  no parseable banner, or an unhandled prompt — a new handler may be needed.
- *"could not find the …"* → the markup changed. See above.

**4. Turn up the logs.** Logging is already at `Debug`; every login step logs as it runs, so the step
sequence tells you exactly how far the flow got.

---

## Conventions

- **No `Thread.Sleep` in tests or page objects.** Use `IWaitService`. (The one sleep in the codebase is in
  `TotpProvider`, rolling into the next code window — that one is unavoidable and load-bearing.)
- **No implicit waits.** They compound with explicit waits and make timeouts unpredictable. Pinned at zero.
- **No `WebDriverWait` outside `WaitService`.**
- **No selectors outside the two locator files.**
- **Comments explain *why*, never *what*.** The code says what it does.
- **Secrets never touch a tracked file.**
- Nullable is enabled; keep it warning-clean.

---

## CI

Two workflows, split by what they need:

| Workflow | Runs on | Needs | Status |
| --- | --- | --- | --- |
| [`ci.yml`](../.github/workflows/ci.yml) | every push and PR | nothing | working |
| [`e2e.yml`](../.github/workflows/e2e.yml) | manual dispatch | a test account **with a TOTP secret** | **not yet working** |

`ci.yml` builds and runs the unit tests. No browser, no account, well under a second — which is why it can
guard every push. It is green on a fresh clone.

`e2e.yml` signs in to a real tenant, and **cannot work until the account has a verification-code method
enrolled**. That is arithmetic rather than policy: with no TOTP secret, `Mfa:Mode` resolves to
`Interactive`, which pauses and hands a human the browser. There is no human on a runner, so the handler
fails fast instead of hanging. Enrol the secret, add these two repository secrets, and it starts working:

```text
CREDENTIALS_PASSWORD       the account's password
CREDENTIALS_TOTP_SECRET    the Base32 secret from the authenticator enrolment screen
```

The nightly schedule in that file is commented out on purpose: a scheduled job that fails every night
trains people to ignore it. Run it by hand, and uncomment the schedule once it has been green.

Everything a pipeline needs to override is an environment variable, because they outrank every config
file — the committed files describe a developer's machine, and these describe a runner:

```yaml
env:
  Browser__Headless: "true"
  Browser__Type: ${{ matrix.browser }}          # Chrome | Edge | Firefox — the scenarios never name one
  Mfa__Mode: "Totp"                             # the only mode that can run unattended
  Evidence__Mode: "OnFailure"
  Credentials__Password: ${{ secrets.CREDENTIALS_PASSWORD }}
  Credentials__TotpSecret: ${{ secrets.CREDENTIALS_TOTP_SECRET }}
```

A browser matrix is a `strategy` block away, and costs no change to any scenario — that is the point of
keeping the browser out of the Gherkin.

Publish `TestArtifacts/` and `LivingDoc/living-doc.html` as build artifacts — the first for debugging red
builds, the second as stakeholder-facing documentation of what was verified.

**CI must supply `TotpSecret`.** Headless runs cannot use interactive MFA (there is no window to act in), and
the framework fails fast saying so rather than hanging.

---

## Pitfalls

**Don't run E2E scenarios in parallel.** Entra ID throttles rapid repeated sign-ins from one account; the
resulting authentication failures look exactly like product bugs. The suite is pinned to sequential in
`AssemblyInfo.cs`. Raise it only with a pool of distinct test accounts.

**Don't set a window narrower than ~1024px.** The M365 rail collapses below that, and navigation assertions
fail for a reason that has nothing to do with the code.

**Don't use `Mode: Normal` casually on a domain-joined machine.** Entra ID will offer the machine's Windows
identity via SSO. The framework always declines it (`AccountPickerStepHandler`), but InPrivate avoids the
whole situation — which is why it is the default.

**Don't assert an exact set of sidebar items.** The rail is licence- and tenant-dependent; pinning it makes
the suite fail on a tenant that is merely configured differently rather than broken.

**Don't add `DotNetSeleniumExtras.WaitHelpers`.** Unmaintained for years and a standard dependency-audit
finding. The wait conditions here are hand-written for that reason.

**Don't reach for SpecFlow.** It was retired end-2024 and never supported .NET 8+. This project uses
Reqnroll, its maintained successor — same Gherkin syntax.

# Office 365 Automation

BDD UI automation for Microsoft 365 — Reqnroll (Gherkin) over Selenium 4 on .NET 10. Signs in to Entra ID
(password + TOTP MFA) and drives the Microsoft 365 app launcher, with the browser and privacy mode
selectable per run.

Behaviour is specified in [feature files](tests/Automation.Specs/Features); every run produces a
**living documentation** report of the scenarios and their outcomes.

📖 **[Architecture](docs/architecture.md)** · **[Technical Reference](docs/technical-reference.md)** ·
**[Developer Guide](docs/developer-guide.md)** — see [docs/](docs/README.md).

## Quick start

```bash
dotnet build

# Needs no credentials.
dotnet test tests/Automation.UnitTests            # framework unit tests
dotnet test --filter "TestCategory=smoke"              # a real browser, to the live sign-in page
```

## BDD

Scenarios live in `tests/Automation.Specs/Features/*.feature`:

```gherkin
Scenario: An incorrect password is reported, not retried
  Given I have an incorrect password
  When I attempt to sign in
  Then sign-in is rejected
  And the failure explains that Microsoft rejected the credentials
```

**Reqnroll, not SpecFlow.** SpecFlow was retired by Tricentis at the end of 2024 and never supported
.NET 8+. Reqnroll is the actively-maintained successor by SpecFlow's original creator, with the same
Gherkin syntax.

Step definitions ([`Steps/`](tests/Automation.Specs/Steps)) are deliberately thin — they translate a
sentence into a page-object call and assert. No waits, no retries, no selectors. All of that stays behind
`IAuthenticationService` and the page objects, so the framework remains usable without Reqnroll and the
specs stay readable by someone who will never open the code.

**Scenarios never mention the browser.** It is environment, not behaviour: it comes from
`Configuration/browser.json` or from `Browser__Type` / `Browser__Mode` in CI, so one specification
describes the behaviour on every browser.

Dependency injection is wired in [`ScenarioDependencies`](tests/Automation.Specs/Support/ScenarioDependencies.cs).
Reqnroll's container plugin creates a scope per scenario, which is exactly how the framework registers its
services — `IWebDriver` is scoped, so **one scenario == one browser**, quit automatically when the scenario
ends. No hook has to remember to close it.

### Living documentation

Reqnroll emits Cucumber Messages as the run proceeds, and the HTML formatter turns them into a
self-contained report of every scenario and its outcome — readable by a stakeholder who will never open
the code. Configured in [`reqnroll.json`](tests/Automation.Specs/reqnroll.json).

```bash
dotnet test tests/Automation.Specs
# → tests/Automation.Specs/bin/Debug/net10.0/LivingDoc/living-doc.html
```

(SpecFlow+ LivingDoc, the old tool for this, was retired along with SpecFlow. This is its replacement, and
it needs no licence.)

To run the end-to-end scenarios, point the suite at a test account (see **Configuration** below):

```bash
cd tests/Automation.Specs
copy Configuration\credentials.template.json Configuration\credentials.json   # then set Username

dotnet user-secrets set "Credentials:Password" "<password>"

# Optional. Without it, MFA becomes interactive: the run pauses and hands you the browser.
dotnet user-secrets set "Credentials:TotpSecret" "<base32-authenticator-secret>"

dotnet test --filter "TestCategory=e2e"
```

Without credentials the `@e2e` scenarios **skip** rather than fail, so a fresh clone stays green.

The admin center scenarios are additionally tagged `@admin`, because they need an account holding an
administrative role — without one Microsoft refuses the portal outright, which is a fact about the account
rather than a defect. On a tenant whose test account is unprivileged, exclude them:

```bash
dotnet test --filter "TestCategory=e2e&TestCategory!=admin"
```

## Configuration

Settings live in `tests/Automation.Specs/Configuration/`, one file per concern — because they have
different lifecycles, and separate files let `.gitignore` draw the line precisely:

| File | Holds | Committed? |
| --- | --- | --- |
| `application.json` | URLs, login host, artifacts folder | Yes |
| `browser.json` | Browser, privacy mode, headless, window, profile | Yes |
| `timeouts.json` | Wait and login timeout policy | Yes |
| `mfa.json` | How MFA is answered — automatic, TOTP, or interactive | Yes |
| `credentials.template.json` | An empty shape to copy from | Yes — **empty values only** |
| `credentials.json` | The real account | **No — git-ignored** |
| `local.json` | Any section, overridden on one machine | **No — git-ignored** |

All are JSON-with-comments, and each option is documented in place.

Sources are layered, later winning over earlier:

```text
application/browser/timeouts.json  →  credentials.json  →  local.json  →  user-secrets  →  environment
```

So the username can sit in `credentials.json` while the password and TOTP secret stay out of every file —
in user-secrets locally, and in the pipeline's secret store as `Credentials__Password` /
`Credentials__TotpSecret` in CI. **No secret ever needs to exist inside the repository.**

The layering lives in [`AutomationConfigurationBuilder`](src/Automation.Core/Configuration/AutomationConfigurationBuilder.cs),
not in the test project, so a console runner or a future project reuses it unchanged.

## Choosing a browser

The scenarios never name a browser — it is environment, not behaviour. Set it in
`Configuration/browser.json`:

```json
"Browser": { "Type": "Edge", "Mode": "InPrivate", "Headless": false }
```

…or override it per run, which is how a CI matrix drives the same specifications across browsers:

```bash
Browser__Type=Chrome  Browser__Mode=Normal  Browser__Headless=true  dotnet test
```

`Type` is Chrome, Edge or Firefox. `Mode` is `Normal` or `InPrivate` (Chrome incognito, Edge InPrivate,
Firefox private browsing). Drivers are resolved automatically by Selenium Manager — there is nothing to
download or keep in step with browser updates.

A hook can still override the choice programmatically via `BrowserSessionContext.Use(...)` before the
browser is first touched — useful if you later want `@chrome`-style tags — but the default keeps the
specifications free of environment detail.

### Why InPrivate is the default

On a domain- or Entra-joined machine, a **Normal** browser session offers the signed-in Windows identity
through seamless SSO, and Entra ID answers with a "Pick an account" screen instead of an email prompt.
Accepting that identity would silently run the suite as whoever is logged into the build agent.

The framework never does: `AccountPickerStepHandler` always chooses "Use another account" so it
authenticates as the configured account. **InPrivate** avoids the situation altogether, which is why it is
the default. Use `Normal` with `Browser:UserDataDirectory` when you deliberately want a signed-in profile
to persist across runs.

## Layout

| Project | Contains |
| --- | --- |
| `src/Automation.Core` | Browser factory, waits, element interactions, the sign-in engine, diagnostics. Knows nothing about Microsoft's markup — or about Reqnroll. |
| `src/Automation.Pages` | Everything Microsoft-specific: sign-in step handlers, selectors, M365 page objects. |
| `tests/Automation.Specs` | The BDD suite: feature files, step definitions, hooks, configuration. |
| `tests/Automation.UnitTests` | Plain NUnit tests of the framework itself (TOTP, session isolation). No browser, no network. |

The unit tests stay NUnit on purpose. They verify framework internals — "a Base32 secret produces a
six-digit code" — which no stakeholder will ever read, so writing them as Gherkin would be ceremony with
no audience. BDD is for the behaviour, not for the plumbing under it.

### Sign-in is a state machine, not a script

Entra ID's sign-in is not a fixed sequence. Depending on tenant policy, account type and machine state a
run may see an account picker, a work/personal chooser, a push-approval prompt, a code prompt, a
"Stay signed in?" prompt — or none of them.

So rather than scripting a fixed order, each screen is an `ILoginStepHandler` that can recognise itself:

```csharp
public interface ILoginStepHandler
{
    int  Order { get; }          // ties broken by priority; errors outrank everything
    bool IsCurrentScreen();      // fast, non-blocking probe
    void Execute(LoginContext context);
}
```

`LoginFlowEngine` loops — "which screen is showing? handle it" — until an authenticated URL is reached.
**Supporting a new screen means adding one class and one registration line; no existing code changes.**
The engine itself contains no Microsoft selectors and would work against any identity provider.

Two details worth knowing:

- **Errors are handled first.** A rejected password renders the error banner *on the password screen*, so
  without priority the flow would simply retype the bad password. Instead it fails immediately with
  Microsoft's own wording ("Your account or password is incorrect").
- **Push approval is escaped, not waited on.** Number-matching push cannot be automated. When a tenant
  defaults to it, `VerificationMethodStepHandler` takes the "I can't use my Authenticator app right now"
  route to a verification-code prompt, which the TOTP secret can satisfy.

A stalled flow (the same screen handled three times) fails with a diagnosis rather than hanging until the
timeout.

### MFA — a secret is optional

Set the mode in `Configuration/mfa.json`:

| Mode | Behaviour | Unattended? |
| --- | --- | --- |
| `Automatic` *(default)* | TOTP if a secret is configured, otherwise Interactive | in CI, yes |
| `Totp` | Generate the code from `Credentials:TotpSecret` | yes |
| `Interactive` | Pause and hand the browser to you | no |

**`Totp`** — `TotpProvider` generates RFC 6238 codes from the account's Base32 secret, rolling over to the
next 30-second window when the current code is nearly expired (which prevents the spurious "that code
didn't work" failures caused by a code expiring mid-submission). The account needs a *verification-code*
method enabled in Entra ID; capture the secret key shown during enrolment — the text under the QR code, not
the QR image.

**`Interactive`** — no secret needed. The run pauses at the MFA screen, prints what to do, and **hands you
the live browser**. You satisfy the challenge exactly as you would signing in by hand, and the framework
detects that you're through and carries on:

```text
==================================================================
  ACTION REQUIRED — multi-factor authentication
  Account : svc.automation@contoso.com
  Approve the sign-in request and enter 47 in your Authenticator app.
  Waiting up to 0:05:00 for you to finish.
==================================================================
```

This supports **every authenticator type** — Authenticator code, push approval with number matching, SMS,
hardware key — without modelling any of them, because a human answers whatever the tenant asks in the real
page. It waits for *all* MFA screens to clear rather than one specific prompt, which is what makes that
work.

Two consequences worth knowing:

- It **cannot run headless or in CI** — there would be no window for you to act in. The handler fails fast
  with that explanation rather than hanging until the timeout.
- The other MFA handlers **stand down** while it is active. In particular the framework stops steering the
  flow away from push approval: push is the easiest method for a human with a phone, and reshaping the
  screen underneath them mid-approval would be worse than useless.

`Automatic` means the same configuration works in both places: CI injects `Credentials__TotpSecret` and
runs unattended; your machine has no secret, so it asks you. No mode switch, no separate config.

### Locators

Microsoft reskins these pages without notice, so every element is an ordered list of candidate locators —
a stable id first, then attribute and text fallbacks — resolved by `IWaitService.FirstDisplayedOrDefault`.
They target the accessibility contract (`role`, `aria-label`, `title`) rather than generated CSS classes.

When a Microsoft UI change breaks the flow, only the locator files should ever need editing — one per
surface: `MicrosoftLoginLocators`, `SidebarLocators`, `AdminCenterLocators` and `ActiveUsersLocators`.

## Failure diagnostics

Any failing test writes a screenshot and a DOM dump to `TestArtifacts/` automatically. No test has to
remember to do it — it is wired into teardown.

## Adding a page

```csharp
public sealed class TeamsPage : PageBase
{
    protected override By PageIdentifier => By.CssSelector("[data-tid='app-bar']");
    // ... only locators and domain actions; waits and retries live in the base class
}
```

Register it in `AddO365Pages()` and it can be resolved from any test.

## Notes

- Tests run **sequentially** (`AssemblyInfo.cs`). Entra ID throttles rapid repeated sign-ins from one
  account, and parallel fixtures against a single test account produce authentication failures that look
  like product bugs. Raise the parallelism only alongside a pool of distinct test accounts.
- Implicit waits are left at zero deliberately. Mixing them with explicit waits produces compounding,
  unpredictable timeouts; all waiting goes through `IWaitService`.

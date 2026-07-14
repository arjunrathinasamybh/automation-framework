# Technical Reference

The contract-level detail: every public abstraction, every configuration key, every failure mode. For *why*
it is shaped this way, see [architecture.md](architecture.md). For *how to work in it*, see
[developer-guide.md](developer-guide.md).

- [Platform and dependencies](#platform-and-dependencies)
- [Configuration reference](#configuration-reference)
- [Core abstractions](#core-abstractions)
- [Service registrations and lifetimes](#service-registrations-and-lifetimes)
- [The sign-in flow](#the-sign-in-flow)
- [Page objects](#page-objects)
- [Failure modes](#failure-modes)
- [Test categories and filters](#test-categories-and-filters)
- [Outputs](#outputs)

---

## Platform and dependencies

| | |
| --- | --- |
| Target framework | `net10.0` |
| Language | C# (nullable enabled, implicit usings) |
| Solution format | `.slnx` (the .NET 10 XML solution format) |

| Package | Version | Used for |
| --- | --- | --- |
| `Selenium.WebDriver` | 4.46.0 | Browser automation; Selenium Manager resolves drivers |
| `Selenium.Support` | 4.46.0 | `WebDriverWait` |
| `Otp.NET` | 1.4.1 | RFC 6238 TOTP generation |
| `Microsoft.Extensions.*` | 10.0.9 | Configuration, DI, options, logging |
| `Reqnroll.NUnit` | 3.3.4 | Gherkin binding, Cucumber Messages |
| `Reqnroll.Microsoft.Extensions.DependencyInjection` | 3.3.4 | Container scope per scenario |
| `NUnit` | 4.3.2 | Assertions and the unit-test suite |
| `NUnit3TestAdapter` | 5.0.0 | Test discovery |

Browser drivers are **not** a dependency: Selenium Manager (built into Selenium 4.6+) downloads and pins the
matching driver automatically.

---

## Configuration reference

Files live in `tests/O365.Automation.Specs/Configuration/`. All are JSON-with-comments (the .NET JSON
configuration provider skips comments).

**Precedence — later wins:**

```text
application.json · browser.json · timeouts.json · mfa.json
   → credentials.json → local.json → user-secrets → environment variables
```

Environment variables use `__` (double underscore) as the section separator: `Browser__Type=Chrome`.

### `Application` — `application.json`

| Key | Default | Notes |
| --- | --- | --- |
| `BaseUrl` | `https://m365.cloud.microsoft/` | The **authenticated landing page**. |
| `SignInUrl` | `https://m365.cloud.microsoft/?auth=2` | Where sign-in **starts**. Deliberately not `BaseUrl` — see below. |
| `LoginHost` | `login.microsoftonline.com` | Used to detect "still on the sign-in flow". |
| `AuthenticatedUrlMarkers` | `m365.cloud.microsoft`, `office.com`, `microsoft365.com` | Any match (and not on `LoginHost`) means authenticated. |
| `ArtifactsDirectory` | `TestArtifacts` | Where failure screenshots and DOM dumps are written. |

> **Why `SignInUrl` exists.** Hitting `BaseUrl` anonymously serves a **public marketing page** with a
> "Sign in" button — it does *not* redirect to Entra ID. This was found by running against the real service,
> not assumed. `?auth=2` forces the work-or-school flow so automation lands on the credential page directly.

### `Browser` — `browser.json`

| Key | Type | Default | Notes |
| --- | --- | --- | --- |
| `Type` | `Chrome` \| `Edge` \| `Firefox` | `Edge` | |
| `Mode` | `Normal` \| `InPrivate` | `InPrivate` | Chrome incognito / Edge InPrivate / Firefox private browsing. |
| `Headless` | bool | `false` | Incompatible with interactive MFA. |
| `StartMaximized` | bool | `true` | Ignored when `WindowWidth`/`Height` are set. |
| `WindowWidth` / `WindowHeight` | int? | `null` | **Do not set below ~1024px wide** — the M365 rail collapses and navigation assertions fail for the wrong reason. |
| `UserDataDirectory` | string? | `null` | Persistent profile. **Only honoured when `Mode` is `Normal`** — InPrivate discards profile state by definition. |
| `DownloadDirectory` | string? | `null` | Created on demand. |
| `AdditionalArguments` | string[] | `[]` | Escape hatch for switches not modelled above. |

### `Timeouts` — `timeouts.json`

| Key | Default | Notes |
| --- | --- | --- |
| `ElementSeconds` | 30 | Default explicit wait. |
| `PageLoadSeconds` | 60 | Applied to the driver's page-load timeout. |
| `LoginSeconds` | 120 | Budget for the whole sign-in sequence. |
| `PollingMilliseconds` | 500 | Explicit-wait poll interval. |

Implicit waits are deliberately left at **zero** and are not configurable.

### `Mfa` — `mfa.json`

| Key | Type | Default |
| --- | --- | --- |
| `Mode` | `Automatic` \| `Totp` \| `Interactive` | `Automatic` |
| `InteractiveTimeoutSeconds` | int | `300` |

`Automatic` resolves to `Totp` when a TOTP secret is configured, otherwise `Interactive`. This is what lets
the same configuration run unattended in CI (secret injected) and hands-on locally (no secret).

### `Credentials` — `credentials.json` *(git-ignored)*

| Key | Notes |
| --- | --- |
| `Username` | UPN of the automation account. |
| `Password` | **Supply via user-secrets or `Credentials__Password`.** |
| `TotpSecret` | **Optional.** Base32 secret from authenticator enrolment. Omit it and MFA becomes interactive. |
| `StaySignedIn` | Answer to the KMSI prompt. Pointless for InPrivate, which discards the cookie. |

`credentials.template.json` is committed and must contain **only empty values**. `credentials.json` is
git-ignored. That separation is the whole reason there are two files.

---

## Core abstractions

Every seam you can extend or replace.

### `IBrowserDriverProvider`

```csharp
BrowserType Browser { get; }
IWebDriver Create(BrowserSettings settings);
```

Launches exactly one browser. **The Open/Closed seam of the driver layer**: a new browser means a new
implementation plus one registration — `WebDriverFactory` never changes.

### `IWebDriverFactory`

```csharp
IWebDriver Create(BrowserSettings settings);
```

Resolves the provider registered for `settings.Type` and applies cross-cutting policy. Throws
`NotSupportedException` naming the registered browsers if none matches.

### `IWaitService`

```csharp
IWebElement UntilVisible(By locator, TimeSpan? timeout = null);
IWebElement UntilClickable(By locator, TimeSpan? timeout = null);
void        UntilGone(By locator, TimeSpan? timeout = null);
void        UntilUrlContains(string fragment, TimeSpan? timeout = null);
void        UntilDocumentReady(TimeSpan? timeout = null);
TResult     Until<TResult>(Func<IWebDriver, TResult> condition, TimeSpan? timeout = null);

bool         IsDisplayed(By locator);                                       // non-blocking
IWebElement? FirstDisplayedOrDefault(IEnumerable<By> locators, TimeSpan? timeout = null);
```

`IsDisplayed` returns **immediately** — it is the probe the login handlers use, and the engine calls it on
every handler on every poll. `FirstDisplayedOrDefault` returns `null` on timeout ("none matched" is a
legitimate answer) and backs the multi-locator strategy.

### `IElementInteractor`

```csharp
void Click(IWebElement element);
void Type(IWebElement element, string text);
```

Holds the resilience quirks so they exist exactly once: scroll-into-view, JavaScript fallback when a click
is intercepted (M365 layers callouts and loading shims over its own chrome), and a DOM-level clear for the
React-controlled AAD inputs, whose internal state survives `Clear()` and re-renders the old value.

### `IAuthenticationService`

```csharp
void SignIn(CredentialSettings? credentials = null);   // null → configured credentials
bool IsAuthenticated { get; }
```

Throws `AuthenticationFailedException` on rejected credentials, failed MFA, or a stalled flow.

### `ILoginStepHandler`

```csharp
string Name  { get; }
int    Order { get; }                            // lower runs first
bool   IsCurrentScreen(LoginContext context);    // fast, non-blocking
void   Execute(LoginContext context);
```

`IsCurrentScreen` takes the context because the answer can depend on the **credentials in play**, not only
the markup — whether the TOTP or the interactive handler owns an MFA screen depends on whether a secret was
supplied for *this* attempt.

### `ITotpProvider`

```csharp
string GenerateCode(string base32Secret);
int    SecondsUntilExpiry(string base32Secret);
```

`GenerateCode` **blocks briefly and rolls into the next 30-second window** if the current code has under 5
seconds left. Without that, a code valid when read can expire during the form round-trip, producing a
spurious "that code didn't work".

Accepts secrets with spaces/dashes and any case — authenticator apps display them in groups of four.

### `IArtifactCollector`

```csharp
string? CaptureScreenshot(string testName);   // null if capture failed
string? CapturePageSource(string testName);
```

Never throws: a capture failure is logged and swallowed, because the test's own exception is what matters.

---

## Service registrations and lifetimes

`AddAutomationCore(IConfiguration)` + `AddO365Pages()`.

| Lifetime | Services |
| --- | --- |
| **Singleton** | `IBrowserDriverProvider` ×3 (Chrome, Edge, Firefox), `IWebDriverFactory`, `ITotpProvider` |
| **Scoped** | `BrowserSessionContext`, `IWebDriver`, `IWaitService`, `IElementInteractor`, `IArtifactCollector`, `IAuthenticationService`, all `ILoginStepHandler`, all page objects |

**One scope == one scenario == one browser.** `IWebDriver` is scoped and `IDisposable`; disposing the scope
quits the browser.

`IWebDriver` resolves **lazily** via a factory lambda, so `BrowserSessionContext.Use(...)` can still change
the browser choice after the container is built. The same lambda sets `DriverLaunched`, which teardown uses
to avoid launching a browser purely to screenshot a scenario that never opened one.

---

## The sign-in flow

Handlers, in priority order. See [architecture.md](architecture.md#why-the-ordering-exists) for why the
order is what it is.

| Order | Handler | Screen | Action |
| --- | --- | --- | --- |
| 0 | `LoginErrorStepHandler` | Any error banner | Throws with **Microsoft's own wording**. |
| 5 | `AccountPickerStepHandler` | "Pick an account" | Always clicks **"Use another account"**. |
| 10 | `UsernameStepHandler` | Email prompt | Types UPN, Next. |
| 20 | `WorkAccountTileStepHandler` | Work-or-personal chooser | Takes the work tile. |
| 30 | `PasswordStepHandler` | Password | Types password, Sign in. |
| 35 | `InteractiveMfaStepHandler` | *Any* MFA screen | Prompts, then waits for the operator. |
| 40 | `VerificationMethodStepHandler` | Push approval / method list | Escapes push → "Use a verification code". |
| 50 | `TotpStepHandler` | Code prompt | Generates and submits the TOTP code. |
| 60 | `StaySignedInStepHandler` | KMSI | Answers from `Credentials:StaySignedIn`. |

**MFA mode gating.** `InteractiveMfaStepHandler` claims its screens only when the resolved mode is
`Interactive`; `TotpStepHandler` and `VerificationMethodStepHandler` claim theirs only when it is `Totp`.
They cannot both act on the same screen.

In interactive mode the framework deliberately **stops steering away from push approval** — push is the
easiest method for a human holding a phone, and reshaping the screen mid-approval would be worse than
useless.

**Loop guard:** one screen handled more than **3 times** throws, rather than retrying forever.

---

## Page objects

All derive from `PageBase`, which supplies `Click`, `Type`, `TextOf`, `IsDisplayed`, `WaitUntilLoaded` and
requires one member:

```csharp
protected abstract By PageIdentifier { get; }   // proves this page is the one rendered
```

| Type | Purpose |
| --- | --- |
| `MicrosoftLoginPage` | Assertions *about* the sign-in surface. `IsDisplayed` is true for **either** the email prompt **or** the account picker — which one appears is a property of the environment, not the product. |
| `M365HomePage` | The authenticated landing page. Composes `SidebarComponent`. |
| `SidebarComponent` | The navigation rail. |

### `SidebarComponent`

```csharp
void NavigateTo(M365NavigationItem item);
void NavigateTo(string displayName);            // for tenant-specific entries
bool IsItemDisplayed(M365NavigationItem item);  // effectively a licence check
bool IsItemSelected(M365NavigationItem item);   // reads aria-current / aria-selected
IReadOnlyList<string> GetItemNames();           // diagnostic
```

`M365NavigationItem` is **not a closed set** — the rail is licence- and tenant-dependent, so the `string`
overloads exist for anything the enum does not model. `NavigateTo` failing lists the items that *were*
present, which is usually the answer.

---

## Failure modes

Every exception the framework raises deliberately, and what it means.

| Exception | Meaning | Fix |
| --- | --- | --- |
| `AuthenticationFailedException`: *"rejected by Microsoft: …"* | Entra ID showed an error banner. The quoted text is Microsoft's. | Wrong password / locked account / bad code. |
| `AuthenticationFailedException`: *"stalled: the 'X' screen was handled N times"* | Loop guard. A screen never advanced. | Usually a wrong credential with no parseable banner, or an unhandled tenant prompt. |
| `AuthenticationFailedException`: *"did not complete within Ns"* | Overall login budget exhausted. | Raise `Timeouts:LoginSeconds`; check the artifacts. |
| `AuthenticationFailedException`: *"needs to be completed by hand, but the browser is headless"* | Interactive MFA with `Headless: true`. | Set `Headless: false`, or configure `TotpSecret`. |
| `AuthenticationFailedException`: *"not valid Base32"* | The TOTP secret is malformed. | Use the secret **key**, not the QR image or a recovery code. |
| `AuthenticationFailedException`: *"push approval … cannot be automated"* | Tenant requires number matching and no TOTP secret is set (in `Totp` mode). | Configure `TotpSecret`, or use interactive MFA. |
| `NotSupportedException`: *"No IBrowserDriverProvider is registered"* | Unknown `Browser:Type`. | Register a provider, or fix the value. |
| `WebDriverTimeoutException`: *"could not find the …"* wording from `Resolve` | **Microsoft changed their markup.** | Update the candidate locators — see below. |

The last one is the important signal: it names the step and lists every locator tried. **Only two files
should ever need editing** — `MicrosoftLoginLocators` and `SidebarLocators`.

---

## Test categories and filters

| Tag / category | Meaning | Credentials? |
| --- | --- | --- |
| `@smoke` | Drives a real browser to the live sign-in page | No |
| `@e2e` | Signs in for real | **Yes** — skips cleanly without |
| `Unit` | Framework internals; no browser, no network | No |

```bash
dotnet test --filter "TestCategory=smoke"
dotnet test --filter "TestCategory=e2e"
dotnet test tests/O365.Automation.UnitTests
```

Without credentials the `@e2e` scenarios **skip rather than fail**, so a fresh clone stays green.

Scenarios run **sequentially** (`AssemblyInfo.cs`): Entra ID throttles rapid repeated sign-ins from one
account, and parallel scenarios against a single test account produce authentication failures that look
like product bugs.

---

## Outputs

| Path | Contents |
| --- | --- |
| `TestArtifacts/` | Screenshot + DOM dump per failed scenario, timestamped. |
| `LivingDoc/living-doc.html` | Self-contained living documentation of every scenario and its outcome. |
| `LivingDoc/messages.ndjson` | Raw Cucumber Messages, for other reporters. |

Both are regenerated every run and are git-ignored. Living-doc formatters are configured in
[`reqnroll.json`](../tests/O365.Automation.Specs/reqnroll.json).

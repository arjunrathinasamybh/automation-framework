# Documentation

| Document | Read it when |
| --- | --- |
| [Architecture](architecture.md) | You want to understand the shape of the system and *why* it is that shape — the design decisions, what they were weighed against, and what they cost. |
| [Technical Reference](technical-reference.md) | You need the contracts: every abstraction, every configuration key, every failure mode and what it means. |
| [Developer Guide](developer-guide.md) | You are about to write a scenario, add a page, handle a new sign-in screen, or debug a red build. |

New to the project? [Developer Guide → Setting up](developer-guide.md#setting-up) gets you running in a few
minutes; the [Architecture](architecture.md) explains what you are looking at. The
[project README](../README.md) is the one-page summary.

## The short version

BDD UI automation for Microsoft 365 — **Reqnroll** (Gherkin) over **Selenium 4** on **.NET 10**. Signs in to
Entra ID including MFA, drives the Microsoft 365 app launcher, and produces living documentation of every
run.

Three ideas carry most of the design:

**Sign-in is a state machine, not a script.** Entra ID's screens vary by tenant, account and machine, so
each screen is a self-recognising handler and the engine loops until authenticated. A new screen is a new
class — nothing else changes.

**One scenario, one browser, enforced by the container.** Everything that touches a browser is scoped, and
Reqnroll opens a scope per scenario. A leaked browser requires a leaked scope, not a forgotten teardown.

**Secrets never enter the repository.** Configuration is split by lifecycle so `.gitignore` can draw the
line precisely; the password and TOTP secret live in user-secrets or the CI secret store, never in a file.

## Honest limits

The sign-in path is verified as far as the credential screen against the live service. **The authenticated
path is not yet verified** — the sidebar locators are educated guesses until the suite runs against a real
tenant. See [Architecture → Known limits](architecture.md#known-limits).

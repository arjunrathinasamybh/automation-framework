@authentication
Feature: Signing in to Microsoft 365

  As an automation engineer
  I want the suite to sign in to Microsoft 365 reliably, including multi-factor authentication
  So that every other scenario can start from an authenticated session

  # The browser and privacy mode are not mentioned anywhere in these scenarios. They are environment,
  # not behaviour: they come from Configuration/browser.json, or from Browser__Type / Browser__Mode in CI.
  # The same specification therefore describes the behaviour on every browser.

  @smoke
  Scenario: An unauthenticated visitor is asked to sign in
    Given I have not signed in
    When I open Microsoft 365
    Then I am asked to sign in
    And I am not signed in

  @e2e
  Scenario: Signing in with valid credentials
    Given I have valid Microsoft 365 credentials
    When I sign in
    Then I land on the Microsoft 365 home page

  @e2e
  Scenario: An incorrect password is reported, not retried
    Given I have an incorrect password
    When I attempt to sign in
    Then sign-in is rejected
    And the failure explains that Microsoft rejected the credentials

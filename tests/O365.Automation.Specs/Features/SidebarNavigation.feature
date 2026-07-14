@navigation
Feature: Microsoft 365 sidebar navigation

  As an automation engineer
  I want to select items from the Microsoft 365 navigation rail
  So that scenarios can reach the app they need to exercise

  Background:
    Given I am signed in to Microsoft 365

  @e2e
  Scenario: The navigation rail lists the apps available to me
    Then the navigation rail is shown
    And the navigation rail lists at least one item

  @e2e
  Scenario Outline: Selecting an app from the navigation rail
    When I select "<item>" from the navigation rail
    Then "<item>" is the active navigation item

    # Deliberately limited to items every licensed account has. The rail is licence- and tenant-dependent,
    # so asserting on an app the account may not own would fail for a configuration reason, not a defect.
    Examples:
      | item    |
      | Outlook |
      | Teams   |

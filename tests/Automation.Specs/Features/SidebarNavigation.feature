@navigation
Feature: Microsoft 365 sidebar navigation

  As an automation engineer
  I want to see what the Microsoft 365 navigation rail offers the signed-in account
  So that scenarios can reach the app they need to exercise

  Background:
    Given I am signed in to Microsoft 365

  @e2e
  Scenario: The navigation rail lists what is available to me
    Then the navigation rail is shown
    And the navigation rail lists at least one item

  # A "Selecting an app from the navigation rail" outline used to live here, selecting Outlook and then
  # Teams. It was written before anything had run against a real tenant, and it was wrong: signing in to
  # this tenant lands on m365.cloud.microsoft/chat, whose rail offers "New chat", "Search", "Library" and
  # "Microsoft 365 Admin" — no Outlook, no Teams, no app rail at all. It could never pass here, and a
  # scenario that is permanently red teaches people to ignore red.
  #
  # It is deleted rather than rewritten because what should replace it is a product question, not a
  # mechanical one: the rail is licence- and tenant-dependent, so the items worth asserting on are the ones
  # your organisation guarantees every account has. Decide those, and the outline comes back with them.
  # The step definitions it used are still here and still work — this is a missing specification, not a
  # missing capability.

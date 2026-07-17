@admin
Feature: Microsoft 365 admin center

  As an automation engineer
  I want the suite to reach Active users in the Microsoft 365 admin center once signed in
  So that scenarios can exercise tenant user administration

  # These scenarios need an account holding an administrative role. Without one Microsoft refuses the admin
  # center outright — a fact about the account, not a defect in it — so they are tagged separately from the
  # rest of the @e2e suite and can be excluded on a tenant whose test account is unprivileged.

  # Sign-in happens at the admin center, not at the Microsoft 365 portal. The portal is not part of this
  # behaviour: routing through it would load a page no scenario here asserts on, and tie these scenarios
  # to a surface that can change under them for reasons that have nothing to do with administration.
  Background:
    Given I am signed in to the Microsoft 365 admin center

  @e2e
  Scenario: Active users is reached from the admin center navigation
    When I select "Active users" under "Users" in the admin navigation
    Then the active users list is shown
    And the active users list has at least one user

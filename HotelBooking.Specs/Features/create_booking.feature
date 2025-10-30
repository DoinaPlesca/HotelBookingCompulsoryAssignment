Feature: Create booking

  Background:
    Given the system is reset to a known empty state

  @happy
  Scenario: Create a valid booking
    Given a room type "double" exists with capacity 2
    And the date range "2025-11-10" to "2025-11-12" is available for "double"
    When I create a booking with:
      | guestName | Alice Smith |
      | roomType  | double      |
      | checkIn   | 2025-11-10  |
      | checkOut  | 2025-11-12  |
      | guests    | 2           |
    Then the response status should be 201
    And the booking should exist with totalNights 2
    And availability for "double" from "2025-11-10" to "2025-11-12" should be reduced by 1 per night

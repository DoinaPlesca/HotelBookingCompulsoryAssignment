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

  @validation
  Scenario Outline: Reject invalid booking
    When I create a booking with:
      | guestName | <guestName> |
      | roomType  | <roomType>  |
      | checkIn   | <checkIn>   |
      | checkOut  | <checkOut>  |
      | guests    | <guests>    |
    Then the response status should be 400
    And the error message should contain "<error>"

    Examples:
      | guestName   | roomType | checkIn     | checkOut    | guests | error                    |
      | Alice Smith | double   | 2025-11-12  | 2025-11-10  | 2      | check-out after check-in |
      |             | double   | 2025-11-10  | 2025-11-12  | 2      | guestName required       |
      | Bob Jones   | suite    | 2025-11-10  | 2025-11-12  | 5      | exceeds room capacity    |
      | Bob Jones   | double   | 2025-11-10  | 2025-11-10  | 1      | minimum 1 night          |

  @overlap
  Scenario: Prevent overlapping booking in a full room
    Given a room type "double" exists with capacity 1
    And an existing booking:
      | guestName | Jane Roe   |
      | roomType  | double     |
      | checkIn   | 2025-11-10 |
      | checkOut  | 2025-11-12 |
      | guests    | 1          |
    When I create a booking with:
      | guestName | John Doe   |
      | roomType  | double     |
      | checkIn   | 2025-11-11 |
      | checkOut  | 2025-11-13 |
      | guests    | 1          |
    Then the response status should be 409
    And the error message should contain "no availability"

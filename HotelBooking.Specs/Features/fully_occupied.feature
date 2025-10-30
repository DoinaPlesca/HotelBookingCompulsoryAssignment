Feature: Fully occupied date calculation

  Background:
    Given the system is reset to a known empty state
    And a room type "double" exists with capacity 2
    And existing bookings:
      | guestName | roomType | checkIn     | checkOut    | guests |
      | A         | double   | 2025-12-20  | 2025-12-22  | 2      |
      | B         | double   | 2025-12-20  | 2025-12-22  | 1      |

  Scenario: Availability endpoint marks dates as fully occupied
    When I query availability for "double" from "2025-12-20" to "2025-12-22"
    Then the availability response should show:
      | date       | available |
      | 2025-12-20 | 0         |
      | 2025-12-21 | 0         |

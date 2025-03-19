# Deliverables Progress Requirement for Claude 3.7 in Windsurf

## 3.5 Deliverables Progress

A progress tracking frontend is required under the project module, similar to @deliverables.tsx. The following functionality must be implemented:

### Reporting Date Selection

- The reporting date is automatically calculated based on the current date.
- Progress for the associated reporting period must be entered accordingly.

### Progress Tracking Mechanism

- Deliverables must be updated every reporting period (weekly).
- Progress gates will be used to determine progress levels.
- Earned value calculation: % progress x total hours.

### Required Columns/Entries (Linked to Deliverables List)

- Booking code
- Area code
- Discipline
- Document type
- Department
- Internal number
- Client number
- Deliverable type
- Document title
- Total hours

### Additional Required Columns/Entries

- Progress gate
- Total percentage (%) earned: Auto-populated from the aggregate of units across all periods for the deliverable and manually overridable up to the maximum progress gate value (if a gate exists).
- Total earned hours: Calculated from the total percentage earned value.
- Period earned hours: Calculated from the additional percentages the user adds above the total percentage.

### Methods of Progressing a Deliverable

#### Manual Editing

- Users can manually enter the percentage in the Total percentage (%) earned column.
- The minimum percentage a user can set must be the aggregate of earned units across all previous periods / total units for the deliverable.
- The maximum percentage a user can set must be the deliverable gate maximum percentage value, if a gate exists.

#### Deliverable Gate Assignment

- A Deliverable Gate column is included, allowing users to set a gate_id, which automatically assigns a percentage.
- A view-only Deliverable Gate column is set, automatically assigning a percentage.

##### Example:

- If a deliverable is in the Started phase (10 to 49%), it will automatically be assigned 10% initially, with a maximum of 49%.
- If a Deliverable Gate exists, it will limit the percentage a user can set.
- If a user enters a percentage exceeding the deliverable gate maximum, an error should be displayed in the cell.

## 3.5.1 Deliverable Progress Gates

| Status Name                  | Auto Percentage | Max Percentage |
|------------------------------|-----------------|----------------|
| Started                      | 10              | 49             |
| Issued for Checking          | 50              | 69             |
| Issued for Client Review     | 70              | 99             |
| Issued for Construction/Use  | 100             | 100            |

## Frontend Implementation

- The new progress tracking frontend must be implemented under the project module.
- Most columns will be read-only, except for the Total percentage (%) earned column, which allows manual percentage entry.
- This percentage (%) value must calculate the period unit earned based on the total units of the deliverables.

## Integration with Backend Components

- @PROGRESS.cs: Contains period and units.
- Each period must move to a new number weekly after the progress start is defined in @PROJECT.cs.
- The period number must be a view-only component and include a calculated date based on the number.
- The period number must be used when saving progress.

### Progress % Aggregation:

- The total progress % must be an aggregate of all periods for a deliverable.
- Each additional % value entered by the user must be added to the UNITS field for the progress of a particular period.

## Expected Behavior

1. **Period Management**:
   - The system identifies reporting periods based on the PROGRESS_START date defined in PROJECT.cs
   - Each week following PROGRESS_START is assigned a sequential period number
   - Period dates always fall on the same day of the week as the PROGRESS_START date
   - The period number and its corresponding date are displayed in the UI as view-only fields

2. **Progress Entry Workflow**:
   - When users open the progress screen, the reporting date is automatically calculated based on the current date, with the corresponding period number
   - The system determines which period number corresponds to the current date
   - For each deliverable, users enter the new total progress in the "Total percentage (%) earned" column, which may already contain a percentage value based on previous periods (calculated as previous period earned units / total units)
   - The system validates that the entered percentage doesn't exceed gate limitations

3. **Progress Calculation**:
   - The total progress percentage for a deliverable is calculated as the aggregate of all units across all periods and is pre-populated when the progress screen is first loaded
   - Each new percentage entered by the user is added to the UNITS field for that specific period
   - Minimum percentage allowed is the aggregate of earned units across all previous periods divided by total units
   - Maximum percentage allowed is determined by the deliverable gate maximum value (if a gate exists)

4. **Earned Value Calculation**:
   - Total earned hours = Total percentage (%) × Total hours
   - Period earned hours = Additional percentage above previous total × Total hours
   - If a user enters a percentage exceeding the maximum allowed by the gate, an error is displayed

5. **Gate Enforcement**:
   - If a deliverable is assigned a gate (e.g., "Started"), it automatically gets the gate's auto percentage (e.g., 10%)
   - The system restricts progress entries to the gate's maximum percentage (e.g., 49% for "Started")
   - When a deliverable advances to a new gate, its minimum percentage is updated to the new gate's auto percentage
   - When a user attempts to change a deliverable's gate when progress percentage is already present:
     * If the new gate's auto percentage is higher than the current progress percentage, the progress percentage is automatically increased to match the new gate's auto percentage
     * If the new gate's auto percentage is lower than the current progress percentage but within the new gate's maximum percentage, the current progress percentage is maintained
     * If the current progress percentage exceeds the new gate's maximum percentage, the system should prevent the gate change until the progress percentage is adjusted downward

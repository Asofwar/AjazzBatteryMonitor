# Testing Guide & Test Coverage

## Test Architecture
The test suite consists of 3 xUnit test projects located in `tests/`:

1. **`AjazzBattery.Protocol.Tests`**: Unit tests for `YichipBatteryParser.cs`.
   - Valid battery reports (74%, 100%, 0%).
   - Range validation (>100% out of bounds).
   - Buffer length and invalid opcode error handling.
   - Sleep state bitmask parsing.
   - Fully charged (`0xFF`) flag detection.
2. **`AjazzBattery.Core.Tests`**: Unit tests for `BatteryMonitorEngine.cs`.
   - Engine state machine and status updates.
   - Low battery notification debouncing (20%, 10%, 5%).
   - Missing device handling (ensures no fake 100%/0% values).
3. **`AjazzBattery.Integration.Tests`**: Integration testing with `MockHidTransport`.
   - Complete poll cycle from mock transport to status callbacks.
   - Handling of locked interface / sharing violations (`ERROR_SHARING_VIOLATION`).

## Running Tests
```powershell
dotnet test --configuration Release
```
or via automated script:
```powershell
powershell -File scripts/test.ps1
```

# TextDisplayPanel UI Tests

This directory contains FlaUI-based UI automation tests for the `TextDisplayPanel` Windows Forms component.

## Overview

The test suite validates the TextDisplayPanel component's functionality including:

1. **Rendering & Identification**: Verifies the panel renders with correct control types
2. **Visibility Toggles**: Tests show/hide behavior
3. **Text Display**: Validates both short and long text content display
4. **Size Constraints**: Ensures min/max width and height constraints are respected
5. **Scrolling**: Verifies vertical scrollbar appears for overflow content
6. **Non-Interactive Behavior**: Confirms the panel is non-focusable and non-interactive
7. **Positioning**: Tests correct positioning relative to reference forms
8. **TopMost Property**: Verifies the panel stays on top
9. **Read-Only Content**: Ensures the RichTextBox is read-only

## Test Framework

- **FlaUI**: UI Automation framework for .NET (wraps Microsoft UI Automation)
- **xUnit**: Test runner
- **Target Framework**: .NET 8.0 Windows

## Prerequisites

1. .NET 8.0 SDK or later
2. Windows 10 or later (UI Automation requires Windows)
3. Built `TextDisplayPanelTestHost` executable (see "Test Host" section)

## Running the Tests

### From Command Line

```bash
> cd WindowsApp/tests/PromptArqApp.UITests
> dotnet test

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName~TextDisplayPanel_RendersWithCorrectIdentification"
```

### From Visual Studio

1. Open the solution in Visual Studio
2. Build the solution
3. Open Test Explorer (Test > Test Explorer)
4. Click "Run All" or run individual tests

### From VS Code

1. Install the ".NET Core Test Explorer" extension
2. Open the test file
3. Click the test icons in the gutter or use the Test Explorer panel

## Test Structure

Each test follows the Arrange-Act-Assert pattern:

```csharp
[Fact(DisplayName = "Test description")]
public void TestMethod()
{
    // Arrange - Set up test data and launch application
    LaunchApplication();
    
    // Act - Trigger the behavior being tested
    // (e.g., show panel with specific content)
    
    // Assert - Verify expected outcomes
    Assert.True(condition, "Failure message");
}
```

## Important Notes

### TODO Items

### Test Host

The FlaUI tests launch `TextDisplayPanelTestHost`, a standalone WinForms host that only displays `TextDisplayPanel`. This isolation avoids launching the full PromptArq UI and prevents port conflicts when running tests locally.

The host is built alongside the tests via a project reference in `PromptArqApp.UITests.csproj` (`ReferenceOutputAssembly="false"`), so you only need to build the solution once.

`LaunchApplication()` currently targets the host executable at:
```
..\..\..\..\TextDisplayPanelTestHost\bin\Debug\net8.0-windows\TextDisplayPanelTestHost.exe
```
Update the `HostExeRelativePath` constant if your configuration outputs elsewhere.

During execution the host exposes automation hooks:

- `PanelHandleTextBox` (hidden TextBox): receives the panel's window handle so FlaUI can attach to the exact window.
- `ScrollStateTextBox` (hidden TextBox): writes the current `ScrollBarsVisibility` so tests assert scrollbar presence without relying on scrolling gestures.
- The "Show Panel" button and text input controls let the tests drive the host via standard FlaUI actions (text entry, button clicks).

These hooks eliminate fragile tree walks and make the tests resilient to window ordering or focus changes.
### Test Stability

UI Automation tests can be sensitive to:
- Screen resolution and DPI settings
- Window positioning
- Timing issues (hence the `Thread.Sleep()` calls)

If tests are flaky, consider:
- Increasing wait times
- Using explicit waits instead of fixed delays
- Running tests on a clean VM or dedicated test machine

## Test Coverage

| Scenario | Test Count | Status |
|----------|-----------|--------|
| Rendering & Identification | 1 | ✅ Implemented |
| Visibility | 1 | ✅ Implemented |
| Content Display | 2 | ✅ Implemented |
| Size Constraints | 4 | ✅ Implemented |
| Scrolling | 1 | ✅ Implemented |
| Interaction Properties | 3 | ✅ Implemented |
| Positioning | 1 | ✅ Implemented |
| Misc | 2 | ✅ Implemented |
| **Total** | **15** | **✅ Complete** |

## Troubleshooting

### Tests fail to find the application

- Verify the application path in `LaunchApplication()`
- Ensure the application builds successfully
- Check that the executable has the correct permissions

### Tests timeout or hang

- Increase `Thread.Sleep()` durations
- Check if the application requires user interaction to start
- Verify no modal dialogs are blocking the UI

### Cannot find TextDisplayPanel

- Ensure the panel is actually shown during the test
- Verify the panel has identifiable properties (AutomationId, ClassName, etc.)
- Use Inspect.exe (Windows SDK tool) to examine the UI Automation tree

### FlaUI API differences

If you encounter API compatibility issues:
- Check FlaUI documentation: https://github.com/FlaUI/FlaUI
- Verify you're using compatible FlaUI versions
- Some properties may require different access patterns in different FlaUI versions

## Contributing

When adding new tests:

1. Follow the existing naming convention
2. Use descriptive `DisplayName` attributes
3. Add appropriate assertions with clear failure messages
4. Document any special setup requirements
5. Update the Test Coverage table in this README

## Resources

- [FlaUI GitHub](https://github.com/FlaUI/FlaUI)
- [FlaUI Documentation](https://github.com/FlaUI/FlaUI/wiki)
- [Microsoft UI Automation](https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/ui-automation-overview)
- [xUnit Documentation](https://xunit.net/)
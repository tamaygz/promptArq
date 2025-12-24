using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Xunit;

namespace PromptArqApp.UITests
{
    /// <summary>
    /// UI automation tests for TextDisplayPanel component using FlaUI.
    /// Tests cover visibility, content display, sizing constraints, scrolling, and interaction properties.
    /// </summary>
    public class TextDisplayPanelTests : IDisposable
    {
        private readonly UIA3Automation _automation;
        private FlaUI.Core.Application? _application;
        private Window? _mainWindow;

        public TextDisplayPanelTests()
        {
            _automation = new UIA3Automation();
        }

        public void Dispose()
        {
            _mainWindow?.Close();
            _application?.Close();
            _application?.Dispose();
            _automation?.Dispose();
        }

        /// <summary>
        /// Helper method to launch the application and find the main window.
        /// </summary>
        private void LaunchApplication()
        {
            // Use absolute path to the built executable
            // BaseDirectory is: WindowsApp/tests/PromptArqApp.UITests/bin/Debug/net8.0-windows
            // Target is:        WindowsApp/bin/Debug/net8.0-windows/PromptArq.exe
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var exePath = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\..\bin\Debug\net8.0-windows\PromptArq.exe"));
            _application = FlaUI.Core.Application.Launch(exePath);
            
            // Wait for the main window to appear
            Thread.Sleep(2000);
            _mainWindow = _application.GetMainWindow(_automation);
            Assert.NotNull(_mainWindow);
        }

        /// <summary>
        /// Helper method to trigger display of TextDisplayPanel.
        /// This assumes there's a way to programmatically show the panel.
        /// Adjust based on actual application interaction.
        /// </summary>
        private AutomationElement? FindTextDisplayPanel()
        {
            // The TextDisplayPanel is a Form with FormBorderStyle.None
            // We'll need to find it by its properties or automation ID
            // Since it's TopMost and has specific styling, we can search for it
            var condition = new PropertyCondition(_automation.PropertyLibrary.Element.ClassName, "WindowsForms10.Window");
            var allWindows = _automation.GetDesktop().FindAllChildren(condition);
            
            foreach (var window in allWindows)
            {
                // Check if this looks like our TextDisplayPanel
                // (you may need to add an AutomationId to the panel in the actual code)
                if (window.Properties.Name.ValueOrDefault == "TextDisplayPanel" || 
                    window.Properties.ClassName.ValueOrDefault.Contains("TextDisplayPanel"))
                {
                    return window;
                }
            }
            
            return null;
        }

        [Fact(DisplayName = "Test 1: TextDisplayPanel renders with correct identification")]
        public void TextDisplayPanel_RendersWithCorrectIdentification()
        {
            // Arrange
            LaunchApplication();
            
            // Act - Trigger showing the TextDisplayPanel
            // This will depend on your application's API
            // For example, you might need to invoke a menu item or button
            // that shows the panel with test content
            
            // TODO: Add actual trigger mechanism here
            // e.g., _mainWindow.FindFirstDescendant("ShowTextPanelButton")?.AsButton()?.Click();
            
            Thread.Sleep(1000);
            var textPanel = FindTextDisplayPanel();
            
            // Assert
            Assert.NotNull(textPanel);
            // Verify it's a window (Form)
            Assert.Equal(ControlType.Window, textPanel.Properties.ControlType.ValueOrDefault);
        }

        [Fact(DisplayName = "Test 2: TextDisplayPanel visibility toggles correctly")]
        public void TextDisplayPanel_VisibilityTogglesCorrectly()
        {
            // Arrange
            LaunchApplication();
            
            // Act - Show panel
            // TODO: Trigger panel display
            Thread.Sleep(500);
            var panelWhenVisible = FindTextDisplayPanel();
            
            // Assert - Panel is visible
            Assert.NotNull(panelWhenVisible);
            Assert.True(panelWhenVisible.IsOffscreen == false);
            
            // Act - Hide panel
            // TODO: Trigger panel hide (e.g., by clearing content or explicit hide)
            Thread.Sleep(500);
            var panelWhenHidden = FindTextDisplayPanel();
            
            // Assert - Panel is hidden
            Assert.True(panelWhenHidden == null || panelWhenHidden.IsOffscreen);
        }

        [Fact(DisplayName = "Test 3: TextDisplayPanel displays short text content correctly")]
        public void TextDisplayPanel_DisplaysShortTextCorrectly()
        {
            // Arrange
            LaunchApplication();
            const string shortText = "This is a short test message.";
            
            // Act - Show panel with short text
            // TODO: Trigger panel display with shortText
            Thread.Sleep(500);
            var textPanel = FindTextDisplayPanel();
            Assert.NotNull(textPanel);
            
            // Find the RichTextBox inside the panel
            var textBox = textPanel.FindFirstDescendant(cf => 
                cf.ByControlType(ControlType.Edit));
            
            // Assert
            Assert.NotNull(textBox);
            var displayedText = textBox.Properties.Name.ValueOrDefault ??
                               textBox.AsTextBox()?.Text ?? "";
            Assert.Contains(shortText, displayedText);
        }

        [Fact(DisplayName = "Test 4: TextDisplayPanel displays long text content correctly")]
        public void TextDisplayPanel_DisplaysLongTextCorrectly()
        {
            // Arrange
            LaunchApplication();
            var longText = string.Join(" ", 
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
                "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
                "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.",
                "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum.",
                "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia.",
                "This text is intentionally long to test scrolling and text wrapping behavior.",
                "The TextDisplayPanel should handle this gracefully with proper scrollbars.",
                "Additional content to ensure we exceed the minimum height constraints.",
                "More text. And more text. And even more text to fill the panel.",
                "Final line of this very long test content.");
            
            // Act - Show panel with long text
            // TODO: Trigger panel display with longText
            Thread.Sleep(500);
            var textPanel = FindTextDisplayPanel();
            Assert.NotNull(textPanel);
            
            // Find the RichTextBox inside the panel
            var textBox = textPanel.FindFirstDescendant(cf => 
                cf.ByControlType(ControlType.Edit));
            
            // Assert
            Assert.NotNull(textBox);
            var displayedText = textBox.Properties.Name.ValueOrDefault ??
                               textBox.AsTextBox()?.Text ?? "";
            Assert.True(displayedText.Length > 100, "Long text should be displayed");
        }

        [Fact(DisplayName = "Test 5: TextDisplayPanel respects minimum width constraint")]
        public void TextDisplayPanel_RespectsMinimumWidthConstraint()
        {
            // Arrange
            LaunchApplication();
            const int expectedMinWidth = 350; // From TextDisplayPanel.cs constant
            
            // Act - Show panel with minimal content
            // TODO: Trigger panel display with short text
            Thread.Sleep(500);
            var textPanel = FindTextDisplayPanel();
            Assert.NotNull(textPanel);
            
            // Get panel dimensions
            var boundingRect = textPanel.Properties.BoundingRectangle.ValueOrDefault;
            
            // Assert
            Assert.True(boundingRect.Width >= expectedMinWidth, 
                $"Panel width {boundingRect.Width} should be at least {expectedMinWidth}px");
        }

        [Fact(DisplayName = "Test 6: TextDisplayPanel respects minimum height constraint")]
        public void TextDisplayPanel_RespectsMinimumHeightConstraint()
        {
            // Arrange
            LaunchApplication();
            const int expectedMinHeight = 200; // From TextDisplayPanel.cs constant
            
            // Act - Show panel with minimal content
            // TODO: Trigger panel display with short text
            Thread.Sleep(500);
            var textPanel = FindTextDisplayPanel();
            Assert.NotNull(textPanel);
            
            // Get panel dimensions
            var boundingRect = textPanel.Properties.BoundingRectangle.ValueOrDefault;
            
            // Assert
            Assert.True(boundingRect.Height >= expectedMinHeight, 
                $"Panel height {boundingRect.Height} should be at least {expectedMinHeight}px");
        }

        [Fact(DisplayName = "Test 7: TextDisplayPanel respects maximum width constraint")]
        public void TextDisplayPanel_RespectsMaximumWidthConstraint()
        {
            // Arrange
            LaunchApplication();
            
            // Calculate expected max width (30% of screen width)
            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            var maxWidth = screen != null ? (int)(screen.WorkingArea.Width * 0.30) : 600;
            
            // Act - Show panel with very long single line
            var veryLongText = new string('A', 1000);
            // TODO: Trigger panel display with veryLongText
            Thread.Sleep(500);
            var textPanel = FindTextDisplayPanel();
            Assert.NotNull(textPanel);
            
            // Get panel dimensions
            var boundingRect = textPanel.Properties.BoundingRectangle.ValueOrDefault;
            
            // Assert
            Assert.True(boundingRect.Width <= maxWidth + 50, // Allow 50px tolerance
                $"Panel width {boundingRect.Width} should not exceed {maxWidth}px significantly");
        }

        [Fact(DisplayName = "Test 8: TextDisplayPanel respects maximum height constraint")]
        public void TextDisplayPanel_RespectsMaximumHeightConstraint()
        {
            // Arrange
            LaunchApplication();
            
            // Calculate expected max height (80% of screen height)
            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            var maxHeight = screen != null ? (int)(screen.WorkingArea.Height * 0.80) : 800;
            
            // Act - Show panel with very long content
            var veryLongText = string.Join("\n", 
                System.Linq.Enumerable.Range(1, 100)
                    .Select(i => $"Line {i}: This is a test line with some content."));
            // TODO: Trigger panel display with veryLongText
            Thread.Sleep(500);
            var textPanel = FindTextDisplayPanel();
            Assert.NotNull(textPanel);
            
            // Get panel dimensions
            var boundingRect = textPanel.Properties.BoundingRectangle.ValueOrDefault;
            
            // Assert
            Assert.True(boundingRect.Height <= maxHeight + 50, // Allow 50px tolerance
                $"Panel height {boundingRect.Height} should not exceed {maxHeight}px significantly");
        }

        [Fact(DisplayName = "Test 9: TextDisplayPanel shows vertical scrollbar for overflow content")]
        public void TextDisplayPanel_ShowsVerticalScrollbarForOverflow()
        {
            // Arrange
            LaunchApplication();
            var longText = string.Join("\n", 
                System.Linq.Enumerable.Range(1, 50)
                    .Select(i => $"Line {i}: This is a test line with some content to create overflow."));
            
            // Act - Show panel with long content
            // TODO: Trigger panel display with longText
            Thread.Sleep(500);
            var textPanel = FindTextDisplayPanel();
            Assert.NotNull(textPanel);
            
            // Find the RichTextBox with scrollbars
            var textBox = textPanel.FindFirstDescendant(cf => 
                cf.ByControlType(ControlType.Edit));
            Assert.NotNull(textBox);
            
            // Check for scrollbar pattern (indicates scrolling is available)
            var scrollPattern = textBox.Patterns.Scroll.PatternOrDefault;
            
            // Assert
            Assert.NotNull(scrollPattern);
            Assert.True(scrollPattern.VerticalScrollPercent >= 0, 
                "Vertical scrollbar should be available for long content");
        }

        [Fact(DisplayName = "Test 10: TextDisplayPanel is non-focusable")]
        public void TextDisplayPanel_IsNonFocusable()
        {
            // Arrange
            LaunchApplication();
            
            // Act - Show panel
            // TODO: Trigger panel display
            Thread.Sleep(500);
            var textPanel = FindTextDisplayPanel();
            Assert.NotNull(textPanel);
            
            // Try to set focus
            var canFocus = textPanel.Properties.IsKeyboardFocusable.ValueOrDefault;
            
            // Assert
            Assert.False(canFocus, "TextDisplayPanel should not be focusable");
        }

        [Fact(DisplayName = "Test 11: TextDisplayPanel is non-interactive (Enabled = false)")]
        public void TextDisplayPanel_IsNonInteractive()
        {
            // Arrange
            LaunchApplication();
            
            // Act - Show panel
            // TODO: Trigger panel display
            Thread.Sleep(500);
            var textPanel = FindTextDisplayPanel();
            Assert.NotNull(textPanel);
            
            // Check if panel is enabled (it should be disabled for interaction)
            var isEnabled = textPanel.Properties.IsEnabled.ValueOrDefault;
            
            // Assert - According to TextDisplayPanel.cs, Enabled is set to false
            Assert.False(isEnabled, "TextDisplayPanel should be non-interactive (Enabled = false)");
        }

        [Fact(DisplayName = "Test 12: TextDisplayPanel positions correctly relative to reference form")]
        public void TextDisplayPanel_PositionsCorrectlyRelativeToReferenceForm()
        {
            // Arrange
            LaunchApplication();
            Assert.NotNull(_mainWindow);
            
            var mainWindowRect = _mainWindow.Properties.BoundingRectangle.ValueOrDefault;
            
            // Act - Show panel (should position to the left of main window)
            // TODO: Trigger panel display
            Thread.Sleep(500);
            var textPanel = FindTextDisplayPanel();
            Assert.NotNull(textPanel);
            
            var panelRect = textPanel.Properties.BoundingRectangle.ValueOrDefault;
            
            // Assert - Panel should be to the left of the reference form
            // with a margin of 15px (MarginBetweenForms constant)
            var expectedX = mainWindowRect.Left - panelRect.Width - 15;
            
            // Allow some tolerance for positioning
            Assert.True(Math.Abs(panelRect.Left - expectedX) < 50,
                $"Panel X position {panelRect.Left} should be approximately {expectedX}");
            
            // Y position should align with reference form top
            Assert.True(Math.Abs(panelRect.Top - mainWindowRect.Top) < 50,
                "Panel Y position should align with reference form top");
        }

        [Fact(DisplayName = "Test 13: TextDisplayPanel has TopMost property")]
        public void TextDisplayPanel_HasTopMostProperty()
        {
            // Arrange
            LaunchApplication();
            
            // Act - Show panel
            // TODO: Trigger panel display
            Thread.Sleep(500);
            var textPanel = FindTextDisplayPanel();
            Assert.NotNull(textPanel);
            
            // Check if window is topmost
            // Note: FlaUI may not expose IsTopmost directly, so we check if it's always on top
            // by verifying its Z-order or window extended styles
            var windowPattern = textPanel.Patterns.Window.PatternOrDefault;
            
            // Assert - verify the window is modal or always on top
            Assert.NotNull(windowPattern);
            Assert.True(windowPattern.IsTopmost, "TextDisplayPanel should have TopMost = true");
        }

        [Fact(DisplayName = "Test 14: TextDisplayPanel RichTextBox has readonly property")]
        public void TextDisplayPanel_RichTextBoxIsReadOnly()
        {
            // Arrange
            LaunchApplication();
            
            // Act - Show panel
            // TODO: Trigger panel display with some text
            Thread.Sleep(500);
            var textPanel = FindTextDisplayPanel();
            Assert.NotNull(textPanel);
            
            // Find the RichTextBox
            var textBox = textPanel.FindFirstDescendant(cf =>
                cf.ByControlType(ControlType.Edit));
            Assert.NotNull(textBox);
            
            // Check if it's read-only using the Value pattern
            var valuePattern = textBox.Patterns.Value.PatternOrDefault;
            
            // Assert - if value pattern exists and IsReadOnly is true, it's read-only
            Assert.NotNull(valuePattern);
            Assert.True(valuePattern.IsReadOnly, "RichTextBox should be read-only");
        }

        [Fact(DisplayName = "Test 15: TextDisplayPanel hides when empty text is provided")]
        public void TextDisplayPanel_HidesWhenEmptyTextProvided()
        {
            // Arrange
            LaunchApplication();
            
            // Act - First show panel with content
            // TODO: Trigger panel display with text
            Thread.Sleep(500);
            var panelWhenVisible = FindTextDisplayPanel();
            Assert.NotNull(panelWhenVisible);
            
            // Act - Then show with empty text (should hide)
            // TODO: Trigger panel display with empty string
            Thread.Sleep(500);
            var panelWhenEmpty = FindTextDisplayPanel();
            
            // Assert - Panel should be hidden or null
            Assert.True(panelWhenEmpty == null || panelWhenEmpty.IsOffscreen,
                "Panel should hide when empty text is provided");
        }
    }
}
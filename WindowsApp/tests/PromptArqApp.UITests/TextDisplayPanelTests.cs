using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
        private const string HostExeRelativePath = @"..\..\..\..\TextDisplayPanelTestHost\bin\Debug\net8.0-windows\TextDisplayPanelTestHost.exe";

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
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var exePath = Path.GetFullPath(Path.Combine(baseDir, HostExeRelativePath));
            Assert.True(File.Exists(exePath), $"Test host executable not found: {exePath}");
            _application = FlaUI.Core.Application.Launch(exePath);

            Thread.Sleep(1200);
            _mainWindow = _application.GetMainWindow(_automation);
            Assert.NotNull(_mainWindow);
        }

        private AutomationElement? WaitForPanel(int timeoutMs = 2000)
        {
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                var panel = TryGetPanelElementFromHandle();
                if (panel != null)
                {
                    return panel;
                }

                Thread.Sleep(100);
            }

            return null;
        }

        private AutomationElement? SetPanelText(string text, bool expectVisible)
        {
            Assert.NotNull(_mainWindow);

            var inputBox = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("InputTextBox"))?.AsTextBox();
            Assert.NotNull(inputBox);
            inputBox.Focus();
            inputBox.Patterns.Value.Pattern.SetValue(text ?? string.Empty);

            var showButton = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ShowButton"))?.AsButton();
            Assert.NotNull(showButton);
            showButton.Invoke();

            Thread.Sleep(250);

            if (expectVisible)
            {
                return WaitForPanel();
            }

            var hideWatch = Stopwatch.StartNew();
            while (hideWatch.ElapsedMilliseconds < 2000)
            {
                if (FindTextDisplayPanel() == null)
                {
                    return null;
                }

                Thread.Sleep(100);
            }

            return FindTextDisplayPanel();
        }

        private AutomationElement ShowPanelWithText(string text)
        {
            return SetPanelText(text, expectVisible: true)!;
        }

        private void HidePanelViaHost()
        {
            Assert.NotNull(_mainWindow);

            var hideButton = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("HideButton"))?.AsButton();
            Assert.NotNull(hideButton);
            hideButton.Invoke();
            Thread.Sleep(250);
        }

        /// <summary>
        /// Helper method to trigger display of TextDisplayPanel.
        /// This assumes there's a way to programmatically show the panel.
        /// Adjust based on actual application interaction.
        /// </summary>
        private AutomationElement? FindTextDisplayPanel()
        {
            return TryGetPanelElementFromHandle();
        }

        private AutomationElement? FindTextEditor(AutomationElement panel)
        {
            var edit = panel.FindFirstDescendant(cf => cf.ByControlType(ControlType.Edit));
            if (edit != null)
            {
                return edit;
            }

            var document = panel.FindFirstDescendant(cf => cf.ByControlType(ControlType.Document));
            if (document != null)
            {
                return document;
            }

            return null;
        }

        private static string GetElementText(AutomationElement element)
        {
            var valuePattern = element.Patterns.Value.PatternOrDefault;
            if (valuePattern != null && !string.IsNullOrEmpty(valuePattern.Value))
            {
                return valuePattern.Value;
            }

            var textPattern = element.Patterns.Text.PatternOrDefault;
            if (textPattern != null)
            {
                var rangeText = textPattern.DocumentRange.GetText(-1);
                return rangeText?.TrimEnd('\r', '\n') ?? string.Empty;
            }

            return element.Properties.Name.ValueOrDefault ?? string.Empty;
        }

        private string GetScrollStateText()
        {
            Assert.NotNull(_mainWindow);
            var scrollBox = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ScrollStateTextBox"))?.AsTextBox();
            Assert.NotNull(scrollBox);
            return scrollBox.Text ?? string.Empty;
        }

        private AutomationElement? TryGetPanelElementFromHandle()
        {
            if (_mainWindow == null)
            {
                return null;
            }

            var helpText = _mainWindow.Properties.HelpText.ValueOrDefault;
            if (TryParseHandleText(helpText, out var helpHandle))
            {
                return FromHandleSafely(helpHandle);
            }

            var handleBox = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("PanelHandleTextBox"))?.AsTextBox();
            if (handleBox == null || !TryParseHandleText(handleBox.Text, out var boxHandle))
            {
                return null;
            }

            return FromHandleSafely(boxHandle);
        }

        private AutomationElement? FromHandleSafely(IntPtr handle)
        {
            try
            {
                return _automation.FromHandle(handle);
            }
            catch
            {
                return null;
            }
        }

        private static bool TryParseHandleText(string? text, out IntPtr handle)
        {
            handle = IntPtr.Zero;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (int.TryParse(text, out var handleValue) && handleValue != 0)
            {
                handle = new IntPtr(handleValue);
                return true;
            }

            return false;
        }

        [Fact(DisplayName = "Test 1: TextDisplayPanel renders with correct identification")]
        public void TextDisplayPanel_RendersWithCorrectIdentification()
        {
            // Arrange
            LaunchApplication();
            var textPanel = ShowPanelWithText("Identification test content");
            
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
            var panelWhenVisible = ShowPanelWithText("Visibility test content");
            
            // Assert - Panel is visible
            Assert.NotNull(panelWhenVisible);
            Assert.True(panelWhenVisible.IsOffscreen == false);
            
            // Act - Hide panel
            HidePanelViaHost();
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
            var textPanel = ShowPanelWithText(shortText);
            Assert.NotNull(textPanel);

            // Find the RichTextBox inside the panel
            var textBox = FindTextEditor(textPanel);
            
            // Assert
            Assert.NotNull(textBox);
            var displayedText = GetElementText(textBox);
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
            
            var textPanel = ShowPanelWithText(longText);
            Assert.NotNull(textPanel);
            
            // Find the RichTextBox inside the panel
            var textBox = FindTextEditor(textPanel);
            
            // Assert
            Assert.NotNull(textBox);
            var displayedText = GetElementText(textBox);
            Assert.True(displayedText.Length > 100, "Long text should be displayed");
        }

        [Fact(DisplayName = "Test 5: TextDisplayPanel respects minimum width constraint")]
        public void TextDisplayPanel_RespectsMinimumWidthConstraint()
        {
            // Arrange
            LaunchApplication();
            const int expectedMinWidth = 350; // From TextDisplayPanel.cs constant
            
            // Act - Show panel with minimal content
            var textPanel = ShowPanelWithText("Min width content");
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
            var textPanel = ShowPanelWithText("Min height content");
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
            
            var veryLongText = new string('A', 1000);
            var textPanel = ShowPanelWithText(veryLongText);
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
            
            var veryLongText = string.Join("\n", 
                System.Linq.Enumerable.Range(1, 100)
                    .Select(i => $"Line {i}: This is a test line with some content."));
            var textPanel = ShowPanelWithText(veryLongText);
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
            
            var textPanel = ShowPanelWithText(longText);
            Assert.NotNull(textPanel);
            
            // Find the RichTextBox with scrollbars
            var textBox = FindTextEditor(textPanel);
            Assert.NotNull(textBox);
            
            var scrollStateText = GetScrollStateText();

            // Assert
            Assert.Contains("Vertical", scrollStateText, StringComparison.OrdinalIgnoreCase);
        }

        [Fact(DisplayName = "Test 10: TextDisplayPanel is non-focusable")]
        public void TextDisplayPanel_IsNonFocusable()
        {
            // Arrange
            LaunchApplication();
            var textPanel = ShowPanelWithText("Focusable check");
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
            var textPanel = ShowPanelWithText("Disabled state content");
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
            var textPanel = ShowPanelWithText("Positioning test");
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
            var textPanel = ShowPanelWithText("Topmost check");
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
            var textPanel = ShowPanelWithText("Readonly check");
            Assert.NotNull(textPanel);
            
            // Find the RichTextBox
            var textBox = FindTextEditor(textPanel);
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
            var panelWhenVisible = ShowPanelWithText("Initial text");
            Assert.NotNull(panelWhenVisible);
            
            // Act - Then show with empty text (should hide)
            var panelWhenEmpty = SetPanelText(string.Empty, expectVisible: false);
            
            // Assert - Panel should be hidden or null
            Assert.True(panelWhenEmpty == null || panelWhenEmpty.IsOffscreen,
                "Panel should hide when empty text is provided");
        }
    }
}
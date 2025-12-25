using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PromptArqApp.Theming;

namespace PromptArqApp
{
    /// <summary>
    /// Dialog for recording keyboard shortcuts with real-time visual feedback.
    /// </summary>
    public class HotkeyRecorderDialog : BorderlessFormBase
    {
        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;

        private Label _displayLabel = null!;
        private Label _instructionLabel = null!;
        private Label _validationLabel = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;

        private bool _ctrl;
        private bool _alt;
        private bool _shift;
        private bool _win;
        private Keys _mainKey = Keys.None;
        private Keys _mainKey2 = Keys.None;
        private bool _isPreloadedHotkey = false; // Track if we loaded an existing hotkey

        public HotkeyConfig? RecordedHotkey { get; private set; }

        public HotkeyRecorderDialog(HotkeyConfig? existingHotkey = null)
        {
            InitializeComponent();
            
            // Load existing hotkey if provided
            if (existingHotkey != null)
            {
                _ctrl = existingHotkey.Ctrl;
                _alt = existingHotkey.Alt;
                _shift = existingHotkey.Shift;
                _win = existingHotkey.Win;
                _mainKey = ParseKey(existingHotkey.Key);
                _mainKey2 = ParseKey(existingHotkey.Key2);
                _isPreloadedHotkey = true;
                UpdateDisplay();
            }

            // Register with ThemeManager
            ThemeManager.Instance.RegisterForm(this);
            ThemeManager.Instance.ApplyThemeToForm(this);

            // Subscribe to theme changes
            ThemeManager.Instance.ThemeChanged += OnThemeChanged;
            FormClosing += (s, e) => ThemeManager.Instance.ThemeChanged -= OnThemeChanged;
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ThemeManager.Instance.ApplyThemeToForm(this)));
            }
            else
            {
                ThemeManager.Instance.ApplyThemeToForm(this);
            }
        }

        private void InitializeComponent()
        {
            Text = "Record Hotkey";
            Size = new Size(500, 280);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            KeyPreview = true;

            // Title label
            var titleLabel = new Label
            {
                Text = "Record Hotkey",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font(Font.FontFamily, 12, FontStyle.Bold)
            };
            Controls.Add(titleLabel);

            // Instruction label
            _instructionLabel = new Label
            {
                Text = "Press your desired key combination (up to 2 keys, e.g., Ctrl+C+C)...",
                Location = new Point(20, 55),
                Size = new Size(460, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(_instructionLabel);

            // Display label (large, centered)
            _displayLabel = new Label
            {
                Text = "Press keys...",
                Location = new Point(20, 95),
                Size = new Size(460, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(240, 240, 240)
            };
            Controls.Add(_displayLabel);

            // Validation label
            _validationLabel = new Label
            {
                Text = "⚠ At least one modifier key (Ctrl, Alt, Shift, Win) is required",
                Location = new Point(20, 165),
                Size = new Size(460, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.OrangeRed,
                Visible = false
            };
            Controls.Add(_validationLabel);

            // Hint label
            var hintLabel = new Label
            {
                Text = "💡 Press a second key for sequences like Ctrl+C+C",
                Location = new Point(20, 185),
                Size = new Size(460, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray,
                Font = new Font(Font.FontFamily, 8)
            };
            Controls.Add(hintLabel);

            // Save button
            _saveButton = new Button
            {
                Text = "Save",
                Location = new Point(280, 210),
                Size = new Size(100, 35),
                Enabled = false
            };
            _saveButton.Click += SaveButton_Click;
            Controls.Add(_saveButton);

            // Cancel button
            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(390, 210),
                Size = new Size(100, 35),
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(_cancelButton);
            CancelButton = _cancelButton;

            // Handle key events
            KeyDown += HotkeyRecorderDialog_KeyDown;
        }

        private void HotkeyRecorderDialog_KeyDown(object? sender, KeyEventArgs e)
        {
            // Check for ESC to cancel
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            // Capture modifiers
            _ctrl = e.Control;
            _alt = e.Alt;
            _shift = e.Shift;
            _win = IsWinKeyPressed();

            // Capture main key (excluding modifiers)
            var keyCode = e.KeyCode;
            
            // Ignore modifier-only keys
            if (keyCode == Keys.ControlKey || keyCode == Keys.ShiftKey || 
                keyCode == Keys.Menu || keyCode == Keys.LWin || keyCode == Keys.RWin)
            {
                // If this is a preloaded hotkey and user just pressed a modifier,
                // clear the old keys to start fresh recording
                if (_isPreloadedHotkey)
                {
                    _mainKey = Keys.None;
                    _mainKey2 = Keys.None;
                    _isPreloadedHotkey = false;
                }
            }
            else if (keyCode == Keys.Enter && IsValidCombination())
            {
                // Enter to save if valid
                SaveButton_Click(null, EventArgs.Empty);
                return;
            }
            else
            {
                // First key or same key again (for Ctrl+C+C style)
                if (_mainKey == Keys.None)
                {
                    _mainKey = keyCode;
                }
                else if (_mainKey2 == Keys.None && _mainKey != keyCode)
                {
                    // Second different key
                    _mainKey2 = keyCode;
                }
                else if (_mainKey2 == Keys.None && _mainKey == keyCode)
                {
                    // Same key pressed twice (e.g., C+C)
                    _mainKey2 = keyCode;
                }
                // If both keys already set, ignore additional key presses
            }

            UpdateDisplay();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private bool IsWinKeyPressed()
        {
            return (GetKeyState(VK_LWIN) & 0x8000) != 0 || (GetKeyState(VK_RWIN) & 0x8000) != 0;
        }

        private void UpdateDisplay()
        {
            var parts = new System.Collections.Generic.List<string>();

            if (_ctrl) parts.Add("Ctrl");
            if (_alt) parts.Add("Alt");
            if (_shift) parts.Add("Shift");
            if (_win) parts.Add("Win");
            
            if (_mainKey != Keys.None)
            {
                parts.Add(GetFriendlyKeyName(_mainKey));
            }
            
            if (_mainKey2 != Keys.None)
            {
                parts.Add(GetFriendlyKeyName(_mainKey2));
            }

            if (parts.Count == 0)
            {
                _displayLabel.Text = "Press keys...";
                _displayLabel.ForeColor = Color.Gray;
            }
            else
            {
                _displayLabel.Text = string.Join(" + ", parts);
                _displayLabel.ForeColor = IsValidCombination() ? Color.Black : Color.DarkOrange;
            }

            bool isValid = IsValidCombination();
            _saveButton.Enabled = isValid;
            _validationLabel.Visible = !isValid && (_ctrl || _alt || _shift || _win || _mainKey != Keys.None);
        }

        private bool IsValidCombination()
        {
            // Must have at least one modifier and at least one main key
            bool hasModifier = _ctrl || _alt || _shift || _win;
            bool hasMainKey = _mainKey != Keys.None;
            return hasModifier && hasMainKey;
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            if (!IsValidCombination())
                return;

            RecordedHotkey = new HotkeyConfig
            {
                Action = "", // Will be set by caller
                Key = FormatKeyForStorage(_mainKey),
                Key2 = _mainKey2 != Keys.None ? FormatKeyForStorage(_mainKey2) : "",
                Ctrl = _ctrl,
                Alt = _alt,
                Shift = _shift,
                Win = _win
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private string GetFriendlyKeyName(Keys key)
        {
            // Remove modifiers from the key
            var baseKey = key & Keys.KeyCode;

            // Handle digit keys (D0-D9 → "0"-"9")
            if (baseKey >= Keys.D0 && baseKey <= Keys.D9)
                return ((int)(baseKey - Keys.D0)).ToString();

            // Handle numpad keys
            if (baseKey >= Keys.NumPad0 && baseKey <= Keys.NumPad9)
                return "Num" + ((int)(baseKey - Keys.NumPad0)).ToString();

            // Handle letter keys (already friendly A-Z)
            if (baseKey >= Keys.A && baseKey <= Keys.Z)
                return baseKey.ToString();

            // Handle function keys (already friendly F1-F24)
            if (baseKey >= Keys.F1 && baseKey <= Keys.F24)
                return baseKey.ToString();

            // Special keys
            return baseKey switch
            {
                Keys.Space => "Space",
                Keys.Back => "Backspace",
                Keys.Tab => "Tab",
                Keys.Return => "Enter",
                Keys.Escape => "Esc",
                Keys.Delete => "Delete",
                Keys.Insert => "Insert",
                Keys.Home => "Home",
                Keys.End => "End",
                Keys.PageUp => "PageUp",
                Keys.PageDown => "PageDown",
                Keys.Left => "Left",
                Keys.Up => "Up",
                Keys.Right => "Right",
                Keys.Down => "Down",
                Keys.OemMinus => "-",
                Keys.Oemplus => "=",
                Keys.OemOpenBrackets => "[",
                Keys.OemCloseBrackets => "]",
                Keys.OemPipe => "\\",
                Keys.OemSemicolon => ";",
                Keys.OemQuotes => "'",
                Keys.Oemcomma => ",",
                Keys.OemPeriod => ".",
                Keys.OemQuestion => "/",
                Keys.Oemtilde => "`",
                _ => baseKey.ToString()
            };
        }

        private string FormatKeyForStorage(Keys key)
        {
            var baseKey = key & Keys.KeyCode;
            return baseKey.ToString();
        }

        private Keys ParseKey(string keyString)
        {
            if (Enum.TryParse<Keys>(keyString, out var key))
                return key;
            return Keys.None;
        }
    }
}

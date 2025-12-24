using System;
using System.Drawing;
using System.Windows.Forms;
using PromptArqApp;

namespace PromptArqApp.TextDisplayPanelTestHost
{
    public class TestHostForm : Form
    {
        private readonly TextDisplayPanel _textPanel;
        private readonly TextBox _inputTextBox;
        private readonly Button _showButton;
        private readonly Button _hideButton;
        private readonly TextBox _panelHandleTextBox;
        private readonly TextBox _scrollInfoTextBox;

        public TestHostForm()
        {
            Text = "TextDisplayPanel Test Host";
            ClientSize = new Size(460, 260);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            _inputTextBox = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                Name = "InputTextBox",
                AccessibleName = "InputTextBox",
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Text = "Enter text for the panel here.",
                Margin = new Padding(0)
            };

            _showButton = new Button
            {
                Text = "Show Panel",
                Name = "ShowButton",
                AccessibleName = "ShowButton",
                Width = 120
            };

            _hideButton = new Button
            {
                Text = "Hide Panel",
                Name = "HideButton",
                AccessibleName = "HideButton",
                Width = 120
            };

            var buttonRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            buttonRow.Controls.AddRange(new Control[] { _showButton, _hideButton });

            var instructionLabel = new Label
            {
                Text = "Input text for the TextDisplayPanel:",
                Dock = DockStyle.Top,
                Height = 24
            };

            _panelHandleTextBox = new TextBox
            {
                ReadOnly = true,
                TabStop = false,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.White,
                Size = new Size(1, 1),
                Visible = true,
                Name = "PanelHandleTextBox",
                AccessibleName = "PanelHandleTextBox",
                Text = string.Empty
            };

            _scrollInfoTextBox = new TextBox
            {
                ReadOnly = true,
                TabStop = false,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.White,
                Size = new Size(1, 1),
                Visible = true,
                Name = "ScrollStateTextBox",
                AccessibleName = "ScrollStateTextBox",
                Text = string.Empty
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(12)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            layout.Controls.Add(instructionLabel, 0, 0);
            layout.Controls.Add(_inputTextBox, 0, 1);
            layout.Controls.Add(buttonRow, 0, 2);
            layout.Controls.Add(_panelHandleTextBox, 0, 3);

            Controls.Add(layout);
            Controls.Add(_scrollInfoTextBox);
            _scrollInfoTextBox.Location = new Point(0, 0);

            _showButton.Click += (_, _) => ShowPanelWithCurrentText();
            _hideButton.Click += (_, _) => HidePanel();

            _textPanel = new TextDisplayPanel();
        }

        private void ShowPanelWithCurrentText()
        {
            _textPanel.ShowText(_inputTextBox.Text, this);
            UpdatePanelHandle();
        }

        private void HidePanel()
        {
            _textPanel.Hide();
            UpdatePanelHandle();
        }

        private void UpdatePanelHandle()
        {
            if (_textPanel.IsHandleCreated && _textPanel.Visible)
            {
                var handleText = _textPanel.Handle.ToString();
                _panelHandleTextBox.Text = handleText;
                _scrollInfoTextBox.Text = _textPanel.ScrollBarsVisibility.ToString();
                AccessibleDescription = handleText;
            }
            else
            {
                _panelHandleTextBox.Clear();
                _scrollInfoTextBox.Clear();
                AccessibleDescription = string.Empty;
            }
        }

        public void UpdateInputText(string text)
        {
            _inputTextBox.Text = text;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _textPanel.Hide();
            _textPanel.Dispose();
        }
    }
}

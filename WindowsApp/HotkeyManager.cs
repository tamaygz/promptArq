using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PromptArqApp
{
    public class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;

        // Modifier key flags
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        private readonly IntPtr _windowHandle;
        private readonly Dictionary<int, Action> _hotkeyActions = new Dictionary<int, Action>();
        private int _currentId = 1;

        public HotkeyManager(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
        }

        public bool RegisterHotkey(HotkeyConfig config, Action action)
        {
            if (string.IsNullOrEmpty(config.Key))
                return false;

            uint modifiers = 0;
            if (config.Ctrl) modifiers |= MOD_CONTROL;
            if (config.Alt) modifiers |= MOD_ALT;
            if (config.Shift) modifiers |= MOD_SHIFT;
            if (config.Win) modifiers |= MOD_WIN;

            // Convert key string to virtual key code
            Keys key;
            if (!Enum.TryParse(config.Key, true, out key))
                return false;

            int id = _currentId++;
            bool registered = RegisterHotKey(_windowHandle, id, modifiers, (uint)key);
            
            if (registered)
            {
                _hotkeyActions[id] = action;
            }

            return registered;
        }

        public void UnregisterAll()
        {
            foreach (var id in _hotkeyActions.Keys)
            {
                UnregisterHotKey(_windowHandle, id);
            }
            _hotkeyActions.Clear();
            _currentId = 1;
        }

        public bool ProcessHotkey(Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (_hotkeyActions.ContainsKey(id))
                {
                    _hotkeyActions[id]?.Invoke();
                    return true;
                }
            }
            return false;
        }

        public void Dispose()
        {
            UnregisterAll();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace PromptArqApp
{
    public class HotkeyConfig
    {
        public string Action { get; set; } = "";
        public string Key { get; set; } = "";
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public bool Win { get; set; }
    }

    public class AppSettings
    {
        public List<HotkeyConfig> Hotkeys { get; set; } = new List<HotkeyConfig>();
        public int WindowWidth { get; set; } = 1400;
        public int WindowHeight { get; set; } = 900;
        public bool StartMinimized { get; set; } = false;

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PromptArq",
            "settings.json"
        );

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsPath) ?? "";
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public void SetDefaultHotkeys()
        {
            Hotkeys = new List<HotkeyConfig>
            {
                new HotkeyConfig { Action = "Show/Hide Window", Key = "P", Ctrl = true, Alt = true, Shift = false, Win = false },
                new HotkeyConfig { Action = "New Prompt", Key = "N", Ctrl = true, Shift = true, Alt = false, Win = false },
                new HotkeyConfig { Action = "Settings", Key = "S", Ctrl = true, Alt = true, Shift = false, Win = false },
                new HotkeyConfig { Action = "Command Palette", Key = "K", Ctrl = true, Alt = false, Shift = false, Win = false }
            };
        }
    }
}

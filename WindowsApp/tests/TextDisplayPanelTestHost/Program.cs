using System;
using System.Windows.Forms;
using PromptArqApp.Theming;

namespace PromptArqApp.TextDisplayPanelTestHost
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ThemeManager.Initialize();
            ThemeManager.Instance.LoadTheme("Nord");


            ApplicationConfiguration.Initialize();
            Application.Run(new TestHostForm());
        }
    }
}

using MaterialDesignThemes.Wpf;
using NLog;
using Sentry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace VRC_OSC_AudioEars
{
    internal class WindowColor
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public WindowColor()
        {
            Logger.Debug("Setting up windows color watcher");
            UISettings? uiSettings = new();
            uiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;
            SetWindowsColors(uiSettings);
        }

        private void UiSettings_ColorValuesChanged(UISettings sender, object args)
        {

            Logger.Info("Windows theme updated");
            SetWindowsColors(sender);
        }

        public static void SetWindowsColors(UISettings uiSettings)
        {
            try
            {
                Logger.Debug("Setting theme colors");
                Windows.UI.Color accentColor = uiSettings.GetColorValue(UIColorType.Accent);
                Windows.UI.Color backGround = uiSettings.GetColorValue(UIColorType.Background);
                System.Windows.Media.Color newColor = System.Windows.Media.Color.FromArgb(accentColor.A, accentColor.R, accentColor.G, accentColor.B);
                var resources = System.Windows.Application.Current.Resources.MergedDictionaries;
                CustomColorTheme theme = (CustomColorTheme)System.Windows.Application.Current.Resources.MergedDictionaries.OfType<CustomColorTheme>().First<CustomColorTheme>();
                theme.PrimaryColor = newColor;
                theme.SecondaryColor = newColor;
                theme.BaseTheme = Constants.darkBackground == backGround ? BaseTheme.Dark : BaseTheme.Light;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }
        }


    }
}

using MaterialDesignThemes.Wpf;
using System.Linq;
using Windows.UI.ViewManagement;

namespace VRC_OSC_AudioEars
{
    internal class WindowColor
    {
        private readonly UISettings _uiSettings = new();

        public WindowColor()
        {
            _uiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;
            
            SetWindowsColors(_uiSettings);
        }

        private void UiSettings_ColorValuesChanged(UISettings sender, object args)
        {

            Helpers.MainWindow?.Dispatcher.Invoke(() =>
                SetWindowsColors(sender));
        }

        private static void SetWindowsColors(UISettings uiSettings)
        {
                Windows.UI.Color accentColor = uiSettings.GetColorValue(UIColorType.Accent);
                Windows.UI.Color backGround = uiSettings.GetColorValue(UIColorType.Background);
                System.Windows.Media.Color newColor = System.Windows.Media.Color.FromArgb(accentColor.A, accentColor.R, accentColor.G, accentColor.B);
                CustomColorTheme theme = System.Windows.Application.Current.Resources.MergedDictionaries.OfType<CustomColorTheme>().First();
                theme.PrimaryColor = newColor;
                theme.SecondaryColor = newColor;
                theme.BaseTheme = Windows.UI.Color.FromArgb(255, 0, 0, 0) == backGround ? BaseTheme.Dark : BaseTheme.Light;
        }


    }
}

using Octokit;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using VRC_OSC_AudioEars.Properties;

namespace VRC_OSC_AudioEars
{
    public static class Helpers
    {
        private static MainWindow? mainWindow;
        public static string AssemblyProductVersion
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly()
                    .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
                return attributes.Length == 0
                    ? ""
                    : ((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
            }
        }

        public static MainWindow? MainWindow { get => mainWindow; set => mainWindow = value; }
        
        public static async Task CheckGitHubNewerVersion()
        {

            try
            {
                GitHubClient client = new(new ProductHeaderValue("VRC-OSC-Audio-Reaction"));
                IReadOnlyList <Release> releases =
                    await client.Repository.Release.GetAll("Codel1417", "VRC-OSC-Audio-Reaction");
                if (AssemblyProductVersion != "" && releases.Count > 0)
                {
                    Version latestGitHubVersion = new(releases[0].TagName);
                    Version localVersion = new(Helpers.AssemblyProductVersion);
                    int versionComparison = localVersion.CompareTo(latestGitHubVersion);
                    if (versionComparison < 0)
                    {
                        if (MainWindow != null) await MainWindow.Dispatcher.InvokeAsync(new Action(() => MainWindow.SnackBar.MessageQueue?.Enqueue(Strings.updateMessage, Strings.updateGo, async _ => await Windows.System.Launcher.LaunchUriAsync(new Uri(releases[0].HtmlUrl)), true, true, false, TimeSpan.FromSeconds(15))));
                    }
                }
            }
            catch (Exception) {}

        }
    }
}
using System;
using System.Resources;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Runtime.InteropServices;
using System.Reflection;

public static class DarkModeHelper
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public static void UseImmersiveDarkMode(IntPtr handle, bool enabled)
    {
        if (Environment.OSVersion.Version.Major >= 10)
        {
            int useDark = enabled ? 1 : 0;
            DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
        }
    }
}

// ---------------- DLL Loader ----------------
public static class NativeLibraryLoader
{
    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    /// <summary>
    /// Extract an embedded DLL and load it into the process.
    /// </summary>
    public static void LoadEmbeddedDll(string resourceName, string dllFileName)
    {
        string tempPath = Path.Combine(Path.GetTempPath(), dllFileName);

        // Copy DLL from embedded resource to %TEMP%
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
        {
            if (stream == null)
                throw new Exception($"Resource '{resourceName}' not found. Check .csproj EmbeddedResource path.");

            using (var file = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                stream.CopyTo(file);
        }

        // Load it
        LoadLibrary(tempPath);
    }
}

namespace OKXMultichart
{
    public class MainForm : Form
    {
        private readonly string[] defaultUrls = new[]
        {
            "https://web3.okx.com",
            "https://web3.okx.com",
            "https://web3.okx.com"
        };

        private WebView2[] webViews;
        private AppSettings settings;
        private string settingsFile;
        private string cacheFolder;

        public MainForm()
        {
            Text = "OKX Multichart";
            MinimumSize = new Size(800, 600);

            var rm = new ResourceManager("OKXMultichart.Resources", typeof(MainForm).Assembly);
            Icon appIcon = (Icon)rm.GetObject("AppIcon");
            this.Icon = appIcon;

            // Enable dark mode for the window (Windows 10 1809+)
            Load += (s, e) => DarkModeHelper.UseImmersiveDarkMode(Handle, true);

            // Get exe folder and name (without extension)
            string exeFolder = AppDomain.CurrentDomain.BaseDirectory;
            string exeName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);

            // Use exeName to isolate profiles
            settingsFile = Path.Combine(exeFolder, $"{exeName}.settings.json");
            cacheFolder = Path.Combine(exeFolder, $"{exeName}.WebView2Cache");
            Directory.CreateDirectory(cacheFolder);

            // Load saved settings (or defaults)
            settings = LoadSettings();

            // Apply saved window size/position
            if (settings.WindowBounds != Rectangle.Empty)
            {
                Bounds = settings.WindowBounds;
                StartPosition = FormStartPosition.Manual;
            }
            else
            {
                WindowState = FormWindowState.Maximized;
            }

            // Layout
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = defaultUrls.Length
            };
            for (int i = 0; i < defaultUrls.Length; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / defaultUrls.Length));

            Controls.Add(layout);

            webViews = new WebView2[defaultUrls.Length];

            for (int i = 0; i < defaultUrls.Length; i++)
            {
                int index = i; // capture loop variable
                var webView = new WebView2
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0)
                };

                webView.CoreWebView2InitializationCompleted += (s, e) =>
                {
                    if (e.IsSuccess)
                    {
                        string urlToLoad = (index < settings.LastUrls.Length && !string.IsNullOrEmpty(settings.LastUrls[index]))
                            ? settings.LastUrls[index]
                            : defaultUrls[index];

                        webView.CoreWebView2.Navigate(urlToLoad);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to init WebView2: {e.InitializationException}");
                    }
                };

                // Use shared cache folder beside exe
                var env = CoreWebView2Environment.CreateAsync(null, cacheFolder).Result;
                webView.EnsureCoreWebView2Async(env);

                webViews[i] = webView;
                layout.Controls.Add(webView);
            }

            // Save settings when closing
            FormClosing += MainForm_FormClosing;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Save window size/position
            settings.WindowBounds = (WindowState == FormWindowState.Normal) ? Bounds : RestoreBounds;

            // Save last visited URLs
            for (int i = 0; i < webViews.Length; i++)
            {
                if (webViews[i]?.CoreWebView2 != null)
                    settings.LastUrls[i] = webViews[i].CoreWebView2.Source;
            }

            SaveSettings(settings);
        }

        // ---------------- Settings Handling ----------------
        private AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(settingsFile))
                {
                    var json = File.ReadAllText(settingsFile);
                    return JsonSerializer.Deserialize<AppSettings>(json);
                }
            }
            catch { }

            // Defaults
            return new AppSettings
            {
                WindowBounds = Rectangle.Empty,
                LastUrls = new string[defaultUrls.Length]
            };
        }

        private void SaveSettings(AppSettings s)
        {
            try
            {
                var json = JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsFile, json);
            }
            catch { }
        }

        // ---------------- AppSettings Class ----------------
        private class AppSettings
        {
            public Rectangle WindowBounds { get; set; }
            public string[] LastUrls { get; set; } = Array.Empty<string>();
        }

        [STAThread]
        public static void Main()
        {
            // ✅ Load embedded WebView2Loader.dll BEFORE anything else
            NativeLibraryLoader.LoadEmbeddedDll(
                "OKXMultichart.runtimes.win_x64.native.WebView2Loader.dll", // adjust if needed
                "WebView2Loader.dll"
            );

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}

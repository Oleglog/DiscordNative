using System.Diagnostics;
using System.IO.Compression;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;

namespace DiscordNative.Installer;

public sealed partial class MainWindow : Window
{
    private readonly string _defaultDir;

    public MainWindow()
    {
        InitializeComponent();
        Title = "Installateur DiscordNative";
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();

        _defaultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "DiscordNative");
        InstallPathBox.Text = _defaultDir;
    }

    private async void OnBrowseClicked(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        picker.FileTypeFilter.Add("*");

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            InstallPathBox.Text = folder.Path;
        }
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnInstallClicked(object sender, RoutedEventArgs e)
    {
        var targetDir = InstallPathBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(targetDir)) return;

        InstallButton.IsEnabled = false;
        InstallProgress.Visibility = Visibility.Visible;
        StatusText.Text = "Installation des composants en cours...";

        try
        {
            await Task.Run(() =>
            {
                Directory.CreateDirectory(targetDir);

                // Emplacement des binaires de l'application
                var baseSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Payload");
                if (!Directory.Exists(baseSource))
                {
                    // Fallback vers le dossier de compilation si exécuté en dev
                    baseSource = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\DiscordNative.App\bin\x64\Release\net8.0-windows10.0.19041.0"));
                }

                if (Directory.Exists(baseSource))
                {
                    foreach (var file in Directory.GetFiles(baseSource, "*.*", SearchOption.AllDirectories))
                    {
                        var relative = Path.GetRelativePath(baseSource, file);
                        var destination = Path.Combine(targetDir, relative);
                        var parent = Path.GetDirectoryName(destination);
                        if (parent != null) Directory.CreateDirectory(parent);
                        File.Copy(file, destination, overwrite: true);
                    }
                }

                var mainExe = Path.Combine(targetDir, "DiscordNative.App.exe");

                // Création du raccourci Bureau si demandé
                if (DesktopShortcutCheck.IsChecked == true)
                {
                    var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    CreateShortcut(mainExe, Path.Combine(desktopPath, "DiscordNative.lnk"));
                }

                // Création du raccourci Menu Démarrer si demandé
                if (StartMenuCheck.IsChecked == true)
                {
                    var startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                    CreateShortcut(mainExe, Path.Combine(startMenuPath, "DiscordNative.lnk"));
                }
            });

            StatusText.Text = "Installation terminée avec succès !";
            InstallProgress.Visibility = Visibility.Collapsed;
            InstallButton.Content = "Lancer DiscordNative";
            InstallButton.IsEnabled = true;
            InstallButton.Click -= OnInstallClicked;
            InstallButton.Click += (_, _) =>
            {
                var exe = Path.Combine(targetDir, "DiscordNative.App.exe");
                if (File.Exists(exe)) Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
                Close();
            };
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Erreur d'installation : {ex.Message}";
            InstallProgress.Visibility = Visibility.Collapsed;
            InstallButton.IsEnabled = true;
        }
    }

    private static void CreateShortcut(string targetPath, string shortcutPath)
    {
        try
        {
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;
            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell == null) return;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
            shortcut.WindowStyle = 1;
            shortcut.Description = "DiscordNative - Client Discord léger en WinUI 3";
            shortcut.Save();
        }
        catch { }
    }
}

using DiscordNative.App.ViewModels;
using DiscordNative.Protocol;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DiscordNative.App.Views;

public sealed partial class MainWindow : Window
{
    private readonly DiscordClient _client;
    public MainViewModel ViewModel { get; }

    public MainWindow(string token)
    {
        InitializeComponent();
        Title = "Diswin";
        ExtendsContentIntoTitleBar = true;
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        SetWindowIcon();

        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = App.Settings.Current.ThemeMode switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }

        _client = new DiscordClient(token);
        ViewModel = new MainViewModel(_client);
        RootContent.ViewModel = ViewModel;

        SetTitleBar(RootContent.TitleBarElement);

        Activated += OnWindowActivatedFirstTime;
    }

    private void SetWindowIcon()
    {
        try
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "app.ico");
            if (System.IO.File.Exists(iconPath))
            {
                appWindow.SetIcon(iconPath);
            }
        }
        catch { }
    }

    private async void OnWindowActivatedFirstTime(object sender, WindowActivatedEventArgs args)
    {
        Activated -= OnWindowActivatedFirstTime;
        Closed += (_, _) => App.Trace("MainWindow fermée");
        try
        {
            RootContent.UpdateSplashStatus("Connexion à Discord...");
            App.Trace("StartAsync...");
            await _client.StartAsync();
            App.Trace($"StartAsync OK: user={_client.CurrentUser?.DisplayName} guilds={_client.Guilds.Count} dms={_client.PrivateChannels.Count} friends={_client.Friends.Count}");
            _client.ConnectionStateChanged += s => App.Trace($"Gateway: {s} guilds={_client.Guilds.Count}");
            ViewModel.OnUserLoaded();
            RootContent.RefreshBackground();
            
            if (_client.PrivateChannels.Count > 0)
            {
                ViewModel.ShowFriendsView();
                _ = ViewModel.SelectChannelAsync(_client.PrivateChannels[0]);
            }

            RootContent.UpdateSplashStatus("Prêt !");
            await RootContent.DismissSplashAsync();
        }
        catch (Exception ex)
        {
            App.LogCrash("StartAsync", ex);
            var dialog = new ContentDialog
            {
                Title = "Connexion impossible",
                Content = $"Le token enregistré semble invalide ou expiré : {ex.Message}",
                CloseButtonText = "Se reconnecter",
                XamlRoot = Content.XamlRoot,
            };
            await dialog.ShowAsync();
            App.Settings.ClearToken();
            var loginWindow = new LoginWindow();
            loginWindow.Activate();
            App.MainAppWindow = loginWindow;
            Close();
        }
    }
}

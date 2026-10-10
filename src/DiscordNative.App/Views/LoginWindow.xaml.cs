using DiscordNative.Protocol.Auth;
using Microsoft.UI.Xaml;

namespace DiscordNative.App.Views;

public sealed partial class LoginWindow : Window
{
    private readonly DiscordAuthClient _authClient = new();
    private string? _pendingMfaTicket;
    private string? _pendingLoginInstanceId;

    public LoginWindow()
    {
        InitializeComponent();
        Title = "Diswin — Connexion";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        SetWindowIcon();
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

    private async void OnLoginClicked(object sender, RoutedEventArgs e)
    {
        ErrorBar.IsOpen = false;
        var email = EmailBox.Text.Trim();
        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Merci de renseigner e-mail et mot de passe.");
            return;
        }

        SetLoading(true);
        try
        {
            var result = await _authClient.LoginAsync(email, password);
            if (result.NeedsMfa)
            {
                _pendingMfaTicket = result.MfaTicket;
                _pendingLoginInstanceId = result.LoginInstanceId;
                CredentialsPanel.Visibility = Visibility.Collapsed;
                MfaPanel.Visibility = Visibility.Visible;
            }
            else
            {
                OnAuthenticated(result.Token!);
            }
        }
        catch (DiscordNative.Protocol.Auth.DiscordAuthException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowError($"Erreur réseau : {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnMfaClicked(object sender, RoutedEventArgs e)
    {
        ErrorBar.IsOpen = false;
        var code = MfaCodeBox.Text.Replace(" ", "").Replace("-", "").Trim();

        if ((code.Length != 6 && code.Length != 8) || _pendingMfaTicket is null)
        {
            ShowError("Code à 6 chiffres (ou code de secours à 8 caractères) requis.");
            return;
        }

        SetLoading(true);
        try
        {
            var result = await _authClient.SubmitTotpAsync(_pendingMfaTicket, code, _pendingLoginInstanceId);
            OnAuthenticated(result.Token!);
        }
        catch (DiscordNative.Protocol.Auth.DiscordAuthException ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void OnMfaBackClicked(object sender, RoutedEventArgs e)
    {
        MfaPanel.Visibility = Visibility.Collapsed;
        CredentialsPanel.Visibility = Visibility.Visible;
        ErrorBar.IsOpen = false;
        MfaCodeBox.Text = string.Empty;
        _pendingMfaTicket = null;
        _pendingLoginInstanceId = null;
    }

    private void OnAuthenticated(string token)
    {
        App.Settings.SaveToken(token);

        var mainWindow = new MainWindow(token);
        mainWindow.Activate();
        App.MainAppWindow = mainWindow;

        _authClient.Dispose();
        Close();
    }

    private void ShowError(string message)
    {
        ErrorBar.Title = "Erreur";
        ErrorBar.Message = message;
        ErrorBar.IsOpen = true;
    }

    private void SetLoading(bool loading)
    {
        LoadingRing.IsActive = loading;
        LoginButton.IsEnabled = !loading;
        MfaButton.IsEnabled = !loading;
    }
}

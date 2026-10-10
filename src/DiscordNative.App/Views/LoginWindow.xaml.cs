using DiscordNative.Protocol.Auth;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

    private void OnTabCredentialsClicked(object sender, RoutedEventArgs e)
    {
        CredentialsPanel.Visibility = Visibility.Visible;
        TokenPanel.Visibility = Visibility.Collapsed;
        TabCredentialsButton.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
        TabTokenButton.Style = null;
        ErrorBar.IsOpen = false;
    }

    private void OnTabTokenClicked(object sender, RoutedEventArgs e)
    {
        CredentialsPanel.Visibility = Visibility.Collapsed;
        TokenPanel.Visibility = Visibility.Visible;
        TabTokenButton.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
        TabCredentialsButton.Style = null;
        ErrorBar.IsOpen = false;
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

                SetupMfaUi(result);

                LoginTabsGrid.Visibility = Visibility.Collapsed;
                CredentialsPanel.Visibility = Visibility.Collapsed;
                TokenPanel.Visibility = Visibility.Collapsed;
                MfaPanel.Visibility = Visibility.Visible;
            }
            else
            {
                OnAuthenticated(result.Token!);
            }
        }
        catch (DiscordAuthException ex)
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

    private void SetupMfaUi(LoginResult result)
    {
        MfaSmsComboItem.Visibility = result.HasSms ? Visibility.Visible : Visibility.Collapsed;
        MfaBackupComboItem.Visibility = result.HasBackup ? Visibility.Visible : Visibility.Collapsed;

        if (!result.HasTotp && result.HasSms)
        {
            MfaMethodCombo.SelectedItem = MfaSmsComboItem;
            MfaDescriptionText.Text = "Un code de confirmation par SMS est nécessaire pour ce compte.";
        }
        else if (result.HasWebAuthn && !result.HasTotp && !result.HasSms)
        {
            MfaDescriptionText.Text = "Votre compte utilise une clé de sécurité WebAuthn (YubiKey/Passkey). Utilisez un code de secours ou connectez-vous par Jeton.";
            MfaMethodCombo.SelectedItem = MfaBackupComboItem;
        }
        else
        {
            MfaDescriptionText.Text = "Entrez le code à 6 chiffres de votre application d'authentification (ou un code de secours à 8 caractères).";
            MfaMethodCombo.SelectedIndex = 0;
        }

        UpdateSmsVisibility();
    }

    private void OnMfaMethodChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSmsVisibility();
    }

    private void UpdateSmsVisibility()
    {
        if (MfaMethodCombo?.SelectedItem is ComboBoxItem item && item.Tag as string == "sms")
        {
            SendSmsButton.Visibility = Visibility.Visible;
            MfaCodeBox.Header = "Code reçu par SMS (6 chiffres)";
        }
        else if (MfaMethodCombo?.SelectedItem is ComboBoxItem backupItem && backupItem.Tag as string == "backup")
        {
            SendSmsButton.Visibility = Visibility.Collapsed;
            MfaCodeBox.Header = "Code de secours (8 caractères)";
        }
        else if (MfaCodeBox is not null)
        {
            SendSmsButton.Visibility = Visibility.Collapsed;
            MfaCodeBox.Header = "Code de votre application d'authentification (6 chiffres)";
        }
    }

    private async void OnSendSmsClicked(object sender, RoutedEventArgs e)
    {
        if (_pendingMfaTicket is null) return;

        SetLoading(true);
        try
        {
            await _authClient.SendSmsAsync(_pendingMfaTicket);
            ErrorBar.Severity = InfoBarSeverity.Informational;
            ErrorBar.Title = "SMS envoyé";
            ErrorBar.Message = "Discord a envoyé un code de validation sur votre numéro de téléphone.";
            ErrorBar.IsOpen = true;
        }
        catch (DiscordAuthException ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnMfaClicked(object sender, RoutedEventArgs e)
    {
        ErrorBar.IsOpen = false;
        var rawCode = MfaCodeBox.Text ?? string.Empty;
        var code = rawCode.Replace(" ", "").Replace("-", "").Trim();

        if (string.IsNullOrEmpty(code) || _pendingMfaTicket is null)
        {
            ShowError("Veuillez saisir votre code de vérification.");
            return;
        }

        string method = "totp";
        if (MfaMethodCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            method = tag;
        }

        if (method == "totp" && code.Length == 8)
        {
            method = "backup";
        }

        SetLoading(true);
        try
        {
            var result = await _authClient.SubmitMfaAsync(_pendingMfaTicket, code, method, _pendingLoginInstanceId);
            OnAuthenticated(result.Token!);
        }
        catch (DiscordAuthException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowError($"Erreur : {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void OnMfaBackClicked(object sender, RoutedEventArgs e)
    {
        MfaPanel.Visibility = Visibility.Collapsed;
        LoginTabsGrid.Visibility = Visibility.Visible;
        CredentialsPanel.Visibility = Visibility.Visible;
        ErrorBar.IsOpen = false;
        MfaCodeBox.Text = string.Empty;
        _pendingMfaTicket = null;
        _pendingLoginInstanceId = null;
    }

    private async void OnTokenLoginClicked(object sender, RoutedEventArgs e)
    {
        ErrorBar.IsOpen = false;
        var token = TokenBox.Text?.Trim().Trim('"').Trim('\'') ?? string.Empty;

        if (string.IsNullOrWhiteSpace(token))
        {
            ShowError("Veuillez saisir ou coller un jeton utilisateur.");
            return;
        }

        SetLoading(true);
        try
        {
            var userTag = await _authClient.ValidateTokenAsync(token);
            OnAuthenticated(token);
        }
        catch (DiscordAuthException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowError($"Erreur lors de la validation du jeton : {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
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
        ErrorBar.Severity = InfoBarSeverity.Error;
        ErrorBar.Title = "Erreur";
        ErrorBar.Message = message;
        ErrorBar.IsOpen = true;
    }

    private void SetLoading(bool loading)
    {
        LoadingRing.IsActive = loading;
        LoginButton.IsEnabled = !loading;
        MfaButton.IsEnabled = !loading;
        SendSmsButton.IsEnabled = !loading;
        TokenLoginButton.IsEnabled = !loading;
    }
}

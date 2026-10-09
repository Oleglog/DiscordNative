using DiscordNative.App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace DiscordNative.App.Views;

public sealed partial class SettingsWindow : Window
{
    private readonly SettingsService _settings = App.Settings;
    private bool _isInitializing = true;

    public SettingsWindow()
    {
        InitializeComponent();
        Title = "DiscordNative — Paramètres";
        ExtendsContentIntoTitleBar = true;
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        SetTitleBar(AppTitleBar);

        LoadCurrentSettings();
        _isInitializing = false;
    }

    private void LoadCurrentSettings()
    {
        var current = _settings.Current;

        ThemeCombo.SelectedItem = ThemeCombo.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(i => (string)i.Tag == current.ThemeMode) ?? ThemeCombo.Items[0];

        CompactModeToggle.IsOn = current.CompactMode;
        OpacitySlider.Value = current.BackgroundOpacity;
        BlurSlider.Value = current.BackgroundBlurRadius;

        if (!string.IsNullOrEmpty(current.BackgroundImagePath) && System.IO.File.Exists(current.BackgroundImagePath))
        {
            BackgroundPreview.Source = new BitmapImage(new Uri(current.BackgroundImagePath));
        }
    }

    private async void OnChooseBackgroundClicked(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".webp");

        var file = await picker.PickSingleFileAsync();
        if (file is null) return;

        var path = await _settings.ImportBackgroundImageAsync(file);
        BackgroundPreview.Source = new BitmapImage(new Uri(path));
    }

    private void OnResetBackgroundClicked(object sender, RoutedEventArgs e)
    {
        _settings.ClearBackgroundImage();
        BackgroundPreview.Source = null;
    }

    private void OnOpacityChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isInitializing) return;
        _settings.Current.BackgroundOpacity = e.NewValue;
        _settings.Save();
    }

    private void OnBlurChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isInitializing) return;
        _settings.Current.BackgroundBlurRadius = e.NewValue;
        _settings.Save();
    }

    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (ThemeCombo.SelectedItem is ComboBoxItem item)
        {
            _settings.Current.ThemeMode = (string)item.Tag;
            _settings.Save();

            var theme = _settings.Current.ThemeMode switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };

            if (Content is FrameworkElement root) root.RequestedTheme = theme;
            if (App.MainAppWindow?.Content is FrameworkElement mainRoot) mainRoot.RequestedTheme = theme;
        }
    }

    private void OnCompactModeToggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _settings.Current.CompactMode = CompactModeToggle.IsOn;
        _settings.Save();
    }

    private async void OnLogoutClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Se déconnecter ?",
            Content = "Vous devrez ressaisir vos identifiants pour vous reconnecter.",
            PrimaryButtonText = "Se déconnecter",
            CloseButtonText = "Annuler",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            _settings.ClearToken();
            var loginWindow = new LoginWindow();
            loginWindow.Activate();
            App.MainAppWindow = loginWindow;
            Close();
        }
    }
}

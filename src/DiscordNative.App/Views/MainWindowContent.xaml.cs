using DiscordNative.App.Services;
using DiscordNative.App.ViewModels;
using DiscordNative.Protocol.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace DiscordNative.App.Views;

public sealed partial class MainWindowContent : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(MainViewModel), typeof(MainWindowContent), new PropertyMetadata(null));

    public MainViewModel ViewModel
    {
        get => (MainViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public FrameworkElement TitleBarElement => AppTitleBar;

    public void RefreshBackground() => BackgroundHost.ApplyFromSettings();

    public void UpdateSplashStatus(string text)
    {
        Protocol.App.RunOnUiThread(() =>
        {
            if (SplashStatusText != null)
            {
                SplashStatusText.Text = text;
            }
        });
    }

    public async System.Threading.Tasks.Task DismissSplashAsync()
    {
        await System.Threading.Tasks.Task.Delay(300);
        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        Protocol.App.RunOnUiThread(() =>
        {
            if (StartupSplash == null)
            {
                tcs.SetResult(true);
                return;
            }
            var fade = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(400))
            };
            var sb = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
            sb.Children.Add(fade);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(fade, StartupSplash);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fade, "Opacity");
            sb.Completed += (_, _) =>
            {
                StartupSplash.Visibility = Visibility.Collapsed;
                tcs.TrySetResult(true);
            };
            sb.Begin();
        });
        await tcs.Task;
    }

    private UpdateInfo? _availableUpdate;

    public MainWindowContent()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateSplashStatus("Vérification des mises à jour...");
        var update = await UpdateService.CheckForUpdatesAsync();
        if (update is not null)
        {
            _availableUpdate = update;
            UpdateSplashStatus($"Mise à jour {update.TagName} détectée...");
            UpdateInfoBar.Title = $"Mise à jour disponible ({update.TagName})";
            UpdateInfoBar.Message = string.IsNullOrWhiteSpace(update.Name) ? "Une nouvelle version de Diswin est prête à être installée." : update.Name;
            UpdateInfoBar.IsOpen = true;
        }
        else
        {
            UpdateSplashStatus("Connexion au compte...");
        }
    }

    private async void OnApplyUpdateClicked(object sender, RoutedEventArgs e)
    {
        if (_availableUpdate is null) return;
        ApplyUpdateButton.IsEnabled = false;

        await UpdateService.ApplyAutomaticUpdateAsync(_availableUpdate, status =>
        {
            Protocol.App.RunOnUiThread(() =>
            {
                UpdateInfoBar.Message = status;
            });
        });
    }

    private async void OnGuildSelected(object sender, SelectionChangedEventArgs e)
    {
        if (GuildList.SelectedItem is DiscordGuild guild)
        {
            await ViewModel.SelectGuildAsync(guild);
        }
    }

    private async void OnChannelSelected(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView { SelectedItem: DiscordChannel channel })
        {
            await ViewModel.SelectChannelAsync(channel);
        }
    }

    private void OnFriendsClicked(object sender, RoutedEventArgs e)
    {
        GuildList.SelectedItem = null;
        ViewModel.ShowFriendsView();
    }

    private async void OnSendClicked(object sender, RoutedEventArgs e)
    {
        await SendCurrentMessageAsync();
    }

    private async void OnAttachFileClicked(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainAppWindow!);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        picker.FileTypeFilter.Add("*");

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            await ViewModel.SendFileAsync(file.Path);
        }
    }

    private async void OnMessageInputKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            await SendCurrentMessageAsync();
        }
    }

    private async System.Threading.Tasks.Task SendCurrentMessageAsync()
    {
        var content = MessageInput.Text;
        if (string.IsNullOrWhiteSpace(content)) return;
        MessageInput.Text = string.Empty;
        await ViewModel.SendMessageAsync(content);
    }

    private void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.Activate();
    }

    private void OnDisconnectVoiceClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.DisconnectVoice();
    }

    private void OnToggleMuteClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleMute();
    }

    private void OnToggleDeafenClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleDeafen();
    }

    private void OnChannelSearchChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            ViewModel.ApplySearchFilter(tb.Text);
        }
    }

    private void OnStatusOnlineClicked(object sender, RoutedEventArgs e) => ViewModel.SetStatus("online");
    private void OnStatusIdleClicked(object sender, RoutedEventArgs e) => ViewModel.SetStatus("idle");
    private void OnStatusDndClicked(object sender, RoutedEventArgs e) => ViewModel.SetStatus("dnd");
    private void OnStatusInvisibleClicked(object sender, RoutedEventArgs e) => ViewModel.SetStatus("invisible");

    // --- Animation au survol des avatars et icônes ---
    private void OnAvatarPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            ControlBitmapInTree(fe, play: true);
        }
    }

    private void OnAvatarPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            ControlBitmapInTree(fe, play: false);
        }
    }

    private static void ControlBitmapInTree(FrameworkElement fe, bool play)
    {
        if (fe is Image img && img.Source is BitmapImage bi)
        {
            if (play) bi.Play(); else bi.Stop();
            return;
        }
        if (fe is Microsoft.UI.Xaml.Shapes.Shape shape && shape.Fill is ImageBrush ib && ib.ImageSource is BitmapImage sbi)
        {
            if (play) sbi.Play(); else sbi.Stop();
            return;
        }
        if (fe is Border b && b.Background is ImageBrush bib && bib.ImageSource is BitmapImage bbi)
        {
            if (play) bbi.Play(); else bbi.Stop();
            return;
        }
        if (fe is Panel p)
        {
            foreach (var child in p.Children)
            {
                if (child is FrameworkElement cfe) ControlBitmapInTree(cfe, play);
            }
        }
        else if (fe is ContentControl cc && cc.Content is FrameworkElement cfe)
        {
            ControlBitmapInTree(cfe, play);
        }
    }

    private static void CopyToClipboard(string text)
    {
        try
        {
            var dp = new DataPackage();
            dp.SetText(text);
            Clipboard.SetContent(dp);
        }
        catch { }
    }

    // --- Menus contextuels Clic Droit : Messages ---
    private static DiscordMessage? GetMessageFromSender(object sender)
    {
        if (sender is FrameworkElement fe && fe.DataContext is DiscordMessage msg) return msg;
        return null;
    }

    private void OnCopyMessageTextClicked(object sender, RoutedEventArgs e)
    {
        var msg = GetMessageFromSender(sender);
        if (msg != null && !string.IsNullOrEmpty(msg.Content))
        {
            CopyToClipboard(msg.Content);
        }
    }

    private void OnQuickReactThumbsUpClicked(object sender, RoutedEventArgs e)
    {
        var msg = GetMessageFromSender(sender);
        if (msg != null) _ = ViewModel.AddReactionAsync(msg.Id, "👍");
    }

    private void OnQuickReactHeartClicked(object sender, RoutedEventArgs e)
    {
        var msg = GetMessageFromSender(sender);
        if (msg != null) _ = ViewModel.AddReactionAsync(msg.Id, "❤️");
    }

    private void OnQuickReactJoyClicked(object sender, RoutedEventArgs e)
    {
        var msg = GetMessageFromSender(sender);
        if (msg != null) _ = ViewModel.AddReactionAsync(msg.Id, "😂");
    }

    private void OnQuickReactFireClicked(object sender, RoutedEventArgs e)
    {
        var msg = GetMessageFromSender(sender);
        if (msg != null) _ = ViewModel.AddReactionAsync(msg.Id, "🔥");
    }

    private void OnDeleteMessageClicked(object sender, RoutedEventArgs e)
    {
        var msg = GetMessageFromSender(sender);
        if (msg != null) _ = ViewModel.DeleteMessageAsync(msg.Id);
    }

    private void OnCopyMessageIdClicked(object sender, RoutedEventArgs e)
    {
        var msg = GetMessageFromSender(sender);
        if (msg != null) CopyToClipboard(msg.Id);
    }

    // --- Menus contextuels Clic Droit : Serveurs ---
    private static DiscordGuild? GetGuildFromSender(object sender)
    {
        if (sender is FrameworkElement fe && fe.DataContext is DiscordGuild g) return g;
        return null;
    }

    private void OnMarkGuildReadClicked(object sender, RoutedEventArgs e)
    {
        // Marquer le serveur comme lu
    }

    private void OnCopyGuildNameClicked(object sender, RoutedEventArgs e)
    {
        var g = GetGuildFromSender(sender);
        if (g != null) CopyToClipboard(g.Name);
    }

    private void OnCopyGuildIdClicked(object sender, RoutedEventArgs e)
    {
        var g = GetGuildFromSender(sender);
        if (g != null) CopyToClipboard(g.Id);
    }

    // --- Menus contextuels Clic Droit : Salons ---
    private static DiscordChannel? GetChannelFromSender(object sender)
    {
        if (sender is FrameworkElement fe && fe.DataContext is DiscordChannel ch) return ch;
        return null;
    }

    private void OnCopyChannelNameClicked(object sender, RoutedEventArgs e)
    {
        var ch = GetChannelFromSender(sender);
        if (ch != null) CopyToClipboard(ch.DisplayName);
    }

    private void OnCopyChannelIdClicked(object sender, RoutedEventArgs e)
    {
        var ch = GetChannelFromSender(sender);
        if (ch != null) CopyToClipboard(ch.Id);
    }

    private void OnOpenDmChannelClicked(object sender, RoutedEventArgs e)
    {
        var ch = GetChannelFromSender(sender);
        if (ch != null) _ = ViewModel.SelectChannelAsync(ch);
    }

    // --- Menus contextuels Clic Droit : Amis ---
    private static DiscordRelationship? GetFriendFromSender(object sender)
    {
        if (sender is FrameworkElement fe && fe.DataContext is DiscordRelationship rel) return rel;
        return null;
    }

    private void OnFriendProfileClicked(object sender, RoutedEventArgs e)
    {
        var rel = GetFriendFromSender(sender);
        if (rel != null)
        {
            var dm = ViewModel.PrivateChannels.FirstOrDefault(c => c.Recipients?.Any(u => u.Id == rel.User.Id) == true);
            if (dm != null)
            {
                _ = ViewModel.SelectChannelAsync(dm);
            }
        }
    }

    private void OnCopyFriendUsernameClicked(object sender, RoutedEventArgs e)
    {
        var rel = GetFriendFromSender(sender);
        if (rel != null) CopyToClipboard(rel.User.DisplayName);
    }

    private void OnCopyFriendIdClicked(object sender, RoutedEventArgs e)
    {
        var rel = GetFriendFromSender(sender);
        if (rel != null) CopyToClipboard(rel.User.Id);
    }

    // --- Édition du Profil Utilisateur ---
    private async void OnEditProfileClicked(object sender, RoutedEventArgs e)
    {
        var stack = new StackPanel { Spacing = 12 };
        var nameBox = new TextBox
        {
            Header = "Nom d'affichage (Display Name)",
            Text = ViewModel.CurrentUserName,
            CornerRadius = new CornerRadius(8)
        };
        var bioBox = new TextBox
        {
            Header = "Bio / À propos de moi",
            Text = ViewModel.CurrentUserBio == "Aucune bio définie." ? "" : ViewModel.CurrentUserBio,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80,
            MaxLength = 190,
            CornerRadius = new CornerRadius(8)
        };
        stack.Children.Add(nameBox);
        stack.Children.Add(bioBox);

        var dialog = new ContentDialog
        {
            Title = "Modifier mon profil",
            Content = stack,
            PrimaryButtonText = "Enregistrer",
            CloseButtonText = "Annuler",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            try
            {
                await ViewModel.UpdateProfileAsync(nameBox.Text.Trim(), bioBox.Text.Trim());
            }
            catch (Exception ex)
            {
                var err = new ContentDialog
                {
                    Title = "Erreur de modification",
                    Content = $"Impossible de mettre à jour le profil : {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await err.ShowAsync();
            }
        }
    }
}


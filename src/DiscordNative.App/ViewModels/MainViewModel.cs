using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DiscordNative.Protocol;
using DiscordNative.Protocol.Models;

namespace DiscordNative.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly DiscordClient _client;
    private DiscordGuild? _selectedGuild;
    private DiscordChannel? _selectedChannel;
    private bool _showingFriends;

    public ObservableCollection<DiscordGuild> Guilds => _client.Guilds;
    public ObservableCollection<DiscordChannel> Channels { get; } = new();
    public ObservableCollection<DiscordRelationship> Friends => _client.Friends;
    public ObservableCollection<DiscordChannel> PrivateChannels => _client.PrivateChannels;
    public ObservableCollection<DiscordMessage> Messages { get; } = new();

    [ObservableProperty]
    private string selectedGuildName = "Sélectionnez un serveur";

    [ObservableProperty]
    private string selectedChannelName = "Sélectionnez un salon";

    [ObservableProperty]
    private string currentUserName = "...";

    [ObservableProperty]
    private bool isVoiceConnected;

    [ObservableProperty]
    private string connectedVoiceChannelName = string.Empty;

    [ObservableProperty]
    private bool isVoiceChannelSelected;

    public ObservableCollection<DiscordUser> VoiceParticipants { get; } = new();

    public Microsoft.UI.Xaml.Visibility VoiceViewVisibility => IsVoiceChannelSelected ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    public Microsoft.UI.Xaml.Visibility TextViewVisibility => IsVoiceChannelSelected ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;

    public string? CurrentUserAvatarUrl => _client.CurrentUser?.AvatarUrl;

    public Microsoft.UI.Xaml.Visibility ShowingGuildChannels => _showingFriends ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
    public Microsoft.UI.Xaml.Visibility ShowingFriendsView => _showingFriends ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    public MainViewModel(DiscordClient client)
    {
        _client = client;
        _client.MessageReceived += OnMessageReceived;
        _client.MessageUpdated += OnMessageUpdated;
        _client.MessageDeleted += OnMessageDeleted;
        _client.ReactionAdded += OnReactionAdded;
    }

    public void OnUserLoaded()
    {
        CurrentUserName = _client.CurrentUser?.DisplayName ?? "Utilisateur inconnu";
    }

    public void ShowFriendsView()
    {
        _showingFriends = true;
        SelectedGuildName = "Amis";
        OnPropertyChanged(nameof(ShowingGuildChannels));
        OnPropertyChanged(nameof(ShowingFriendsView));
    }

    public async Task SelectGuildAsync(DiscordGuild guild)
    {
        _showingFriends = false;
        OnPropertyChanged(nameof(ShowingGuildChannels));
        OnPropertyChanged(nameof(ShowingFriendsView));
        _selectedGuild = guild;
        SelectedGuildName = guild.Name;
        Channels.Clear();

        // Si la guilde a déjà ses salons chargés
        if (guild.Channels.Count > 0)
        {
            foreach (var channel in guild.Channels.Where(c => c.IsTextLike || c.Type == DiscordChannelType.GuildVoice).OrderBy(c => c.Position))
            {
                channel.GuildId ??= guild.Id;
                Channels.Add(channel);
            }
        }
        else
        {
            // Récupération REST dynamique des salons du serveur
            try
            {
                var fetchedChannels = await _client.Rest.GetGuildChannelsAsync(guild.Id);
                guild.Channels = fetchedChannels;
                foreach (var channel in fetchedChannels.Where(c => c.IsTextLike || c.Type == DiscordChannelType.GuildVoice).OrderBy(c => c.Position))
                {
                    channel.GuildId ??= guild.Id;
                    Channels.Add(channel);
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DiscordNative_crash.log"),
                    $"[{DateTime.Now}] GetGuildChannelsAsync failed: {ex}\n\n");
            }
        }
    }

    public async Task SelectChannelAsync(DiscordChannel channel)
    {
        _selectedChannel = channel;
        SelectedChannelName = channel.DisplayName;
        Messages.Clear();

        if (channel.Type == DiscordChannelType.GuildVoice)
        {
            IsVoiceChannelSelected = true;
            OnPropertyChanged(nameof(VoiceViewVisibility));
            OnPropertyChanged(nameof(TextViewVisibility));

            if (channel.GuildId != null)
            {
                _ = _client.JoinVoiceChannelAsync(channel.GuildId, channel.Id);
                IsVoiceConnected = true;
                ConnectedVoiceChannelName = channel.DisplayName;
            }

            VoiceParticipants.Clear();
            if (_client.CurrentUser != null)
            {
                VoiceParticipants.Add(_client.CurrentUser);
            }
            if (_client.Friends.Count > 0)
            {
                foreach (var friend in _client.Friends.Take(4))
                {
                    VoiceParticipants.Add(friend.User);
                }
            }
        }
        else
        {
            IsVoiceChannelSelected = false;
            OnPropertyChanged(nameof(VoiceViewVisibility));
            OnPropertyChanged(nameof(TextViewVisibility));

            try
            {
                var history = await _client.Rest.GetMessagesAsync(channel.Id, limit: 50);
                
                DiscordMessage? prev = null;
                foreach (var message in history.OrderBy(m => m.Timestamp))
                {
                    ProcessMessageForGrouping(message, prev);
                    Messages.Add(message);
                    prev = message;
                }
            }
            catch (Exception)
            {
            }
        }
    }

    public async Task SendMessageAsync(string content)
    {
        if (_selectedChannel is null || string.IsNullOrWhiteSpace(content)) return;
        try
        {
            await _client.Rest.SendMessageAsync(_selectedChannel.Id, content);
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DiscordNative_crash.log"),
                $"[{DateTime.Now}] SendMessageAsync failed: {ex}\n\n");
        }
    }

    public async Task SendFileAsync(string filePath)
    {
        if (_selectedChannel is null || string.IsNullOrWhiteSpace(filePath)) return;
        try
        {
            await _client.Rest.SendFileAsync(_selectedChannel.Id, filePath);
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DiscordNative_crash.log"),
                $"[{DateTime.Now}] SendFileAsync failed: {ex}\n\n");
        }
    }

    public async Task AddReactionAsync(string messageId, string emoji)
    {
        if (_selectedChannel is null) return;
        try
        {
            await _client.Rest.AddReactionAsync(_selectedChannel.Id, messageId, emoji);
        }
        catch (Exception) { }
    }

    public async Task DeleteMessageAsync(string messageId)
    {
        if (_selectedChannel is null) return;
        try
        {
            await _client.Rest.DeleteMessageAsync(_selectedChannel.Id, messageId);
        }
        catch (Exception) { }
    }

    private void OnMessageReceived(DiscordMessage message)
    {
        if (message.ChannelId != _selectedChannel?.Id) return;
        Protocol.App.RunOnUiThread(() =>
        {
            ProcessMessageForGrouping(message, Messages.LastOrDefault());
            Messages.Add(message);
        });
    }

    private void OnMessageUpdated(DiscordMessage message)
    {
        if (message.ChannelId != _selectedChannel?.Id) return;
        Protocol.App.RunOnUiThread(() =>
        {
            var existing = Messages.FirstOrDefault(m => m.Id == message.Id);
            if (existing != null)
            {
                var idx = Messages.IndexOf(existing);
                message.ShowHeader = existing.ShowHeader;
                Messages[idx] = message;
            }
        });
    }

    private void OnMessageDeleted(string channelId, string messageId)
    {
        if (channelId != _selectedChannel?.Id) return;
        Protocol.App.RunOnUiThread(() =>
        {
            var existing = Messages.FirstOrDefault(m => m.Id == messageId);
            if (existing != null) Messages.Remove(existing);
        });
    }

    private void OnReactionAdded(string channelId, string messageId, DiscordEmoji emoji)
    {
        if (channelId != _selectedChannel?.Id) return;
        Protocol.App.RunOnUiThread(() =>
        {
            var existing = Messages.FirstOrDefault(m => m.Id == messageId);
            if (existing != null)
            {
                var reaction = existing.Reactions.FirstOrDefault(r => r.Emoji.Name == emoji.Name);
                if (reaction != null)
                {
                    reaction.Count++;
                }
                else
                {
                    existing.Reactions.Add(new DiscordReaction { Count = 1, Emoji = emoji });
                }
                var idx = Messages.IndexOf(existing);
                Messages[idx] = existing; // Trigger notify
            }
        });
    }

    private void ProcessMessageForGrouping(DiscordMessage current, DiscordMessage? previous)
    {
        if (previous == null) return;
        if (!current.IsNormal || !previous.IsNormal) return;
        if (current.Author.Id != previous.Author.Id) return;

        var timeDiff = current.Timestamp - previous.Timestamp;
        if (timeDiff.TotalMinutes < 7)
        {
            current.ShowHeader = false;
        }
    }

    public void DisconnectVoice()
    {
        if (_selectedChannel?.GuildId != null)
        {
            _ = _client.JoinVoiceChannelAsync(_selectedChannel.GuildId, null);
        }
        IsVoiceConnected = false;
        ConnectedVoiceChannelName = string.Empty;
        if (IsVoiceChannelSelected)
        {
            IsVoiceChannelSelected = false;
            OnPropertyChanged(nameof(VoiceViewVisibility));
            OnPropertyChanged(nameof(TextViewVisibility));
        }
    }
}

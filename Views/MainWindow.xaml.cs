using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LibVLCSharp.Shared;
using MeuIPTVPro.Models;
using MeuIPTVPro.Services;

namespace MeuIPTVPro.Views;

public partial class MainWindow : Window
{
    private readonly string _server;
    private readonly string _username;
    private readonly string _password;
    private readonly XtreamService _xtream = new();
    private readonly ObservableCollection<Channel> _channels = new();
    private readonly ObservableCollection<string> _categories = new();
    private List<Channel> _allChannels = new();

    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private bool _isCinemaMode;
    private WindowState _previousWindowState;
    private WindowStyle _previousWindowStyle;
    private ResizeMode _previousResizeMode;

    public MainWindow(string server, string username, string password)
    {
        InitializeComponent();

        _server = server;
        _username = username;
        _password = password;

        ChannelList.ItemsSource = _channels;
        CategoryBox.ItemsSource = _categories;

        Loaded += async (_, _) =>
        {
            InitializePlayer();
            await LoadChannelsAsync();
        };

        Closed += (_, _) =>
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVlc?.Dispose();
        };
    }

    private void InitializePlayer()
    {
        Core.Initialize();

        _libVlc = new LibVLC("--no-video-title-show");
        _mediaPlayer = new MediaPlayer(_libVlc)
        {
            Volume = (int)VolumeSlider.Value
        };

        VideoPlayer.MediaPlayer = _mediaPlayer;
        PlayerPlaceholder.Text = "Selecione um canal";
    }

    private async Task LoadChannelsAsync()
    {
        PlayerPlaceholder.Text = "Carregando canais...";

        try
        {
            _allChannels = await _xtream.GetLiveChannelsAsync(_server, _username, _password);

            if (_allChannels.Count == 0)
            {
                PlayerPlaceholder.Text = "Nenhum canal encontrado";
                MessageBox.Show(
                    "Não foi possível carregar canais.\n\nConfira servidor, usuário, senha e compatibilidade Xtream.",
                    "Meu IPTV Pro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            BuildCategories();
            ApplyFilters();
            PlayerPlaceholder.Text = "Selecione um canal";
        }
        catch
        {
            PlayerPlaceholder.Text = "Falha na conexão";
            MessageBox.Show(
                "Não foi possível conectar ao servidor.",
                "Meu IPTV Pro",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void BuildCategories()
    {
        _categories.Clear();
        _categories.Add("Todos");

        foreach (var category in _allChannels
                     .Select(c => string.IsNullOrWhiteSpace(c.Category) ? "Sem categoria" : c.Category)
                     .Distinct()
                     .OrderBy(c => c))
        {
            _categories.Add(category);
        }

        CategoryBox.SelectedIndex = 0;
    }

    private void ApplyFilters()
    {
        var term = SearchBox.Text.Trim();
        var selectedCategory = CategoryBox.SelectedItem as string ?? "Todos";

        IEnumerable<Channel> result = _allChannels;

        if (selectedCategory != "Todos")
        {
            result = result.Where(c =>
                (string.IsNullOrWhiteSpace(c.Category) ? "Sem categoria" : c.Category) == selectedCategory);
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            result = result.Where(c =>
                c.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        RefreshChannelList(result);
    }

    private void RefreshChannelList(IEnumerable<Channel> source)
    {
        _channels.Clear();

        foreach (var channel in source.OrderBy(c => c.Name))
        {
            _channels.Add(channel);
        }

        ChannelCountText.Text = $"{_channels.Count} canais";
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void CategoryBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void ChannelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ChannelList.SelectedItem is Channel channel)
        {
            NowPlayingText.Text = channel.Name;
        }
    }

    private void ChannelList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        PlaySelectedChannel();
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        PlaySelectedChannel();
    }

    private void PlaySelectedChannel()
    {
        if (ChannelList.SelectedItem is not Channel channel)
        {
            MessageBox.Show("Selecione um canal primeiro.", "Meu IPTV Pro");
            return;
        }

        if (_libVlc is null || _mediaPlayer is null)
        {
            MessageBox.Show("O player ainda não foi iniciado.", "Meu IPTV Pro");
            return;
        }

        if (string.IsNullOrWhiteSpace(channel.StreamUrl))
        {
            MessageBox.Show("Este canal não possui URL de reprodução.", "Meu IPTV Pro");
            return;
        }

        try
        {
            using var media = new Media(_libVlc, new Uri(channel.StreamUrl));
            _mediaPlayer.Play(media);

            OverlayPanel.Visibility = Visibility.Collapsed;
            NowPlayingText.Text = channel.Name;
        }
        catch
        {
            MessageBox.Show(
                "Não foi possível reproduzir este canal.",
                "Meu IPTV Pro",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        _mediaPlayer?.Pause();
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_mediaPlayer is not null)
        {
            _mediaPlayer.Volume = (int)e.NewValue;
        }
    }

    private void Fullscreen_Click(object sender, RoutedEventArgs e)
    {
        ToggleCinemaMode();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _isCinemaMode)
        {
            ToggleCinemaMode();
        }
    }

    private void ToggleCinemaMode()
    {
        if (!_isCinemaMode)
        {
            _previousWindowState = WindowState;
            _previousWindowStyle = WindowStyle;
            _previousResizeMode = ResizeMode;

            SideMenu.Visibility = Visibility.Collapsed;
            ChannelPanel.Visibility = Visibility.Collapsed;
            MenuColumn.Width = new GridLength(0);
            ListColumn.Width = new GridLength(0);
            PlayerControls.Visibility = Visibility.Collapsed;
            ControlsRow.Height = new GridLength(0);
            PlayerFrame.Margin = new Thickness(0);
            PlayerFrame.CornerRadius = new CornerRadius(0);

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            _isCinemaMode = true;
        }
        else
        {
            SideMenu.Visibility = Visibility.Visible;
            ChannelPanel.Visibility = Visibility.Visible;
            MenuColumn.Width = new GridLength(220);
            ListColumn.Width = new GridLength(390);
            PlayerControls.Visibility = Visibility.Visible;
            ControlsRow.Height = new GridLength(82);
            PlayerFrame.Margin = new Thickness(18, 18, 18, 0);
            PlayerFrame.CornerRadius = new CornerRadius(18);

            WindowStyle = _previousWindowStyle;
            ResizeMode = _previousResizeMode;
            WindowState = _previousWindowState;
            _isCinemaMode = false;
        }
    }
}

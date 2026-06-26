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
    private readonly FavoritesService _favoritesService = new();
    private bool _showOnlyFavorites = false;

    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;

    private bool _isFullscreen;

    private WindowState _previousWindowState;
    private WindowStyle _previousWindowStyle;
    private ResizeMode _previousResizeMode;

    private GridLength _previousMenuWidth;
    private GridLength _previousListWidth;
    private GridLength _previousControlsHeight;

    private Thickness _previousPlayerFrameMargin;
    private CornerRadius _previousPlayerFrameCornerRadius;

    private int _previousPlayerColumn;
    private int _previousPlayerColumnSpan;

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

            // Carrega favoritos e marca os canais
            var favs = _favoritesService.LoadFavorites();
            foreach (var ch in _allChannels)
            {
                ch.IsFavorite = favs.Contains(ch.StreamUrl);
            }

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

        if (_showOnlyFavorites)
        {
            result = result.Where(c => c.IsFavorite);
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

    private void FavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Channel channel)
        {
            _favoritesService.ToggleFavorite(channel);

            // Se estivermos visualizando apenas favoritos e o canal foi removido, atualizar lista
            if (_showOnlyFavorites && !channel.IsFavorite)
            {
                ApplyFilters();
            }
        }
    }

    private void FavoriteButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Prevenir seleção e dupla-clique de reprodução
        e.Handled = true;
    }

    private void FavoritesButton_Click(object sender, RoutedEventArgs e)
    {
        _showOnlyFavorites = true;
        ApplyFilters();
    }

    private void LiveTvButton_Click(object sender, RoutedEventArgs e)
    {
        _showOnlyFavorites = false;
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
        ToggleFullscreen();
    }

    private void PlayerArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleFullscreen();
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _isFullscreen)
        {
            ExitFullscreen();
        }
    }

    private void ToggleFullscreen()
    {
        if (_isFullscreen)
        {
            ExitFullscreen();
        }
        else
        {
            EnterFullscreen();
        }
    }

    private void EnterFullscreen()
    {
        if (_isFullscreen)
        {
            return;
        }

        _previousWindowState = WindowState;
        _previousWindowStyle = WindowStyle;
        _previousResizeMode = ResizeMode;

        _previousMenuWidth = MenuColumn.Width;
        _previousListWidth = ListColumn.Width;
        _previousControlsHeight = ControlsRow.Height;

        _previousPlayerFrameMargin = PlayerFrame.Margin;
        _previousPlayerFrameCornerRadius = PlayerFrame.CornerRadius;

        _previousPlayerColumn = Grid.GetColumn(PlayerArea);
        _previousPlayerColumnSpan = Grid.GetColumnSpan(PlayerArea);

        _isFullscreen = true;

        SideMenu.Visibility = Visibility.Collapsed;
        ChannelPanel.Visibility = Visibility.Collapsed;
        PlayerControls.Visibility = Visibility.Collapsed;

        MenuColumn.Width = new GridLength(0);
        ListColumn.Width = new GridLength(0);
        ControlsRow.Height = new GridLength(0);

        Grid.SetColumn(PlayerArea, 0);
        Grid.SetColumnSpan(PlayerArea, 3);

        PlayerFrame.Margin = new Thickness(0);
        PlayerFrame.CornerRadius = new CornerRadius(0);

        Background = System.Windows.Media.Brushes.Black;
        PlayerArea.Background = System.Windows.Media.Brushes.Black;
        PlayerFrame.Background = System.Windows.Media.Brushes.Black;
        VideoPlayer.Background = System.Windows.Media.Brushes.Black;

        WindowState = WindowState.Normal;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowState = WindowState.Maximized;
    }

    private void ExitFullscreen()
    {
        if (!_isFullscreen)
        {
            return;
        }

        _isFullscreen = false;

        SideMenu.Visibility = Visibility.Visible;
        ChannelPanel.Visibility = Visibility.Visible;
        PlayerControls.Visibility = Visibility.Visible;

        MenuColumn.Width = _previousMenuWidth;
        ListColumn.Width = _previousListWidth;
        ControlsRow.Height = _previousControlsHeight;

        Grid.SetColumn(PlayerArea, _previousPlayerColumn);
        Grid.SetColumnSpan(PlayerArea, _previousPlayerColumnSpan);

        PlayerFrame.Margin = _previousPlayerFrameMargin;
        PlayerFrame.CornerRadius = _previousPlayerFrameCornerRadius;

        PlayerArea.Background = System.Windows.Media.Brushes.Black;
        PlayerFrame.Background = System.Windows.Media.Brushes.Black;
        VideoPlayer.Background = System.Windows.Media.Brushes.Black;

        WindowStyle = _previousWindowStyle;
        ResizeMode = _previousResizeMode;
        WindowState = _previousWindowState;
    }
}
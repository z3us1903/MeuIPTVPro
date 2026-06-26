using System;
using System.Windows;
using System.Windows.Input;
using LibVLCSharp.Shared;

namespace MeuIPTVPro.Views;

public partial class FullscreenPlayerWindow : Window
{
    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private readonly string _streamUrl;

    public FullscreenPlayerWindow(string streamUrl)
    {
        InitializeComponent();

        _streamUrl = streamUrl ?? string.Empty;

        Loaded += FullscreenPlayerWindow_Loaded;
        KeyDown += Window_KeyDown;

        // double click to close
        FullscreenVideoView.MouseDoubleClick += (_, _) => Close();

        Closed += (_, _) =>
        {
            try
            {
                _mediaPlayer?.Stop();
                _mediaPlayer?.Dispose();
            }
            catch { }

            try
            {
                _libVlc?.Dispose();
            }
            catch { }

            try
            {
                FullscreenVideoView.MediaPlayer = null;
            }
            catch { }
        };
    }

    private void FullscreenPlayerWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_streamUrl))
        {
            MessageBox.Show("Nenhuma stream para reproduzir.", "Meu IPTV Pro", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
            return;
        }

        try
        {
            Core.Initialize();
            _libVlc = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVlc);

            FullscreenVideoView.MediaPlayer = _mediaPlayer;

            using var media = new Media(_libVlc, new Uri(_streamUrl));
            _mediaPlayer.Play(media);
        }
        catch
        {
            MessageBox.Show("Falha ao iniciar reprodução em tela cheia.", "Meu IPTV Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }
}

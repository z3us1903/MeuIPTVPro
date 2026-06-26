using System;
using System.Windows;
using MeuIPTVPro.Services;

namespace MeuIPTVPro.Views;

public partial class LoginWindow : Window
{
    private readonly AppSettingsService _settingsService = new();

    public LoginWindow()
    {
        InitializeComponent();

        var settings = _settingsService.Load();

        if (settings.RememberLogin)
        {
            ServerBox.Text = settings.Server;
            UserBox.Text = settings.Username;
            PasswordBox.Password = settings.Password;
            RememberBox.IsChecked = settings.RememberLogin;
        }
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        var server = ServerBox.Text.Trim();
        var username = UserBox.Text.Trim();
        var password = PasswordBox.Password.Trim();

        if (string.IsNullOrWhiteSpace(server))
        {
            StatusText.Text = "Informe o servidor.";
            return;
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            StatusText.Text = "Informe o usuário.";
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            StatusText.Text = "Informe a senha.";
            return;
        }

        try
        {
            if (RememberBox.IsChecked == true)
            {
                _settingsService.Save(new AppSettings
                {
                    Server = server,
                    Username = username,
                    Password = password,
                    RememberLogin = true
                });
            }

            var main = new MainWindow(server, username, password);
            main.Show();

            Close();
        }
        catch (Exception)
        {
            StatusText.Text = "Não foi possível abrir o aplicativo.";
        }
    }
}
using System.Net.Http;
using System.Text.Json;
using MeuIPTVPro.Models;

namespace MeuIPTVPro.Services;

public class XtreamService
{
    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public async Task<bool> TestLoginAsync(string server, string username, string password)
    {
        try
        {
            var baseUrl = NormalizeServer(server);

            var url =
                $"{baseUrl}/player_api.php?username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";

            var json = await _http.GetStringAsync(url);

            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            if (!doc.RootElement.TryGetProperty("user_info", out var user))
                return false;

            if (!user.TryGetProperty("auth", out var auth))
                return false;

            return auth.GetInt32() == 1;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<Channel>> GetLiveChannelsAsync(string server, string username, string password)
    {
        var channels = new List<Channel>();

        try
        {
            var baseUrl = NormalizeServer(server);

            var url =
                $"{baseUrl}/player_api.php?username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}&action=get_live_streams";

            var json = await _http.GetStringAsync(url);

            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return channels;

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                channels.Add(new Channel
                {
                    Name = Get(item, "name"),
                    Category = Get(item, "category_id"),
                    LogoUrl = Get(item, "stream_icon"),
                    StreamUrl =
                        $"{baseUrl}/live/{username}/{password}/{Get(item,"stream_id")}.m3u8"
                });
            }
        }
        catch
        {
            // retorna lista vazia
        }

        return channels;
    }

    private static string NormalizeServer(string server)
    {
        server = server.Trim().TrimEnd('/');

        if (!server.StartsWith("http://") &&
            !server.StartsWith("https://"))
        {
            server = "http://" + server;
        }

        return server;
    }

    private static string Get(JsonElement e, string property)
    {
        if (e.TryGetProperty(property, out var value))
            return value.ToString();

        return "";
    }
}
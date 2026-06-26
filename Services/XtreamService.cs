using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
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

            // Primeiro: buscar categorias reais
            var categories = new Dictionary<string, string>();
            try
            {
                var catUrl = $"{baseUrl}/player_api.php?username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}&action=get_live_categories";
                var catJson = await _http.GetStringAsync(catUrl);
                using var catDoc = JsonDocument.Parse(catJson);

                if (catDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var c in catDoc.RootElement.EnumerateArray())
                    {
                        var id = Get(c, "category_id");
                        if (string.IsNullOrWhiteSpace(id))
                            id = Get(c, "id");

                        var name = Get(c, "category_name");
                        if (string.IsNullOrWhiteSpace(name))
                            name = Get(c, "name");
                        if (string.IsNullOrWhiteSpace(name))
                            name = Get(c, "category");

                        if (!string.IsNullOrWhiteSpace(id) && !categories.ContainsKey(id))
                            categories[id] = name;
                    }
                }
            }
            catch
            {
                // falha ao buscar categorias: continua sem nomes legíveis
            }

            // Agora buscar canais
            var url =
                $"{baseUrl}/player_api.php?username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}&action=get_live_streams";

            var json = await _http.GetStringAsync(url);

            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return channels;

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var categoryId = Get(item, "category_id");

                // resolve category name from previously loaded categories
                string categoryName = "";
                if (!string.IsNullOrWhiteSpace(categoryId) && categories.TryGetValue(categoryId, out var cname))
                    categoryName = cname;

                if (string.IsNullOrWhiteSpace(categoryName))
                    categoryName = "Sem categoria";

                var channel = new Channel
                {
                    Name = Get(item, "name"),
                    // set Category to the readable name so UI bindings that use Category show the name
                    Category = categoryName,
                    CategoryName = categoryName,
                    LogoUrl = Get(item, "stream_icon"),
                    StreamUrl =
                        $"{baseUrl}/live/{username}/{password}/{Get(item, "stream_id")}.m3u8"
                };

                channels.Add(channel);
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

using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using MeuIPTVPro.Models;

namespace MeuIPTVPro.Services;

public class FavoritesService
{
    private readonly string _filePath;

    public FavoritesService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "MeuIPTVPro");
        try
        {
            Directory.CreateDirectory(dir);
        }
        catch
        {
            // ignore
        }

        _filePath = Path.Combine(dir, "favorites.json");
    }

    public HashSet<string> LoadFavorites()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new HashSet<string>();

            var json = File.ReadAllText(_filePath);
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return list is null ? new HashSet<string>() : new HashSet<string>(list);
        }
        catch
        {
            return new HashSet<string>();
        }
    }

    public void SaveFavorites(IEnumerable<string> favorites)
    {
        try
        {
            var list = favorites.Distinct().ToList();
            var json = JsonSerializer.Serialize(list);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // ignore
        }
    }

    public void ToggleFavorite(Channel channel)
    {
        if (channel is null)
            return;

        var fav = LoadFavorites();
        if (fav.Contains(channel.StreamUrl))
        {
            fav.Remove(channel.StreamUrl);
            channel.IsFavorite = false;
        }
        else
        {
            fav.Add(channel.StreamUrl);
            channel.IsFavorite = true;
        }

        SaveFavorites(fav);
    }
}

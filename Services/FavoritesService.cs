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

    public bool IsFavorite(Channel channel)
    {
        if (channel is null) return false;
        var fav = LoadFavorites();
        var id = !string.IsNullOrWhiteSpace(channel.StreamUrl) ? channel.StreamUrl : string.Empty;
        return fav.Contains(id);
    }

    public void ToggleFavorite(Channel channel)
    {
        if (channel is null)
            return;

        var fav = LoadFavorites();
        var id = !string.IsNullOrWhiteSpace(channel.StreamUrl) ? channel.StreamUrl : string.Empty;
        if (fav.Contains(id))
        {
            fav.Remove(id);
            channel.IsFavorite = false;
        }
        else
        {
            fav.Add(id);
            channel.IsFavorite = true;
        }

        SaveFavorites(fav);
    }

    public void SaveFavoritesFromChannels(IEnumerable<Channel> channels)
    {
        var ids = channels.Where(c => c != null && c.IsFavorite)
                          .Select(c => c.StreamUrl)
                          .Where(s => !string.IsNullOrWhiteSpace(s))
                          .Distinct();
        SaveFavorites(ids);
    }
}

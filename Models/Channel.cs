using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MeuIPTVPro.Models;

public class Channel : INotifyPropertyChanged
{
    private string _name = "";
    private string _category = "";
    private string _categoryName = "";
    private string _logoUrl = "";
    private string _streamUrl = "";
    private bool _isFavorite;

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public string Category
    {
        get => _category;
        set
        {
            if (_category != value)
            {
                _category = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayCategory));
            }
        }
    }

    public string CategoryName
    {
        get => _categoryName;
        set
        {
            if (_categoryName != value)
            {
                _categoryName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayCategory));
            }
        }
    }

    public string DisplayCategory
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(CategoryName))
            {
                return CategoryName;
            }

            if (!string.IsNullOrWhiteSpace(Category))
            {
                return Category;
            }

            return "Sem categoria";
        }
    }

    public string LogoUrl
    {
        get => _logoUrl;
        set
        {
            if (_logoUrl != value)
            {
                _logoUrl = value;
                OnPropertyChanged();
            }
        }
    }

    public string StreamUrl
    {
        get => _streamUrl;
        set
        {
            if (_streamUrl != value)
            {
                _streamUrl = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (_isFavorite != value)
            {
                _isFavorite = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FavoriteIcon));
            }
        }
    }

    public string FavoriteIcon => IsFavorite ? "★" : "☆";

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Name) ? "Canal sem nome" : Name;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
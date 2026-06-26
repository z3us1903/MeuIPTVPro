using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MeuIPTVPro.Models;

public class Channel : INotifyPropertyChanged
{
    // Nome do canal mostrado na UI
    private string _name = "";
    public string Name { get => _name; set { if (_name != value) { _name = value; OnPropertyChanged(); } } }

    // Id da categoria (geralmente numérico em string)
    private string _category = "";
    public string Category { get => _category; set { if (_category != value) { _category = value; OnPropertyChanged(); } } }

    // Nome legível da categoria (preenchido pelo XtreamService)
    private string _categoryName = "";
    public string CategoryName { get => _categoryName; set { if (_categoryName != value) { _categoryName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayCategory)); } } }

    // URL do logo do canal
    private string _logoUrl = "";
    public string LogoUrl { get => _logoUrl; set { if (_logoUrl != value) { _logoUrl = value; OnPropertyChanged(); } } }

    // URL/Stream para reprodução
    private string _streamUrl = "";
    public string StreamUrl { get => _streamUrl; set { if (_streamUrl != value) { _streamUrl = value; OnPropertyChanged(); } } }

    // Marca como favorito
    private bool _isFavorite = false;
    public bool IsFavorite { get => _isFavorite; set { if (_isFavorite != value) { _isFavorite = value; OnPropertyChanged(); OnPropertyChanged(nameof(FavoriteIcon)); } } }

    // Texto exibido abaixo do nome: usa o nome legível da categoria quando disponível
    public string DisplayCategory => !string.IsNullOrWhiteSpace(CategoryName) ? CategoryName : (!string.IsNullOrWhiteSpace(Category) ? Category : "Sem categoria");

    // Ícone simples (caracter) para favoritos — a UI pode usar este texto ou substituir por imagem
    public string FavoriteIcon => IsFavorite ? "★" : "☆";

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Name) ? "Canal sem nome" : Name;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

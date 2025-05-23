using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LORA_Preparator;

public class ImageModels : INotifyPropertyChanged
{
    private bool _processed;

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;

    public bool Processed
    {
        get => _processed;
        set
        {
            if (_processed != value)
            {
                _processed = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
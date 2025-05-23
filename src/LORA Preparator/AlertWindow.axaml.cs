using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace LORA_Preparator;

public partial class AlertWindow : Window
{
    public AlertWindow(string message, string color = "blue")
    {
        InitializeComponent();

        var background = color.ToLower() switch
        {
            "red" => new SolidColorBrush(Color.Parse("#FF1111")),
            "blue" => new SolidColorBrush(Color.Parse("#8888FF")),
            _ => new SolidColorBrush(Color.Parse("#8888FF")) // padrão azul pastel
        };

        this.Resources["WinBackgroundBrush"] = background;

        TextBlock textBlock = this.FindControl<TextBlock>("MessageText") ?? new();
        textBlock.Text = message;
    }

    private void OkButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SkiaSharp;
using Avalonia.Threading;
using System.Collections.ObjectModel;
using LibAPNG;

namespace LORA_Preparator;

public partial class MainWindow : Window
{
    public ObservableCollection<ImageModels> Images { get; private set; } = new();

    public MainWindow()
    {
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Images = []; // Clear List
        dtgCollection.ItemsSource = Images;
    }

    #region Load Folder

    private async void btnInput_OnSelectFolderClicked(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;

        var options = new FolderPickerOpenOptions
        {
            Title = "Select a folder",
            AllowMultiple = false,
            SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(string.IsNullOrEmpty(txtInput.Text) ? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) : txtInput.Text)
        };

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
        if (folders.Count > 0)
        {
            // Obtém o caminho da pasta selecionada
            var folder = folders[0];
            var path = folder.Path?.LocalPath ?? "";

            txtInput.Text = path;
            Images = []; // Clear List
            Images = ImageLoader.LoadImages(path);
            dtgCollection.ItemsSource = Images; // Fill DataGrid 
        }
    }

    #endregion

    #region Load Output

    private async void btnOutput_OnSelectFolderClicked(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;

        var options = new FolderPickerOpenOptions
        {
            Title = "Select a output folder",
            AllowMultiple = false,
            SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(string.IsNullOrEmpty(txtOutput.Text) ? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) : txtOutput.Text)
        };

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
        if (folders.Count > 0)
        {
            // Obtém o caminho da pasta selecionada
            var folder = folders[0];
            var path = folder.Path?.LocalPath ?? "";

            if (IsFolderEmpty(path))
                txtOutput.Text = path;
            else
                await ShowErrorDialog("The selected folder is not empty. Please select an empty folder.");
        }

    }

    #endregion

    #region WorkLoader

    private async void btnProcess_WorkLoader(object sender, RoutedEventArgs e)
    {
        #region Validations
        if (string.IsNullOrEmpty(txtInput.Text))
        {
            await ShowErrorDialog("You need to select a folder with images.");
            txtInput.Focus();
            return;
        }

        if (string.IsNullOrEmpty(txtOutput.Text))
        {
            await ShowErrorDialog("You need to select a ouput folder.");
            txtOutput.Focus();
            return;
        }

        if (Images.Count < 1)
        {
            await ShowErrorDialog("There are no images to work with.");
            return;
        }

        if (!IsFolderEmpty(txtOutput.Text))
        {
            await ShowErrorDialog("The selected output folder is not empty. Please select an empty folder.");
            return;
        }

        if (cmbBackgroundColor.SelectedIndex == -1)
            cmbBackgroundColor.SelectedIndex = 0;

        if (cmbResolution.SelectedIndex == -1)
            cmbResolution.SelectedIndex = 0;

        #endregion

        partFolder.IsEnabled = false;
        partOptions.IsEnabled = false;

        foreach (var i in Images)
            i.Processed = false;

        prgWorkProgress.Value = 0;
        prgWorkProgress.Maximum = cmbResolution.SelectedIndex == 2 ? (Images.Count * 2) : Images.Count;

        // Capture UI values ​​before the task 
        int resolution = cmbResolution.SelectedIndex;
        bool useBlackBg = cmbBackgroundColor.SelectedIndex == 0;
        bool extractAnimations = cbxExtractAnimations.IsChecked ?? false;

        await ResizeImagesUniformly(resolution, useBlackBg, extractAnimations);
    }

    #endregion

    #region Resize Images

    private async Task ResizeImagesUniformly(int modeIndex, bool useTransparentBackground = true, bool extractFrames = false)
    {
        int[] sizes = modeIndex switch
        {
            0 => [512],
            1 => [1024],
            2 => [512, 1024],
            _ => [512]
        };

        foreach (var size in sizes)
        {
            string outputDir = System.IO.Path.Combine(txtOutput.Text, $"x{size}");
            Directory.CreateDirectory(outputDir);

            foreach (var img in Images)
            {
                if (!File.Exists(img.Path))
                    continue;

                try
                {
                    if (System.IO.Path.GetExtension(img.Path).TrimStart('.').ToUpper().Equals("PNG") && new APNG(img.Path).DefaultImageIsAnimated)
                    {
                        using FileStream input = File.OpenRead(img.Path);
                        using var codec = SKCodec.Create(input);
                        if (codec == null)
                            continue;

                        List<SKBitmap> pngFrames = ExtractApngFrames(img.Path);
                        if (pngFrames.Count < 1)
                            continue;

                        int frameCount = extractFrames ? pngFrames.Count : 1;
                        for (int i = 0; i < frameCount; i++)
                        {
                            var options = new SKCodecOptions(i);
                            var result = codec.GetPixels(pngFrames[i].Info, pngFrames[i].GetPixels(), options);
                            if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
                                continue;

                            await ResizeAndSaveFrame(pngFrames[i], img.Path, outputDir, size, useTransparentBackground, i, frameCount, img.Id);
                        }
                    }
                    else
                    {
                        using FileStream input = File.OpenRead(img.Path);
                        using var codec = SKCodec.Create(input);
                        if (codec == null)
                            continue;

                        int frameCount = extractFrames ? codec.FrameCount : 1;
                        for (int i = 0; i <= frameCount; i++)
                        {
                            SKImageInfo originalInfo = codec.Info;
                            using var bitmap = new SKBitmap(originalInfo.Width, originalInfo.Height);

                            var options = new SKCodecOptions(i);
                            var result = codec.GetPixels(bitmap.Info, bitmap.GetPixels(), options);
                            if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
                                continue;

                            await ResizeAndSaveFrame(bitmap, img.Path, outputDir, size, useTransparentBackground, i, frameCount, img.Id);
                        }
                    }

                    // Dispache
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        img.Processed = cmbResolution.SelectedIndex != 2 || size == 1024;
                        prgWorkProgress.Value += 1;
                    });
                }
                catch
                {
#if DEBUG
                    throw;
#endif

                    await ShowErrorDialog($"Error on processing item Id: '{img.Id}' ");
                }
            }            
        }

        await ShowInfoDialog("Process finished!");
        partFolder.IsEnabled = true;
        partOptions.IsEnabled = true;
        await Task.CompletedTask;
    }

    #endregion

    #region Resize And Save PNG Frame

    private async Task ResizeAndSaveFrame(SKBitmap bitmap, string originalPath, string outputDir, int size, bool useTransparentBackground, int frameIndex, int totalFrames, string fileName)
    {
        int maxSide = Math.Max(bitmap.Width, bitmap.Height);
        float scale = (float)size / maxSide;

        int newWidth = (int)(bitmap.Width * scale);
        int newHeight = (int)(bitmap.Height * scale);

        using SKBitmap resized = bitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);

        SKImageInfo info = new SKImageInfo(size, size, SKColorType.Rgba8888, useTransparentBackground ? SKAlphaType.Premul : SKAlphaType.Opaque);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(useTransparentBackground ? SKColors.Transparent : new SKColor(128, 128, 128, 255));

        int x = (size - newWidth) / 2;
        int y = (size - newHeight) / 2;
        canvas.DrawBitmap(resized, x, y);

        using SKImage image = surface.Snapshot();
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

        string frameSuffix = totalFrames > 1 ? $"_f{frameIndex:D2}" : "";
        string outPath = System.IO.Path.Combine(outputDir, $"{fileName}{frameSuffix}.png");

        using var output = File.OpenWrite(outPath);
        data.SaveTo(output);

        await Task.CompletedTask;
    }

    #endregion

    #region Extract Animated PNG Frames

    public static List<SKBitmap> ExtractApngFrames(string filePath)
    {
        var frames = new List<SKBitmap>();
        var apng = new APNG(filePath);

        if (apng.Frames.Count() > 1)
        {
            foreach (Frame frame in apng.Frames)
            {
                var bitmap = SKBitmap.Decode(frame.GetStream().ToArray());
                frames.Add(bitmap);
            }
        }    

        return frames;
    }

    #endregion

    #region Clear Combo

    private void OnComboBoxRightClick(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint((Interactive)sender).Properties.IsRightButtonPressed)
        {
            var comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                comboBox.SelectedIndex = -1; // Limpa a seleção
            }
        }
    }

    #endregion

    #region Erro Dialog

    private async Task ShowErrorDialog(string message)
    {
        var alert = new AlertWindow(message, "red");
        await alert.ShowDialog(this);
    }

    #endregion

    #region Info Dialog

    private async Task ShowInfoDialog(string message)
    {
        var alert = new AlertWindow(message, "blue");
        await alert.ShowDialog(this);
    }

    #endregion

    #region IsFolderEmpty

    private bool IsFolderEmpty(string path)
    {
        try
        {
            return Directory.Exists(path) && !Directory.GetFiles(path).Any() && !Directory.GetDirectories(path).Any();
        }
        catch
        {
#if DEBUG
            throw;
#endif
            return false;
        }
    }

    #endregion
}
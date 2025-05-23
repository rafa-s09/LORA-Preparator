using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace LORA_Preparator;

public class ImageLoader
{
    public static ObservableCollection<ImageModels> LoadImages(string folderPath)
    {
        if (!Directory.Exists(folderPath)) 
            return new();

        string[] validExtensions = [".jpg", ".png", ".bmp", ".gif", ".webp"];
        List<string>? files = [.. Directory.GetFiles(folderPath).Where(f => validExtensions.Contains(Path.GetExtension(f).ToLower()))];

        ObservableCollection<ImageModels> imageList = [];
        int idPadding = files.Count.ToString().Length;
        int idCounter = 1;

        foreach (var file in files)
        {
            imageList.Add(new ImageModels
            {
                Id = "LP" + idCounter.ToString().PadLeft(idPadding, '0'),
                Name = Path.GetFileNameWithoutExtension(file),
                Format = Path.GetExtension(file).TrimStart('.').ToUpper(),
                Path = file,
                Processed = false
            });

            idCounter++;
        }

        return imageList;
    }
}

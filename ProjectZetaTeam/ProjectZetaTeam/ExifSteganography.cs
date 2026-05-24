using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System.IO;


namespace ProjectZetaTeam
{


// class ExifSteganography{
//     HideMessageInExif()
//     ExtractMessageFromExif()    
// }


    internal static class ExifSteganography
    {
        public static void HideMessageInExif(string inputPath, string message, string outputPath)
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException("Исходное изображение не найдено.", inputPath);

            string? outDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            using (var image = Image.Load(inputPath))
            {
                ExifProfile exif = image.Metadata.ExifProfile ?? new ExifProfile();

                exif.SetValue(ExifTag.ImageDescription, message);
                image.Metadata.ExifProfile = exif;

                image.Save(outputPath);
            }
        }

        public static string ExtractMessageFromExif(string imagePath)
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException("Изображение не найдено.", imagePath);

            using (var image = Image.Load(imagePath))
            {
                ExifProfile? exif = image.Metadata.ExifProfile;
                if (exif == null)
                    return string.Empty;

                IExifValue? exifValue = exif.GetValue(ExifTag.ImageDescription);
                if (exifValue == null)
                    return string.Empty;

                object? rawValue = exifValue.GetValue();
                return rawValue?.ToString() ?? string.Empty;
            }
        }

        public static bool IsSupportedImageFormat(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                IImageFormat format = Image.DetectFormat(filePath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

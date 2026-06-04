using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ProjectZetaTeam.Views
{
    public static class LsbSteganography
    {
        // magic header: 0xAA 0xBB 0xCC
        private static readonly byte[] MAGIC_HEADER = { 0xAA, 0xBB, 0xCC };
        private const int MAGIC_LEN = 3;
        private const int LENGTH_LEN = 4;

        public static void HideText(string inputPath, string outputPath, string secretText)
        {
            if (string.IsNullOrEmpty(inputPath))
                throw new ArgumentNullException(nameof(inputPath));
            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentNullException(nameof(outputPath));
            if (secretText == null)
                throw new ArgumentNullException(nameof(secretText));

            byte[] secretBytes = Encoding.UTF8.GetBytes(secretText);

            byte[] lengthBytes = BitConverter.GetBytes(secretBytes.Length);
            byte[] allData = new byte[MAGIC_LEN + LENGTH_LEN + secretBytes.Length];
            Buffer.BlockCopy(MAGIC_HEADER, 0, allData, 0, MAGIC_LEN);
            Buffer.BlockCopy(lengthBytes, 0, allData, MAGIC_LEN, LENGTH_LEN);
            Buffer.BlockCopy(secretBytes, 0, allData, MAGIC_LEN + LENGTH_LEN, secretBytes.Length);

            BitArray bits = new BitArray(allData);
            int bitIndex = 0;

            using Image<Rgba32> image = Image.Load<Rgba32>(inputPath);

            long totalPixels = (long)image.Width * image.Height;
            if (bits.Length > totalPixels * 3)
            {
                throw new InvalidOperationException("Текст слишком большой для этого изображения!");
            }

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> row = accessor.GetRowSpan(y);

                    for (int x = 0; x < row.Length; x++)
                    {
                        if (bitIndex >= bits.Length) return;

                        ref Rgba32 pixel = ref row[x];

                        pixel.R = (byte)((pixel.R & 0xFE) | (bits[bitIndex++] ? 1 : 0));

                        if (bitIndex < bits.Length)
                            pixel.G = (byte)((pixel.G & 0xFE) | (bits[bitIndex++] ? 1 : 0));

                        if (bitIndex < bits.Length)
                            pixel.B = (byte)((pixel.B & 0xFE) | (bits[bitIndex++] ? 1 : 0));
                    }
                }
            });

            // Ensure the output filename has .png extension
            if (!outputPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                outputPath += ".png";
            var pngEncoder = new PngEncoder();
            image.Save(outputPath, pngEncoder);
        }

        public static string ExtractText(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                throw new ArgumentNullException(nameof(imagePath));

            using Image<Rgba32> image = Image.Load<Rgba32>(imagePath);


            List<byte> extractedBytes = new List<byte>();
            byte currentByte = 0;
            int bitPosition = 0;

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        Rgba32 pixel = row[x];

                        for (int channel = 0; channel < 3; channel++)
                        {
                            int bitValue = 0;
                            switch (channel)
                            {
                                case 0: bitValue = pixel.R & 1; break;
                                case 1: bitValue = pixel.G & 1; break;
                                case 2: bitValue = pixel.B & 1; break;
                            }

                            if (bitValue == 1)
                                currentByte |= (byte)(1 << bitPosition);

                            bitPosition++;

                            if (bitPosition == 8)
                            {
                                extractedBytes.Add(currentByte);
                                currentByte = 0;
                                bitPosition = 0;
                            }
                        }
                    }
                }
            });

            // check is place enough
            if (extractedBytes.Count < MAGIC_LEN + LENGTH_LEN)
                return string.Empty;

            // magic header
            for (int i = 0; i < MAGIC_LEN; i++)
            {
                if (extractedBytes[i] != MAGIC_HEADER[i])
                    return string.Empty;
            }

            // lenth 
            int messageLength = BitConverter.ToInt32(extractedBytes.ToArray(), MAGIC_LEN);
            if (messageLength <= 0 || messageLength > extractedBytes.Count - (MAGIC_LEN + LENGTH_LEN))
                return string.Empty;

            // message 
            byte[] messageBytes = extractedBytes.GetRange(MAGIC_LEN + LENGTH_LEN, messageLength).ToArray();

            string result = Encoding.UTF8.GetString(messageBytes);
            return result;
        }
    }
}

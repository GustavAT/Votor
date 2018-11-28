using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ZXing;
using ZXing.QrCode;

namespace Votor
{
    public static class Util
    {
        public static Guid? ParseGuid(string source)
        {
            return Guid.TryParse(source, out var guid) ? guid : (Guid?) null;
        }

        /// <summary>
        /// Generate QR code from given string with given width and height.
        /// </summary>
        /// <param name="content">Content</param>
        /// <param name="height">Height</param>
        /// <param name="width">Width</param>
        /// <returns>Base64 of the QR code</returns>
        public static string GenerateQrCode(string content, int width = 250, int height = 250)
        {
            var options = new QrCodeEncodingOptions
            {
                DisableECI = true,
                CharacterSet = "UTF-8",
                Height = height,
                Width = width
            };

            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = options
            };

            var pixelData = writer.Write(content);

            using (var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb))
            {
                using (var stream = new MemoryStream())
                {
                    var bitmapData = bitmap.LockBits(new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                        ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);

                    try
                    {
                        System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0,
                            pixelData.Pixels.Length);
                    }
                    finally
                    {
                        bitmap.UnlockBits(bitmapData);
                    }

                    bitmap.Save(stream, ImageFormat.Png);
                    return Convert.ToBase64String(stream.ToArray());
                }
            }
        }

        /// <summary>
        /// Convert given base64 string to string that can be used for html img tags.
        /// </summary>
        /// <param name="base64">Base64 source string.</param>
        /// <returns>Image tag string.</returns>
        public static string ToImageSourceString(string base64)
        {
            return $"data:image/png;base64,{base64}";
        }
    }
}

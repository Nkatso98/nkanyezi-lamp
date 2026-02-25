using System.Drawing;
using Tesseract;
using System.Runtime.InteropServices;

namespace NkanyeziLamp.Api.Services
{
    public static class PixConverter
    {
        public static Pix ToPix(Bitmap bitmap)
        {
            // Convert Bitmap to Pix using MemoryStream
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                ms.Position = 0;
                return Pix.LoadFromMemory(ms.ToArray());
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace MillionHero.Services.Ocrs
{
    public abstract class AbstractOcr:IOcr
    {
        public string[] Ocr(string imageFile)
        {
            return Ocr(System.IO.File.ReadAllBytes(imageFile));
        }

        public abstract string[] Ocr(byte[] imageData);

        public string[] Ocr(Image img)
        {
            using (var ms = new MemoryStream())
            {
                img.Save(ms, ImageFormat.Png);
                return Ocr(ms.ToArray());
            }
        }
    }
}

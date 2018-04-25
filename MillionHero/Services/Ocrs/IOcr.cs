using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace MillionHero.Services.Ocrs
{
    public interface IOcr
    {
        string[] Ocr(string imageFile);
        string[] Ocr(byte[] imageData);
        string[] Ocr(Image img); 
    }
}

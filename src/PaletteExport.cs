using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ExtractPokemonCrystalPalette
{
    public static class PaletteExport
    {
        public static void Export(Dictionary<Colour, HashSet<string>> colourDictionary)
        {
            var keyCount = colourDictionary.Keys.Count;
            var sideLength = (int)Math.Ceiling(Math.Sqrt(keyCount));
            var image = new Bitmap(sideLength, sideLength);

            // Convert colors to HSB and sort them
            var sortedColors = colourDictionary.Keys
                .Select(c => new { Color = c, HSB = ColorToHSB(c) })
                .OrderBy(c => c.HSB.H)
                .ThenBy(c => c.HSB.S)
                .ThenBy(c => c.HSB.B)
                .Select(c => c.Color)
                .ToList();

            for (int index = 0; index < sortedColors.Count; index++)
            {
                var item = sortedColors[index];
                var color = Color.FromArgb(item.Red, item.Green, item.Blue);
                int x = index % sideLength;
                int y = index / sideLength;
                image.SetPixel(x, y, color);
            }

            image.Save(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CrystalPalette.png"));
        }

        private static (float H, float S, float B) ColorToHSB(Colour color)
        {
            float r = color.Red / 255f;
            float g = color.Green / 255f;
            float b = color.Blue / 255f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;

            float h = 0;
            if (delta != 0)
            {
                if (max == r)
                {
                    h = (g - b) / delta;
                }
                else if (max == g)
                {
                    h = 2 + (b - r) / delta;
                }
                else
                {
                    h = 4 + (r - g) / delta;
                }
                h *= 60;
                if (h < 0) h += 360;
            }

            float s = max == 0 ? 0 : delta / max;
            float v = max;

            return (h, s, v);
        }
    }
}

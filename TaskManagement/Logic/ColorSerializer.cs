﻿using System.Drawing;

namespace TaskManagement.Logic
{
    class ColorSerializer
    {
        public static string Serialize(Color c)
        {
            return c.R.ToString() + "/" + c.G.ToString() + "/" + c.B.ToString();
        }

        public static Color Deserialize(string text)
        {
            var words = text.Split('/');
            var r = int.Parse(words[0]);
            var g = int.Parse(words[1]);
            var b = int.Parse(words[2]);
            return Color.FromArgb(r, g, b);
        }
    }
}

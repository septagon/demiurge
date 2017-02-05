using System.Drawing;

using DemiurgeLib;
using DemiurgeLib.Common;
using System;

namespace DemiurgeConsole
{
    public class Program
    {
        static void Main(string[] args)
        {
            const int Width = 1024;
            const int Height = 1024;
            const float Scale = 0.01f;

            //IField2d<float> basis = new DemiurgeLib.Noise.DiamondSquare2D(Width, Height);

            //IField2d<float> field;
            //field = new DemiurgeLib.Noise.Simplex2D(Height, Width, Scale);
            //field = new InvertTransform(new AbsTransform(field));

            //IField2d<float> field2;
            //field2 = new DemiurgeLib.Noise.Simplex2D(Height, Width, Scale * 10);
            //field2 = new ScaleTransform(new InvertTransform(new AbsTransform(field2)), 0.1f);

            //field = new NormalizedComposition2d<float>(basis, field, field2);

            //Bitmap bmp = new Bitmap(Height, Width);
            //for (int x = 0, y = 0; y < bmp.Height; y += ++x / bmp.Width, x %= bmp.Width)
            //{
            //    int intensity = (int)(255.0 * field[x, y]);
            //    bmp.SetPixel(x, y, Color.FromArgb(intensity, intensity, intensity));
            //}
            //bmp.Save("C:\\Users\\Justin Murray\\Desktop\\output.png");

            //// ====================================================================
            //IField2d<float> field = new DemiurgeLib.Noise.Simplex2D(Height, Width, Scale);
            //IField2d<BrownianTree.Availability> transf = new Transformation2d<float, BrownianTree.Availability>(field, (x, y, val) =>
            //{
            //    if (x < Width / 20 || x > Width * 19 / 20 || y < Height / 20 || y > Height * 19 / 20)
            //    {
            //        return BrownianTree.Availability.Unavailable;
            //    }
            //    return BrownianTree.Availability.Available;
            //});
            //BrownianTree tree = new BrownianTree(transf);
            //tree.RunDefaultTree();

            Bitmap jranjana = new Bitmap("C:\\Users\\Justin Murray\\Desktop\\jranjana_landmasses_rivers.png");
            Field2d<float> field = new FieldFromBitmap(jranjana);
            BrownianTree tree = new BrownianTree(field, (x) => x > 0.5f ? BrownianTree.Availability.Available : BrownianTree.Availability.Unavailable);
            tree.RunDefaultTree();

            Bitmap bmp = new Bitmap(tree.Width, tree.Height);
            for (int x = 0, y = 0; y < bmp.Height; y += ++x / bmp.Width, x %= bmp.Width)
            {
                bmp.SetPixel(x, y, tree[y, x] == BrownianTree.Availability.Available ? Color.White : Color.Black);
            }
            bmp.Save("C:\\Users\\Justin Murray\\Desktop\\tree.png");
        }

        private class FieldFromBitmap : Field2d<float>
        {
            public FieldFromBitmap(Bitmap bmp) : base(bmp.Width, bmp.Height)
            {
                for (int x = 0, y = 0; y < this.Height; y += ++x / this.Width, x %= this.Width)
                {
                    this[y, x] = bmp.GetPixel(x, y).GetBrightness();
                }
            }
        }
    }
}

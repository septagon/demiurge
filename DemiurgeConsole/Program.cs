using DemiurgeLib;
using DemiurgeLib.Common;
using System.Drawing;

namespace DemiurgeConsole
{
    public class Program
    {
        const int Width = 1024;
        const int Height = 1024;
        const float Scale = 0.01f;

        static void Main(string[] args)
        {
            RunWateryScenario();
        }

        private static void RunWateryScenario()
        {
            Bitmap jranjana = new Bitmap("C:\\Users\\Justin Murray\\Desktop\\jranjana_landmasses_rivers_small.png");
            Field2d<float> field = new FieldFromBitmap(jranjana);
            BrownianTree tree = BrownianTree.CreateFromOther(field, (x) => x > 0.5f ? BrownianTree.Availability.Available : BrownianTree.Availability.Unavailable);
            tree.RunDefaultTree();
            HydrologicalField hydro = new HydrologicalField(tree);
            var sets = ContiguousSets.FindContiguousSets(hydro);
            System.Collections.Generic.List<ContiguousSets.TreeNode<Point2d>> riverForest = new System.Collections.Generic.List<ContiguousSets.TreeNode<Point2d>>();
            foreach (var river in sets[HydrologicalField.LandType.Shore])
            {
                riverForest.Add(ContiguousSets.MakeTreeFromContiguousSet(river, pt =>
                {
                    // Warning: naive non-boundary-checking test-only implementation.  This will probably CRASH THE PROGRAM
                    // if a river happens to border the edge of the map.
                    return
                        hydro[pt.y + 1, pt.x + 1] == HydrologicalField.LandType.Ocean ||
                        hydro[pt.y + 1, pt.x + 0] == HydrologicalField.LandType.Ocean ||
                        hydro[pt.y + 1, pt.x - 1] == HydrologicalField.LandType.Ocean ||
                        hydro[pt.y + 0, pt.x - 1] == HydrologicalField.LandType.Ocean ||
                        hydro[pt.y - 1, pt.x - 1] == HydrologicalField.LandType.Ocean ||
                        hydro[pt.y - 1, pt.x + 0] == HydrologicalField.LandType.Ocean ||
                        hydro[pt.y - 1, pt.x + 1] == HydrologicalField.LandType.Ocean ||
                        hydro[pt.y + 0, pt.x + 1] == HydrologicalField.LandType.Ocean;
                }));
            }

            foreach (var river in riverForest)
            {
                System.Console.WriteLine("Got a river with depth " + river.Depth() + "\tsize " + river.Size() + "\tfork rank " + river.ForkRank() + "\tmax fork " + river.MaxForkRank());
            }

            // Contiguous shores
            // BFS for river trees

            Bitmap bmp = new Bitmap(hydro.Width, hydro.Height);
            //for (int x = 0, y = 0; y < bmp.Height; y += ++x / bmp.Width, x %= bmp.Width)
            //{
            //    //bmp.SetPixel(x, y, tree[y, x] == BrownianTree.Availability.Available ? Color.White : Color.Black);
            //    switch (hydro[y, x])
            //    {
            //        case HydrologicalField.LandType.Land:
            //            bmp.SetPixel(x, y, Color.Wheat);
            //            break;
            //        case HydrologicalField.LandType.Shore:
            //            bmp.SetPixel(x, y, Color.Teal);
            //            break;
            //        case HydrologicalField.LandType.Ocean:
            //            bmp.SetPixel(x, y, Color.DarkBlue);
            //            break;
            //    }
            //}
            System.Random rand = new System.Random();
            foreach (var hs in sets.Values)
            {
                foreach (var ps in hs)
                {
                    Color color = Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));
                    foreach (Point2d p in ps)
                    {
                        bmp.SetPixel(p.x, p.y, color);
                    }
                }
            }
            bmp.Save("C:\\Users\\Justin Murray\\Desktop\\tree.png");
        }

        private static void RunNoisyScenario()
        {
            IField2d<float> basis = new DemiurgeLib.Noise.DiamondSquare2D(Width, Height);

            IField2d<float> field;
            field = new DemiurgeLib.Noise.Simplex2D(Height, Width, Scale);
            field = new InvertTransform(new AbsTransform(field));

            IField2d<float> field2;
            field2 = new DemiurgeLib.Noise.Simplex2D(Height, Width, Scale * 10);
            field2 = new ScaleTransform(new InvertTransform(new AbsTransform(field2)), 0.1f);

            field = new NormalizedComposition2d<float>(basis, field, field2);

            Bitmap bmp = new Bitmap(Height, Width);
            for (int x = 0, y = 0; y < bmp.Height; y += ++x / bmp.Width, x %= bmp.Width)
            {
                int intensity = (int)(255.0 * field[x, y]);
                bmp.SetPixel(x, y, Color.FromArgb(intensity, intensity, intensity));
            }
            bmp.Save("C:\\Users\\Justin Murray\\Desktop\\output.png");
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

using DemiurgeLib;
using DemiurgeLib.Common;
using DemiurgeLib.Noise;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace DemiurgeLib
{
    public class WaterTableArgs
    {
        public string inputPath = "C:\\Users\\Justin Murray\\Desktop\\maps\\input\\";
        public string outputPath = "C:\\Users\\Justin Murray\\Desktop\\maps\\output\\";
        public long seed = 0;
        public float baseNoiseScale = 0.015f;
        public float baseNoiseScalar = 0.1f;
        public int hydroSensitivity = 8;
        public float hydroShoreThreshold = 0.5f;
        public float wtfShore = 0.01f;
        public int wtfIt = 10;
        public int wtfLen = 5;
        public float wtfGrade = 0f;
        public float wtfCarveAdd = 0.3f;
        public float wtfCarveMul = 1.3f;
    }

    public partial class Utils
    {
        public static WaterTableField GenerateWaters(Bitmap bmp, IField2d<float> baseField = null, WaterTableArgs args = null, Random random = null)
        {
            args = args ?? new WaterTableArgs() { seed = System.DateTime.UtcNow.Ticks };
            random = random ?? new Random((int)args.seed);
            baseField = baseField ?? new Simplex2D(bmp.Width, bmp.Height, args.baseNoiseScale, args.seed);

            Field2d<float> field = new DemiurgeLib.Common.Utils.FieldFromBitmap(bmp);

            baseField = new NormalizedComposition2d<float>(baseField, new ScaleTransform(new Simplex2D(baseField.Width, baseField.Height, args.baseNoiseScale, args.seed), args.baseNoiseScalar));

            BrownianTree tree = BrownianTree.CreateFromOther(field, (x) => x > 0.5f ? BrownianTree.Availability.Available : BrownianTree.Availability.Unavailable, random);
            tree.RunDefaultTree();

            HydrologicalField hydro = new HydrologicalField(tree, args.hydroSensitivity, args.hydroShoreThreshold);
            WaterTableField wtf = new WaterTableField(baseField, hydro, args.wtfShore, args.wtfIt, args.wtfLen, args.wtfGrade, () =>
            {
                return (float)(args.wtfCarveAdd + random.NextDouble() * args.wtfCarveMul);
            });

            return wtf;
        }

        public static IField2d<float> GetWaterinessMap(WaterTableField wtf, IField2d<float> rainfall, float waterPortability = 5f, float waterinessAttenuation = 20f)
        {
            // Generate "water flow" map using the rainfall map and the water table map, characterizing
            // how much water is in an area based on upstream.  Note that, in the simplistic case of 
            // identical universal rainfall, this is just a scalar on depth; this whole shindig is
            // intended to support variable rainfall, as characterized by the rainfall map.
            Field2d<float> waterFlow = new Field2d<float>(rainfall);
            float maxValue = float.MinValue;
            foreach (TreeNode<Point2d> waterway in wtf.Waterways)
            {
                List<TreeNode<Point2d>> flattenedReversedRiverTree = new List<TreeNode<Point2d>>(waterway);
                flattenedReversedRiverTree.Reverse();

                foreach (var node in flattenedReversedRiverTree)
                {
                    if (node.parent != null)
                    {
                        Point2d cur = node.value;
                        Point2d par = node.parent.value;

                        waterFlow[par.y, par.x] += waterFlow[cur.y, cur.x];
                    }

                    maxValue = Math.Max(maxValue, waterFlow[node.value.y, node.value.x]);
                }
            }
            IField2d<float> waterinessUnblurred = new FunctionField<float>(waterFlow.Width, waterFlow.Height,
                (x, y) => 1f / (1f + (float)Math.Pow(Math.E, -waterinessAttenuation * waterFlow[y, x] / maxValue)));
            return new BlurredField(waterinessUnblurred, waterPortability);
        }
    }
}

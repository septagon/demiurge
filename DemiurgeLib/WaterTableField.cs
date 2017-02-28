using DemiurgeLib.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemiurgeLib
{
    /// <summary>
    /// TODO: This is a draft class as it doesn't quite behave properly for all scenarios.
    /// Flaws include the tendency to produce singularly prominent soures of for extremely
    /// short rivers, and to completely eliminate the heightmaps for landmasses that contain
    /// few or no rivers.  Both of these are deal-breaking flaws and must be overcome before
    /// this approach can be accepted as a method of generating base altitudes; that said,
    /// other than those flaws, it's working splendidly!
    /// </summary>
    public class WaterTableField : Field2d<float>
    {
        public Dictionary<HydrologicalField.LandType, HashSet<PointSet2d>> GeographicFeatures { get; private set; }
        public List<TreeNode<Point2d>> Waterways { get; private set; }
        public List<TreeNode<TreeNode<Point2d>>> RiverSystems { get; private set; }
        public DrainageField DrainageField { get; private set; }

        public WaterTableField(
            IField2d<float> baseField,
            IField2d<HydrologicalField.LandType> hydroField,
            float epsilon = 0.05f,
            int blurIterations = 10,
            int minWaterwayLength = 5)
            : base(baseField.Width, baseField.Height)
        {
            GeographicFeatures = hydroField.FindContiguousSets();
            Waterways = GeographicFeatures.GetRiverSystems(hydroField).Where(ww => ww.Depth() >= minWaterwayLength).ToList();
            RiverSystems = Waterways.GetRivers();
            DrainageField = new DrainageField(hydroField, Waterways);

            foreach (var sea in GeographicFeatures[HydrologicalField.LandType.Ocean])
            {
                foreach (var p in sea)
                {
                    this[p.y, p.x] = 0f;
                }
            }

            // Set the heights of all the river systems.
            foreach (var river in RiverSystems)
            {
                Queue<TreeNode<TreeNode<Point2d>>> mouths = new Queue<TreeNode<TreeNode<Point2d>>>();
                mouths.Enqueue(river);

                Point2d p = river.value.value;
                this[p.y, p.x] = 0f;

                while (mouths.Count > 0)
                {
                    var mouth = mouths.Dequeue();

                    // Discarded alternative approach: instead of using an incrementor, use a low pass filter
                    // against base field altitude (with a no-lowering caveat).  Produces some nice effects and
                    // leaves mountains remarkably well intact; however, abandoned because, when faced with a
                    // river that flows all the way through a mountain range, the filter will choose to extend
                    // the mountain range rather than carve a valley through it.

                    p = mouth.value.value;
                    float mouthAlti = this[p.y, p.x];
                    mouth.IteratePrimarySubtree().Iterate(node => p = node.value);
                    // Prevent rivers from ever flowing uphill.
                    float sourceAlti = Math.Max(mouthAlti, baseField[p.y, p.x]);

                    float inc = (sourceAlti - mouthAlti) / mouth.value.Depth();

                    mouth.IteratePrimarySubtree().Iterate(node =>
                    {
                        if (node.parent != null)
                        {
                            p = node.value;
                            Point2d pt = node.parent.value;
                            this[p.y, p.x] = this[pt.y, pt.x] + inc;
                        }
                    });

                    foreach (var child in mouth.children)
                    {
                        mouths.Enqueue(child);
                    }
                }
            }

            // At this point, all the water pixels have a defined height; set every
            // land pixel to be the same height as its drain iff it drains to a river.
            foreach (var land in GeographicFeatures[HydrologicalField.LandType.Land])
            {
                foreach (var p in land)
                {
                    Point2d drain = DrainageField[p.y, p.x];
                    if (hydroField[drain.y, drain.x] == HydrologicalField.LandType.Shore)
                    {
                        this[p.y, p.x] = this[drain.y, drain.x] + epsilon;
                    }
                    else
                    {
                        this[p.y, p.x] = baseField[p.y, p.x] + epsilon;
                    }
                }
            }

            for (int idx = 0; idx < blurIterations; idx++)
            {
                BlurredField bf = new BlurredField(this, 1);
                foreach (var land in GeographicFeatures[HydrologicalField.LandType.Land])
                {
                    foreach (var p in land)
                    {
                        this[p.y, p.x] = bf[p.y, p.x];
                    }
                }
            }

            System.Diagnostics.Debug.Assert(Waterways.AreWaterwaysLegalForField(this));
        }
    }
}

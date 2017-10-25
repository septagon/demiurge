using DemiurgeLib;
using DemiurgeLib.Common;
using DemiurgeLib.Noise;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using static DemiurgeConsole.Utils;

namespace DemiurgeConsole
{
    public class Program
    {
        const int StackSize = 10485760;

        static void Main(string[] args)
        {
            //TestScenarios.RunWateryScenario();
            //new Thread(TestScenarios.RunWaterHeightScenario, StackSize).Start();
            //TestScenarios.RunMountainousScenario(1024, 1024, 0.005f);
            //new Thread(TestScenarios.RunPopulationScenario, StackSize).Start();
            //TestScenarios.RunSplineScenario();
            //TestScenarios.RunZoomedInScenario();
            //TestScenarios.RunPathScenario();

            /*
             * TODO: Let's not ignore the magnitude of the first render success just because it isn't the end of the road:
             * we just proved that we could render out the entire island -- all Madagascar-esque size of it -- down to 20
             * meter horizontal precision in 60 hours on a single CPU core.  This means that, as kinks get worked out and
             * advances are made, it's very possible to do precise and advanced reasoning about the land by actually 
             * rendering it out.
             * 
             * Now, notes about the next steps:
             *   - The single-value "canyon factor" carving of rivers in mountains is MUCH too agressive and consistent, 
             *     produces extremely characteristic and noticeable "U-valley" artifacts that show up badly in gradient
             *     analysis.  Not the end of the world, doesn't preclude most utility; but revisit the canyon/valley
             *     logic in MeterScaleMap's damping code and see if there isn't something smarter to do there.
             *   - Creating the MeterScaleMap took around half an hour; rendering the map to disk took almost three days.
             *     Run some quick performance analysis on the up-res portion to figure out what the is taking so long 
             *     (it's probably the spline tree) and see if there's something to be done about it.  Global "spline tree
             *     sample map" computed upfront?
             *   - Rivers don't meet the sea.  It's past time that we really looked deeply into this and got it solved,
             *     this should be an easy one.
             *   - Tributaries sometimes seem to narrow to nothing before they join the river they feed.  Spline tree
             *     acting weird again?  Is this one of the "endpoint mirroring" hacks coming back to bite me in the ass?
             *   - The roughness map and the base heights map need to be separated.  This is probably the quickest and
             *     easiest thing that can be done which will dramatically improve the quality of the resulting maps.  In
             *     the first render, the Kirai river rises 1200m in 80km (-0.015 slope, 20x that of the Amazon); keep in
             *     mind that the source of the Mississippi is only around 500m above sea level.
             *   - Similarly, the use of just "mountain terrain" may not be ideal long-term, although it seems to be more
             *     than adequate for right now.  Low pri, but hills?
             *   - Erosion.
             */
            //new Thread(TestScenarios.RunMeterScaleMapScenarioUR, StackSize).Start();

            //var wta = new WaterTableArgs();
            //wta.inputPath = "C:\\Users\\Justin Murray\\Desktop\\egwethoon\\input\\";
            //var waters = new FieldFromBitmap(new Bitmap(wta.inputPath + "coastline.png"));
            //var heights = new FieldFromBitmap(new Bitmap(wta.inputPath + "heights.png"));
            //var roughness = new FieldFromBitmap(new Bitmap(wta.inputPath + "roughness.png"));
            //var msmArgs = new MeterScaleMap.Args(waters, heights, roughness, null);
            //msmArgs.seed = System.DateTime.UtcNow.Ticks;
            //msmArgs.metersPerPixel = 800;
            //msmArgs.riverCapacityToMetersWideFunc = c => (float)Math.Pow(msmArgs.metersPerPixel * SplineTree.CAPACITY_DIVISOR * c, 0.5f) / 4f;
            //msmArgs.baseHeightMaxInMeters = 500;
            //msmArgs.valleyStrength = 0.98f;
            //var msm = new MeterScaleMap(msmArgs);
            //
            //msm.OutputHighLevelMaps(new Bitmap(waters.Width, waters.Height), "C:\\Users\\Justin Murray\\Desktop\\egwethoon\\");
            //msm.OutputMapGrid(100, "C:\\Users\\Justin Murray\\Desktop\\egwethoon\\", "submap", 32);
            //TestScenarios.RunStreamedMapCombinerScenario();

            var heightmap = new FieldFromBitmap(new Bitmap("C:\\Users\\Justin Murray\\Desktop\\input.png"));
            var eroded = Erosion.DropletHydraulic(heightmap, 300000, 100);
            OutputField(new NormalizedComposition2d<float>(eroded), new Bitmap(eroded.Width, eroded.Height), "C:\\Users\\Justin Murray\\Desktop\\output.png");
        }
    }
}

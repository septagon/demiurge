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

            /*var wta = new WaterTableArgs();
            wta.inputPath = "C:\\Users\\Justin Murray\\Desktop\\egwethoon\\input\\";
            var waters = new FieldFromBitmap(new Bitmap(wta.inputPath + "coastline.png"));
            var heights = new FieldFromBitmap(new Bitmap(wta.inputPath + "heights.png"));
            var roughness = new FieldFromBitmap(new Bitmap(wta.inputPath + "roughness.png"));
            var msmArgs = new MeterScaleMap.Args(waters, heights, roughness, null);
            msmArgs.seed = System.DateTime.UtcNow.Ticks;
            msmArgs.metersPerPixel = 800;
            msmArgs.riverCapacityToMetersWideFunc = c => (float)Math.Pow(msmArgs.metersPerPixel * SplineTree.CAPACITY_DIVISOR * c, 0.5f) / 4f;
            msmArgs.baseHeightMaxInMeters = 500;
            msmArgs.valleyStrength = 0.98f;
            var msm = new MeterScaleMap(msmArgs);
            
            msm.OutputHighLevelMaps(new Bitmap(waters.Width, waters.Height), "C:\\Users\\Justin Murray\\Desktop\\egwethoon\\");
            msm.OutputMapGrid(100, "C:\\Users\\Justin Murray\\Desktop\\egwethoon\\", "submap", 32);
            TestScenarios.RunStreamedMapCombinerScenario();*/


            // So, if I change the scale of the map, it becomes visibly grainy and noisy.  Blurring (even by a tiny radius) removes the noise, 
            // but even then it's clear that much of the sophistication has been lost.  Traverse the logic of the erosion algorithm, understand
            // exactly what it is that causes things to degrade when the world scales up, then come up with a strategy to fix it.
            // ANSWER: It's all down to pGravity!  Just scale that by the inverse of the max height and everything's hunky-dory!
            var heightmap = new ScaleTransform(new FieldFromBitmap(new Bitmap("C:\\Users\\Justin Murray\\Desktop\\input.png")), 2500);

            var eroded0 = Erosion.DropletHydraulic(heightmap, heightmap.Width * heightmap.Height, 100, maxHeight: 2500);
            OutputField(new NormalizedComposition2d<float>(eroded0), new Bitmap(eroded0.Width, eroded0.Height), "C:\\Users\\Justin Murray\\Desktop\\output0.png");

            var eroded1 = Erosion.DropletHydraulic(eroded0, heightmap.Width * heightmap.Height, 100, maxHeight: 2500);
            OutputField(new NormalizedComposition2d<float>(eroded1), new Bitmap(eroded1.Width, eroded1.Height), "C:\\Users\\Justin Murray\\Desktop\\output1.png");

            var eroded2 = Erosion.DropletHydraulic(eroded1, heightmap.Width * heightmap.Height, 100, maxHeight: 2500);
            OutputField(new NormalizedComposition2d<float>(eroded2), new Bitmap(eroded2.Width, eroded2.Height), "C:\\Users\\Justin Murray\\Desktop\\output2.png");

            var eroded3 = Erosion.DropletHydraulic(eroded2, heightmap.Width * heightmap.Height, 100, maxHeight: 2500);
            OutputField(new NormalizedComposition2d<float>(eroded3), new Bitmap(eroded3.Width, eroded3.Height), "C:\\Users\\Justin Murray\\Desktop\\output3.png");

            var eroded4 = Erosion.DropletHydraulic(eroded3, heightmap.Width * heightmap.Height, 100, maxHeight: 2500);
            OutputField(new NormalizedComposition2d<float>(eroded4), new Bitmap(eroded4.Width, eroded4.Height), "C:\\Users\\Justin Murray\\Desktop\\output4.png");
        }
    }
}

using DemiurgeLib;
using DemiurgeLib.Common;
using DemiurgeLib.Noise;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DemiurgeConsole.Utils;

namespace DemiurgeConsole
{
    class MeterScaleMap
    {
        private WaterTableField wtf;
        private IField2d<List<SplineTree>> splines;
        private ContinuousMountainNoise mountainNoise;

        /// <summary>
        /// The following types of direct input are implemented:
        ///   - watersDrawing: a sketch of oceans and other major waters, where land is white and water is black.
        ///   - heightsDrawing: a sketch indicating the approximate regional altitudes, where light is high and dark is low.
        ///   - rainDrawing: a sketch indicating the approximate rainfall by region, where light is wet and dark is dry.
        /// The following types of direct input are posited:
        ///   - hillDrawing: a sketch indicating how rugged the terrain is by area, where light is rough and dark is smooth.
        ///   - valleyDrawing: a sketch indicating the typical shape of valleys (format undecided)
        ///   - volcanoDrawing: a sketch indicating the location of stratovolcanos, where any color other than black or white indicates a volcano.
        /// </summary>
        public void CreateMeterScaleMap(IField2d<float> watersDrawing, IField2d<float> heightsDrawing, IField2d<float> rainDrawing)
        {

        }

        /// <summary>
        /// These mountains generally want to be around 2048 meters in prominence, or
        /// a little shorter.  Too much shorter and they'll be pretty shallow (and at
        /// that point should probably be replaced with hills), too much taller and
        /// they'll be VERY steep indeed.
        /// </summary>
        private static float GetMountainNoiseStartingScale(float metersPerPixel)
        {
            return (float)(0.00015625 * metersPerPixel);
        }
    }
}

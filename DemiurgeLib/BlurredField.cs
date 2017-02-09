using System;
using System.Collections.Generic;

namespace DemiurgeLib.Common
{
    /// <summary>
    /// A linearized and highly optimized rendition of Gauss Blur.
    /// Based on learnings decrypted from http://blog.ivank.net/fastest-gaussian-blur.html
    /// </summary>
    public class BlurredField : Field2d<float>
    {
        public BlurredField(IField2d<float> field, float radius = 10) : base(field.Width, field.Height)
        {
            GaussBlur(field, this, radius);
        }

        public static void GaussBlur(IField2d<float> source, Field2d<float> target, float radius, int n = 3)
        {
            var boxSizes = BoxRadiiForGaussBlur(radius, n);
            Field2d<float> buffer = new Field2d<float>(source);

            RecursivelyBoxBlur(buffer, target, boxSizes);

            // If n was an even number, then the final blurring wound up
            // in the buffer; replicate it to the target.
            if (n % 2 == 0)
            {
                target.Replicate(buffer);
            }
        }

        private static void RecursivelyBoxBlur(Field2d<float> source, Field2d<float> target, Queue<int> boxSizes)
        {
            if (boxSizes.Count == 0)
            {
                return;
            }
            else
            {
                BoxBlur(source, target, (boxSizes.Dequeue() - 1) / 2);
                RecursivelyBoxBlur(target, source, boxSizes);
            }
        }

        private static void BoxBlur(Field2d<float> source, Field2d<float> target, int radius)
        {
            target.Replicate(source);

            BoxBlurHorizontal(target, source, radius);
            BoxBlurVertical(source, target, radius);
        }

        private static void BoxBlurHorizontal(Field2d<float> source, Field2d<float> target, int radius)
        {
            // One over the number of values contained in the accumulator, used to 
            // compute "rolling average."
            float accNormalizationFactor = 1f / (radius + radius + 1f);

            for (int y = 0; y < source.Height; y++)
            {
                int xTarget = 0;
                int xFirstSrc = xTarget;
                int xSecondSrc = xTarget + radius;

                float firstValue = source[y, 0];
                float lastValue = source[y, source.Width - 1];

                // Seed the accumulator with "radius" + 1 times the leftmost value.
                // This simulates having that value extend infinitely off to the left.
                float acc = (radius + 1f) * firstValue;

                // For the first "radius" values in the row, accumulate the values.
                // No actual blurring can take place until the accumulator has reached
                // the rightmost edge of the relevant box.
                for (var x = 0; x < radius; x++)
                {
                    acc += source[y, x];
                }

                // Begin actual blurring by updating the accumulator, then using the
                // "averaged out" value.  In this way, the accumulator generates a 
                // "rolling average."  This loop removes the "firstValue" values 
                // artificially added when the accumulator was initialized.
                for (var x = 0; x <= radius; x++)
                {
                    acc += source[y, xSecondSrc++] - firstValue;
                    target[y, xTarget++] = acc * accNormalizationFactor;
                }

                // Continue actual blurring through the center of the image.  This
                // loop commences once the accumulator has been purged of artificial
                // values, and so uses real blurring throughout.
                for (var x = radius + 1; x < source.Width - radius; x++)
                {
                    acc += source[y, xSecondSrc++] - source[y, xFirstSrc++];
                    target[y, xTarget++] = acc * accNormalizationFactor;
                }

                // Finish the actual blurring as the accumulator advances off the 
                // right-hand edge of the image.  Use the right-most value as the
                // artificial value to keep the accumulator working properly.
                for (var x = source.Width - radius; x < source.Width; x++)
                {
                    acc += lastValue - source[y, xFirstSrc++];
                    target[y, xTarget++] = acc * accNormalizationFactor;
                }
            }
        }

        private static void BoxBlurVertical(Field2d<float> source, Field2d<float> target, int radius)
        {
            // See BoxBlurHorizontal for detailed commentary.  The process here is 
            // identical, just in the Y axis instead of the X.
            float accNormalizationFactor = 1f / (radius + radius + 1f);

            for (var x = 0; x < source.Width; x++)
            {
                int yTarget = 0;
                int yFirstSrc = yTarget;
                int ySecondSrc = yTarget + radius;

                float firstValue = source[0, yTarget];
                float lastValue = source[source.Height - 1, yTarget];
                float acc = (radius + 1f) * firstValue;

                for (var y = 0; y < radius; y++)
                {
                    acc += source[y, x];
                }

                for (var y = 0; y <= radius; y++)
                {
                    acc += source[ySecondSrc++, x] - firstValue;
                    target[yTarget++, x] = acc * accNormalizationFactor;
                }

                for (var y = radius + 1; y < source.Height - radius; y++)
                {
                    acc += source[ySecondSrc++, x] - source[yFirstSrc++, x];
                    target[yTarget++, x] = acc * accNormalizationFactor;
                }

                for (var y = source.Height - radius; y < source.Height; y++)
                {
                    acc += lastValue - source[yFirstSrc++, x];
                    target[yTarget++, x] = acc * accNormalizationFactor;
                }
            }
        }

        /// <summary>
        /// In the referenced website, this method was provided apropos of nothing, 
        /// referenced only with a dead link to a forgotten website, and with no 
        /// explanation for the reasoning behind it except "calculus."  Consider it
        /// a TODO to figure out WHY this does what it does; but it's a Pri3 at best.
        /// </summary>
        /// <param name="sigma">Standard deviation of desired Gaussian</param>
        /// <param name="n">Number of boxes</param>
        /// <returns></returns>
        private static Queue<int> BoxRadiiForGaussBlur(float sigma, int n)
        {
            // Ideal averaging filter width
            float widthIdeal = (float)Math.Sqrt(12f * sigma * sigma / n + 1);

            int widthLow = (int)Math.Floor(widthIdeal);
            if (widthLow % 2 == 0)
                widthLow--;

            int widthHigh = widthLow + 2;

            float mIdeal = (12f * sigma * sigma - n * widthLow * widthLow - 4 * n * widthLow - 3 * n) / (-4 * widthLow - 4);

            int m = (int)Math.Round(mIdeal);

            var sizes = new Queue<int>();

            for (var i = 0; i < n; i++)
            {
                sizes.Enqueue(i < m ? widthLow : widthHigh);
            }

            return sizes;
        }
    }
}

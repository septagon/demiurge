using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemiurgeLib.NoiseAlgorithms
{
    /// <summary>
    /// Legacy implementation, copied from prior project.
    /// TODO: Clean this up, possibly rewrite completely.
    /// </summary>
    class DiamondSquareNoise
    {
        public class DiamondSquareArguments
        {
            public int width;
            public int height;
            public double roughness = 1;
            public Func<double, double> decayFunction = null;
            public Random random = null;
        }

        public static double[,] GenerateNoise(DiamondSquareArguments args)
        {
            Func<double, double> decayFunction = args.decayFunction ?? new Func<double, double>(n => n / 2);
            Random random = args.random ?? new Random();
            double roughness = args.roughness;

            int superSize = Math.Max(NonSmallerPowerOfTwo(args.width), NonSmallerPowerOfTwo(args.height));

            double[,] heightmap = new double[superSize, superSize];

            heightmap[0, 0] = args.roughness * random.NextDouble();
            heightmap[superSize - 1, 0] = roughness * random.NextDouble();
            heightmap[0, superSize - 1] = roughness * random.NextDouble();
            heightmap[superSize - 1, superSize - 1] = roughness * random.NextDouble();

            for (int stepSize = superSize / 2; stepSize > 0; stepSize /= 2)
            {
                Diamond(ref heightmap, stepSize, roughness, random);
                Square(ref heightmap, stepSize, roughness, random);

                roughness = decayFunction(roughness);
            }

            double[,] sizedMap = new double[args.height, args.width];

            for (int j = 0; j < args.height; j++)
            {
                for (int i = 0; i < args.width; i++)
                {
                    sizedMap[j, i] = heightmap[j, i];
                }
            }

            double min = 0, max = 1;

            for (int j = 0; j < args.height; j++)
            {
                for (int i = 0; i < args.width; i++)
                {
                    min = Math.Min(min, sizedMap[j, i]);
                    max = Math.Max(max, sizedMap[j, i]);
                }
            }

            // max stores range now
            max -= min;

            for (int j = 0; j < args.height; j++)
            {
                for (int i = 0; i < args.width; i++)
                {
                    sizedMap[j, i] = (sizedMap[j, i] - min) / max;
                }
            }

            return sizedMap;
        }

        private static void Diamond(ref double[,] heightmap, int stepSize, double volatility, Random random)
        {
            if (stepSize <= 0)
            {
                return;
            }

            for (int j = stepSize; j < heightmap.GetLength(0); j += stepSize * 2)
            {
                for (int i = stepSize; i < heightmap.GetLength(1); i += stepSize * 2)
                {
                    double num = 0f;
                    double denom = 0f;

                    AddPointIfInBounds(heightmap, i - stepSize, j - stepSize, ref num, ref denom);
                    AddPointIfInBounds(heightmap, i + stepSize, j - stepSize, ref num, ref denom);
                    AddPointIfInBounds(heightmap, i - stepSize, j + stepSize, ref num, ref denom);
                    AddPointIfInBounds(heightmap, i + stepSize, j + stepSize, ref num, ref denom);

                    heightmap[j, i] = volatility * ((float)random.NextDouble() - 0.5f) + num / denom;
                }
            }
        }

        private static void Square(ref double[,] heightmap, int stepSize, double volatility, Random random)
        {
            for (int it = 0, j; (j = it * stepSize) < heightmap.GetLength(0); it++)
            {
                for (int i = ((it + 1) % 2) * stepSize; i < heightmap.GetLength(1); i += stepSize * 2)
                {
                    double num = 0f;
                    double denom = 0f;

                    AddPointIfInBounds(heightmap, i, j - stepSize, ref num, ref denom);
                    AddPointIfInBounds(heightmap, i - stepSize, j, ref num, ref denom);
                    AddPointIfInBounds(heightmap, i + stepSize, j, ref num, ref denom);
                    AddPointIfInBounds(heightmap, i, j + stepSize, ref num, ref denom);

                    heightmap[j, i] = volatility * ((float)random.NextDouble() - 0.5f) + num / denom;
                }
            }
        }

        private static void AddPointIfInBounds(double[,] heightmap, int x, int y, ref double sum, ref double count)
        {
            double width = heightmap.GetLength(1);
            double height = heightmap.GetLength(0);

            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                sum += heightmap[y, x];
                count += 1f;
            }
        }

        public static int NonSmallerPowerOfTwo(int i)
        {
            double logTwo = Math.Log(i, 2);

            // If already a power of 2, no additional work required.
            if (logTwo % 1 == 0)
            {
                return i;
            }

            return (int)Math.Pow(2, Math.Floor(logTwo) + 1);
        }
    }
}

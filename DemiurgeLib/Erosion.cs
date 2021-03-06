﻿using DemiurgeLib.Common;
using System;

namespace DemiurgeLib
{
    public static class Erosion
    {
        private struct Droplet
        {
            public vFloat Position;
            public vFloat Direction;
            public float Speed;
            public float Water;
            public float Sediment;
        }

        // TODO: Consider a more sophisticated gradient function, as it recommends in the paper.
        // But for now, I can't imagine subpixel accuracy is going to make a whole lot of difference.
        private static vFloat GradientAtPoint(this IField2d<float> field, vFloat point)
        {
            int pX = (int)point[0];
            int pY = (int)point[1];
            float x, y;

            if (pX % 2 == 1 || pX == field.Width - 1)
            {
                x = field[pY, pX - 1] - field[pY, pX];
            }
            else
            {
                x = field[pY, pX] - field[pY, pX + 1];
            }

            if (pY % 2 == 1 || pY == field.Height - 1)
            {
                y = field[pY - 1, pX] - field[pY, pX];
            }
            else
            {
                y = field[pY, pX] - field[pY + 1, pX];
            }

            return new vFloat(x, y);
        }

        private static IField2d<float> GetKernel(int radius)
        {
            var seed = new SparseField2d<float>(2 * radius + 1, 2 * radius + 1, 0f);
            seed[radius, radius] = 1f;

            // Because "radius" to the blurred field roughly equates to "standard deviation" while
            // it means "bounding dimension" in the context of the kernel, we halve the kernel in 
            // in order to (very roughly) convert between the paradigms.
            var blurred = new BlurredField(seed, radius / 2);
            
            // Normalize.
            float sum = 0f;
            for (int y = 0; y < blurred.Height; y++)
                for (int x = 0; x < blurred.Width; x++)
                    sum += blurred[y, x];

            // If I return "seed" here, then it operates perfectly normally.  There's some
            // kind of strange oscillation case that results from the area-based nature of
            // using a larger kernel.  But what?
            return new ScaleTransform(blurred, 1f / sum);
        }

        // TODO: Don't allow the field to erode below zero.
        // TODO: Here, at last, is the source of the insane bug.  The way this bug works is easiest to understand
        // if one envisions a kernel of small radius and a "trough" consisting of two elevated walls and a lower 
        // middle.  The nature of a trough is that a particle can become "trapped" in one, rolling back and forth
        // as each wall turns the particle back.  If a trough is narrow enough that a particle dropping sediment in
        // the middle will also drop sediment on both walls by virtue of its kernel, then oscillation in the trough
        // can cause the particle to "build towers."  This happens because when the particle is picking up sediment--
        // i.e., when it's beginning to go down-hill--its kernel covers part of the trough and a little bit outside;
        // however, when the particle is depositing sediment--i.e., when it's beginning to go uphill--its kernel is
        // entirely over the trough.  By repeated action, this allows the particle to "dig" sediment from just outside
        // the walls of its trough and bring that sediment back into the trough, creating the extremely 
        // characteristic "bars" of high and low elevation in extremely close proximity.  The solution to this, 
        // presumably, is to prevent this "digging" behavior, presumably by taking sediment in a more cautious 
        // manner that won't allow erosion computed from a high place to induce the removal of sediment from a low
        // place.
        private static void PickUpSedimentFromKernel(Field2d<float> field, IField2d<float> kernel, int centerX, int centerY, float targetSediment)
        {
            if (targetSediment == 0)
                return;

            float targetMin = field[centerY, centerX] - kernel[kernel.Height / 2, kernel.Width / 2] * targetSediment;

            float collected = 0f;
            for (int y = 0; y < kernel.Height; y++)
            {
                for (int x = 0; x < kernel.Width; x++)
                {
                    int cX = centerX + x - kernel.Width / 2;
                    int cY = centerY + y - kernel.Height / 2;

                    if (cX >= 0 && cY >= 0 && cX < field.Width && cY < field.Height && field[cY, cX] >= targetMin)
                    {
                        collected += targetSediment * kernel[y, x];
                    }
                }
            }

            float scalar = targetSediment / collected;
            for (int y = 0; y < kernel.Height; y++)
            {
                for (int x = 0; x < kernel.Width; x++)
                {
                    int cX = centerX + x - kernel.Width / 2;
                    int cY = centerY + y - kernel.Height / 2;
            
                    if (cX >= 0 && cY >= 0 && cX < field.Width && cY < field.Height && field[cY, cX] >= targetMin)
                    {
                        
                        field[cY, cX] -= targetSediment * kernel[y, x] * scalar;
                    }
                }
            }
        }

        private static void DropSedimentFromKernel(Field2d<float> field, IField2d<float> kernel, int centerX, int centerY, float targetSediment)
        {
            for (int y = 0; y < kernel.Height; y++)
            {
                for (int x = 0; x < kernel.Width; x++)
                {
                    int cX = centerX + x - kernel.Width / 2;
                    int cY = centerY + y - kernel.Height / 2;
            
                    if (cX >= 0 && cY >= 0 && cX < field.Width && cY < field.Height)
                    {
                        field[cY, cX] += targetSediment * kernel[y, x];
                    }
                }
            }
        }

        // Based on the approach by Hans Theobald Beyer, "Implementation of a method for hydraulic erosion," 2015
        public static Field2d<float> DropletHydraulic(IField2d<float> inputHeightmap, int numDroplets, int iterationsPerDrop, float minSlope = 0f, float maxHeight = 1f, int radius = 0)
        {
            Random random = new Random();
            float pFriction = 0.3f;
            float pCapacity = 1f;
            float pErode = 0.3f;
            float pDeposit = 0.3f;
            float pGravity = 0.8f / maxHeight;
            float pEvaporate = 0.01f;

            const int STARTING_DIRECTION_GRANULARITY = 32;

            var kernel = GetKernel(radius);

            Field2d<float> heightmap = new Field2d<float>(inputHeightmap);
            
            for (int idx = 0; idx < numDroplets; idx++)
            {
                Droplet droplet = new Droplet()
                {
                    Position = new vFloat(random.Next(heightmap.Width), random.Next(heightmap.Height)),
                    Direction = new vFloat(random.Next(STARTING_DIRECTION_GRANULARITY) - STARTING_DIRECTION_GRANULARITY / 2,
                                            random.Next(STARTING_DIRECTION_GRANULARITY) - STARTING_DIRECTION_GRANULARITY / 2).norm(),
                    Speed = 0,
                    Water = 1,
                    Sediment = 0,
                };

                for (int iteration = 0; iteration < iterationsPerDrop; iteration++)
                {
                    (int x, int y) oldPos = ((int)droplet.Position[0], (int)droplet.Position[1]);
                    float oldHeight = heightmap[oldPos.y, oldPos.x];

                    var gradient = heightmap.GradientAtPoint(droplet.Position);
                    droplet.Direction = (droplet.Direction + gradient) * (1 - pFriction);

                    if (droplet.Direction.magSq() > 0)
                    {
                        droplet.Position += droplet.Direction.norm();
                    }

                    if (droplet.Position[0] < 0f || droplet.Position[1] < 0f ||
                        droplet.Position[0] >= heightmap.Width || droplet.Position[1] >= heightmap.Height)
                    {
                        break;
                    }

                    (int x, int y) newPos = ((int)droplet.Position[0], (int)droplet.Position[1]);
                    float newHeight = heightmap[newPos.y, newPos.x];

                    if (newHeight > oldHeight)
                    {
                        float droppedSediment = Math.Min(newHeight - oldHeight, droplet.Sediment);

                        DropSedimentFromKernel(heightmap, kernel, oldPos.x, oldPos.y, droppedSediment);
                        droplet.Sediment -= droppedSediment;
                    }
                    else if (newHeight < oldHeight)
                    {
                        float capacity = Math.Max(oldHeight - newHeight, minSlope) * droplet.Speed * droplet.Water * pCapacity;
                        
                        if (droplet.Sediment > capacity)
                        {
                            float droppedSediment = (droplet.Sediment - capacity) * pDeposit;

                            DropSedimentFromKernel(heightmap, kernel, oldPos.x, oldPos.y, droppedSediment);
                            droplet.Sediment -= droppedSediment;
                        }
                        else
                        {
                            float pickedUpSediment = Math.Min((capacity - droplet.Sediment) * pErode, oldHeight - newHeight);

                            PickUpSedimentFromKernel(heightmap, kernel, oldPos.x, oldPos.y, pickedUpSediment);
                            droplet.Sediment += pickedUpSediment;
                        }
                    }

                    // This is from the paper, but it's super weird.  So, the drops will pick up speed even if they go uphill?
                    // I think speed is the wrong term for this variable.  In fact, this whole concept is very magical.  Speed should
                    // be determined by the magnitude of the velocity, not by some random accumulator.  On the other hand, I tried 
                    // that, and this works way better.  So...
                    droplet.Speed = (float)Math.Sqrt(droplet.Speed * droplet.Speed + Math.Abs(newHeight - oldHeight) * pGravity);
                    droplet.Water = droplet.Water * (1 - pEvaporate);
                }
            }

            return heightmap;
        }

        private struct Pixel
        {
            public (int x, int y) Position;
            //public float Energy;  // TODO: Jump about if we have too much energy?  Or store more sediment?
            public float Water;
            public float Sediment;
        }
        /// <summary>
        /// So, the idea is to just descend, one pixel at a time, until we run out of water.
        /// </summary>
        /// <param name="originalHeightmap"></param>
        /// <returns></returns>
        public static Field2d<float> PixelNoiseErosion(IField2d<float> originalHeightmap)
        {
            Random random = new Random();
            int iterationsCount = originalHeightmap.Width * originalHeightmap.Height * 2;
            float epsilon = 0.001f;
            float evaporation = 0.1f;
            int radius = 0;
            float waterToCapacity = 100f;
            float erosion = 0.1f;
            float deposition = 0.1f;

            var heightmap = new Field2d<float>(originalHeightmap);

            var kernel = GetKernel(radius);

            for (int iteration = 0; iteration < iterationsCount; iteration++)
            {
                var pixel = new Pixel()
                {
                    Position = (random.Next(heightmap.Width), random.Next(heightmap.Height)),
                    Water = 1f,
                    Sediment = 0f
                };

                while (pixel.Water > epsilon)
                {
                    float oldHeight = heightmap[pixel.Position.y, pixel.Position.x];

                    (int x, int y) newPos = pixel.Position;
                    for (int j = -1; j <= 1; j++)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            int x = pixel.Position.x + i;
                            int y = pixel.Position.y + j;

                            if (x >= 0 && x < heightmap.Width && y >= 0 && y < heightmap.Width &&
                                heightmap[y, x] < heightmap[newPos.y, newPos.x])
                            {
                                newPos.x = x;
                                newPos.y = y;
                            }
                        }
                    }

                    float newHeight = heightmap[newPos.y, newPos.x];

                    // Grab from the old.
                    float erodedSediment = (oldHeight - newHeight) * erosion;
                    PickUpSedimentFromKernel(heightmap, kernel, pixel.Position.x, pixel.Position.y, erodedSediment);
                    pixel.Sediment += erodedSediment;

                    // Drop on the new.
                    {
                        float capacity = pixel.Water * waterToCapacity;
                        float depositedSediment = Math.Max(pixel.Sediment - capacity, 0f) * deposition;

                        DropSedimentFromKernel(heightmap, kernel, newPos.x, newPos.y, depositedSediment);
                        pixel.Sediment -= depositedSediment;
                    }

                    if (float.IsNaN(heightmap[newPos.y, newPos.x]))
                    {
                        heightmap[newPos.y, newPos.x] = 0f;
                    }

                    pixel.Position = newPos;
                    pixel.Water *= (1f - evaporation);
                }
            }

            return heightmap;
        }
    }
}

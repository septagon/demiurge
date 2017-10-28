using DemiurgeLib.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            float x, y;

            if ((int)point[0] % 2 == 1)
            {
                x = field[(int)point[1], (int)point[0] - 1] - field[(int)point[1], (int)point[0]];
            }
            else
            {
                x = field[(int)point[1], (int)point[0]] - field[(int)point[1], (int)point[0] + 1];
            }

            if ((int)point[1] % 2 == 1)
            {
                y = field[(int)point[1] - 1, (int)point[0]] - field[(int)point[1], (int)point[0]];
            }
            else
            {
                y = field[(int)point[1], (int)point[0]] - field[(int)point[1] + 1, (int)point[0]];
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

        private static void PickUpSedimentFromKernel(Field2d<float> field, IField2d<float> kernel, int centerX, int centerY, float targetSediment)
        {
            for (int y = 0; y < kernel.Height; y++)
            {
                for (int x = 0; x < kernel.Width; x++)
                {
                    int cX = centerX + x - kernel.Width / 2;
                    int cY = centerY + y - kernel.Height / 2;

                    if (cX >= 0 && cY >= 0 && cX < field.Width && cY < field.Height)
                    {
                        field[cY, cX] -= Math.Min(field[cY, cX], targetSediment * kernel[y, x]);
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
    }
}

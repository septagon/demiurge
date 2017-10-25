using System;
using System.Collections.Generic;
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

        private static vFloat GradientAtPoint(this IField2d<float> field, vFloat point)
        {
            float x, y;

            if ((int)point[0] >= field.Width - 1)
            {
                x = field[(int)point[1], (int)point[0] - 1] - field[(int)point[1], (int)point[0]];
            }
            else
            {
                x = field[(int)point[1], (int)point[0]] - field[(int)point[1], (int)point[0] + 1];
            }

            if (point[1] >= field.Height - 1)
            {
                y = field[(int)point[1] - 1, (int)point[0]] - field[(int)point[1], (int)point[0]];
            }
            else
            {
                y = field[(int)point[1], (int)point[0]] - field[(int)point[1] + 1, (int)point[0]];
            }

            return new vFloat(x, y);
        }

        // Based on the approach by Hans Theobald Beyer, "Implementation of a method for hydraulic erosion," 2015
        public static Field2d<float> DropletHydraulic(IField2d<float> inputHeightmap, int iterationsPerDrop, float minSlope = 0f)
        {
            float pInertia = 0.5f;
            float pCapacity = 1f;
            float pErode = 0.3f;
            float pDeposit = 0.3f;
            float pGravity = 0.8f;
            float pEvaporate = 0.01f;

            Field2d<float> heightmap = new Field2d<float>(inputHeightmap);

            for (int j = 0; j < heightmap.Height; j++)
            {
                for (int i = 0; i < heightmap.Width; i++)
                {
                    Droplet droplet = new Droplet()
                    {
                        Position = new vFloat(i, j),
                        Direction = new vFloat(0, 0),
                        Speed = 0,
                        Water = 1,
                        Sediment = 0,
                    };

                    for (int iteration = 0; iteration < iterationsPerDrop; iteration++)
                    {
                        if (i == 127 && j == 127 && iteration == 24)
                        {
                            iteration = 24;
                        }

                        (int x, int y) oldPos = ((int)droplet.Position[0], (int)droplet.Position[1]);
                        float oldHeight = heightmap[oldPos.y, oldPos.x];

                        var gradient = heightmap.GradientAtPoint(droplet.Position);
                        if (gradient.magSq() > 0)
                        {
                            droplet.Direction = (pInertia * droplet.Direction + (1f - pInertia) * gradient.norm()).norm();
                            droplet.Position += droplet.Direction;
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

                            // TODO: Pick up or drop sediment over a region, not a single point.
                            heightmap[oldPos.y, oldPos.x] += droppedSediment;
                            droplet.Sediment -= droppedSediment;
                        }
                        else
                        {
                            float capacity = Math.Max(oldHeight - newHeight, minSlope) * droplet.Speed * droplet.Water * pCapacity;

                            if (droplet.Sediment > capacity)
                            {
                                float droppedSediment = (droplet.Sediment - capacity) * pDeposit;

                                // TODO: Pick up or drop sediment over a region, not a single point.
                                heightmap[oldPos.y, oldPos.x] += droppedSediment;
                                droplet.Sediment -= droppedSediment;
                            }
                            else
                            {
                                float pickedUpSediment = (droplet.Sediment - capacity) * pErode;

                                // TODO: Pick up or drop sediment over a region, not a single point.
                                heightmap[oldPos.y, oldPos.x] -= pickedUpSediment;
                                droplet.Sediment += pickedUpSediment;
                            }
                        }

                        // This is from the paper, but it's super weird.  So, the drops will pick up speed even if they go uphill?  I think speed is the wrong term for this variable.
                        droplet.Speed = (float)Math.Sqrt(droplet.Speed * droplet.Speed + Math.Abs(newHeight - oldHeight) * pGravity);
                        droplet.Water = droplet.Water * (1 - pEvaporate);
                    }
                }
            }

            return heightmap;
        }
    }
}

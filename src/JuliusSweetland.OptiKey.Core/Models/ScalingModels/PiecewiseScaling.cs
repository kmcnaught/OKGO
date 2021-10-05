using JuliusSweetland.OptiKey.Properties;
using JuliusSweetland.OptiKey.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace JuliusSweetland.OptiKey.Models.ScalingModels
{
    class PiecewiseScaling : ISensitivityFunction
    {
        List<Tuple<Tuple<float, float>, Tuple<float, float>>> pairwiseCoords;
        List<Tuple<float, float>> individualCoords;

        double screenScale;
        double screenMax;

        public PiecewiseScaling()
        {
            // To make sure things don't get stretched, we scale to the smaller axis
            // which makes things spherical in the centre, and extend beyond 1 outside        
            screenScale = Math.Min(Graphics.PrimaryScreenHeightInPixels, Graphics.PrimaryScreenWidthInPixels)/2;
            screenMax = (Math.Max(Graphics.PrimaryScreenHeightInPixels, Graphics.PrimaryScreenWidthInPixels) / 2) / screenScale;
        }

        public void SetCoords(float[] list_of_all_coords)
        {
            // Parse list of coords: x1, y1, x2, y2, ...
            // Apply appropriate end conditions and 
            // create pairwise coords for each segment:
            // < (x1, y1), (x2, y2) > , < (x2, y2), (x3, y3) > , ...

            int n = list_of_all_coords.Length / 2; // number of pairs
            if (n > 0)
            {
                individualCoords = new List<Tuple<float, float>>();

                if (list_of_all_coords[0] > 0)
                {
                    // manually insert (0,0) so we get full coverage
                    individualCoords.Add(Tuple.Create(0.0f, 0.0f));
                }
                for (int i = 0; i < n; i++)
                {
                    Tuple<float, float> coord = Tuple.Create(list_of_all_coords[i * 2], list_of_all_coords[i * 2 + 1]);
                    individualCoords.Add(coord);
                }

                if (individualCoords.Last().Item1 < screenMax)
                {
                    // manually extend last value to the end of the range
                    // we extend to 2.0f to cover full aspect ratio of screen
                    individualCoords.Add(Tuple.Create((float)screenMax, individualCoords.Last().Item2));
                }

                pairwiseCoords = individualCoords.Zip(individualCoords.Skip(1), (a, b) => Tuple.Create(a, b)).ToList();
            }
        }

        public List<Point> GetContours()
        {
            List<Point> ctrs = new List<Point>();
            float lastVal = 0;
            float lastX = 0;
            foreach (var coord in individualCoords)
            {
                float currX = coord.Item1;
                float currVal = coord.Item2;

                if (lastVal == 0 && currVal > 0)
                {
                    // previous contour was zero-crossing
                    ctrs.Add(new Point(screenScale * lastX, screenScale * lastX));
                }
                else if (lastVal > 0 && currVal == 0)
                {
                    // new contour is zero-crossing
                    ctrs.Add(new Point(screenScale * currX, screenScale * currX));
                }
                lastVal = currVal;
                lastX = currX;
            }

            return ctrs;
        }


        private float map_val(float f)
        {
            foreach (var pair in pairwiseCoords)
            {
                if (f >= pair.Item1.Item1 && f <= pair.Item2.Item1) // are we in this segment?
                {
                    float interp_x = (f - pair.Item1.Item1) / (pair.Item2.Item1 - pair.Item1.Item1);
                    float interp_y = pair.Item1.Item2 + interp_x * (pair.Item2.Item2 - pair.Item1.Item2);
                    return interp_y;
                }
            }
            return 0.0f;
        }

        public Vector CalculateScaling(Point current, Point centre)
        {
            // Calculate the direction and distance from the centre to the current value. 
            Vector distance = current - centre;

            // Distances proportional to screen
            // map to [0, 1] in smaller axis and [0, aspectRatio(>1)] in longer axis
            distance.X /= screenScale;
            distance.Y /= screenScale;

            // Compute scale 
            var amount = map_val((float)distance.Length);
            var theta = Math.Atan2(distance.Y, distance.X);

            // Map back to x, y
            return new Vector(amount * Math.Cos((double)theta), amount * Math.Sin((double)theta));
        }

        private double signedSqrt(double x)
        {
            // return sqrt of magnitude but with original sign
            return Math.Sign(x) * Math.Sqrt(Math.Abs(x));
        }
    }
}

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
    class SegmentScaling : ISensitivityFunction
    {
        List<Tuple<Tuple<float, float>, Tuple<float, float>>> pairwiseCoords;
        List<Tuple<float, float>> individualCoords;

        float innerRadius;
        float outerRadius;
        double scale;
        int nSegments; // expected to be 4 or 8, but other n would work

        double screenScale;

        public SegmentScaling(int n, float innerRadius, float outerRadius, double scale=1.0)
        {
            this.nSegments = n;
            this.innerRadius = innerRadius;
            this.outerRadius = outerRadius;
            this.scale = scale;

            // To make sure things don't get stretched, we scale to the smaller axis
            // which makes things spherical in the centre, and extend beyond 1 outside        
            screenScale = Math.Min(Graphics.PrimaryScreenHeightInPixels, Graphics.PrimaryScreenWidthInPixels)/2;
        }

        public List<Region> GetContours()
        {
            return new List<Region>()
            {
                new Region(.5 - innerRadius, .5 - innerRadius * Graphics.PrimaryScreenWidthInPixels / Graphics.PrimaryScreenHeightInPixels, 2 * innerRadius, 2 * innerRadius * Graphics.PrimaryScreenWidthInPixels / Graphics.PrimaryScreenHeightInPixels, 2 * innerRadius),
                new Region(.5 - outerRadius, .5 - outerRadius * Graphics.PrimaryScreenWidthInPixels / Graphics.PrimaryScreenHeightInPixels, 2 * outerRadius, 2 * outerRadius * Graphics.PrimaryScreenWidthInPixels / Graphics.PrimaryScreenHeightInPixels, 2 * outerRadius)
            };
        }
        

        public Vector CalculateScaling(Point current, Point centre)
        {
            // Calculate the direction and distance from the centre to the current value. 
            Vector distance = current - centre;

            // Distances proportional to screen
            // map to [0, 1] in smaller axis and [0, aspectRatio(>1)] in longer axis
            distance.X /= screenScale;
            distance.Y /= screenScale;

            if (distance.Length < innerRadius ||
                distance.Length > outerRadius)
            {
                return new Vector(0.0, 0.0);
            }

            // Snap theta into distinct segments
            var theta = Math.Atan2(distance.Y, distance.X);

            double theta_snap = theta;
            theta_snap *= (nSegments / 2) / Math.PI;
            theta_snap = Math.Round(theta_snap);
            theta_snap /= (nSegments / 2) / Math.PI;            

            // Map back to x, y
            return new Vector(scale * Math.Cos(theta_snap), scale * Math.Sin(theta_snap));
        }

        public bool Contains(Point point) { return true; }
    }
}

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
    class SqrtScalingFromSettings : ISensitivityFunction
    {
        private double baseSpeed;
        private double acceleration;
        private float scaleX = 1.0f;
        private float scaleY = 1.0f;

        public SqrtScalingFromSettings(double baseSpeed, double acceleration) {
            this.baseSpeed = baseSpeed;
            this.acceleration = acceleration;
        }


        public void SetScaleFactor(float[] scaleXY)
        {
            scaleX = scaleXY[0];
            scaleY = scaleXY[1];
        }

        public Vector CalculateScaling(Point current, Point centre)
        {
            double baseSpeed = 0;
            double acceleration = 0.02;

            double deadzoneWidth = (double)Settings.Default.JoystickHorizontalDeadzonePercentScreen * Graphics.PrimaryScreenWidthInPixels / 100.0d;
            double deadzoneHeight = deadzoneWidth / Settings.Default.JoystickDeadzoneAspectRatio;

            Vector velocity = CalculateLookToScrollVelocityVec(
                current, centre,
                deadzoneWidth, deadzoneHeight,
                baseSpeed, acceleration
            );

            velocity.X *= scaleX;
            velocity.Y *= scaleY;

            return velocity;
        }

        private double signedSqrt(double x)
        {
            // return sqrt of magnitude but with original sign
            return Math.Sign(x) * Math.Sqrt(Math.Abs(x));
        }

        private Vector CalculateLookToScrollVelocityVec(
            Point current,
            Point centre,
            double deadzoneWidth,
            double deadzoneHeight,
            double baseSpeed,
            double acceleration)
        {
            // Calculate the direction and distance from the centre to the current value. 
            Vector distance = current - centre;
            Vector intersectionPoint = ellipseIntersection(distance, deadzoneWidth / 2, deadzoneHeight / 2);
            if (distance.LengthSquared < intersectionPoint.LengthSquared)
            {
                // inside the deadzone
                return new Vector(0, 0);
            }
            else
            {
                // Remove the deadzone.
                distance -= intersectionPoint;

                // Calculate total speed using base speed and distance-based acceleration.
                Vector speed = new Vector(baseSpeed + signedSqrt(distance.X) * acceleration,
                                          baseSpeed + signedSqrt(distance.Y) * acceleration);

                return speed;
            }
        }


        private Vector ellipseIntersection(Vector vector, double rx, double ry)
        {
            if (vector.LengthSquared == 0)
            {
                return new Vector(0, 0);
            }
            else
            {
                // Intersection computed using parametric form for ellipse
                // x = rx*cos(t), y = ry*sin(t) for parameter t [0,2π]
                var t = Math.Atan2((rx * vector.Y), (ry * vector.X));

                return new Vector(rx * Math.Cos(t), ry * Math.Sin(t));
            }
        }
    }
}

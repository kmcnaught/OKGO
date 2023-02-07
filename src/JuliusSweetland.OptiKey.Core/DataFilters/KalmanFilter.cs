﻿// Copyright (c) 2022 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using JuliusSweetland.OptiKey.Properties;
using System;
using System.Windows;

namespace JuliusSweetland.OptiKey.DataFilters
{
    public class KalmanFilter
    {
        private Point EstimatedPoint;     //The point used after applying the filter.
        private double EstimationNoise;   //This value fluctuates to balance each new measurment with the previous estimate.
        private double Gain;              //Scale from 0% to 100% applied to the movement delta when updating the Estimate
        private Point Measurement1;
        private Point Measurement2;
        private readonly double MeasurementNoise;

        public KalmanFilter()
        {
            EstimatedPoint = new Point(0, 0);
            EstimationNoise = 1000;
            MeasurementNoise = 2000;
        }

        public Point Update(Point measurement)
        {
            // Prediction - process model is "we haven't moved" but with some uncertainty
            // The uncertainty increases with distance of new data from current estimate - if within a fixations-distance
            // we have a narrow prior to enforce smoothness. If far away, we want a uniform prior over all positions. 
            // The exponential process noise captures this smoothly

            // == INPUT VARIABLES: this drive behavior of our model == //
            var smoothingFactor = (double)Settings.Default.GazeSmoothingLevel;

            // this dictates how quickly process noise scales up across all deltas
            // higher = extend smoothness for longer saccades
            var processScale = 10.0 * smoothingFactor - 5.0;

            // this realigns the curve to ensure we always scale up smoothly from delta = 0 to max
            var curveOffset = -1.0 * smoothingFactor;

            //A weighted average of the last 3 measurements will reduce the recoil inherent with large movements
            // EyeMine hack: I've adjusted this to forget faster than the Optikey implementation. 
            // Ideally we'd turn it off and instead re-tune the KF model to have a less-fast drop-off 
            // so we get smooth behaviour locally but still rapid saccades
            if (Settings.Default.SmoothWhenChangingGazeTarget)
            {
                measurement = new Point(
                    measurement.X * 0.75 + Measurement1.X * 0.25,
                    measurement.Y * 0.75 + Measurement1.Y * 0.25);

                Measurement2 = Measurement1;
                Measurement1 = measurement;
            }

            // == PROCESS MODEL: this encodes all our desired behavior wrt smoothness == //
            var delta = (measurement - EstimatedPoint).Length;
            var currentProcessNoise = Math.Exp((delta + (processScale * curveOffset)) / processScale) - Math.Exp(curveOffset);

            // == KALMAN FILTER:  Standard update equations from the KF framework - these shouldn't be changed == //
            EstimationNoise += currentProcessNoise;
            EstimationNoise = Math.Min(currentProcessNoise, 1e100); // Prevent saturation to Inf

            Gain = (EstimationNoise) / (EstimationNoise + MeasurementNoise);
            EstimationNoise = (1.0 - Gain) * EstimationNoise;
            EstimatedPoint += (measurement - EstimatedPoint) * Gain;

            return EstimatedPoint;
        }
    }
}

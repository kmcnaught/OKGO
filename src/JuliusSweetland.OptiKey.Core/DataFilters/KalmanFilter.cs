// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved

using System;

namespace JuliusSweetland.OptiKey.DataFilters
{
    public class KalmanFilter
    {
        double ProcessNoise; //Standard deviation - Q
        double MeasurementNoise; // R
        double EstimationConfidence; //P
        double? EstimatedValue; // X 
        double Gain; // K

        // We'll discard our model when saccades exceed this: this is the point at which our plant model is very non-gaussian
        private double MaxMicroSaccade; 

        public KalmanFilter(double initialValue = 0f, double confidenceOfInitialValue = 1f, double processNoise = 0.0001f, double measurementNoise = 0.01f)
        {
            // TODO: remove "initial value" settings
            this.ProcessNoise = processNoise;
            this.MeasurementNoise = measurementNoise;
            this.EstimationConfidence = 0.1f;
            this.EstimatedValue = null; 
            this.MaxMicroSaccade = 50; // pixels? default to % of screen?
        }

        public double Update(double measurement)
        {
            // Initialisation, or re-initialisation after a big jump
            if (!EstimatedValue.HasValue || Math.Abs(EstimatedValue.Value - measurement) > MaxMicroSaccade)
            {
                EstimatedValue = measurement;
                EstimationConfidence = 0.1f;
            }

            Gain = (EstimationConfidence + ProcessNoise) / (EstimationConfidence + ProcessNoise + MeasurementNoise);
            EstimationConfidence = MeasurementNoise * (EstimationConfidence + ProcessNoise) / (MeasurementNoise + EstimationConfidence + ProcessNoise);
            double result = EstimatedValue.Value + (measurement - EstimatedValue.Value) * Gain;
            EstimatedValue = result;

            return result;
        }
    }
}

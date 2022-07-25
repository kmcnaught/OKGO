using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JuliusSweetland.OptiKey.Models.ScalingModels
{
    class SensitivityFunctionFactory
    {
        public static ISensitivityFunction Create(string inputString)
        {
            var deadzones = new List<Region>();
            var regions = new List<Region>();
            // Empty string: classic SqrtScalingFromSettings
            double baseSpeed = 0;
            double acceleration = 0.02;
            if (String.IsNullOrEmpty(inputString))
            {
                SqrtScalingFromSettings sqrtScaling = new SqrtScalingFromSettings(baseSpeed, acceleration);
                sqrtScaling.SetScaleFactor(new float[] { 1.0f, 1.0f });
                return sqrtScaling;
            }
            else
            {
                inputString = inputString.Trim();
            }

            string inputStringWithoutWhitespace = inputString.RemoveWhitespace().ToLower();

            // String contains numbers, separator, whitespace only: classic SqrtScalingFromSettings
            if (inputString.All(x => char.IsWhiteSpace(x) || x == ',' || char.IsDigit(x)))
            {
                try
                {
                    float xScale = 1.0f;
                    float yScale = 1.0f;

                    char[] delimChars = { ',' };
                    float[] parts = inputString.ToFloatArray(delimChars);

                    if (parts.Length == 1)
                    {
                        xScale = yScale = parts[0];
                    }
                    else if (parts.Length > 1)
                    {
                        xScale = parts[0];
                        yScale = parts[1];
                    }

                    SqrtScalingFromSettings sqrtScaling = new SqrtScalingFromSettings(baseSpeed, acceleration);
                    sqrtScaling.SetScaleFactor(new float[] { xScale, yScale });

                    return sqrtScaling;
                }
                catch (Exception e)
                {
                    // Silently pass through to attempt other interpretations
                }
            }

            var regex = new Regex(@"deadzones\(([\d.]*,[\d.]*,[\d.]*,[\d.]*,[\d])\)");
            if (regex.IsMatch(inputStringWithoutWhitespace))
            {
                Match m = regex.Match(inputStringWithoutWhitespace);
                if (m.Success)
                {
                    var inputData = m.Groups[1].Captures[0].ToString();
                    var inputList = Regex.Split(inputData, @"[\s \)\(]").Where(s => s != String.Empty);
                    if (inputList.Any())
                    {
                        foreach (var input in inputList)
                        {
                            var split = input.Split(',').Where(s => s != String.Empty);
                            var nums = (from number in split select number.ToFloat()).ToArray();
                            if (nums.Any() && nums.Count() > 4)
                            {
                                var visible = nums.Count() > 4 && nums[4] == 1;

                                deadzones.Add(new Region(nums[0], nums[1], nums[2], nums[3], 0, 0, 0, visible));
                            }
                        }
                    }
                }
            }

            regex = new Regex(@"rings([\d.,()]*)");
            if (regex.IsMatch(inputStringWithoutWhitespace))
            {
                Match m = regex.Match(inputStringWithoutWhitespace);
                if (m.Success)
                {
                    var inputData = m.Groups[1].Captures[0].ToString();
                    var inputList = Regex.Split(inputData, @"[\s \)\(]").Where(s => s != String.Empty);
                    if (inputList.Any())
                    {
                        foreach (var input in inputList)
                        {
                            var split = input.Split(',').Where(s => s != String.Empty);
                            var nums = (from number in split select number.ToFloat()).ToArray();
                            if (nums.Any() && nums.Count() > 1)
                            {
                                var ratio = Graphics.PrimaryScreenWidthInPixels / Graphics.PrimaryScreenHeightInPixels;
                                var visible = nums.Count() > 3 ? nums[3] == 1 : input == inputList.First() || input ==   inputList.Last();

                                if (!regions.Any())
                                {
                                    var radius = nums[2];
                                    var left = nums[0] - radius;
                                    var top = nums[1] - radius;
                                    var width = 2 * radius;
                                    var height = width * ratio;
                                    regions.Add(new Region(left, top, width, height, radius, 0, 0, visible));
                                }
                                else
                                {
                                    var radius = nums[0];
                                    var left = regions[0].Center.X - radius;
                                    var top = regions[0].Center.Y - radius;
                                    var width = 2 * radius;
                                    var height = width * ratio;
                                    var amount = nums[1];
                                    var gradient = nums.Count() > 2 ? nums[2] : .5;
                                    regions.Add(new Region(left, top, width, height, radius, amount, gradient, visible));
                                }
                            }
                        }
                    }
                    if (regions.Count > 1)
                    {
                        RegionScaling regionScaling = new RegionScaling(deadzones, regions);
                        return regionScaling;
                    }
                }
            }

            regex = new Regex(@"rectangles([\d.,()]*)");
            if (regex.IsMatch(inputStringWithoutWhitespace))
            {
                Match m = regex.Match(inputStringWithoutWhitespace);
                if (m.Success)
                {
                    var inputData = m.Groups[1].Captures[0].ToString();
                    var inputList = Regex.Split(inputData, @"[\s \)\(]").Where(s => s != String.Empty);
                    if (inputList.Any())
                    {
                        foreach (var input in inputList)
                        {
                            var split = input.Split(',').Where(s => s != String.Empty);
                            var nums = (from number in split select number.ToFloat()).ToArray();
                            if (nums.Any() && nums.Count() > 4)
                            {
                                var gradient = nums.Count() > 5 ? nums[5] : .5;
                                var visible = nums.Count() > 6 ? nums[6] == 1 : input == inputList.First() || input == inputList.Last();
                                regions.Add(new Region(nums[0], nums[1], nums[2], nums[3],
                                    0, nums[4], gradient, visible));
                            }
                        }
                    }
                    if (regions.Count > 1)
                    {
                        RegionScaling regionScaling = new RegionScaling(deadzones, regions);
                        return regionScaling;
                    }
                }
            }

            // special case for "ring": i,e spherical tophat
            // "ring[inner radius, outer radius, amount]"
            // e.g ring[0.3, 0.5, 1.0]
            regex = new Regex(@"ring\[([\d.]*),([\d.]*),([\d.]*)\]");
            if (regex.IsMatch(inputStringWithoutWhitespace))
            {
                Match m = regex.Match(inputStringWithoutWhitespace);
                if (m.Success)
                {
                    if (m.Groups.Count >= 4)
                    {
                        // TODO: add some exception handling for these conversions?
                        float innerRadius = Convert.ToSingle(m.Groups[1].Captures[0].ToString());
                        float outerRadius = Convert.ToSingle(m.Groups[2].Captures[0].ToString());
                        float scale = 1.0f;
                        if (m.Groups.Count >= 4 && m.Groups[3].Length > 0)
                        {
                            scale = Convert.ToSingle(m.Groups[3].Captures[0].ToString());
                        }

                        PiecewiseScaling piecewiseScaling = new PiecewiseScaling();
                        float eps = 1e-3f;
                        float[] coords = new float[] { innerRadius, 0,
                                                       innerRadius + eps, scale,
                                                       outerRadius - eps, scale,
                                                       outerRadius, 0};
                        piecewiseScaling.SetCoords(coords);

                        return piecewiseScaling;
                    }
                }
            }

            // string "piecewise" and a list of coordinates
            // e.g. triangle would be "piecewise[(0.25, 0), (0.5, 1), (0.75, 0)]"

            regex = new Regex(@"piecewise\[([\d.,()]*)\]");
            if (regex.IsMatch(inputStringWithoutWhitespace))
            {
                Match m = regex.Match(inputStringWithoutWhitespace);
                if (m.Success)
                {
                    string coords = m.Groups[1].Captures[0].ToString();
                    var split = Regex.Split(coords, @"[\s ,\)\(]").Where(s => s != String.Empty); ;
                    IEnumerable<float> allCoords = from number in split select number.ToFloat();

                    PiecewiseScaling piecewiseScaling = new PiecewiseScaling();
                    piecewiseScaling.SetCoords(allCoords.ToArray());

                    return piecewiseScaling;
                }
            }

            // string "segments" and params
            // e.g. "segments[4, 0.5, 0.8, 0.5]" where params are
            // number of segments, inner radius, outer radius, (optional) maximum scaling
            regex = new Regex(@"segments?\[(\d*),([\d.]*),([\d.]*),?([\d.]*)?\]");
            if (regex.IsMatch(inputStringWithoutWhitespace))
            {
                Match m = regex.Match(inputStringWithoutWhitespace);
                if (m.Success)
                {
                    if (m.Groups.Count >= 4) {
                        // TODO: add some exception handling for these conversions?
                        int n = Convert.ToInt32(m.Groups[1].Captures[0].ToString());
                        float innerRadius = Convert.ToSingle(m.Groups[2].Captures[0].ToString());
                        float outerRadius = Convert.ToSingle(m.Groups[3].Captures[0].ToString());
                        float scale = 1.0f;
                        if (m.Groups.Count >= 5 && m.Groups[4].Length > 0)
                        {                            
                            scale = Convert.ToSingle(m.Groups[4].Captures[0].ToString());
                        }
                        SegmentScaling segmentScaling = new SegmentScaling(n, innerRadius, outerRadius, scale);                        
                        return segmentScaling;
                    }
                }
            }

            //fallback - basic 
            SqrtScalingFromSettings sqrtScalingFallback = new SqrtScalingFromSettings(baseSpeed, acceleration);
            sqrtScalingFallback.SetScaleFactor(new float[] { 1.0f, 1.0f });
            return sqrtScalingFallback;
        }
    }
}

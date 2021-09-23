using JuliusSweetland.OptiKey.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace JuliusSweetland.OptiKey.Models.ScalingModels
{
    class SensitivityFunctionFactory
    {
        public static ISensitivityFunction Create(string inputString)
        {
            inputString = inputString.Trim();

            // Empty string: classic SqrtScalingFromSettings
            double baseSpeed = 0;
            double acceleration = 0.02;
            if (String.IsNullOrEmpty(inputString))
            {
                SqrtScalingFromSettings sqrtScaling = new SqrtScalingFromSettings(baseSpeed, acceleration);
                sqrtScaling.SetScaleFactor(new float[] { 1.0f, 1.0f });
                return sqrtScaling;
            }

            // String contains numbers, separator, whitespace only: classic SqrtScalingFromSettings
            else if (inputString.All(x => char.IsWhiteSpace(x) || x == ',' || char.IsDigit(x)))
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

            // string "piecewise" and a list of coordinates
            // e.g. triangle would be "piecewise[(0.25, 0), (0.5, 1), (0.75, 0)]"

            Regex rgxMatchPiecewise = new Regex(@"piecewise\s*\[([\d.,()\s]*)\]");
            if (rgxMatchPiecewise.IsMatch(inputString))
            {
                Match m = rgxMatchPiecewise.Match(inputString);
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


            //fallback - basic 
            SqrtScalingFromSettings sqrtScalingFallback = new SqrtScalingFromSettings(baseSpeed, acceleration);
            sqrtScalingFallback.SetScaleFactor(new float[] { 1.0f, 1.0f });
            return sqrtScalingFallback;
        }

    }
}

using JuliusSweetland.OptiKey.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JuliusSweetland.OptiKey.Models
{
    // Encapsulate a number represented as a string, possibly as
    // a percentage relevant to some reference. 
    // e.g. "3.14", "50%", "42", "99%"
    class RelativeNumberFromString
    {
        private readonly double _value;        
        private readonly bool _isValid;
        private readonly bool _isRelative;

        public RelativeNumberFromString(string input)
        {            
            _isValid = TryParse(input, out _value, out _isRelative);
        }

        public bool IsValid => _isValid;
        public bool IsRelative => _isRelative;

        public double GetValue(double referenceValue = 1)
        {
            if (!_isValid)
            {
                throw new InvalidOperationException("Invalid number");
            }

            if (_isRelative)
            {
                return _value * referenceValue;
            }
            else
            {
                return _value;
            }
        }

        private bool TryParse(string input, out double result, out bool isRelative)
        {
            isRelative = false;

            if (string.IsNullOrWhiteSpace(input))
            {
                result = 0;
                return false;
            }

            // Remove all whitespace from string
            input = input.RemoveWhitespace();

            if (input.EndsWith("%"))
            {
                isRelative = true;
                input = input.Replace("%", "");
                if (double.TryParse(input, out double percentage))
                {
                    result = percentage / 100;
                    return !IsNaNOrInf(result);                    
                }
            }
            else if (double.TryParse(input, out double amount))
            {
                result = amount;
                return !IsNaNOrInf(result);
            }

            result = 0;
            return false;
        }

        private bool IsNaNOrInf(double d)
        {
            return double.IsNaN(d) ||
                double.IsNegativeInfinity(d) ||
                double.IsPositiveInfinity(d);
        }
    }
}

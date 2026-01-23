using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Pondskater{
    public class OffsetParserException : Exception
    {
        public OffsetParserException(string message) : base(message) { }
    }

    public class OffsetParser
    {
        // Regex patterns
        private static readonly string oneOffsetPattern = @"(?:([0-9]+)\s*\*\s*)?([0-9]+(?:\.\d+)?)";
        private static readonly string oneBlockPattern = $@"({oneOffsetPattern})(\s*\+\s*{oneOffsetPattern})*";
        private static readonly string offsetSpecPattern = $@"^\s*{oneBlockPattern}(\s*,\s*{oneBlockPattern})*\s*$";

        public static List<int> ParseOffsetSpec(string input)
        {
            // Check if the input matches the offset-spec pattern
            if (!Regex.IsMatch(input, offsetSpecPattern))
            {
                throw new OffsetParserException("Invalid format for --sk-offset. Please follow the format 'offset-spec = <one-block> [, <one-block> [, ... ]]'.");
            }

            // Remove spaces to normalize the input
            input = input.Replace(" ", "");

            // Split the input by commas (this gives us each one-block)
            string[] blocks = input.Split(',');

            // List to hold the number of polylines for each block
            List<int> polylineCounts = new List<int>();

            foreach (string block in blocks)
            {
                string[] offsets = block.Split('+');
                int totalPolylines = 0;
                foreach (string offset in offsets)
                {
                    // For each offset, check if it matches the one-offset pattern
                    if (!Regex.IsMatch(offset.Trim(), oneOffsetPattern))
                    {
                        throw new OffsetParserException($"Invalid one-offset format in '{offset.Trim()}'");
                    }
                    // Determine the number of polylines for each one-offset
                    totalPolylines += GetPolylineCount(offset);
                }
            polylineCounts.Add(totalPolylines);
            }
            return polylineCounts;
        }
        
        // Determines the number of polylines in a single one-offset (e.g., '3 * 0.025' returns 3, '0.01' returns 1)
        private static int GetPolylineCount(string offset)
        {
            // Match the one-offset against the pattern
            Match match = Regex.Match(offset, oneOffsetPattern);

            if (!match.Success)
            {
                throw new ArgumentException($"Invalid one-offset format: {offset}");
            }

            // Extract the cnt (multiplier), defaulting to 1 if not provided
            string cntStr = match.Groups[1].Value; // Optional multiplier
            string timeStr = match.Groups[2].Value; // Time value (decimal or integer)

            // Convert to an integer value
            int cnt = string.IsNullOrEmpty(cntStr) ? 1 : int.Parse(cntStr); // Default to 1 if no multiplier

            // Return the number of polylines (cnt)
            return cnt;
        }
    }
}

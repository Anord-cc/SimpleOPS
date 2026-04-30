// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SimpleOps.GsxRamp
{
    internal static class FuelParser
    {
        private static readonly Dictionary<string, decimal> NumberWords = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            { "zero", 0 }, { "one", 1 }, { "two", 2 }, { "three", 3 }, { "four", 4 }, { "five", 5 }, { "six", 6 }, { "seven", 7 }, { "eight", 8 }, { "nine", 9 },
            { "ten", 10 }, { "eleven", 11 }, { "twelve", 12 }, { "thirteen", 13 }, { "fourteen", 14 }, { "fifteen", 15 }, { "sixteen", 16 }, { "seventeen", 17 }, { "eighteen", 18 }, { "nineteen", 19 },
            { "twenty", 20 }, { "thirty", 30 }, { "forty", 40 }, { "fifty", 50 }, { "sixty", 60 }, { "seventy", 70 }, { "eighty", 80 }, { "ninety", 90 }
        };

        private static readonly Dictionary<string, string> Units = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "kg", "kg" }, { "kilo", "kilo" }, { "kilos", "kilos" }, { "kilogram", "kilogram" }, { "kilograms", "kilograms" },
            { "ton", "ton" }, { "tons", "tons" }, { "tonne", "tonne" }, { "tonnes", "tonnes" },
            { "lb", "lb" }, { "lbs", "lbs" }, { "pound", "pound" }, { "pounds", "pounds" }
        };

        public static FuelRequest TryParse(string rawPhrase, string normalizedPhrase)
        {
            decimal rawAmount;
            string rawUnit;
            if (TryParseFromRaw(rawPhrase, out rawAmount, out rawUnit))
            {
                return new FuelRequest
                {
                    Amount = rawAmount,
                    Unit = rawUnit,
                    RawAmountText = rawAmount.ToString(CultureInfo.InvariantCulture)
                };
            }

            var tokens = TextUtility.Tokenize(normalizedPhrase);
            for (int i = 0; i < tokens.Length; i++)
            {
                string unit;
                if (!Units.TryGetValue(tokens[i], out unit))
                {
                    continue;
                }

                if (TryParseNumberTokens(tokens, i - 1, out decimal amount, out int startIndex))
                {
                    var raw = string.Join(" ", Slice(tokens, startIndex, i - startIndex));
                    return new FuelRequest
                    {
                        Amount = amount,
                        Unit = unit,
                        RawAmountText = raw
                    };
                }
            }

            return null;
        }

        private static bool TryParseFromRaw(string rawPhrase, out decimal amount, out string unit)
        {
            amount = 0m;
            unit = null;
            if (string.IsNullOrWhiteSpace(rawPhrase))
            {
                return false;
            }

            var candidates = new[]
            {
                "kilograms","kilogram","kilos","kilo","kg",
                "tonnes","tonne","tons","ton",
                "pounds","pound","lbs","lb"
            };

            var lower = rawPhrase.ToLowerInvariant();
            for (int i = 0; i < candidates.Length; i++)
            {
                var marker = candidates[i];
                var index = lower.IndexOf(marker, StringComparison.Ordinal);
                if (index <= 0)
                {
                    continue;
                }

                unit = marker;
                int start = index - 1;
                while (start >= 0 && (char.IsDigit(lower[start]) || lower[start] == '.' || lower[start] == ' '))
                {
                    start--;
                }

                var numericText = lower.Substring(start + 1, index - (start + 1)).Trim();
                if (decimal.TryParse(numericText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseNumberTokens(string[] tokens, int endIndex, out decimal amount, out int startIndex)
        {
            amount = 0m;
            startIndex = -1;

            for (int start = Math.Max(0, endIndex - 5); start <= endIndex; start++)
            {
                var candidate = string.Join(" ", Slice(tokens, start, endIndex - start + 1));
                if (TryParseNumeric(candidate, out amount))
                {
                    startIndex = start;
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseNumeric(string text, out decimal amount)
        {
            amount = 0m;
            decimal parsed;
            if (decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out parsed))
            {
                amount = parsed;
                return true;
            }

            var tokens = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                return false;
            }

            decimal total = 0m;
            decimal current = 0m;
            bool seen = false;
            bool decimalMode = false;
            decimal decimalFactor = 0.1m;

            for (int i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                if (token == "and")
                {
                    continue;
                }

                if (token == "point")
                {
                    decimalMode = true;
                    continue;
                }

                decimal wordValue;
                if (!NumberWords.TryGetValue(token, out wordValue))
                {
                    if (token == "hundred")
                    {
                        current = current == 0m ? 100m : current * 100m;
                        seen = true;
                        continue;
                    }

                    if (token == "thousand")
                    {
                        current = current == 0m ? 1000m : current * 1000m;
                        total += current;
                        current = 0m;
                        seen = true;
                        continue;
                    }

                    return false;
                }

                seen = true;
                if (decimalMode)
                {
                    amount += wordValue * decimalFactor;
                    decimalFactor /= 10m;
                    continue;
                }

                current += wordValue;
            }

            if (!seen)
            {
                return false;
            }

            amount += total + current;
            return amount > 0m;
        }

        private static string[] Slice(string[] tokens, int start, int count)
        {
            var slice = new string[count];
            Array.Copy(tokens, start, slice, 0, count);
            return slice;
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Hosting
{
    using static ObjectFormatterHelpers;

    public class CSharpPrimitiveFormatter : CommonPrimitiveFormatter
    {
        protected override string NullLiteral => ObjectDisplay.NullLiteral;

        protected override string FormatLiteral(bool value)
        {
            return ObjectDisplay.FormatLiteral(value);
        }

        protected override string FormatLiteral(string value, bool quote, bool useHexadecimalNumbers = false)
        {
            var options = ObjectDisplayOptions.None;
            if (quote)
            {
                options |= ObjectDisplayOptions.UseQuotes;
            }

            if (useHexadecimalNumbers)
            {
                options |= ObjectDisplayOptions.UseHexadecimalNumbers;
            }

            return ObjectDisplay.FormatLiteral(value, options);
        }

        protected override string FormatLiteral(char c, bool quote, bool includeCodePoints = false, bool useHexadecimalNumbers = false)
        {
            var options = ObjectDisplayOptions.None;
            if (quote)
            {
                options |= ObjectDisplayOptions.UseQuotes;
            }

            if (includeCodePoints)
            {
                options |= ObjectDisplayOptions.IncludeCodePoints;
            }

            if (useHexadecimalNumbers)
            {
                options |= ObjectDisplayOptions.UseHexadecimalNumbers;
            }

            return ObjectDisplay.FormatLiteral(c, options);
        }

        protected override string FormatLiteral(sbyte value, bool useHexadecimalNumbers = false)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers));
        }

        protected override string FormatLiteral(byte value, bool useHexadecimalNumbers = false)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers));
        }

        protected override string FormatLiteral(short value, bool useHexadecimalNumbers = false)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers));
        }

        protected override string FormatLiteral(ushort value, bool useHexadecimalNumbers = false)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers));
        }

        protected override string FormatLiteral(int value, bool useHexadecimalNumbers = false)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers));
        }

        protected override string FormatLiteral(uint value, bool useHexadecimalNumbers = false)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers));
        }

        protected override string FormatLiteral(long value, bool useHexadecimalNumbers = false)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers));
        }

        protected override string FormatLiteral(ulong value, bool useHexadecimalNumbers = false)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers));
        }

        protected override string FormatLiteral(double value)
        {
            return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None);
        }

        protected override string FormatLiteral(float value)
        {
            return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None);
        }

        protected override string FormatLiteral(decimal value)
        {
            return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None);
        }

        protected override string FormatLiteral(DateTime value)
        {
            // DateTime is not primitive in C#
            return null;
        }
    }
}

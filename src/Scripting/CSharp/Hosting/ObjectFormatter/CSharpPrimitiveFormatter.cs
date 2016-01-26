// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
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

        protected override string FormatLiteral(string value, bool useQuotes, bool escapeNonPrintable, int numberRadix = NumberRadixDecimal)
        {
            var options = GetObjectDisplayOptions(useQuotes: useQuotes, escapeNonPrintable: escapeNonPrintable, numberRadix: numberRadix);
            return ObjectDisplay.FormatLiteral(value, options);
        }

        protected override string FormatLiteral(char c, bool useQuotes, bool escapeNonPrintable, bool includeCodePoints = false, int numberRadix = NumberRadixDecimal)
        {
            var options = GetObjectDisplayOptions(useQuotes: useQuotes, escapeNonPrintable: escapeNonPrintable, includeCodePoints: includeCodePoints, numberRadix: numberRadix);
            return ObjectDisplay.FormatLiteral(c, options);
        }

        protected override string FormatLiteral(sbyte value, int numberRadix = NumberRadixDecimal, CultureInfo cultureInfo = null)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix: numberRadix), cultureInfo);
        }

        protected override string FormatLiteral(byte value, int numberRadix = NumberRadixDecimal, CultureInfo cultureInfo = null)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix: numberRadix), cultureInfo);
        }

        protected override string FormatLiteral(short value, int numberRadix = NumberRadixDecimal, CultureInfo cultureInfo = null)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix: numberRadix), cultureInfo);
        }

        protected override string FormatLiteral(ushort value, int numberRadix = NumberRadixDecimal, CultureInfo cultureInfo = null)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix: numberRadix), cultureInfo);
        }

        protected override string FormatLiteral(int value, int numberRadix = NumberRadixDecimal, CultureInfo cultureInfo = null)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix: numberRadix), cultureInfo);
        }

        protected override string FormatLiteral(uint value, int numberRadix = NumberRadixDecimal, CultureInfo cultureInfo = null)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix: numberRadix), cultureInfo);
        }

        protected override string FormatLiteral(long value, int numberRadix = NumberRadixDecimal, CultureInfo cultureInfo = null)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix: numberRadix), cultureInfo);
        }

        protected override string FormatLiteral(ulong value, int numberRadix = NumberRadixDecimal, CultureInfo cultureInfo = null)
        {
            return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix: numberRadix), cultureInfo);
        }

        protected override string FormatLiteral(double value, CultureInfo cultureInfo = null)
        {
            return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None, cultureInfo);
        }

        protected override string FormatLiteral(float value, CultureInfo cultureInfo = null)
        {
            return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None, cultureInfo);
        }

        protected override string FormatLiteral(decimal value, CultureInfo cultureInfo = null)
        {
            return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None, cultureInfo);
        }

        protected override string FormatLiteral(DateTime value, CultureInfo cultureInfo = null)
        {
            // DateTime is not primitive in C#
            return null;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Globalization;
using Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests
{
    internal sealed class TestVisualBasicObjectFormatter : VisualBasicObjectFormatterImpl
    {
        private readonly bool _quoteStringsAndCharacters;
        private readonly int _maximumLineLength;
        private readonly CultureInfo _cultureInfo;

        public TestVisualBasicObjectFormatter(
            bool quoteStringsAndCharacters = true,
            int maximumLineLength = int.MaxValue,
            CultureInfo cultureInfo = null)
        {
            _quoteStringsAndCharacters = quoteStringsAndCharacters;
            _maximumLineLength = maximumLineLength;
            _cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
        }

        protected override BuilderOptions GetInternalBuilderOptions(PrintOptions printOptions)
            => new BuilderOptions(
                indentation: "  ",
                newLine: Environment.NewLine,
                ellipsis: printOptions.Ellipsis,
                maximumLineLength: _maximumLineLength,
                maximumOutputLength: printOptions.MaximumOutputLength);

        protected override CommonPrimitiveFormatterOptions GetPrimitiveOptions(PrintOptions printOptions)
            => new CommonPrimitiveFormatterOptions(
                numberRadix: printOptions.NumberRadix,
                includeCodePoints: false,
                escapeNonPrintableCharacters: printOptions.EscapeNonPrintableCharacters,
                quoteStringsAndCharacters: _quoteStringsAndCharacters,
                cultureInfo: _cultureInfo);
    }
}

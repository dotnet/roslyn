// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests
{
    public sealed class TestVisualBasicObjectFormatter : VisualBasicObjectFormatter
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

        internal override BuilderOptions GetInternalBuilderOptions(PrintOptions printOptions) =>
            new BuilderOptions(
                indentation: "  ",
                newLine: Environment.NewLine,
                ellipsis: printOptions.Ellipsis,
                maximumLineLength: _maximumLineLength,
                maximumOutputLength: printOptions.MaximumOutputLength);

        protected override CommonPrimitiveFormatterOptions GetPrimitiveOptions(PrintOptions printOptions) =>
            new CommonPrimitiveFormatterOptions(
                numberRadix: printOptions.NumberRadix, 
                includeCodePoints: false,
                escapeNonPrintableCharacters: printOptions.EscapeNonPrintableCharacters, 
                quoteStringsAndCharacters: _quoteStringsAndCharacters,
                cultureInfo: _cultureInfo);
    }
}
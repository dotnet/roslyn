// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests
{
    public sealed class TestCSharpObjectFormatter : CSharpObjectFormatter
    {
        private readonly bool _includeCodePoints;
        private readonly bool _quoteStringsAndCharacters;
        private readonly int _maximumLineLength;

        public TestCSharpObjectFormatter(bool includeCodePoints = false, bool quoteStringsAndCharacters = true, int maximumLineLength = int.MaxValue)
        {
            _includeCodePoints = includeCodePoints;
            _quoteStringsAndCharacters = quoteStringsAndCharacters;
            _maximumLineLength = maximumLineLength;
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
                includeCodePoints: _includeCodePoints,
                escapeNonPrintableCharacters: printOptions.EscapeNonPrintableCharacters,
                quoteStringsAndCharacters: _quoteStringsAndCharacters);
    }
}
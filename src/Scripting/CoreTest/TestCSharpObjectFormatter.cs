// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests
{
    public sealed class TestCSharpObjectFormatter : CSharpObjectFormatter
    {
        private readonly bool _omitStringQuotes;
        private readonly int _maximumLineLength;

        public TestCSharpObjectFormatter(bool omitStringQuotes = false, int maximumLineLength = int.MaxValue)
        {
            _omitStringQuotes = omitStringQuotes;
            _maximumLineLength = maximumLineLength;
        }

        internal override BuilderOptions GetInternalBuilderOptions(PrintOptions printOptions) =>
            new BuilderOptions(
                indentation: "  ",
                newLine: Environment.NewLine,
                ellipsis: printOptions.Ellipsis,
                maximumLineLength: _maximumLineLength,
                maximumOutputLength: printOptions.MaximumOutputLength);

        protected override CommonPrimitiveFormatter.Options GetPrimitiveOptions(PrintOptions printOptions) =>
            new CommonPrimitiveFormatter.Options(printOptions.NumberRadix == NumberRadix.Hexadecimal, printOptions.EscapeNonPrintableCharacters, _omitStringQuotes);
    }
}
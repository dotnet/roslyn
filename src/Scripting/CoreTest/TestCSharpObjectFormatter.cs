// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests
{
    public sealed class TestCSharpObjectFormatter : CSharpObjectFormatter
    {
        public static readonly CommonObjectFormatter SeparateLines = new TestCSharpObjectFormatter(MemberDisplayFormat.SeparateLines);
        public static readonly CommonObjectFormatter SingleLine = new TestCSharpObjectFormatter(MemberDisplayFormat.SingleLine);
        public static readonly CommonObjectFormatter Hidden = new TestCSharpObjectFormatter(MemberDisplayFormat.Hidden);

        public TestCSharpObjectFormatter(
            MemberDisplayFormat memberDisplayFormat = default(MemberDisplayFormat),
            bool useHexadecimalNumbers = false,
            int lineLengthLimit = int.MaxValue,
            int totalLengthLimit = int.MaxValue)
        {
            MemberDisplayFormat = memberDisplayFormat;
            PrimitiveOptions = new CommonPrimitiveFormatter.Options(useHexadecimalNumbers, includeCodePoints: false, omitStringQuotes: false);
            InternalBuilderOptions = new BuilderOptions(
                indentation: "  ",
                newLine: Environment.NewLine,
                ellipsis: "...",
                lineLengthLimit: lineLengthLimit,
                totalLengthLimit: totalLengthLimit);
        }

        protected override MemberDisplayFormat MemberDisplayFormat { get; }
        protected override CommonPrimitiveFormatter.Options PrimitiveOptions { get; }

        internal override BuilderOptions InternalBuilderOptions { get; }
    }
}
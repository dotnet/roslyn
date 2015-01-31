// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public abstract class AbstractOutlinerTests
    {
        internal void AssertRegion(OutliningSpan expected, OutliningSpan actual)
        {
            Assert.Equal(expected.TextSpan.Start, actual.TextSpan.Start);
            Assert.Equal(expected.TextSpan.End, actual.TextSpan.End);
            Assert.Equal(expected.HintSpan.Start, actual.HintSpan.Start);
            Assert.Equal(expected.HintSpan.End, actual.HintSpan.End);
            Assert.Equal(expected.BannerText, actual.BannerText);
            Assert.Equal(expected.AutoCollapse, actual.AutoCollapse);
        }

        protected SyntaxTree ParseCode(string code)
        {
            return SyntaxFactory.ParseSyntaxTree(code);
        }

        protected string StringFromLines(params string[] lines)
        {
            return string.Join(Environment.NewLine, lines);
        }

        protected SyntaxTree ParseLines(params string[] lines)
        {
            var code = StringFromLines(lines);
            return ParseCode(code);
        }
    }
}

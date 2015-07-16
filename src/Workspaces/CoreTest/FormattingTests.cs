// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Test.Utilities;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class FormattingTests : TestBase
    {
        [Fact]
        public void TestCSharpFormatting()
        {
            var text = @"public class C{public int X;}";
            var expectedFormattedText = @"public class C { public int X; }";

            var tree = CS.SyntaxFactory.ParseSyntaxTree(text);
            var formattedRoot = Formatter.Format(tree.GetRoot(), new TestWorkspace());
            var actualFormattedText = formattedRoot.ToFullString();

            Assert.Equal(expectedFormattedText, actualFormattedText);
        }

        [Fact]
        public void TestCSharpDefaultRules()
        {
            var rules = Formatter.GetDefaultFormattingRules(new TestWorkspace(), LanguageNames.CSharp);

            Assert.NotNull(rules);
            Assert.NotEmpty(rules);
        }

        [Fact]
        public void TestVisualBasicFormatting()
        {
            var text = @"
Public Class C
Public X As Integer
End Class
";
            var expectedFormattedText = @"
Public Class C
    Public X As Integer
End Class
";

            var tree = VB.SyntaxFactory.ParseSyntaxTree(text);
            var formattedRoot = Formatter.Format(tree.GetRoot(), new TestWorkspace());
            var actualFormattedText = formattedRoot.ToFullString();

            Assert.Equal(expectedFormattedText, actualFormattedText);
        }

        [Fact]
        public void TestVisualBasicDefaultFormattingRules()
        {
            var rules = Formatter.GetDefaultFormattingRules(new TestWorkspace(), LanguageNames.VisualBasic);

            Assert.NotNull(rules);
            Assert.NotEmpty(rules);
        }
    }
}

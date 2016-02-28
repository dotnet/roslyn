// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using System;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class FormattingTests : TestBase
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task TestCSharpFormatting()
        {
            var text = @"public class C{public int X;}";
            var expectedFormattedText = @"public class C { public int X; }";

            await AssertFormatCSharpAsync(expectedFormattedText, text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestCSharpDefaultRules()
        {
            var rules = Formatter.GetDefaultFormattingRules(new TestWorkspace(), LanguageNames.CSharp);

            Assert.NotNull(rules);
            Assert.NotEmpty(rules);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task TestVisualBasicFormatting()
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

            await AssertFormatVBAsync(expectedFormattedText, text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestVisualBasicDefaultFormattingRules()
        {
            var rules = Formatter.GetDefaultFormattingRules(new TestWorkspace(), LanguageNames.VisualBasic);

            Assert.NotNull(rules);
            Assert.NotEmpty(rules);
        }

        private Task AssertFormatCSharpAsync(string expected, string input)
        {
            var tree = CS.SyntaxFactory.ParseSyntaxTree(input);
            return AssertFormatAsync(expected, tree);
        }

        private Task AssertFormatVBAsync(string expected, string input)
        {
            var tree = VB.SyntaxFactory.ParseSyntaxTree(input);
            return AssertFormatAsync(expected, tree);
        }

        private async Task AssertFormatAsync(string expected, SyntaxTree tree)
        {
            using (var workspace = new TestWorkspace())
            {
                var formattedRoot = await Formatter.FormatAsync(tree.GetRoot(), workspace);
                var actualFormattedText = formattedRoot.ToFullString();

                Assert.Equal(expected, actualFormattedText);
            }
        }
    }
}

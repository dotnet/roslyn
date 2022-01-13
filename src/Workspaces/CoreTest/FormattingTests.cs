﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public partial class FormattingTests : TestBase
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestCSharpFormatting()
        {
            var text = @"public class C{public int X;}";
            var expectedFormattedText = @"public class C { public int X; }";

            AssertFormatCSharp(expectedFormattedText, text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestCSharpDefaultRules()
        {
            using var workspace = new AdhocWorkspace();

            var service = workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService<ISyntaxFormattingService>();
            var rules = service.GetDefaultFormattingRules();

            Assert.NotNull(rules);
            Assert.NotEmpty(rules);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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

            AssertFormatVB(expectedFormattedText, text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestVisualBasicDefaultFormattingRules()
        {
            using var workspace = new AdhocWorkspace();
            var service = workspace.Services.GetLanguageServices(LanguageNames.VisualBasic).GetService<ISyntaxFormattingService>();
            var rules = service.GetDefaultFormattingRules();

            Assert.NotNull(rules);
            Assert.NotEmpty(rules);
        }

        private static void AssertFormatCSharp(string expected, string input)
        {
            var tree = CS.SyntaxFactory.ParseSyntaxTree(input);
            AssertFormat(expected, tree);
        }

        private static void AssertFormatVB(string expected, string input)
        {
            var tree = VB.SyntaxFactory.ParseSyntaxTree(input);
            AssertFormat(expected, tree);
        }

        private static void AssertFormat(string expected, SyntaxTree tree)
        {
            using var workspace = new AdhocWorkspace();

            var options = SyntaxFormattingOptions.Default;
            var formattedRoot = Formatter.Format(tree.GetRoot(), workspace.Services, options, CancellationToken.None);
            var actualFormattedText = formattedRoot.ToFullString();

            Assert.Equal(expected, actualFormattedText);
        }
    }
}

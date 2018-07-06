// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpOutlining : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpOutlining()
            : base(nameof(CSharpOutlining))
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task OutliningAsync()
        {
            var input = @"
using [|System;
using System.Collections.Generic;
using System.Text;|]

namespace ConsoleApplication1[|
{
    public class Program[|
    {
        public static void Main(string[] args)[|
        {
            Console.WriteLine(""Hello World"");
        }|]
    }|]
}|]";
            MarkupTestFile.GetSpans(input, out var text, out ImmutableArray<TextSpan> spans);
            await VisualStudio.Editor.SetTextAsync(text);
            Assert.Equal(spans.OrderBy(s => s.Start), await VisualStudio.Editor.GetOutliningSpansAsync());
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task OutliningConfigChangeAsync()
        {
            var input = @"
namespace ClassLibrary1[|
{
    public class Class1[|
    {
#if DEBUG
{|Release:        void Goo(){|Debug:
        {
        }|}
        
        void Goo2(){|Debug:
        {
        }|}|}
#else
{|Debug:        void Bar(){|Release:
        {
        }|}|}
#endif
    }|]
}|]";
            MarkupTestFile.GetSpans(input, out var text, out IDictionary<string, ImmutableArray<TextSpan>> spans);
            await VisualStudio.Editor.SetTextAsync(text);

            await VerifySpansInConfigurationAsync(spans, "Release");
            await VerifySpansInConfigurationAsync(spans, "Debug");
        }

        private async Task VerifySpansInConfigurationAsync(IDictionary<string, ImmutableArray<TextSpan>> spans, string configuration)
        {
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Build_SolutionConfigurations, configuration);

            var expectedSpans = spans[""].Concat(spans[configuration]).OrderBy(s => s.Start);
            Assert.Equal(expectedSpans, await VisualStudio.Editor.GetOutliningSpansAsync());
        }
    }
}

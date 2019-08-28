// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpOutlining : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpOutlining(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(CSharpOutlining))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void Outlining()
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
            VisualStudio.Editor.SetText(text);
            Assert.Equal(spans.OrderBy(s => s.Start), VisualStudio.Editor.GetOutliningSpans());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void OutliningConfigChange()
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
            VisualStudio.Editor.SetText(text);

            VerifySpansInConfiguration(spans, "Release");
            VerifySpansInConfiguration(spans, "Debug");
        }

        private void VerifySpansInConfiguration(IDictionary<string, ImmutableArray<TextSpan>> spans, string configuration)
        {
            VisualStudio.ExecuteCommand("Build.SolutionConfigurations", configuration);

            var expectedSpans = spans[""].Concat(spans[configuration]).OrderBy(s => s.Start);
            Assert.Equal(expectedSpans, VisualStudio.Editor.GetOutliningSpans());
        }
    }
}

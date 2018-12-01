// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpOutlining : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpOutlining() : base(nameof(CSharpOutlining)) { }

        [TestMethod, TestCategory(Traits.Features.Outlining)]
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
            VisualStudioInstance.Editor.SetText(text);
            Assert.AreEqual(spans.OrderBy(s => s.Start), VisualStudioInstance.Editor.GetOutliningSpans());
        }

        [TestMethod, TestCategory(Traits.Features.Outlining)]
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
            VisualStudioInstance.Editor.SetText(text);

            VerifySpansInConfiguration(spans, "Release");
            VerifySpansInConfiguration(spans, "Debug");
        }

        private void VerifySpansInConfiguration(IDictionary<string, ImmutableArray<TextSpan>> spans, string configuration)
        {
            VisualStudioInstance.ExecuteCommand("Build.SolutionConfigurations", configuration);

            var expectedSpans = spans[""].Concat(spans[configuration]).OrderBy(s => s.Start);
            Assert.AreEqual(expectedSpans, VisualStudioInstance.Editor.GetOutliningSpans());
        }
    }
}

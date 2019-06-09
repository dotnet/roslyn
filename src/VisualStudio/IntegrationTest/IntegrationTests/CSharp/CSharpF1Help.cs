// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpF1Help : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpF1Help(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(CSharpF1Help))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        void F1Help()
        {
            var text = @"
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace F1TestNamespace
{
    #region TaoRegion
    abstract class ShapesClass { }

    class Program$$
    {
        public static void Main()
        {
        }

        public IEnumerable<int> Linq1()
        {
            int[] numbers = { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 };
            int i = numbers.First();
            int j = Array.IndexOf(numbers, 1);

            var lowNums1 =
                from n in numbers
                orderby n ascending
                where n < 5
                select n;

            var numberGroups =
              from n in numbers
              let m = 1
              join p in numbers on i equals p
              group n by n % 5 into g
              select new { Remainder = g.Key, Numbers = g };

            foreach (int element in numbers) yield return i;
        }

    }
    #endregion TaoRegion
}";

            SetUpEditor(text);
            Verify("abstract", "abstract_CSharpKeyword");
            Verify("ascending", "ascending_CSharpKeyword");
            Verify("from", "from_CSharpKeyword");
            Verify("First();", "System.Linq.Enumerable.First``1");
        }

        private void Verify(string word, string expectedKeyword)
        {
            VisualStudio.Editor.PlaceCaret(word, charsOffset: -1);
            Assert.Contains(expectedKeyword, VisualStudio.Editor.GetF1Keyword());
        }
    }
}

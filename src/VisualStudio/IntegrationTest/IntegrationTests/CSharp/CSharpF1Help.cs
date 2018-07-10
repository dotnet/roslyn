// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpF1Help : AbstractIdeEditorTest
    {
        public CSharpF1Help()
            : base(nameof(CSharpF1Help))
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        [IdeFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task F1HelpAsync()
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

            await SetUpEditorAsync(text);
            await VerifyAsync("abstract", "abstract_CSharpKeyword");
            await VerifyAsync("ascending", "ascending_CSharpKeyword");
            await VerifyAsync("from", "from_CSharpKeyword");
            await VerifyAsync("First();", "System.Linq.Enumerable.First``1");
        }

        private async Task VerifyAsync(string word, string expectedKeyword)
        {
            await VisualStudio.Editor.PlaceCaretAsync(word, charsOffset: -1);
            Assert.Contains(expectedKeyword, await VisualStudio.Editor.GetF1KeywordsAsync());
        }
    }
}

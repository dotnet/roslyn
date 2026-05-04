// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.F1Help)]
public class CSharpF1Help : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpF1Help()
        : base(nameof(CSharpF1Help))
    {
    }

    [IdeFact]
    public async Task F1Help()
    {
        await SetUpEditorAsync("""

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
            }
            """, HangMitigatingCancellationToken);
        await VerifyAsync("abstract", "abstract_CSharpKeyword", HangMitigatingCancellationToken);
        await VerifyAsync("ascending", "ascending_CSharpKeyword", HangMitigatingCancellationToken);
        await VerifyAsync("from", "from_CSharpKeyword", HangMitigatingCancellationToken);
        await VerifyAsync("First();", "System.Linq.Enumerable.First``1", HangMitigatingCancellationToken);
    }

    private async Task VerifyAsync(string word, string expectedKeyword, CancellationToken cancellationToken)
    {
        await TestServices.Editor.PlaceCaretAsync(word, charsOffset: -1, cancellationToken);
        Assert.Contains(expectedKeyword, await TestServices.Editor.GetF1KeywordsAsync(cancellationToken));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BracePairs;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.BracePairs;

[UseExportProvider]
public abstract class AbstractBracePairsTests
{
    protected abstract EditorTestWorkspace CreateWorkspace(string input);

    public async Task Test(string test)
    {
        using var workspace = CreateWorkspace(test);

        var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
        var service = document.GetRequiredLanguageService<IBracePairsService>();

        var text = await document.GetTextAsync();

        using var _ = ArrayBuilder<BracePairData>.GetInstance(out var bracePairs);
        await service.AddBracePairsAsync(document, new TextSpan(0, text.Length), bracePairs, CancellationToken.None);

        var expected = workspace.Documents.Single().AnnotatedSpans;

        Assert.Equal(expected.Count, bracePairs.Count);

        foreach (var bracePair in bracePairs)
        {
            if (!FindMatch(expected, bracePair))
                AssertEx.Fail($"Unexpected brace pair: {bracePair}");
        }
    }

    private static bool FindMatch(IDictionary<string, ImmutableArray<TextSpan>> expected, BracePairData bracePair)
    {
        foreach (var (_, expectedSpans) in expected)
        {
            Assert.Equal(2, expectedSpans.Length);
            var sortedSpans = expectedSpans.Sort((s1, s2) => s1.Start - s2.Start);

            if (sortedSpans.SequenceEqual(ImmutableArray.Create(bracePair.Start, bracePair.End)))
                return true;
        }

        return false;
    }
}

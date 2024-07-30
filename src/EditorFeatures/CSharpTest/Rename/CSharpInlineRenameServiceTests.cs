// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Rename
{
    [UseExportProvider]
    public class CSharpInlineRenameServiceTests
    {
        private class ContextDictionaryComparer : IEqualityComparer<ImmutableDictionary<string, ImmutableArray<string>>?>
        {
            public static ContextDictionaryComparer Instance = new();

            public bool Equals(ImmutableDictionary<string, ImmutableArray<string>>? x, ImmutableDictionary<string, ImmutableArray<string>>? y)
            {
                if (x == y)
                    return true;

                if (x is null || y is null)
                    return false;

                if (x.Count != y.Count())
                    return false;

                foreach (var (elementFromX, elementFromY) in x.Zip(y, (elementFromX, elementFromY) => (elementFromX, elementFromY)))
                {
                    var (keyFromX, valueFromX) = elementFromX;
                    var (keyFromY, valueFromY) = elementFromY;

                    if (keyFromX != keyFromY || !valueFromX.SequenceEqual(valueFromY))
                        return false;
                }

                return true;
            }

            public int GetHashCode(ImmutableDictionary<string, ImmutableArray<string>>? obj)
                => EqualityComparer<ImmutableDictionary<string, ImmutableArray<string>>?>.Default.GetHashCode(obj);
        }

        private static async Task VerifyGetRenameContextAsync(
            string markup, string expectedContextJson, SymbolRenameOptions options, CancellationToken cancellationToken)
        {
            using var workspace = TestWorkspace.CreateCSharp(markup, composition: EditorTestCompositions.EditorFeatures);
            var documentId = workspace.Documents.Single().Id;
            var document = workspace.CurrentSolution.GetRequiredDocument(documentId);
            var inlineRenameService = document.GetRequiredLanguageService<IEditorInlineRenameService>();
            MarkupTestFile.GetPosition(markup, out _, out int cursorPosition);
            var inlineRenameInfo = await inlineRenameService.GetRenameInfoAsync(document, cursorPosition, cancellationToken).ConfigureAwait(false);
            var inlineRenameLocationSet = await inlineRenameInfo.FindRenameLocationsAsync(options, cancellationToken).ConfigureAwait(false);
            var context = await inlineRenameService.GetRenameContextAsync(inlineRenameInfo, inlineRenameLocationSet, cancellationToken).ConfigureAwait(false);
            var expectedContext = JsonSerializer.Deserialize<ImmutableDictionary<string, ImmutableArray<string>>>(expectedContextJson);
            AssertEx.AreEqual(expectedContext, context, comparer: ContextDictionaryComparer.Instance);
        }

        [Fact]
        public async Task Test()
        {
            var markup = @"
public class Sampl$$eClass()
{
}";
            await VerifyGetRenameContextAsync(
                markup,
                @"
{
    ""definition"" : [ ""public class SampleClass()\r\n{\r\n}"" ]
}",
                new SymbolRenameOptions(),
                CancellationToken.None);
        }
    }
}

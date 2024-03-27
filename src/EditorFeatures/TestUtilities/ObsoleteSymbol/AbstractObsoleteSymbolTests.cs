// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ObsoleteSymbol;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ObsoleteSymbol;

[UseExportProvider]
public abstract class AbstractObsoleteSymbolTests
{
    protected abstract EditorTestWorkspace CreateWorkspace(string markup);

    protected async Task TestAsync(string markup)
    {
        using var workspace = CreateWorkspace(markup);

        var project = workspace.CurrentSolution.Projects.Single();
        var language = project.Language;
        var documents = project.Documents.ToImmutableArray();

        for (var i = 0; i < documents.Length; i++)
        {
            var document = documents[i];
            var text = await document.GetTextAsync();

            var service = document.GetRequiredLanguageService<IObsoleteSymbolService>();
            var textSpans = ImmutableArray.Create(new TextSpan(0, text.Length));
            var result = await service.GetLocationsAsync(document, textSpans, CancellationToken.None);

            var expectedSpans = workspace.Documents[i].SelectedSpans.OrderBy(s => s.Start);
            var actualSpans = result.OrderBy(s => s.Start);

            AssertEx.EqualOrDiff(
                string.Join(Environment.NewLine, expectedSpans),
                string.Join(Environment.NewLine, actualSpans));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ReassignedVariable;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ReassignedVariable;

[UseExportProvider]
public abstract class AbstractReassignedVariableTests
{
    protected abstract TestWorkspace CreateWorkspace(string markup);

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

            var service = document.GetRequiredLanguageService<IReassignedVariableService>();
            var result = await service.GetLocationsAsync(document, new TextSpan(0, text.Length), CancellationToken.None);

            var expectedSpans = workspace.Documents[i].SelectedSpans.OrderBy(s => s.Start);
            var actualSpans = result.OrderBy(s => s.Start);

            Assert.Equal(expectedSpans, actualSpans);
        }
    }
}

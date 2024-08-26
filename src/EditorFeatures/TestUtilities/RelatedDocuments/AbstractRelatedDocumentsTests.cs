// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.RelatedDocuments;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.RelatedDocuments;

[UseExportProvider]
public abstract class AbstractRelatedDocumentsTests
{
    protected static async Task TestAsync(string testMarkup, TestHost testHost)
    {
        using var workspace = TestWorkspace.CreateWorkspace(
            XElement.Parse(testMarkup),
            composition: FeaturesTestCompositions.Features.WithTestHostParts(testHost));

        var caretDocument = workspace.Documents.Single(d => d.CursorPosition.HasValue);
        var caretPosition = caretDocument.CursorPosition!.Value;
        var documentId = caretDocument.Id;

        var startingDocument = workspace.CurrentSolution.GetRequiredDocument(documentId);
        var service = startingDocument.GetRequiredLanguageService<IRelatedDocumentsService>();

        var results = new List<DocumentId>();
        await service.GetRelatedDocumentIdsAsync(
            startingDocument, caretPosition, (documentIds, _) =>
            {
                lock (results)
                    results.AddRange(documentIds);

                return ValueTaskFactory.CompletedTask;
            }, CancellationToken.None);

        Assert.True(results.Distinct().Count() == results.Count);

        var actualSortedResults = results.OrderBy(d => d.Id);
        var expectedSortedResults = workspace.Documents.Where(d => d.SelectedSpans.Count > 0).Select(d => d.Id).OrderBy(d => d.Id);

        AssertEx.Equal(expectedSortedResults, actualSortedResults);
    }
}

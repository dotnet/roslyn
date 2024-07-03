// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.ExternalAccess.IntelliCode.Api;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Intents;

public class IntentTestsBase
{
    internal static async Task VerifyIntentMissingAsync(
        string intentName,
        string priorDocumentText,
        string currentDocumentText,
        OptionsCollection? options = null,
        string? intentData = null)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(priorDocumentText, composition: EditorTestCompositions.EditorFeatures);
        var results = await GetIntentsAsync(workspace, intentName, currentDocumentText, options, intentData).ConfigureAwait(false);
        Assert.Empty(results);
    }

    internal static Task VerifyExpectedTextAsync(
        string intentName,
        string priorDocumentText,
        string currentDocumentText,
        string expectedText,
        OptionsCollection? options = null,
        string? intentData = null)
    {
        return VerifyExpectedTextAsync(intentName, priorDocumentText, currentDocumentText, [], [expectedText], options, intentData);
    }

    internal static async Task VerifyExpectedTextAsync(
        string intentName,
        string priorDocumentText,
        string currentDocumentText,
        string[] additionalDocuments,
        string[] expectedTexts,
        OptionsCollection? options = null,
        string? intentData = null)
    {
        // Create the workspace from the prior document + any additional documents.
        var documentSet = additionalDocuments.Prepend(priorDocumentText).ToArray();
        using var workspace = EditorTestWorkspace.CreateCSharp(documentSet, composition: EditorTestCompositions.EditorFeatures);
        var results = await GetIntentsAsync(workspace, intentName, currentDocumentText, options, intentData).ConfigureAwait(false);

        // For now, we're just taking the first result to match intellicode behavior.
        var result = results.First();

        var actualDocumentTexts = new List<string>();
        foreach (var documentChange in result.DocumentChanges)
        {
            // Get the document and open it.  Since we're modifying the text buffer we don't care about linked documents.
            var documentBuffer = workspace.GetTestDocument(documentChange.Key)!.GetTextBuffer();

            using var edit = documentBuffer.CreateEdit();
            foreach (var change in documentChange.Value)
            {
                edit.Replace(change.Span.ToSpan(), change.NewText);
            }

            edit.Apply();

            actualDocumentTexts.Add(documentBuffer.CurrentSnapshot.GetText());
        }

        actualDocumentTexts.Sort();
        Array.Sort(expectedTexts);

        Assert.Equal(expectedTexts.Length, actualDocumentTexts.Count);
        for (var i = 0; i < actualDocumentTexts.Count; i++)
        {
            AssertEx.EqualOrDiff(expectedTexts[i], actualDocumentTexts[i]);
        }
    }

    internal static async Task<ImmutableArray<IntentSource>> GetIntentsAsync(
        EditorTestWorkspace workspace,
        string intentName,
        string currentDocumentText,
        OptionsCollection? options = null,
        string? intentData = null)
    {
        if (options != null)
        {
            workspace.SetAnalyzerFallbackOptions(options);
        }

        var intentSource = workspace.ExportProvider.GetExportedValue<IIntentSourceProvider>();

        // Get the prior test document from the workspace.
        var testDocument = workspace.Documents.Single(d => d.Name == "test1.cs");
        var priorDocument = workspace.CurrentSolution.GetRequiredDocument(testDocument.Id);

        // Extract the prior selection annotated region from the prior document.
        var priorSelection = testDocument.AnnotatedSpans["priorSelection"].Single();

        // Move the test document buffer forward to the current document.
        testDocument.Update(SourceText.From(currentDocumentText));
        var currentTextBuffer = testDocument.GetTextBuffer();

        // Get the text change to pass into the API that rewinds the current document to the prior document.
        var currentDocument = currentTextBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        var textDiffService = workspace.CurrentSolution.Services.GetRequiredService<IDocumentTextDifferencingService>();
        var changes = await textDiffService.GetTextChangesAsync(currentDocument, priorDocument, CancellationToken.None).ConfigureAwait(false);

        // Get the current snapshot span to pass in.
        var currentSnapshot = new SnapshotSpan(currentTextBuffer.CurrentSnapshot, new Span(0, currentTextBuffer.CurrentSnapshot.Length));

        var intentContext = new IntentRequestContext(
            intentName,
            currentSnapshot,
            changes,
            priorSelection,
            intentData: intentData);
        var results = await intentSource.ComputeIntentsAsync(intentContext, CancellationToken.None).ConfigureAwait(false);
        return results;
    }
}

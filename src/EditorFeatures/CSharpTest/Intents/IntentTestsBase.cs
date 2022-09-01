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
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ExternalAccess.IntelliCode.Api;
using Microsoft.CodeAnalysis.Features.Intents;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Intents
{
    public class IntentTestsBase
    {
        internal static Task VerifyExpectedTextAsync(
            string intentName,
            string markup,
            string expectedText,
            OptionsCollection? options = null,
            string? intentData = null,
            string? priorText = null)
        {
            return VerifyExpectedTextAsync(intentName, markup, new string[] { }, new string[] { expectedText }, options, intentData, priorText);
        }

        internal static async Task VerifyExpectedTextAsync(
            string intentName,
            string activeDocument,
            string[] additionalDocuments,
            string[] expectedTexts,
            OptionsCollection? options = null,
            string? intentData = null,
            string? priorText = null)
        {
            var documentSet = additionalDocuments.Prepend(activeDocument).ToArray();
            using var workspace = TestWorkspace.CreateCSharp(documentSet, exportProvider: EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider());
            if (options != null)
            {
                workspace.ApplyOptions(options!);
            }

            var intentSource = workspace.ExportProvider.GetExportedValue<IIntentSourceProvider>();

            // The first document will be the active document.
            var document = workspace.Documents.Single(d => d.Name == "test1.cs");
            var textBuffer = document.GetTextBuffer();

            // Get the text change to rewind the document to the correct pre-intent location.
            var rewindTextChange = new TextChange(document.AnnotatedSpans["typed"].Single(), priorText ?? string.Empty);

            // Get the current snapshot span to pass in.
            var currentSnapshot = new SnapshotSpan(textBuffer.CurrentSnapshot, new Span(0, textBuffer.CurrentSnapshot.Length));

            var priorSelection = TextSpan.FromBounds(rewindTextChange.Span.Start, rewindTextChange.Span.Start);
            if (document.AnnotatedSpans.ContainsKey("priorSelection"))
            {
                priorSelection = document.AnnotatedSpans["priorSelection"].Single();
            }

            var intentContext = new IntentRequestContext(
                intentName,
                currentSnapshot,
                ImmutableArray.Create(rewindTextChange),
                priorSelection,
                intentData: intentData);
            var results = await intentSource.ComputeIntentsAsync(intentContext, CancellationToken.None).ConfigureAwait(false);

            // For now, we're just taking the first result to match intellicode behavior.
            var result = results.First();

            var actualDocumentTexts = new List<string>();
            foreach (var documentChange in result.DocumentChanges)
            {
                // Get the document and open it.  Since we're modifying the text buffer we don't care about linked documents.
                var documentBuffer = workspace.GetTestDocument(documentChange.Key).GetTextBuffer();

                using var edit = documentBuffer.CreateEdit();
                foreach (var change in documentChange.Value)
                {
                    edit.Replace(change.Span.ToSpan(), change.NewText);
                }

                edit.Apply();

                actualDocumentTexts.Add(documentBuffer.CurrentSnapshot.GetText());
            }

            Assert.Equal(expectedTexts.Length, actualDocumentTexts.Count);
            foreach (var expectedText in expectedTexts)
            {
                Assert.True(actualDocumentTexts.Contains(expectedText));
            }
        }
    }
}

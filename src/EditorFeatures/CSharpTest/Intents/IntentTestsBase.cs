// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Intents
{
    public class IntentTestsBase
    {
        internal static Task VerifyExpectedTextAsync(string intentName, string markup, string expectedText, OptionsCollection? options = null, string? intentData = null)
        {
            return VerifyExpectedTextAsync(intentName, markup, new string[] { }, expectedText, options, intentData);
        }

        internal static async Task VerifyExpectedTextAsync(string intentName, string activeDocument, string[] additionalDocuments, string expectedText, OptionsCollection? options = null, string? intentData = null)
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
            var typedSpan = document.AnnotatedSpans["typed"].Single();

            // Get the current snapshot span and selection.
            var currentSelectedSpan = document.SelectedSpans.FirstOrDefault();
            if (currentSelectedSpan.IsEmpty)
            {
                currentSelectedSpan = TextSpan.FromBounds(typedSpan.End, typedSpan.End);
            }

            var currentSnapshotSpan = new SnapshotSpan(textBuffer.CurrentSnapshot, currentSelectedSpan.ToSpan());

            // Determine the edits to rewind to the prior snapshot by removing the changes in the annotated span.
            var rewindTextChange = new TextChange(typedSpan, "");
            var priorSelection = TextSpan.FromBounds(rewindTextChange.Span.Start, rewindTextChange.Span.Start);
            if (document.AnnotatedSpans.ContainsKey("priorSelection"))
            {
                priorSelection = document.AnnotatedSpans["priorSelection"].Single();
            }

            var intentContext = new IntentRequestContext(
                intentName,
                currentSnapshotSpan,
                ImmutableArray.Create(rewindTextChange),
                priorSelection,
                intentData: intentData);
            var results = await intentSource.ComputeIntentsAsync(intentContext, CancellationToken.None).ConfigureAwait(false);

            // For now, we're just taking the first result to match intellicode behavior.
            var result = results.First();

            using var edit = textBuffer.CreateEdit();
            foreach (var change in result.TextChanges)
            {
                edit.Replace(change.Span.ToSpan(), change.NewText);
            }

            edit.Apply();

            Assert.Equal(expectedText, textBuffer.CurrentSnapshot.GetText());
        }
    }
}

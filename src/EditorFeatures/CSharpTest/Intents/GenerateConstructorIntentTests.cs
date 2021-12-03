// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
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
    [UseExportProvider]
    public class GenerateConstructorIntentTests
    {
        [Fact]
        public async Task GenerateConstructorSimpleResult()
        {
            var initialText =
@"class C
{
    private readonly int _someInt;

    {|typed:public C|}
}";
            var expectedText =
@"class C
{
    private readonly int _someInt;

    public C(int someInt)
    {
        _someInt = someInt;
    }
}";

            await VerifyExpectedTextAsync(initialText, expectedText).ConfigureAwait(false);
        }

        [Fact]
        public async Task GenerateConstructorTypedPrivate()
        {
            var initialText =
@"class C
{
    private readonly int _someInt;

    {|typed:private C|}
}";
            var expectedText =
@"class C
{
    private readonly int _someInt;

    public C(int someInt)
    {
        _someInt = someInt;
    }
}";

            await VerifyExpectedTextAsync(initialText, expectedText).ConfigureAwait(false);
        }

        [Fact]
        public async Task GenerateConstructorWithFieldsInPartial()
        {
            var initialText =
@"partial class C
{
    {|typed:public C|}
}";
            var additionalDocuments = new string[]
            {
@"partial class C
{
    private readonly int _someInt;
}"
            };
            var expectedText =
@"partial class C
{
    public C(int someInt)
    {
        _someInt = someInt;
    }
}";

            await VerifyExpectedTextAsync(initialText, additionalDocuments, expectedText).ConfigureAwait(false);
        }

        [Fact]
        public async Task GenerateConstructorWithReferenceType()
        {
            var initialText =
@"class C
{
    private readonly object _someObject;

    {|typed:public C|}
}";
            var expectedText =
@"class C
{
    private readonly object _someObject;

    public C(object someObject)
    {
        _someObject = someObject;
    }
}";

            await VerifyExpectedTextAsync(initialText, expectedText).ConfigureAwait(false);
        }

        [Fact]
        public async Task GenerateConstructorWithExpressionBodyOption()
        {
            var initialText =
@"class C
{
    private readonly int _someInt;

    {|typed:public C|}
}";
            var expectedText =
@"class C
{
    private readonly int _someInt;

    public C(int someInt) => _someInt = someInt;
}";

            await VerifyExpectedTextAsync(initialText, expectedText,
                options: new OptionsCollection(LanguageNames.CSharp)
                {
                    { CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement }
                }).ConfigureAwait(false);
        }

        private static Task VerifyExpectedTextAsync(string markup, string expectedText, OptionsCollection? options = null)
        {
            return VerifyExpectedTextAsync(markup, new string[] { }, expectedText, options);
        }

        private static async Task VerifyExpectedTextAsync(string activeDocument, string[] additionalDocuments, string expectedText, OptionsCollection? options = null)
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
            var annotatedSpan = document.AnnotatedSpans["typed"].Single();

            // Get the current snapshot span and selection.
            var currentSelectedSpan = document.SelectedSpans.FirstOrDefault();
            if (currentSelectedSpan.IsEmpty)
            {
                currentSelectedSpan = TextSpan.FromBounds(annotatedSpan.End, annotatedSpan.End);
            }

            var currentSnapshotSpan = new SnapshotSpan(textBuffer.CurrentSnapshot, currentSelectedSpan.ToSpan());

            // Determine the edits to rewind to the prior snapshot by removing the changes in the annotated span.
            var rewindTextChange = new TextChange(annotatedSpan, "");

            var intentContext = new IntentRequestContext(
                WellKnownIntents.GenerateConstructor,
                currentSnapshotSpan,
                ImmutableArray.Create(rewindTextChange),
                TextSpan.FromBounds(rewindTextChange.Span.Start, rewindTextChange.Span.Start),
                intentData: null);
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

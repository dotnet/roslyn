// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.Intents;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Features.Intents;
using Microsoft.CodeAnalysis.Options;
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

    $$
}";
            var typedText = "public C";
            var expectedText =
@"class C
{
    private readonly int _someInt;

    public C(int someInt)
    {
        _someInt = someInt;
    }
}";

            await VerifyExpectedTextAsync(initialText, typedText, expectedText).ConfigureAwait(false);
        }

        [Fact]
        public async Task GenerateConstructorTypedPrivate()
        {
            var initialText =
@"class C
{
    private readonly int _someInt;

    $$
}";
            var typedText = "private C";
            var expectedText =
@"class C
{
    private readonly int _someInt;

    public C(int someInt)
    {
        _someInt = someInt;
    }
}";

            await VerifyExpectedTextAsync(initialText, typedText, expectedText).ConfigureAwait(false);
        }

        [Fact]
        public async Task GenerateConstructorWithFieldsInPartial()
        {
            var initialText =
@"partial class C
{
    $$
}";
            var additionalDocuments = new string[]
            {
@"partial class C
{
    private readonly int _someInt;
}"
            };
            var typedText = "public C";
            var expectedText =
@"partial class C
{
    public C(int someInt)
    {
        _someInt = someInt;
    }
}";

            await VerifyExpectedTextAsync(initialText, additionalDocuments, typedText, expectedText).ConfigureAwait(false);
        }

        [Fact]
        public async Task GenerateConstructorWithReferenceType()
        {
            var initialText =
@"class C
{
    private readonly object _someObject;

    $$
}";
            var typedText = "public C";
            var expectedText =
@"class C
{
    private readonly object _someObject;

    public C(object someObject)
    {
        _someObject = someObject;
    }
}";

            await VerifyExpectedTextAsync(initialText, typedText, expectedText).ConfigureAwait(false);
        }

        [Fact]
        public async Task GenerateConstructorWithExpressionBodyOption()
        {
            var initialText =
@"class C
{
    private readonly int _someInt;

    $$
}";
            var typedText = "public C";
            var expectedText =
@"class C
{
    private readonly int _someInt;

    public C(int someInt) => _someInt = someInt;
}";

            await VerifyExpectedTextAsync(initialText, typedText, expectedText,
                options: new OptionsCollection(LanguageNames.CSharp)
                {
                    { CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement }
                }).ConfigureAwait(false);
        }

        private static Task VerifyExpectedTextAsync(string markupBeforeIntent, string typedText, string expectedText, OptionsCollection? options = null)
        {
            return VerifyExpectedTextAsync(markupBeforeIntent, new string[] { }, typedText, expectedText, options);
        }

        private static async Task VerifyExpectedTextAsync(string activeDocument, string[] additionalDocuments, string typedText, string expectedText, OptionsCollection? options = null)
        {
            var documentSet = additionalDocuments.Prepend(activeDocument).ToArray();
            using var workspace = TestWorkspace.CreateCSharp(documentSet, exportProvider: EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider());
            if (options != null)
            {
                workspace.ApplyOptions(options!);
            }

            var intentSource = workspace.ExportProvider.GetExportedValue<IIntentProcessor>();

            // The first document will be the active document.
            var document = workspace.Documents.Single(d => d.Name == "test1.cs");
            var selectedSpan = document.SelectedSpans.FirstOrDefault();
            if (selectedSpan.IsEmpty)
            {
                selectedSpan = TextSpan.FromBounds(document.CursorPosition!.Value, document.CursorPosition.Value);
            }

            var textBuffer = document.GetTextBuffer();
            var initialSnapshotSpan = new SnapshotSpan(textBuffer.CurrentSnapshot, selectedSpan.ToSpan());
            var snapshotSpanAfterTyping = new SnapshotSpan(textBuffer.Replace(selectedSpan.ToSpan(), typedText), new Span(selectedSpan.Start, typedText.Length));

            var intentContext = new IntentRequestContext(WellKnownIntents.GenerateConstructor, initialSnapshotSpan, snapshotSpanAfterTyping, intentData: null);
            var result = await intentSource.ComputeEditsAsync(intentContext, CancellationToken.None).ConfigureAwait(false);

            using var edit = textBuffer.CreateEdit();
            foreach (var change in result.Value.TextChanges)
            {
                edit.Replace(change.Span.ToSpan(), change.NewText);
            }
            edit.Apply();

            Assert.Equal(expectedText, textBuffer.CurrentSnapshot.GetText());
        }
    }
}

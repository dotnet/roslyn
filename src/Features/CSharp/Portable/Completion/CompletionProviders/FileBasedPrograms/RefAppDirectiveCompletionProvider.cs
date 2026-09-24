// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(RefAppDirectiveCompletionProvider), LanguageNames.CSharp), Shared]
[ExtensionOrder(After = nameof(IncludeAppDirectiveCompletionProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RefAppDirectiveCompletionProvider() : AbstractAppDirectiveCompletionProvider
{
    protected override string DirectiveKind => "ref";

    protected sealed override void AddDirectiveKindCompletion(CompletionContext context)
    {
        context.AddItem(CommonCompletionItem.Create(DirectiveKind, displayTextSuffix: "", CompletionItemRules.Default, glyph: Glyph.Keyword,
            description: [
                new(SymbolDisplayPartKind.Keyword, symbol: null, "#:ref"),
                new(SymbolDisplayPartKind.Space, symbol: null, " "),
                new(SymbolDisplayPartKind.StringLiteral, symbol: null, CSharpFeaturesResources.Ref_directive_file_path),
                new(SymbolDisplayPartKind.LineBreak, symbol: null, ""),
                new(SymbolDisplayPartKind.Text, symbol: null, CSharpFeaturesResources.Adds_a_file_based_app_reference),
            ]));
    }

    protected override async Task AddDirectiveContentCompletionsAsync(CompletionContext context, ReadOnlyMemory<char> contentPrefix)
    {
        // Suppose we have a directive '#:ref path/to/fi$$'
        // In this case, 'contentPrefix' is 'path/to/fi'.

        var documentDirectory = PathUtilities.GetDirectoryName(context.Document.FilePath);
        var baseDirectory = PathUtilities.IsAbsolute(documentDirectory) ? documentDirectory : null;
        var fileSystemHelper = new FileSystemCompletionHelper(
            Glyph.OpenFolder,
            Glyph.CSharpFile,
            searchPaths: [],
            baseDirectory,
            allowableExtensions: [".cs"],
            CompletionItemRules.Default);

        var contentDirectory = PathUtilities.GetDirectoryName(contentPrefix.ToString());
        var items = await fileSystemHelper.GetItemsAsync(contentDirectory, context.CancellationToken).ConfigureAwait(false);
        context.AddItems(items);
    }
}

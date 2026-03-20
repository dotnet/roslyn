// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(IncludeAppDirectiveCompletionProvider), LanguageNames.CSharp), Shared]
[ExtensionOrder(After = nameof(ProjectAppDirectiveCompletionProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class IncludeAppDirectiveCompletionProvider() : AbstractAppDirectiveCompletionProvider
{
    protected override string DirectiveKind => "include";

    protected sealed override void AddDirectiveKindCompletion(CompletionContext context)
    {
        context.AddItem(CommonCompletionItem.Create(DirectiveKind, displayTextSuffix: "", CompletionItemRules.Default, glyph: Glyph.Keyword,
            description: [
                new(SymbolDisplayPartKind.Keyword, symbol: null, "#:include"),
                new(SymbolDisplayPartKind.Space, symbol: null, " "),
                new(SymbolDisplayPartKind.StringLiteral, symbol: null, CSharpFeaturesResources.Include_directive_file_path),
                new(SymbolDisplayPartKind.LineBreak, symbol: null, ""),
                new(SymbolDisplayPartKind.Text, symbol: null, CSharpFeaturesResources.Adds_a_file_reference),
            ]));
    }

    protected override async Task AddDirectiveContentCompletionsAsync(CompletionContext context, ReadOnlyMemory<char> contentPrefix)
    {
        // Suppose we have a directive '#:include path/to/fi$$'
        // In this case, 'contentPrefix' is 'path/to/fi'.

        var documentDirectory = PathUtilities.GetDirectoryName(context.Document.FilePath);
        var baseDirectory = PathUtilities.IsAbsolute(documentDirectory) ? documentDirectory : null;
        var fileSystemHelper = new FileSystemCompletionHelper(
            Glyph.OpenFolder,
            Glyph.CSharpFile,
            searchPaths: [],
            baseDirectory,
            // Note: in the future, we may wish to use '<FileBasedProgramsItemMapping>' property
            // as a hint for which file extensions to show in this completion.
            // For now, we just allow any extension, and if user chooses a file with invalid extension, they'll just get a build error.
            allowableExtensions: [],
            CompletionItemRules.Default);

        var contentDirectory = PathUtilities.GetDirectoryName(contentPrefix.ToString());
        var items = await fileSystemHelper.GetItemsAsync(contentDirectory, context.CancellationToken).ConfigureAwait(false);
        context.AddItems(items);
        if (baseDirectory != null || PathUtilities.IsAbsolute(contentDirectory))
        {
            addItem("*.cs");
            addItem("**/*.cs");
        }

        void addItem(string text)
        {
            context.AddItem(CommonCompletionItem.Create(
                text,
                displayTextSuffix: "",
                glyph: Glyph.CSharpFile,
                description: text.ToSymbolDisplayParts(),
                rules: CompletionItemRules.Default));
        }
    }
}

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

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(ProjectAppDirectiveCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(PackageAppDirectiveCompletionProvider))]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ProjectAppDirectiveCompletionProvider() : AbstractAppDirectiveCompletionProvider
{
    protected override string DirectiveKind => "project";

    protected sealed override void AddDirectiveKindCompletion(CompletionContext context)
    {
        context.AddItem(CommonCompletionItem.Create(DirectiveKind, displayTextSuffix: "", CompletionItemRules.Default, glyph: Glyph.Keyword,
            description: [
                new(SymbolDisplayPartKind.Keyword, symbol: null, "#:project"),
                new(SymbolDisplayPartKind.Space, symbol: null, " "),
                new(SymbolDisplayPartKind.StringLiteral, symbol: null, CSharpFeaturesResources.Project_directive_file_path),
                new(SymbolDisplayPartKind.LineBreak, symbol: null, ""),
                new(SymbolDisplayPartKind.Text, symbol: null, CSharpFeaturesResources.Adds_a_project_reference),
                ]));
    }

    protected override async Task AddDirectiveContentCompletionsAsync(CompletionContext context, ReadOnlyMemory<char> contentPrefix)
    {
        // Suppose we have a directive '#:project path/to/pr$$'
        // In this case, 'contentPrefix' is 'path/to/pr'.

        var fileSystemHelper = new FileSystemCompletionHelper(
            Glyph.OpenFolder,
            Glyph.CSharpProject,
            searchPaths: [],
            baseDirectory: PathUtilities.GetDirectoryName(context.Document.FilePath),
            [".csproj", ".vbproj"],
            CompletionItemRules.Default);

        var contentDirectory = PathUtilities.GetDirectoryName(contentPrefix.ToString());
        var items = await fileSystemHelper.GetItemsAsync(contentDirectory, context.CancellationToken).ConfigureAwait(false);
        context.AddItems(items);
    }
}

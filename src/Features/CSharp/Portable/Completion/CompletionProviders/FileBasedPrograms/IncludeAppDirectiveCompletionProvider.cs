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

[ExportCompletionProvider(nameof(IncludeAppDirectiveCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(ProjectAppDirectiveCompletionProvider))]
[Shared]
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
                new(SymbolDisplayPartKind.Text, symbol: null, CSharpFeaturesResources.Adds_a_project_reference),
                ]));
    }

    protected override async Task AddDirectiveContentCompletionsAsync(CompletionContext context, ReadOnlyMemory<char> contentPrefix)
    {
        // Suppose we have a directive '#:include path/to/fi$$'
        // In this case, 'contentPrefix' is 'path/to/fi'.

        // If 'FileBasedProgramsItemMapping' is not specified, or it is empty (corner case), allow any extension.
        ImmutableArray<string> allowableExtensions = [];

        // TODO2: thread this property through
        var globalOptions = context.Document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions;
        if (globalOptions.TryGetValue("build_property.FileBasedProgramsItemMapping", out var mappingString)
            && !string.IsNullOrEmpty(mappingString))
        {
            using var builder = TemporaryArray<string>.Empty;
            // example value: ".cs=Compile;.resx=EmbeddedResource;.json=None;.razor=Content"
            for (var entryIndex = 0; ;)
            {
                // scan for permitted extensions
                var equalsIndex = mappingString.IndexOf('=', startIndex: entryIndex);
                if (equalsIndex == -1)
                    break;

                builder.Add(mappingString[entryIndex..equalsIndex]);

                // No more entries
                var semicolonIndex = mappingString.IndexOf(';', startIndex: equalsIndex);
                if (semicolonIndex == -1)
                    break;

                entryIndex = semicolonIndex + 1;
            }

            allowableExtensions = builder.ToImmutableAndClear();
        }

        var documentDirectory = PathUtilities.GetDirectoryName(context.Document.FilePath);
        var fileSystemHelper = new FileSystemCompletionHelper(
            Glyph.OpenFolder,
            Glyph.CSharpFile,
            searchPaths: [],
            baseDirectory: PathUtilities.IsAbsolute(documentDirectory) ? documentDirectory : null,
            allowableExtensions,
            CompletionItemRules.Default);

        var contentDirectory = PathUtilities.GetDirectoryName(contentPrefix.ToString());
        var items = await fileSystemHelper.GetItemsAsync(contentDirectory, context.CancellationToken).ConfigureAwait(false);
        context.AddItems(items);
    }
}

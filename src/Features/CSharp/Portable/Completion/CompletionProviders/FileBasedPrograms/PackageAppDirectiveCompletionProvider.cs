// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(PackageAppDirectiveCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(PropertyAppDirectiveCompletionProvider))]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class PackageAppDirectiveCompletionProvider() : AbstractAppDirectiveCompletionProvider
{
    protected override string DirectiveKind => "package";

    protected sealed override void AddDirectiveKindCompletion(CompletionContext context)
    {
        context.AddItem(CommonCompletionItem.Create(DirectiveKind, displayTextSuffix: "", CompletionItemRules.Default, glyph: Glyph.Keyword,
            description: [
                new(SymbolDisplayPartKind.Keyword, symbol: null, "#:package"),
                new(SymbolDisplayPartKind.Space, symbol: null, " "),
                new(SymbolDisplayPartKind.StringLiteral, symbol: null, CSharpFeaturesResources.Package_name),
                new(SymbolDisplayPartKind.StringLiteral, symbol: null, "@"),
                new(SymbolDisplayPartKind.StringLiteral, symbol: null, CSharpFeaturesResources.Package_version),
                new(SymbolDisplayPartKind.LineBreak, symbol: null, ""),
                new(SymbolDisplayPartKind.Text, symbol: null, CSharpFeaturesResources.Adds_a_NuGet_package_reference),
                ]));
    }

    protected override Task AddDirectiveContentCompletionsAsync(CompletionContext context, ReadOnlyMemory<char> contentPrefix)
        => Task.CompletedTask;
}

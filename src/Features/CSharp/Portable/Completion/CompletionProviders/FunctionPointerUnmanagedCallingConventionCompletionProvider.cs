// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(FunctionPointerUnmanagedCallingConventionCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(AggregateEmbeddedLanguageCompletionProvider))]
[Shared]
internal partial class FunctionPointerUnmanagedCallingConventionCompletionProvider : LSPCompletionProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FunctionPointerUnmanagedCallingConventionCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.CSharp;

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
        => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);

    public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharacters;

    private static readonly ImmutableArray<string> s_predefinedCallingConventions = ["Cdecl", "Fastcall", "Thiscall", "Stdcall"];

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        try
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxTree.IsInNonUserCode(position, cancellationToken))
            {
                return;
            }

            var token = syntaxTree
                .FindTokenOnLeftOfPosition(position, cancellationToken)
                .GetPreviousTokenIfTouchingWord(position);

            if (token.Kind() is not (SyntaxKind.OpenBracketToken or SyntaxKind.CommaToken))
            {
                return;
            }

            if (token.Parent is not FunctionPointerUnmanagedCallingConventionListSyntax callingConventionList)
            {
                return;
            }

            var contextPosition = token.SpanStart;
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(callingConventionList, cancellationToken).ConfigureAwait(false);

            var completionItems = new HashSet<CompletionItem>(CompletionItemComparer.Instance);
            AddTypes(completionItems, contextPosition, semanticModel, cancellationToken);

            // Even if we didn't have types, there are four magic calling conventions recognized regardless.
            // We add these after doing the type lookup so if we had types we can show that instead
            foreach (var callingConvention in s_predefinedCallingConventions)
            {
                completionItems.Add(CompletionItem.Create(callingConvention, tags: GlyphTags.GetTags(Glyph.Keyword)));
            }

            context.AddItems(completionItems);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, ErrorSeverity.General))
        {
            // nop
        }
    }

    private static void AddTypes(HashSet<CompletionItem> completionItems, int contextPosition, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        // We have to find the set of types that meet the criteria listed in
        // https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/function-pointers.md#mapping-the-calling_convention_specifier-to-a-callkind
        // We skip the check of an type being in the core assembly since that's not really necessary for our work.
        var compilerServicesNamespace = semanticModel.Compilation.GlobalNamespace.GetQualifiedNamespace("System.Runtime.CompilerServices");
        if (compilerServicesNamespace == null)
        {
            return;
        }

        foreach (var type in compilerServicesNamespace.GetTypeMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            const string CallConvPrefix = "CallConv";

            if (type.DeclaredAccessibility == Accessibility.Public && type.Name.StartsWith(CallConvPrefix))
            {
                var displayName = type.Name[CallConvPrefix.Length..];
                completionItems.Add(
                    SymbolCompletionItem.CreateWithSymbolId(
                        displayName,
                        ImmutableArray.Create(type),
                        rules: CompletionItemRules.Default,
                        contextPosition));
            }
        }
    }

    internal override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
        => SymbolCompletionItem.GetDescriptionAsync(item, document, displayOptions, cancellationToken);

    private class CompletionItemComparer : IEqualityComparer<CompletionItem>
    {
        public static readonly IEqualityComparer<CompletionItem> Instance = new CompletionItemComparer();

        public bool Equals(CompletionItem? x, CompletionItem? y)
        {
            return x?.DisplayText == y?.DisplayText;
        }

        public int GetHashCode(CompletionItem obj)
        {
            return obj?.DisplayText.GetHashCode() ?? 0;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(ExternAliasCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(SnippetCompletionProvider))]
[Shared]
internal class ExternAliasCompletionProvider : LSPCompletionProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ExternAliasCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.CSharp;

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
        => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);

    public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharacters;

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        try
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            if (tree.IsInNonUserCode(position, cancellationToken))
            {
                return;
            }

            var targetToken = tree
                .FindTokenOnLeftOfPosition(position, cancellationToken)
                .GetPreviousTokenIfTouchingWord(position);

            if (!targetToken.IsKind(SyntaxKind.AliasKeyword)
                && !(targetToken.IsKind(SyntaxKind.IdentifierToken) && targetToken.HasMatchingText(SyntaxKind.AliasKeyword)))
            {
                return;
            }

            if (targetToken.Parent.IsKind(SyntaxKind.ExternAliasDirective)
                || (targetToken.Parent.IsKind(SyntaxKind.IdentifierName) && targetToken.Parent.IsParentKind(SyntaxKind.IncompleteMember)))
            {
                var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var aliases = compilation.ExternalReferences.SelectMany(r => r.Properties.Aliases).ToSet();

                if (aliases.Any())
                {
                    var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    var usedAliases = root.ChildNodes().OfType<ExternAliasDirectiveSyntax>()
                        .Where(e => !e.Identifier.IsMissing)
                        .Select(e => e.Identifier.ValueText);

                    aliases.RemoveRange(usedAliases);
                    aliases.Remove(MetadataReferenceProperties.GlobalAlias);

                    var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

                    foreach (var alias in aliases)
                    {
                        context.AddItem(CommonCompletionItem.Create(
                            alias, displayTextSuffix: "", CompletionItemRules.Default, glyph: Glyph.Namespace));
                    }
                }
            }
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, ErrorSeverity.General))
        {
            // nop
        }
    }
}

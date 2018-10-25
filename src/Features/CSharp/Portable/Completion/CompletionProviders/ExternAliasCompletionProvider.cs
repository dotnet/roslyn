// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class ExternAliasCompletionProvider : CommonCompletionProvider
    {
        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            try
            {
                var document = context.Document;
                var position = context.Position;
                var cancellationToken = context.CancellationToken;

                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                if (tree.IsInNonUserCode(position, cancellationToken))
                {
                    return;
                }

                var targetToken = tree
                    .FindTokenOnLeftOfPosition(position, cancellationToken)
                    .GetPreviousTokenIfTouchingWord(position);

                if (targetToken.IsKind(SyntaxKind.AliasKeyword) && targetToken.Parent.IsKind(SyntaxKind.ExternAliasDirective))
                {
                    var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var aliases = compilation.ExternalReferences.SelectMany(r => r.Properties.Aliases).ToSet();

                    if (aliases.Any())
                    {
                        var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                        var usedAliases = root.ChildNodes().OfType<ExternAliasDirectiveSyntax>()
                            .Where(e => !e.Identifier.IsMissing)
                            .Select(e => e.Identifier.ValueText);

                        aliases.RemoveRange(usedAliases);
                        aliases.Remove(MetadataReferenceProperties.GlobalAlias);

                        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                        foreach (var alias in aliases)
                        {
                            context.AddItem(CommonCompletionItem.Create(
                                alias, displayTextSuffix: "", CompletionItemRules.Default, glyph: Glyph.Namespace));
                        }
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                // nop
            }
        }
    }
}

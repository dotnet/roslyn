// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class ExternAliasCompletionProvider : AbstractCompletionProvider
    {
        public override bool IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            return CompletionUtilities.IsCommitCharacter(completionItem, ch, textTypedSoFar);
        }

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        public override bool SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar)
        {
            return CompletionUtilities.SendEnterThroughToEditor(completionItem, textTypedSoFar);
        }

        protected override async Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            if (tree.IsInNonUserCode(position, cancellationToken))
            {
                return null;
            }

            var targetToken = tree.FindTokenOnLeftOfPosition(position, cancellationToken).GetPreviousTokenIfTouchingWord(position);
            if (targetToken.IsKind(SyntaxKind.AliasKeyword) && targetToken.Parent.IsKind(SyntaxKind.ExternAliasDirective))
            {
                var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var aliases = compilation.ExternalReferences.SelectMany(r => r.Properties.Aliases).ToSet();

                if (aliases.Any())
                {
                    var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    var usedAliases = root.ChildNodes().OfType<ExternAliasDirectiveSyntax>().Where(e => !e.Identifier.IsMissing).Select(e => e.Identifier.ValueText);
                    aliases.RemoveRange(usedAliases);
                    aliases.Remove(MetadataReferenceProperties.GlobalAlias);
                    var textChangeSpan = CompletionUtilities.GetTextChangeSpan(await document.GetTextAsync(cancellationToken).ConfigureAwait(false), position);
                    return aliases.Select(e =>
                        new CompletionItem(this, e, textChangeSpan, glyph: Glyph.Namespace));
                }
            }

            return SpecializedCollections.EmptyEnumerable<CompletionItem>();
        }
    }
}

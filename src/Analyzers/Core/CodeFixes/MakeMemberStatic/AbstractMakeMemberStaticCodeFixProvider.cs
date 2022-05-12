// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.OrderModifiers;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MakeMemberStatic
{
    internal abstract class AbstractMakeMemberStaticCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private readonly ISyntaxFacts _syntaxFacts;
        private readonly Option2<CodeStyleOption2<string>> _option;
        private readonly AbstractOrderModifiersHelpers _helpers;

        protected AbstractMakeMemberStaticCodeFixProvider(
            ISyntaxFacts syntaxFacts,
            Option2<CodeStyleOption2<string>> option,
            AbstractOrderModifiersHelpers helpers)
        {
            _syntaxFacts = syntaxFacts;
            _option = option;
            _helpers = helpers;
        }

        protected abstract SyntaxToken StaticModifier { get; }

        protected abstract bool TryGetMemberDeclaration(SyntaxNode node, [NotNullWhen(true)] out SyntaxNode? memberDeclaration);

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (context.Diagnostics.Length == 1 &&
                TryGetMemberDeclaration(context.Diagnostics[0].Location.FindNode(context.CancellationToken), out _))
            {
                RegisterCodeFix(context, CodeFixesResources.Make_member_static, nameof(AbstractMakeMemberStaticCodeFixProvider));
            }

            return Task.CompletedTask;
        }

        protected sealed override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor,
            CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var option = document.Project.AnalyzerOptions.GetOption(_option, tree, cancellationToken);

            if (!_helpers.TryGetOrComputePreferredOrder(option.Value, out var preferredOrder))
            {
                return;
            }

            for (var i = 0; i < diagnostics.Length; i++)
            {
                var declaration = diagnostics[i].Location.FindNode(cancellationToken);

                if (TryGetMemberDeclaration(declaration, out var memberDeclaration))
                {
                    var modifiers = _syntaxFacts.GetModifiers(memberDeclaration).Add(StaticModifier);
                    if (!AbstractOrderModifiersHelpers.IsOrdered(preferredOrder, modifiers))
                    {
                        modifiers = new SyntaxTokenList(modifiers.OrderBy(CompareModifiers)
                            .Select((token, index) => token.WithTriviaFrom(modifiers[index])));
                    }

                    var newNode = _syntaxFacts.WithModifiers(memberDeclaration, modifiers);
                    editor.ReplaceNode(declaration, newNode);
                }
            }

            return;

            // Local functions

            int CompareModifiers(SyntaxToken t1, SyntaxToken t2)
                => GetOrder(t1) - GetOrder(t2);

            int GetOrder(SyntaxToken token)
                => preferredOrder.TryGetValue(token.RawKind, out var value) ? value : int.MaxValue;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MakeMemberStatic
{
    internal abstract class AbstractMakeMemberStaticCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private readonly ISyntaxFacts _syntaxFacts;
        private readonly Option2<CodeStyleOption2<string>> _option;
        private static Tuple<string, string[]>? s_lastParsed;

        protected AbstractMakeMemberStaticCodeFixProvider(
            ISyntaxFacts syntaxFacts,
            Option2<CodeStyleOption2<string>> option)
        {
            _syntaxFacts = syntaxFacts;
            _option = option;
        }

        protected abstract SyntaxToken PartialModifier { get; }

        protected abstract SyntaxToken StaticModifier { get; }

        protected abstract int GetKeywordRawKind(string trimmed);

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

        private static string[] Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            var lastParsed = Volatile.Read(ref s_lastParsed);
            if (lastParsed?.Item1 != value)
            {
                var split = value.Split(',').Select(m => m.Trim()).ToArray();

                lastParsed = Tuple.Create(value, split);
                Volatile.Write(ref s_lastParsed, lastParsed);
            }

            return lastParsed.Item2;
        }

        private SyntaxTokenList InsertStaticModifier(SyntaxTokenList modifiers, string preferredOrder)
        {
            if (modifiers.Count == 0)
            {
                return new SyntaxTokenList(StaticModifier);
            }

            var order = Parse(preferredOrder);
            var staticIndex = Array.IndexOf(order, "static");

            if (staticIndex == -1)
            {
                var lastModifier = modifiers[^1];
                if (lastModifier.RawKind == PartialModifier.RawKind)
                {
                    // If last modifier is partial then place static before it
                    return modifiers.Replace(lastModifier, StaticModifier).Add(lastModifier);
                }

                return modifiers.Add(StaticModifier);
            }

            for (var i = staticIndex - 1; i >= 0; i--)
            {
                var rawKind = GetKeywordRawKind(order[i]);
                if (modifiers.Any(m => m.RawKind == rawKind))
                {
                    using var _ = ArrayBuilder<SyntaxToken>.GetInstance(modifiers.Count + 1, out var keywords);

                    for (i = 0; i < modifiers.Count; i++)
                    {
                        keywords.Add(modifiers[i]);

                        if (modifiers[i].RawKind == rawKind)
                        {
                            keywords.Add(StaticModifier);
                        }
                    }

                    return new SyntaxTokenList(keywords);
                }
            }

            // When the static modifier is added at the beginning of the modifiers it needs to
            // get the leading trivia from the previous first modifier
            return new SyntaxTokenList(StaticModifier.WithLeadingTrivia(modifiers[0].LeadingTrivia),
                modifiers[0].WithoutLeadingTrivia()).AddRange(modifiers.Skip(1));
        }

        protected sealed override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor,
            CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var option = document.Project.AnalyzerOptions.GetOption(_option, tree, cancellationToken);

            for (var i = 0; i < diagnostics.Length; i++)
            {
                var declaration = diagnostics[i].Location.FindNode(cancellationToken);

                if (TryGetMemberDeclaration(declaration, out var memberDeclaration))
                {
                    var modifiers = _syntaxFacts.GetModifiers(memberDeclaration);

                    modifiers = InsertStaticModifier(modifiers, option.Value);

                    var newNode = _syntaxFacts.WithModifiers(memberDeclaration, modifiers);
                    editor.ReplaceNode(declaration, newNode);
                }
            }
        }
    }
}

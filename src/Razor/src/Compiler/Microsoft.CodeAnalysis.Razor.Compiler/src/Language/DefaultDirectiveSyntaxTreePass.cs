// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultDirectiveSyntaxTreePass : RazorEngineFeatureBase, IRazorSyntaxTreePass
{
    public int Order => 75;

    public RazorSyntaxTree Execute(
        RazorCodeDocument codeDocument,
        RazorSyntaxTree syntaxTree,
        CancellationToken cancellationToken = default)
    {
        if (codeDocument.FileKind.IsComponent())
        {
            // Nothing to do here.
            return syntaxTree;
        }

        var sectionVerifier = new NestedSectionVerifier(syntaxTree, cancellationToken);
        return sectionVerifier.Verify();
    }

    private sealed class NestedSectionVerifier(RazorSyntaxTree syntaxTree, CancellationToken cancellationToken) : SyntaxRewriter
    {
        private readonly RazorSyntaxTree _syntaxTree = syntaxTree;
        private readonly CancellationToken _cancellationToken = cancellationToken;

        private ImmutableArray<RazorDiagnostic>.Builder? _diagnostics;
        private int _nestedLevel;

        public RazorSyntaxTree Verify()
        {
            var root = Visit(_syntaxTree.Root);
            var diagnostics = _diagnostics?.ToImmutableAndClear() ?? _syntaxTree.Diagnostics;

            return new RazorSyntaxTree(root, _syntaxTree.Source, diagnostics, _syntaxTree.Options);
        }

        [return: NotNullIfNotNull(nameof(node))]
        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return base.Visit(node);
            }
            catch (InsufficientExecutionStackException)
            {
                // We're very close to reaching the stack limit. Let's not go any deeper.
                // It's okay to not show nested section errors in deeply nested cases instead of crashing.
                _diagnostics ??= _syntaxTree.Diagnostics.ToBuilder();
                _diagnostics.Add(RazorDiagnosticFactory.CreateRewriter_InsufficientStack());

                return node;
            }
        }

        public override SyntaxNode VisitRazorDirective(RazorDirectiveSyntax node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (!node.IsDirective(SectionDirective.Directive))
            {
                // We only want to track the nesting of section directives.
                return base.VisitRazorDirective(node);
            }

            _nestedLevel++;
            var result = (RazorDirectiveSyntax)base.VisitRazorDirective(node);

            if (_nestedLevel > 1)
            {
                var directiveStart = node.Transition.GetSourceLocation(_syntaxTree.Source);
                var errorLength = /* @ */ 1 + SectionDirective.Directive.Directive.Length;
                var error = RazorDiagnosticFactory.CreateParsing_SectionsCannotBeNested(new SourceSpan(directiveStart, errorLength));
                result = result.AppendDiagnostic(error);
            }

            _nestedLevel--;

            return result;
        }
    }
}

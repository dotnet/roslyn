// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// <Metalama>

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Metalama.Compiler;
using Metalama.Compiler.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Test.Utilities
{
    public static class MetalamaCompilerTest
    {
        private static readonly AsyncLocal<bool> shouldExecuteTransformer = new();

        public static bool ShouldExecuteTransformer
        {
            get => shouldExecuteTransformer.Value;
            set => shouldExecuteTransformer.Value = value;
        }

        public static Compilation ExecuteTransformer(Compilation compilation, ISourceTransformer transformer)
        {
            var transformers = ImmutableArray.Create(transformer);
            var diagnostics = new DiagnosticBag();

            var transformersResult = CSharpCompiler.RunTransformers(
                compilation, transformers, null, CompilerAnalyzerConfigOptionsProvider.Empty, null, diagnostics,
                ImmutableArray<ResourceDescription>.Empty, null!, null,
                CancellationToken.None);

            diagnostics.ToReadOnlyAndFree().Verify();

            return transformersResult.TransformedCompilation;
        }

        public class TokenPerLineTransformer : ISourceTransformer
        {
            public void Execute(TransformerContext context)
            {
                var rewriter = new Rewriter();

                var compilation = context.Compilation;
                foreach (var tree in compilation.SyntaxTrees)
                {
                    var root = tree.GetRoot();
                    if (root is CSharpSyntaxNode)
                    {
                        context.ReplaceSyntaxTree(tree, tree.WithRootAndOptions(rewriter.Visit(root), tree.Options));
                    }
                }

            }

            private class Rewriter : CSharpSyntaxRewriter
            {
                public Rewriter() : base(false)
                {

                }

                public override SyntaxToken VisitToken(SyntaxToken token)
                {
                    token = token.ReplaceTrivia(
                      token.GetAllTrivia().Where(
                          t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia)),
                      (_, _) => default);
                    token = token.WithTrailingTrivia(
                        token.TrailingTrivia.Add(SyntaxFactory.CarriageReturnLineFeed));
                    return token;
                }

                public override SyntaxNode? VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node) => node;

            }
        }
    }
}

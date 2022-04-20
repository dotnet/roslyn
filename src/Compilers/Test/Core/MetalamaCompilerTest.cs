using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Metalama.Compiler;
using Metalama.Backstage.Extensibility;

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

            var serviceProviderBuilder = new ServiceProviderBuilder();

            var transformersResult = CSharpCompiler.RunTransformers(
                compilation, transformers, null, ImmutableArray.Create<object>(), CompilerAnalyzerConfigOptionsProvider.Empty, diagnostics, ImmutableArray<ResourceDescription>.Empty, null!, serviceProviderBuilder.ServiceProvider, CancellationToken.None);

            diagnostics.ToReadOnlyAndFree().Verify();

            return transformersResult.TransformedCompilation;
        }

        public class TokenPerLineTransformer : ISourceTransformer
        {
            public void Execute(TransformerContext context)
            {
                static SyntaxToken ChangeWhitespace(SyntaxToken token)
                {
                    if (token.IsPartOfStructuredTrivia())
                    {
                        if (token.Kind() is SyntaxKind.XmlTextLiteralToken)
                        {
                            token = token.WithTrailingTrivia(
                                token.TrailingTrivia.Add(SyntaxFactory.Space));
                        }
                        return token;
                    }

                    token = token.ReplaceTrivia(
                        token.GetAllTrivia().Where(
                            t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia)),
                        (_, _) => default);
                    token = token.WithTrailingTrivia(
                        token.TrailingTrivia.Add(SyntaxFactory.CarriageReturnLineFeed));
                    return token;
                }

                var compilation = context.Compilation;
                foreach (var tree in compilation.SyntaxTrees)
                {
                    var newRoot = tree.GetRoot().ReplaceTokens(
                        tree.GetRoot().DescendantTokens(descendIntoTrivia: true), (_, token) => ChangeWhitespace(token));

                    context.ReplaceSyntaxTree(tree, tree.WithRootAndOptions(newRoot, tree.Options));
                }
                
            }
        }
    }
}

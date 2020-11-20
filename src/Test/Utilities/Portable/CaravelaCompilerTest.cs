using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Caravela.Compiler;

namespace Roslyn.Test.Utilities
{
    public static class CaravelaCompilerTest
    {
        private static readonly AsyncLocal<bool> shouldExecuteTransformer = new();

        public static bool ShouldExecuteTransformer
        {
            get => shouldExecuteTransformer.Value;
            set => shouldExecuteTransformer.Value = value;
        }

        public static Compilation ExecuteTransformer(Compilation compilation, ISourceTransformer transformer)
        {
            var transformers = ImmutableArray.Create<ISourceTransformer>(transformer);
            var diagnostics = new DiagnosticBag();

            var result = CSharpCompiler.RunTransformers(
                ref compilation, transformers, ImmutableArray.Create<object>(), CompilerAnalyzerConfigOptionsProvider.Empty, diagnostics, null!, null!);

            diagnostics.ToReadOnlyAndFree().Verify();

            return result;
        }

        public class TokenPerLineTransformer : ISourceTransformer
        {
            public Compilation Execute(TransformerContext context)
            {
                static SyntaxToken ChangeWhitespace(SyntaxToken token)
                {
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
                        tree.GetRoot().DescendantTokens(), (_, token) => ChangeWhitespace(token));

                    compilation = compilation.ReplaceSyntaxTree(tree, tree.WithRootAndOptions(newRoot, tree.Options));
                }
                return compilation;
            }
        }
    }
}

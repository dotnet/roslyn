#if false
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Utilities;

namespace Roslyn.Services.NameSimplification
{
    public static class NameSimplificationExtensions
    {
        private static INameSimplificationService GetNameSimplifierService(string language)
        {
            var factory = WorkspaceComposition.Composition.GetExportedValue<ILanguageServiceProviderFactory>();
            var languageService = factory.CreateLanguageServiceProvider(language);
            return languageService.GetService<INameSimplificationService>();
        }

        public static INameSimplificationResult SimplifyNames(this ISemanticModel semanticModel, CancellationToken cancellationToken = default(CancellationToken))
        {
            var tree = semanticModel.SyntaxTree;
            return SimplifyNames(semanticModel, tree.GetRoot(cancellationToken).FullSpan, cancellationToken);
        }

        public static INameSimplificationResult SimplifyNames(this ISemanticModel semanticModel, TextSpan textSpan, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SimplifyNames(semanticModel, SpecializedCollections.SingletonEnumerable(textSpan), cancellationToken);
        }

        public static INameSimplificationResult SimplifyNames(this ISemanticModel semanticModel, IEnumerable<TextSpan> spans, CancellationToken cancellationToken = default(CancellationToken))
        {
            var nameSimplifier = GetNameSimplifierService(semanticModel.Language);
            return nameSimplifier.Simplify(semanticModel, spans, cancellationToken);
        }

        public static INameSimplificationResult SimplifyNames(this CommonSyntaxNode root, ISemanticModel semanticModel, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SimplifyNames(root, semanticModel, SpecializedCollections.SingletonEnumerable(root.FullSpan), cancellationToken);
        }

        public static INameSimplificationResult SimplifyNames(this CommonSyntaxNode root, ISemanticModel semanticModel, TextSpan textSpan, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SimplifyNames(root, semanticModel, SpecializedCollections.SingletonEnumerable(textSpan), cancellationToken);
        }

        public static INameSimplificationResult SimplifyNames(this CommonSyntaxNode root, ISemanticModel semanticModel, IEnumerable<TextSpan> spans, CancellationToken cancellationToken = default(CancellationToken))
        {
            var nameSimplifier = GetNameSimplifierService(semanticModel.Language);
            return nameSimplifier.Simplify(root, semanticModel, spans, cancellationToken);
        }

        public static INameSimplificationResult SimplifyAnnotatedNodes(this CommonSyntaxTree syntaxTree, ISemanticModel semanticModel, SyntaxAnnotation annotation, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SimplifyNames(semanticModel, syntaxTree.GetRoot(cancellationToken).GetAnnotatedNodesAndTokens(annotation).Select(n => n.Span), cancellationToken);
        }

        public static INameSimplificationResult SimplifyAnnotatedNodes(this CommonSyntaxNode root, ISemanticModel semanticModel, SyntaxAnnotation annotation, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SimplifyNames(root, semanticModel, root.GetAnnotatedNodesAndTokens(annotation).Select(n => n.Span), cancellationToken);
        }
    }
}
#endif
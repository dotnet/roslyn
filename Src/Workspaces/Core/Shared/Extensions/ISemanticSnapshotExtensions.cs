using System.Threading;
using Roslyn.Compilers.Common;
using Roslyn.Services.Shared.LanguageServices;

namespace Roslyn.Services.Shared.Extensions
{
    internal static class ISemanticSnapshotExtensions
    {
        public static bool TryGetSymbolTouchingPosition(
            this IDocument document,
            int position,
            CancellationToken cancellationToken,
            out ISymbol symbol)
        {
            CommonSyntaxToken token;
            var semanticModel = document.GetSemanticModel(cancellationToken);
            var syntaxFacts = document.GetService<ISyntaxFactsService>();
            if (TryGetTokenTouchingPosition(syntaxFacts, semanticModel, position, out token) ||
                TryGetTokenTouchingPosition(syntaxFacts, semanticModel, position - 1, out token))
            {
                if ((symbol = semanticModel.GetAliasInfo(token.Parent, cancellationToken)) != null ||
                    (symbol = semanticModel.GetSymbolInfo(token.Parent, cancellationToken).GetAnySymbol()) != null ||
                    (symbol = semanticModel.GetDeclaredSymbol(token, cancellationToken)) != null)
                {
                    return true;
                }
            }

            symbol = null;
            return false;
        }

        private static bool TryGetTokenTouchingPosition(
            ISyntaxFactsService syntaxFacts,
            ISemanticModel semanticModel,
            int position,
            out CommonSyntaxToken result)
        {
            if (position >= 0)
            {
                var syntaxTree = semanticModel.SyntaxTree;
                var token = syntaxTree.Root.FindToken(position);
                if (!token.IsMissing && token.Span.Contains(position))
                {
                    // TODO: Add constant literals when we support metadata as source
                    if (syntaxFacts.IsWord(token) ||
                        syntaxFacts.IsOperator(token) ||
                        syntaxFacts.IsPredefinedType(token))
                    {
                        result = token;
                        return true;
                    }
                }
            }

            result = default(CommonSyntaxToken);
            return false;
        }
    }
}
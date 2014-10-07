using System.Linq;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;

namespace Roslyn.Services.Editor.CSharp.Extensions
{
    internal static class MethodSymbolExtensions
    {
        public static bool IsExtensionUsedAsInstance(
            this MethodSymbol method,
            SemanticModel semanticModel,
            ExpressionSyntax expression)
        {
            if (expression is InvocationExpressionSyntax)
            {
                expression = ((InvocationExpressionSyntax)expression).Expression;
            }

            if (expression is MemberAccessExpressionSyntax)
            {
                var leftSide = ((MemberAccessExpressionSyntax)expression).Expression;
                var leftSideInfo = semanticModel.GetSymbolInfo(leftSide);
                return !leftSideInfo.GetBestOrAllSymbols().OfType<ITypeSymbol>().Any();
            }

            return false;
        }
    }
}
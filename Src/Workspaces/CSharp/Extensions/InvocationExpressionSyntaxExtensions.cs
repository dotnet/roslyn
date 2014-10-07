using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;

namespace Roslyn.Services.CSharp.Extensions
{
    internal static class InvocationExpressionSyntaxExtensions
    {
        public static bool IsDelegateInvocation(this InvocationExpressionSyntax invocation, ISemanticModel semanticModel)
        {
            var namedType = semanticModel.GetTypeInfo(invocation.Expression).Type as INamedTypeSymbol;

            return namedType != null
                && namedType.TypeKind == CommonTypeKind.Delegate;
        }

        public static IMethodSymbol GetDelegateInvokeMethod(this InvocationExpressionSyntax invocation, ISemanticModel semanticModel)
        {
            var namedType = semanticModel.GetTypeInfo(invocation.Expression).Type as INamedTypeSymbol;
            if (namedType == null || namedType.TypeKind != CommonTypeKind.Delegate)
            {
                return null;
            }

            return namedType.DelegateInvokeMethod;
        }

        public static bool IsExtensionMethodInstanceInvocation(this InvocationExpressionSyntax invocation, ISemanticModel semanticModel)
        {
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            if (memberAccess == null)
            {
                return false;
            }

            var semanticInfo = semanticModel.GetSymbolInfo(memberAccess.Expression);
            if (semanticInfo.Symbol != null)
            {
                switch (semanticInfo.Symbol.Kind)
                {
                    case CommonSymbolKind.NamedType:
                    case CommonSymbolKind.Alias:
                        return false;
                }
            }

            return true;
        }
    }
}

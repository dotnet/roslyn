// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;

namespace Microsoft.CodeAnalysis.StackTraceExplorer;

internal abstract class AbstractStackTraceSymbolResolver
{
    public abstract Task<IMethodSymbol?> TryGetBestMatchAsync(
        Project project,
        INamedTypeSymbol type,
        StackFrameSimpleNameNode methodNode,
        StackFrameParameterList methodArguments,
        StackFrameTypeArgumentList? methodTypeArguments,
        CancellationToken cancellationToken);

    protected static bool MatchTypeArguments(ImmutableArray<ITypeSymbol> typeArguments, StackFrameTypeArgumentList? stackFrameTypeArgumentList)
    {
        if (stackFrameTypeArgumentList is null)
        {
            return typeArguments.IsEmpty;
        }

        if (typeArguments.IsEmpty)
        {
            return false;
        }

        var stackFrameTypeArguments = stackFrameTypeArgumentList.TypeArguments;
        return typeArguments.Length == stackFrameTypeArguments.Length;
    }

    protected static bool MatchType(ITypeSymbol type, StackFrameTypeNode stackFrameType)
    {
        if (type is IArrayTypeSymbol arrayType)
        {
            if (stackFrameType is not StackFrameArrayTypeNode arrayTypeNode)
            {
                return false;
            }

            ITypeSymbol currentType = arrayType;

            // Iterate through each array expression and make sure the dimensions
            // match the element types in an array.
            // Ex: string[,][] 
            // [,] is a 2 dimension array with element type string[]
            // [] is a 1 dimension array with element type string
            foreach (var arrayExpression in arrayTypeNode.ArrayRankSpecifiers)
            {
                if (currentType is not IArrayTypeSymbol currentArrayType)
                {
                    return false;
                }

                if (currentArrayType.Rank != arrayExpression.CommaTokens.Length + 1)
                {
                    return false;
                }

                currentType = currentArrayType.ElementType;
            }

            // All array types have been exchausted from the
            // stackframe identifier and the type is still an array
            if (currentType is IArrayTypeSymbol)
            {
                return false;
            }

            return MatchType(currentType, arrayTypeNode.TypeIdentifier);
        }

        return type.Name == stackFrameType.ToString();
    }

    protected static bool MatchParameters(ImmutableArray<IParameterSymbol> parameters, StackFrameParameterList stackFrameParameters)
    {
        if (parameters.Length != stackFrameParameters.Parameters.Length)
        {
            return false;
        }

        for (var i = 0; i < stackFrameParameters.Parameters.Length; i++)
        {
            var stackFrameParameter = stackFrameParameters.Parameters[i];
            var paramSymbol = parameters[i];

            if (paramSymbol.Name != stackFrameParameter.Identifier.ToString())
            {
                return false;
            }

            if (!MatchType(paramSymbol.Type, stackFrameParameter.Type))
            {
                return false;
            }
        }

        return true;
    }

    protected static IMethodSymbol? TryGetBestMatch(ImmutableArray<IMethodSymbol> candidateFunctions,
        StackFrameTypeArgumentList? methodTypeArguments,
        StackFrameParameterList methodArguments)
        => candidateFunctions
            .Where(m => MatchTypeArguments(m.TypeArguments, methodTypeArguments))
            .FirstOrDefault(m => MatchParameters(m.Parameters, methodArguments));
}

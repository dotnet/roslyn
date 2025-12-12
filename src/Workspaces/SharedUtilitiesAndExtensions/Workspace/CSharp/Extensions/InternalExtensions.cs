// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp;

internal static class InternalExtensions
{
    public static ITypeSymbol DetermineParameterType(
        this ArgumentSyntax argument,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        return DetermineParameterType(argument.Expression, semanticModel, cancellationToken);
    }

    public static ITypeSymbol DetermineParameterType(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (expression is DeclarationExpressionSyntax decl)
        {
            var typeInfo = semanticModel.GetTypeInfo(decl.Type, cancellationToken);
            return typeInfo.Type?.IsErrorType() == false ? typeInfo.Type : semanticModel.Compilation.ObjectType;
        }
        else
        {
            // If a parameter appears to have a void return type, then just use 'object'
            // instead.
            var typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);
            if (typeInfo.Type != null && typeInfo.Type.SpecialType == SpecialType.System_Void)
                return semanticModel.Compilation.ObjectType;
        }

        return semanticModel.GetType(expression, cancellationToken);
    }
}

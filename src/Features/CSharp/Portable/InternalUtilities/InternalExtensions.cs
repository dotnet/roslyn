// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class InternalExtensions
    {
        public static ITypeSymbol DetermineParameterType(
            this ArgumentSyntax argument,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            TypeInfo typeInfo;

            if (argument.Expression.Kind() == SyntaxKind.DeclarationExpression)
            {
                var decl = (DeclarationExpressionSyntax)argument.Expression;
                typeInfo = semanticModel.GetTypeInfo(decl.Type, cancellationToken);
                return typeInfo.Type?.IsErrorType() == false ? typeInfo.Type : semanticModel.Compilation.ObjectType;
            }

            // If a parameter appears to have a void return type, then just use 'object'
            // instead.
            typeInfo = semanticModel.GetTypeInfo(argument.Expression, cancellationToken);
            if (typeInfo is { Type: { SpecialType: SpecialType.System_Void } })
            {
                return semanticModel.Compilation.ObjectType;
            }

            return semanticModel.GetType(argument.Expression, cancellationToken);
        }
    }
}

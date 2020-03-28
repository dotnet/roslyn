﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            if (typeInfo.Type != null && typeInfo.Type.SpecialType == SpecialType.System_Void)
            {
                return semanticModel.Compilation.ObjectType;
            }

            return semanticModel.GetType(argument.Expression, cancellationToken);
        }
    }
}

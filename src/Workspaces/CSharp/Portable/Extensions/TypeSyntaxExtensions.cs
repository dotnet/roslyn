﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class TypeSyntaxExtensions
    {
        public static bool IsVoid(this TypeSyntax typeSyntax)
        {
            return typeSyntax.IsKind(SyntaxKind.PredefinedType) &&
                ((PredefinedTypeSyntax)typeSyntax).Keyword.IsKind(SyntaxKind.VoidKeyword);
        }

        public static bool IsPartial(this TypeSyntax typeSyntax)
        {
            return typeSyntax is IdentifierNameSyntax &&
                ((IdentifierNameSyntax)typeSyntax).Identifier.IsKind(SyntaxKind.PartialKeyword);
        }

        public static bool IsPotentialTypeName(this TypeSyntax typeSyntax, SemanticModel semanticModelOpt, CancellationToken cancellationToken)
        {
            if (typeSyntax == null)
            {
                return false;
            }

            if (typeSyntax is PredefinedTypeSyntax ||
                typeSyntax is ArrayTypeSyntax ||
                typeSyntax is GenericNameSyntax ||
                typeSyntax is PointerTypeSyntax ||
                typeSyntax is NullableTypeSyntax)
            {
                return true;
            }

            if (semanticModelOpt == null)
            {
                return false;
            }

            if (!(typeSyntax is NameSyntax nameSyntax))
            {
                return false;
            }

            var nameToken = nameSyntax.GetNameToken();

            var symbols = semanticModelOpt.LookupName(nameToken, namespacesAndTypesOnly: true, cancellationToken);
            var firstSymbol = symbols.FirstOrDefault();

            var typeSymbol = firstSymbol != null && firstSymbol.Kind == SymbolKind.Alias
                ? (firstSymbol as IAliasSymbol).Target
                : firstSymbol as ITypeSymbol;

            return typeSymbol != null
                && !typeSymbol.IsErrorType();
        }

        /// <summary>
        /// Determines whether the specified TypeSyntax is actually 'var'.
        /// </summary>
        public static bool IsTypeInferred(this TypeSyntax typeSyntax, SemanticModel semanticModel)
        {
            if (!typeSyntax.IsVar)
            {
                return false;
            }

            if (semanticModel.GetAliasInfo(typeSyntax) != null)
            {
                return false;
            }

            var type = semanticModel.GetTypeInfo(typeSyntax).Type;
            if (type == null)
            {
                return false;
            }

            if (type.Name == "var")
            {
                return false;
            }

            return true;
        }

        public static TypeSyntax GenerateReturnTypeSyntax(this IMethodSymbol method)
        {
            var returnType = method.ReturnType;

            if (method.ReturnsByRef)
            {
                return returnType.GenerateRefTypeSyntax();
            }
            else if (method.ReturnsByRefReadonly)
            {
                return returnType.GenerateRefReadOnlyTypeSyntax();
            }
            else
            {
                return returnType.GenerateTypeSyntax();
            }
        }

        public static TypeSyntax StripRefIfNeeded(this TypeSyntax type)
            => type is RefTypeSyntax refType ? refType.Type : type;
    }
}

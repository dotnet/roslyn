// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class TypeSyntaxExtensions
    {
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

            var symbols = semanticModelOpt.LookupName(nameToken, cancellationToken);
            var firstSymbol = symbols.FirstOrDefault();

            var typeSymbol = firstSymbol != null && firstSymbol.Kind == SymbolKind.Alias
                ? (firstSymbol as IAliasSymbol).Target
                : firstSymbol as ITypeSymbol;

            return typeSymbol != null
                && !typeSymbol.IsErrorType();
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
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class AllowsRefLikeTypeSymbolVisitor : SymbolVisitor
    {
        public bool Result { get; private set; } = true;

        public required ITypeParameterSymbol TypeParameter { get; init; }

        private bool IsTypeParameterSymbol(ITypeSymbol typeSymbol)
        {
            return Microsoft.CodeAnalysis.SymbolEqualityComparer.Default.Equals(typeSymbol, TypeParameter);
        }

        public override void VisitArrayType(IArrayTypeSymbol symbol)
        {
            if (IsTypeParameterSymbol(symbol.ElementType))
            {
                Result = false;
                return;
            }
            Visit(symbol.ElementType);
        }

        public override void VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol)
        {
            Visit(symbol.Signature);
        }

        public override void VisitMethod(IMethodSymbol symbol)
        {
            Visit(symbol.ReturnType);
            if (!Result) return;

            foreach (var p in symbol.Parameters)
            {
                Visit(p);
                if (!Result) return;
            }
        }

        public override void VisitParameter(IParameterSymbol symbol)
        {
            Visit(symbol.Type);
        }

        public override void VisitPointerType(IPointerTypeSymbol symbol)
        {
            Visit(symbol.PointedAtType);
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            int idx = 0;
            foreach (var arg in symbol.TypeArguments)
            {
                if (IsTypeParameterSymbol(arg))
                {
                    var parameter = symbol.TypeParameters[idx];
                    if (!parameter.AllowsRefLikeType)
                    {
                        Result = false;
                        return;
                    }
                }
                idx++;
            }
        }
    }
}

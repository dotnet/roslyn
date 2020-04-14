// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal partial class ITypeSymbolExtensions
    {
        private class MinimalAccessibilityVisitor : SymbolVisitor<Accessibility>
        {
            public static readonly SymbolVisitor<Accessibility> Instance = new MinimalAccessibilityVisitor();

            public override Accessibility DefaultVisit(ISymbol node)
                => throw new NotImplementedException();

            public override Accessibility VisitAlias(IAliasSymbol symbol)
                => symbol.Target.Accept(this);

            public override Accessibility VisitArrayType(IArrayTypeSymbol symbol)
                => symbol.ElementType.Accept(this);

            public override Accessibility VisitDynamicType(IDynamicTypeSymbol symbol)
                => Accessibility.Public;

            public override Accessibility VisitNamedType(INamedTypeSymbol symbol)
            {
                var accessibility = symbol.DeclaredAccessibility;

                foreach (var arg in symbol.TypeArguments)
                {
                    accessibility = AccessibilityUtilities.Minimum(accessibility, arg.Accept(this));
                }

                if (symbol.ContainingType != null)
                {
                    accessibility = AccessibilityUtilities.Minimum(accessibility, symbol.ContainingType.Accept(this));
                }

                return accessibility;
            }

            public override Accessibility VisitPointerType(IPointerTypeSymbol symbol)
                => symbol.PointedAtType.Accept(this);

            public override Accessibility VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                // TODO(cyrusn): Do we have to consider the constraints?
                return Accessibility.Public;
            }
        }
    }
}

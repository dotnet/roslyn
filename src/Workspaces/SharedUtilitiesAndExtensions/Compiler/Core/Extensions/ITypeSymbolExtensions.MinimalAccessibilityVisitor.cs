// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ITypeSymbolExtensions
{
    private sealed class MinimalAccessibilityVisitor : SymbolVisitor<Accessibility>
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

        public override Accessibility VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol)
        {
            var accessibility = symbol.DeclaredAccessibility;

            accessibility = AccessibilityUtilities.Minimum(accessibility, symbol.Signature.ReturnType.Accept(this));

            foreach (var parameter in symbol.Signature.Parameters)
            {
                accessibility = AccessibilityUtilities.Minimum(accessibility, parameter.Type.Accept(this));
            }

            // CallingConvention types are currently specced to always be public, but if that spec ever changes
            // or the runtime creates special private types for it's own use, we'll be ready.
            foreach (var callingConventionType in symbol.Signature.UnmanagedCallingConventionTypes)
            {
                accessibility = AccessibilityUtilities.Minimum(accessibility, callingConventionType.Accept(this));
            }

            return accessibility;
        }

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

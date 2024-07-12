// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal partial class ISymbolExtensions
{
    /// <summary>
    /// Visits types or members that have signatures (i.e. methods, fields, etc.) and determines
    /// if any of them reference a pointer type and should thus have the <see
    /// langword="unsafe"/> modifier on them.
    /// </summary>
    private class RequiresUnsafeModifierVisitor : SymbolVisitor<bool>
    {
        private readonly HashSet<ISymbol> _visited = [];

        public override bool DefaultVisit(ISymbol node)
        {
            Debug.Fail("Unhandled symbol kind in RequiresUnsafeModifierVisitor: " + node.Kind);
            return false;
        }

        public override bool VisitArrayType(IArrayTypeSymbol symbol)
        {
            if (!_visited.Add(symbol))
            {
                return false;
            }

            return symbol.ElementType.Accept(this);
        }

        public override bool VisitDynamicType(IDynamicTypeSymbol symbol)
        {
            // The dynamic type is never unsafe (well....you know what I mean)
            return false;
        }

        public override bool VisitField(IFieldSymbol symbol)
        {
            if (!_visited.Add(symbol))
            {
                return false;
            }

            return symbol.Type.Accept(this);
        }

        public override bool VisitNamedType(INamedTypeSymbol symbol)
        {
            if (!_visited.Add(symbol))
            {
                return false;
            }

            return symbol.GetAllTypeArguments().Any(ts => ts.Accept(this));
        }

        public override bool VisitPointerType(IPointerTypeSymbol symbol)
        {
            if (!_visited.Add(symbol))
            {
                return false;
            }

            return true;
        }

        public override bool VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol)
        {
            if (!_visited.Add(symbol))
            {
                return false;
            }

            return true;
        }

        public override bool VisitProperty(IPropertySymbol symbol)
        {
            if (!_visited.Add(symbol))
            {
                return false;
            }

            return
                symbol.Type.Accept(this) ||
                symbol.Parameters.Any(predicate: static (p, self) => p.Accept(self), arg: this);
        }

        public override bool VisitTypeParameter(ITypeParameterSymbol symbol)
        {
            if (!_visited.Add(symbol))
            {
                return false;
            }

            return symbol.ConstraintTypes.Any(predicate: static (ts, self) => ts.Accept(self), arg: this);
        }

        public override bool VisitMethod(IMethodSymbol symbol)
        {
            if (!_visited.Add(symbol))
            {
                return false;
            }

            return
                symbol.ReturnType.Accept(this) ||
                symbol.Parameters.Any(predicate: static (p, self) => p.Accept(self), arg: this) ||
                symbol.TypeParameters.Any(predicate: static (tp, self) => tp.Accept(self), arg: this);
        }

        public override bool VisitParameter(IParameterSymbol symbol)
        {
            if (!_visited.Add(symbol))
            {
                return false;
            }

            return symbol.Type.Accept(this);
        }

        public override bool VisitEvent(IEventSymbol symbol)
        {
            if (!_visited.Add(symbol))
            {
                return false;
            }

            return symbol.Type.Accept(this);
        }

        public override bool VisitAlias(IAliasSymbol symbol)
        {
            if (!_visited.Add(symbol))
            {
                return false;
            }

            return symbol.Target.Accept(this);
        }
    }
}

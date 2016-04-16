// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal partial class ISymbolExtensions
    {
        private class IsUnsafeVisitor : SymbolVisitor<bool>
        {
            private readonly HashSet<ISymbol> _visited = new HashSet<ISymbol>();

            public IsUnsafeVisitor()
            {
            }

            public override bool DefaultVisit(ISymbol node)
            {
                throw new NotImplementedException();
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
                // The dynamic type is never unsafe (well....you know what I mean
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

            public override bool VisitProperty(IPropertySymbol symbol)
            {
                if (!_visited.Add(symbol))
                {
                    return false;
                }

                return
                    symbol.Type.Accept(this) ||
                    symbol.Parameters.Any(p => p.Accept(this));
            }

            public override bool VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                if (!_visited.Add(symbol))
                {
                    return false;
                }

                return symbol.ConstraintTypes.Any(ts => ts.Accept(this));
            }

            public override bool VisitMethod(IMethodSymbol symbol)
            {
                if (!_visited.Add(symbol))
                {
                    return false;
                }

                return
                    symbol.ReturnType.Accept(this) ||
                    symbol.Parameters.Any(p => p.Accept(this)) ||
                    symbol.TypeParameters.Any(tp => tp.Accept(this));
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
}

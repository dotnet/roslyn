// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal partial class AbstractAnonymousTypeDisplayService
    {
        private class NormalAnonymousTypeCollectorVisitor : SymbolVisitor
        {
            private readonly ISet<INamedTypeSymbol> _seenTypes = new HashSet<INamedTypeSymbol>();
            private readonly ICollection<INamedTypeSymbol> _namedTypes;

            public NormalAnonymousTypeCollectorVisitor(ICollection<INamedTypeSymbol> namedTypes)
            {
                _namedTypes = namedTypes;
            }

            public override void DefaultVisit(ISymbol node)
            {
                throw new NotImplementedException();
            }

            public override void VisitAlias(IAliasSymbol symbol)
            {
                // TODO(cyrusn): I don't think we need to inspect the target of an alias.
            }

            public override void VisitArrayType(IArrayTypeSymbol symbol)
            {
                symbol.ElementType.Accept(this);
            }

            public override void VisitAssembly(IAssemblySymbol symbol)
            {
            }

            public override void VisitDynamicType(IDynamicTypeSymbol symbol)
            {
            }

            public override void VisitField(IFieldSymbol symbol)
            {
                symbol.Type.Accept(this);
            }

            public override void VisitLabel(ILabelSymbol symbol)
            {
            }

            public override void VisitLocal(ILocalSymbol symbol)
            {
                symbol.Type.Accept(this);
            }

            public override void VisitMethod(IMethodSymbol symbol)
            {
                // Visit the type arguments first.  That way we'll see things in the proper order.
                // i.e. if we have:  anon Select<anon, anon>(anon a) it will come out as: 
                //
                // 'b Select<'a, 'b>('a a);

                foreach (var typeArgument in symbol.TypeArguments)
                {
                    typeArgument.Accept(this);
                }

                foreach (var parameter in symbol.Parameters)
                {
                    parameter.Accept(this);
                }

                symbol.ReturnType.Accept(this);
            }

            public override void VisitModule(IModuleSymbol symbol)
            {
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                if (_seenTypes.Add(symbol))
                {
                    if (symbol.IsNormalAnonymousType())
                    {
                        _namedTypes.Add(symbol);

                        foreach (var property in symbol.GetValidAnonymousTypeProperties())
                        {
                            property.Accept(this);
                        }
                    }
                    else if (symbol.IsAnonymousDelegateType())
                    {
                        symbol.DelegateInvokeMethod.Accept(this);
                    }
                    else
                    {
                        foreach (var typeArgument in symbol.GetAllTypeArguments())
                        {
                            typeArgument.Accept(this);
                        }
                    }
                }
            }

            public override void VisitNamespace(INamespaceSymbol symbol)
            {
            }

            public override void VisitParameter(IParameterSymbol symbol)
            {
                symbol.Type.Accept(this);
            }

            public override void VisitPointerType(IPointerTypeSymbol symbol)
            {
                symbol.PointedAtType.Accept(this);
            }

            public override void VisitProperty(IPropertySymbol symbol)
            {
                symbol.Type.Accept(this);

                foreach (var parameter in symbol.Parameters)
                {
                    parameter.Accept(this);
                }
            }

            public override void VisitEvent(IEventSymbol symbol)
            {
                symbol.Type.Accept(this);
            }

            public override void VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                foreach (var constraint in symbol.ConstraintTypes)
                {
                    constraint.Accept(this);
                }
            }

            public override void VisitRangeVariable(IRangeVariableSymbol symbol)
            {
            }
        }
    }
}

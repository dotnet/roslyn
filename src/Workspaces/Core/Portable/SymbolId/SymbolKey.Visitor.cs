// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        private class Visitor : SymbolVisitor<SymbolKey>
        {
            internal readonly Dictionary<ISymbol, SymbolKey> SymbolCache = new Dictionary<ISymbol, SymbolKey>();
            internal readonly Compilation Compilation;
            internal readonly CancellationToken CancellationToken;

            public Visitor(Compilation compilation, CancellationToken cancellationToken)
            {
                this.Compilation = compilation;
                this.CancellationToken = cancellationToken;
            }

            public override SymbolKey VisitAlias(IAliasSymbol aliasSymbol)
            {
                return GetOrCreate(aliasSymbol.Target, this);
            }

            public override SymbolKey VisitArrayType(IArrayTypeSymbol arrayTypeSymbol)
            {
                return new ArrayTypeSymbolKey(arrayTypeSymbol, this);
            }

            public override SymbolKey VisitAssembly(IAssemblySymbol assemblySymbol)
            {
                return new AssemblySymbolKey(assemblySymbol);
            }

            public override SymbolKey VisitDynamicType(IDynamicTypeSymbol dynamicTypeSymbol)
            {
                return DynamicTypeSymbolKey.Instance;
            }

            public override SymbolKey VisitField(IFieldSymbol fieldSymbol)
            {
                return new FieldSymbolKey(fieldSymbol, this);
            }

            public override SymbolKey VisitLabel(ILabelSymbol labelSymbol)
            {
                return new NonDeclarationSymbolKey<ILabelSymbol>(labelSymbol, this);
            }

            public override SymbolKey VisitLocal(ILocalSymbol localSymbol)
            {
                return new NonDeclarationSymbolKey<ILocalSymbol>(localSymbol, this);
            }

            public override SymbolKey VisitMethod(IMethodSymbol methodSymbol)
            {
                return new MethodSymbolKey(methodSymbol, this);
            }

            public override SymbolKey VisitModule(IModuleSymbol moduleSymbol)
            {
                return new ModuleSymbolKey(moduleSymbol, this);
            }

            public override SymbolKey VisitNamedType(INamedTypeSymbol namedTypeSymbol)
            {
                return namedTypeSymbol.TypeKind == TypeKind.Error
                    ? new ErrorTypeSymbolKey(namedTypeSymbol, this)
                    : (SymbolKey)new NamedTypeSymbolKey(namedTypeSymbol, this);
            }

            public override SymbolKey VisitNamespace(INamespaceSymbol namespaceSymbol)
            {
                return new NamespaceSymbolKey(namespaceSymbol, this);
            }

            public override SymbolKey VisitParameter(IParameterSymbol parameterSymbol)
            {
                return new ParameterSymbolKey(parameterSymbol, this);
            }

            public override SymbolKey VisitPointerType(IPointerTypeSymbol pointerTypeSymbol)
            {
                return new PointerTypeSymbolKey(pointerTypeSymbol, this);
            }

            public override SymbolKey VisitProperty(IPropertySymbol propertySymbol)
            {
                return new PropertySymbolKey(propertySymbol, this);
            }

            public override SymbolKey VisitEvent(IEventSymbol eventSymbol)
            {
                return new EventSymbolKey(eventSymbol, this);
            }

            public override SymbolKey VisitTypeParameter(ITypeParameterSymbol typeParameterSymbol)
            {
                if (typeParameterSymbol.TypeParameterKind == TypeParameterKind.Method)
                {
                    // In the method type parameter case, getting the containingId above will cause
                    // the symbol id for the type parameter to be created and added to the symbol
                    // table.  So just fish the type parameter out of that.
                    var containingId = GetOrCreate(typeParameterSymbol.ContainingSymbol, this);

                    SymbolKey symbolId;
                    var succeeded = SymbolCache.TryGetValue(typeParameterSymbol, out symbolId);
                    Debug.Assert(!ReferenceEquals(symbolId, null));
                    Debug.Assert(succeeded);

                    return symbolId;
                }
                else if (typeParameterSymbol.TypeParameterKind == TypeParameterKind.Type)
                {
                    return new TypeParameterSymbolKey(typeParameterSymbol, this);
                }
                else
                {
                    return SymbolKey.s_null;
                }
            }

            public override SymbolKey VisitRangeVariable(IRangeVariableSymbol rangeVariableSymbol)
            {
                return new NonDeclarationSymbolKey<IRangeVariableSymbol>(rangeVariableSymbol, this);
            }
        }
    }
}

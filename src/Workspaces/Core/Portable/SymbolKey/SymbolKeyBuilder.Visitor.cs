// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Symbols
{
    internal partial class SymbolKeyBuilder
    {
        private class Visitor : SymbolVisitor
        {
            private readonly SymbolKeyBuilder _writer;

            public Visitor(SymbolKeyBuilder writer)
            {
                _writer = writer;
            }

            public override void VisitAlias(IAliasSymbol symbol)
            {
                _writer.AppendAliasSymbol(symbol);
            }

            public override void VisitArrayType(IArrayTypeSymbol symbol)
            {
                _writer.AppendArrayTypeSymbol(symbol);
            }

            public override void VisitAssembly(IAssemblySymbol symbol)
            {
                _writer.AppendAssemblySymbol(symbol);
            }

            public override void VisitDynamicType(IDynamicTypeSymbol symbol)
            {
                _writer.AppendDynamicTypeSymbol(symbol);
            }

            public override void VisitEvent(IEventSymbol symbol)
            {
                _writer.AppendEventSymbol(symbol);
            }

            public override void VisitField(IFieldSymbol symbol)
            {
                _writer.AppendFieldSymbol(symbol);
            }

            public override void VisitLabel(ILabelSymbol symbol)
            {
                _writer.AppendBodyLevelSymbol(symbol);
            }

            public override void VisitLocal(ILocalSymbol symbol)
            {
                _writer.AppendBodyLevelSymbol(symbol);
            }

            public override void VisitModule(IModuleSymbol symbol)
            {
                _writer.AppendModuleSymbol(symbol);
            }

            public override void VisitNamespace(INamespaceSymbol symbol)
            {
                _writer.AppendNamespaceSymbol(symbol);
            }

            public override void VisitMethod(IMethodSymbol symbol)
            {
                _writer.AppendMethodSymbol(symbol);
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                _writer.AppendNamedTypeSymbol(symbol);
            }

            public override void VisitParameter(IParameterSymbol symbol)
            {
                _writer.AppendParameterSymbol(symbol);
            }

            public override void VisitPointerType(IPointerTypeSymbol symbol)
            {
                _writer.AppendPointerTypeSymbol(symbol);
            }

            public override void VisitProperty(IPropertySymbol symbol)
            {
                _writer.AppendPropertySymbol(symbol);
            }

            public override void VisitRangeVariable(IRangeVariableSymbol symbol)
            {
                _writer.AppendRangeVariableSymbol(symbol);
            }

            public override void VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                _writer.AppendTypeParameterSymbol(symbol);
            }
        }
    }
}

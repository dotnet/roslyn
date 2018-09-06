// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    /// <summary>
    /// Returns the set of members that contain reference types with IsNullable set.
    /// </summary>
    internal sealed class UsesIsNullableVisitor : CSharpSymbolVisitor<bool>
    {
        private readonly ArrayBuilder<Symbol> _builder;

        private UsesIsNullableVisitor(ArrayBuilder<Symbol> builder)
        {
            _builder = builder;
        }

        internal static void GetUses(ArrayBuilder<Symbol> builder, Symbol symbol)
        {
            var visitor = new UsesIsNullableVisitor(builder);
            visitor.Visit(symbol);
        }

        private void Add(Symbol symbol)
            => _builder.Add(symbol);

        public override bool VisitNamespace(NamespaceSymbol symbol)
        {
            return VisitList(symbol.GetMembers());
        }

        public override bool VisitNamedType(NamedTypeSymbol symbol)
        {
            if (AddIfUsesIsNullable(symbol, symbol.BaseTypeNoUseSiteDiagnostics) ||
                AddIfUsesIsNullable(symbol, symbol.InterfacesNoUseSiteDiagnostics()) ||
                AddIfUsesIsNullable(symbol, symbol.TypeParameters))
            {
                return true;
            }
            return VisitList(symbol.GetMembers());
        }

        public override bool VisitMethod(MethodSymbol symbol)
        {
            return AddIfUsesIsNullable(symbol, symbol.TypeParameters) ||
                AddIfUsesIsNullable(symbol, symbol.ReturnType) ||
                AddIfUsesIsNullable(symbol, symbol.Parameters);
        }

        public override bool VisitProperty(PropertySymbol symbol)
        {
            return AddIfUsesIsNullable(symbol, symbol.Type) ||
                AddIfUsesIsNullable(symbol, symbol.Parameters);
        }

        public override bool VisitEvent(EventSymbol symbol)
        {
            return AddIfUsesIsNullable(symbol, symbol.Type);
        }

        public override bool VisitField(FieldSymbol symbol)
        {
            return AddIfUsesIsNullable(symbol, symbol.Type);
        }

        private bool VisitList<TSymbol>(ImmutableArray<TSymbol> symbols) where TSymbol : Symbol
        {
            bool result = false;
            foreach (var symbol in symbols)
            {
                if (this.Visit(symbol))
                {
                    result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Check the parameters of a method or property, but report that method/property rather than
        /// the parameter itself.
        /// </summary>
        private bool AddIfUsesIsNullable(Symbol symbol, ImmutableArray<ParameterSymbol> parameters)
        {
            foreach (var parameter in parameters)
            {
                if (UsesIsNullable(parameter.Type))
                {
                    Add(symbol);
                    return true;
                }
            }
            return false;
        }

        private bool AddIfUsesIsNullable(Symbol symbol, ImmutableArray<TypeParameterSymbol> typeParameters)
        {
            foreach (var type in typeParameters)
            {
                if (UsesIsNullable(type))
                {
                    Add(symbol);
                    return true;
                }
            }
            return false;
        }

        private bool AddIfUsesIsNullable(Symbol symbol, ImmutableArray<NamedTypeSymbol> types)
        {
            foreach (var type in types)
            {
                if (UsesIsNullable(type))
                {
                    Add(symbol);
                    return true;
                }
            }
            return false;
        }

        private bool AddIfUsesIsNullable(Symbol symbol, TypeSymbolWithAnnotations type)
        {
            if (UsesIsNullable(type))
            {
                Add(symbol);
                return true;
            }
            return false;
        }

        private bool AddIfUsesIsNullable(Symbol symbol, TypeSymbol type)
        {
            if (UsesIsNullable(type))
            {
                Add(symbol);
                return true;
            }
            return false;
        }

        private bool UsesIsNullable(TypeSymbolWithAnnotations type)
        {
            if (type.IsNull)
            {
                return false;
            }
            var typeSymbol = type.TypeSymbol;
            return (type.IsNullable != null && type.IsReferenceType && !type.IsErrorType()) ||
                UsesIsNullable(type.TypeSymbol);
        }

        private bool UsesIsNullable(TypeSymbol type)
        {
            if (type is null)
            {
                return false;
            }
            switch (type.TypeKind)
            {
                case TypeKind.Class:
                case TypeKind.Delegate:
                case TypeKind.Interface:
                case TypeKind.Struct:
                case TypeKind.Enum:
                    if (UsesIsNullable(type.ContainingType))
                    {
                        return true;
                    }
                    break;
            }
            switch (type.TypeKind)
            {
                case TypeKind.Array:
                    return UsesIsNullable(((ArrayTypeSymbol)type).ElementType);
                case TypeKind.Class:
                case TypeKind.Delegate:
                case TypeKind.Error:
                case TypeKind.Interface:
                case TypeKind.Struct:
                    return UsesIsNullable(((NamedTypeSymbol)type).TypeArgumentsNoUseSiteDiagnostics);
                case TypeKind.Dynamic:
                case TypeKind.Enum:
                    return false;
                case TypeKind.Pointer:
                    return UsesIsNullable(((PointerTypeSymbol)type).PointedAtType);
                case TypeKind.TypeParameter:
                    // PROTOTYPE(NullableReferenceTypes): Nullability for constraint types is not being erased.
                    //return UsesIsNullable(((TypeParameterSymbol)type).ConstraintTypesNoUseSiteDiagnostics);
                    return false;
                default:
                    throw ExceptionUtilities.UnexpectedValue(type.TypeKind);
            }
        }

        private bool UsesIsNullable(ImmutableArray<TypeSymbolWithAnnotations> types)
        {
            return types.Any(t => UsesIsNullable(t));
        }
    }
}

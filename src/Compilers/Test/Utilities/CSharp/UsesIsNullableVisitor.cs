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
            if (AddIfUsesIsNullable(symbol, symbol.BaseTypeNoUseSiteDiagnostics, inProgress: null) ||
                AddIfUsesIsNullable(symbol, symbol.InterfacesNoUseSiteDiagnostics(), inProgress: null) ||
                AddIfUsesIsNullable(symbol, symbol.TypeParameters, inProgress: null))
            {
                return true;
            }
            return VisitList(symbol.GetMembers());
        }

        public override bool VisitMethod(MethodSymbol symbol)
        {
            return AddIfUsesIsNullable(symbol, symbol.TypeParameters, inProgress: null) ||
                AddIfUsesIsNullable(symbol, symbol.ReturnTypeWithAnnotations, inProgress: null) ||
                AddIfUsesIsNullable(symbol, symbol.Parameters, inProgress: null);
        }

        public override bool VisitProperty(PropertySymbol symbol)
        {
            return AddIfUsesIsNullable(symbol, symbol.TypeWithAnnotations, inProgress: null) ||
                AddIfUsesIsNullable(symbol, symbol.Parameters, inProgress: null);
        }

        public override bool VisitEvent(EventSymbol symbol)
        {
            return AddIfUsesIsNullable(symbol, symbol.TypeWithAnnotations, inProgress: null);
        }

        public override bool VisitField(FieldSymbol symbol)
        {
            return AddIfUsesIsNullable(symbol, symbol.TypeWithAnnotations, inProgress: null);
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
        private bool AddIfUsesIsNullable(Symbol symbol, ImmutableArray<ParameterSymbol> parameters, ConsList<TypeParameterSymbol> inProgress)
        {
            foreach (var parameter in parameters)
            {
                if (UsesIsNullable(parameter.TypeWithAnnotations, inProgress))
                {
                    Add(symbol);
                    return true;
                }
            }
            return false;
        }

        private bool AddIfUsesIsNullable(Symbol symbol, ImmutableArray<TypeParameterSymbol> typeParameters, ConsList<TypeParameterSymbol> inProgress)
        {
            foreach (var type in typeParameters)
            {
                if (UsesIsNullable(type, inProgress))
                {
                    Add(symbol);
                    return true;
                }
            }
            return false;
        }

        private bool AddIfUsesIsNullable(Symbol symbol, ImmutableArray<NamedTypeSymbol> types, ConsList<TypeParameterSymbol> inProgress)
        {
            foreach (var type in types)
            {
                if (UsesIsNullable(type, inProgress))
                {
                    Add(symbol);
                    return true;
                }
            }
            return false;
        }

        private bool AddIfUsesIsNullable(Symbol symbol, TypeWithAnnotations type, ConsList<TypeParameterSymbol> inProgress)
        {
            if (UsesIsNullable(type, inProgress))
            {
                Add(symbol);
                return true;
            }
            return false;
        }

        private bool AddIfUsesIsNullable(Symbol symbol, TypeSymbol type, ConsList<TypeParameterSymbol> inProgress)
        {
            if (UsesIsNullable(type, inProgress))
            {
                Add(symbol);
                return true;
            }
            return false;
        }

        private bool UsesIsNullable(TypeWithAnnotations type, ConsList<TypeParameterSymbol> inProgress)
        {
            if (!type.HasType)
            {
                return false;
            }
            var typeSymbol = type.Type;
            return (type.NullableAnnotation != NullableAnnotation.Oblivious && typeSymbol.IsReferenceType && !typeSymbol.IsErrorType()) ||
                UsesIsNullable(typeSymbol, inProgress);
        }

        private bool UsesIsNullable(TypeSymbol type, ConsList<TypeParameterSymbol> inProgress)
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
                    if (UsesIsNullable(type.ContainingType, inProgress))
                    {
                        return true;
                    }
                    break;
            }
            switch (type.TypeKind)
            {
                case TypeKind.Array:
                    return UsesIsNullable(((ArrayTypeSymbol)type).ElementTypeWithAnnotations, inProgress);
                case TypeKind.Class:
                case TypeKind.Delegate:
                case TypeKind.Error:
                case TypeKind.Interface:
                case TypeKind.Struct:
                    return UsesIsNullable(((NamedTypeSymbol)type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics, inProgress);
                case TypeKind.Dynamic:
                case TypeKind.Enum:
                    return false;
                case TypeKind.Pointer:
                    return UsesIsNullable(((PointerTypeSymbol)type).PointedAtTypeWithAnnotations, inProgress);
                case TypeKind.TypeParameter:
                    var typeParameter = (TypeParameterSymbol)type;
                    if (inProgress?.ContainsReference(typeParameter) == true)
                    {
                        return false;
                    }
                    inProgress = inProgress ?? ConsList<TypeParameterSymbol>.Empty;
                    inProgress = inProgress.Prepend(typeParameter);
                    return UsesIsNullable(typeParameter.ConstraintTypesNoUseSiteDiagnostics, inProgress) ||
                        typeParameter.ReferenceTypeConstraintIsNullable == true;
                default:
                    throw ExceptionUtilities.UnexpectedValue(type.TypeKind);
            }
        }

        private bool UsesIsNullable(ImmutableArray<TypeWithAnnotations> types, ConsList<TypeParameterSymbol> inProgress)
        {
            return types.Any(t => UsesIsNullable(t, inProgress));
        }
    }
}

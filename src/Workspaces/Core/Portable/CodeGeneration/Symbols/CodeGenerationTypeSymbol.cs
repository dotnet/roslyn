﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal abstract class CodeGenerationTypeSymbol : CodeGenerationNamespaceOrTypeSymbol, ITypeSymbol
    {
        public SpecialType SpecialType { get; protected set; }

        protected CodeGenerationTypeSymbol(
            IAssemblySymbol containingAssembly,
            INamedTypeSymbol containingType,
            ImmutableArray<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            string name,
            SpecialType specialType,
            NullableAnnotation nullableAnnotation)
            : base(containingAssembly, containingType, attributes, declaredAccessibility, modifiers, name)
        {
            this.SpecialType = specialType;
            this.NullableAnnotation = nullableAnnotation;
        }

        public abstract TypeKind TypeKind { get; }

        public virtual INamedTypeSymbol BaseType => null;

        public virtual ImmutableArray<INamedTypeSymbol> Interfaces
            => ImmutableArray.Create<INamedTypeSymbol>();

        public ImmutableArray<INamedTypeSymbol> AllInterfaces
            => ImmutableArray.Create<INamedTypeSymbol>();

        public bool IsReferenceType => false;

        public bool IsValueType => TypeKind == TypeKind.Struct || TypeKind == TypeKind.Enum;

        public bool IsAnonymousType => false;

        public bool IsTupleType => false;

        public bool IsNativeIntegerType => false;

        public static ImmutableArray<ITypeSymbol> TupleElementTypes => default;

        public static ImmutableArray<string> TupleElementNames => default;

        public new ITypeSymbol OriginalDefinition => this;

        public ISymbol FindImplementationForInterfaceMember(ISymbol interfaceMember) => null;

        public string ToDisplayString(NullableFlowState topLevelNullability, SymbolDisplayFormat format = null)
            => throw new System.NotImplementedException();

        public ImmutableArray<SymbolDisplayPart> ToDisplayParts(NullableFlowState topLevelNullability, SymbolDisplayFormat format = null)
            => throw new System.NotImplementedException();

        public string ToMinimalDisplayString(SemanticModel semanticModel, NullableFlowState topLevelNullability, int position, SymbolDisplayFormat format = null)
            => throw new System.NotImplementedException();

        public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, NullableFlowState topLevelNullability, int position, SymbolDisplayFormat format = null)
            => throw new System.NotImplementedException();

        public override bool IsNamespace => false;

        public override bool IsType => true;

        bool ITypeSymbol.IsRefLikeType => throw new System.NotImplementedException();

        bool ITypeSymbol.IsUnmanagedType => throw new System.NotImplementedException();

        bool ITypeSymbol.IsReadOnly => Modifiers.IsReadOnly;

        public NullableAnnotation NullableAnnotation { get; }

        public ITypeSymbol WithNullableAnnotation(NullableAnnotation nullableAnnotation)
        {
            if (this.NullableAnnotation == nullableAnnotation)
            {
                return this;
            }

            return CloneWithNullableAnnotation(nullableAnnotation);
        }

        protected sealed override CodeGenerationSymbol Clone()
            => CloneWithNullableAnnotation(this.NullableAnnotation);

        protected abstract CodeGenerationTypeSymbol CloneWithNullableAnnotation(NullableAnnotation nullableAnnotation);
    }
}

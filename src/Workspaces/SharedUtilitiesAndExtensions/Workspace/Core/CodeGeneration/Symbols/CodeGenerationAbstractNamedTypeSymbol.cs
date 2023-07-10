// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal abstract class CodeGenerationAbstractNamedTypeSymbol : CodeGenerationTypeSymbol, INamedTypeSymbol
    {
        public new INamedTypeSymbol OriginalDefinition { get; protected set; }

        public ImmutableArray<IFieldSymbol> TupleElements { get; protected set; }

        internal readonly ImmutableArray<CodeGenerationAbstractNamedTypeSymbol> TypeMembers;

        protected CodeGenerationAbstractNamedTypeSymbol(
            IAssemblySymbol containingAssembly,
            INamedTypeSymbol containingType,
            ImmutableArray<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            string name,
            SpecialType specialType,
            NullableAnnotation nullableAnnotation,
            ImmutableArray<CodeGenerationAbstractNamedTypeSymbol> typeMembers)
            : base(containingAssembly, containingType, attributes, declaredAccessibility, modifiers, name, specialType, nullableAnnotation)
        {
            this.TypeMembers = typeMembers;

            foreach (var member in typeMembers)
            {
                member.ContainingType = this;
            }
        }

        public override SymbolKind Kind => SymbolKind.NamedType;

        public override void Accept(SymbolVisitor visitor)
            => visitor.VisitNamedType(this);

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            => visitor.VisitNamedType(this);

        public override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitNamedType(this, argument);

        public INamedTypeSymbol Construct(params ITypeSymbol[] typeArguments)
        {
            if (typeArguments.Length == 0)
            {
                return this;
            }

            return new CodeGenerationConstructedNamedTypeSymbol(
                ConstructedFrom, typeArguments.ToImmutableArray(), this.TypeMembers);
        }

        public INamedTypeSymbol Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<NullableAnnotation> typeArgumentNullableAnnotations)
        {
            return new CodeGenerationConstructedNamedTypeSymbol(
                ConstructedFrom, typeArguments, this.TypeMembers);
        }

        public abstract int Arity { get; }
        public abstract bool IsGenericType { get; }
        public abstract bool IsUnboundGenericType { get; }
        public abstract bool IsScriptClass { get; }
        public abstract bool IsImplicitClass { get; }
        public abstract IEnumerable<string> MemberNames { get; }
        public abstract IMethodSymbol DelegateInvokeMethod { get; }
        public abstract INamedTypeSymbol EnumUnderlyingType { get; }
        protected abstract CodeGenerationNamedTypeSymbol ConstructedFrom { get; }
        INamedTypeSymbol INamedTypeSymbol.ConstructedFrom => this.ConstructedFrom;
        public abstract INamedTypeSymbol ConstructUnboundGenericType();
        public abstract ImmutableArray<IMethodSymbol> InstanceConstructors { get; }
        public abstract ImmutableArray<IMethodSymbol> StaticConstructors { get; }
        public abstract ImmutableArray<IMethodSymbol> Constructors { get; }
        public abstract ImmutableArray<ITypeSymbol> TypeArguments { get; }
        public abstract ImmutableArray<NullableAnnotation> TypeArgumentNullableAnnotations { get; }

        public ImmutableArray<CustomModifier> GetTypeArgumentCustomModifiers(int ordinal)
        {
            if (ordinal < 0 || ordinal >= Arity)
            {
                throw new IndexOutOfRangeException();
            }

            return ImmutableArray.Create<CustomModifier>();
        }

        public abstract ImmutableArray<ITypeParameterSymbol> TypeParameters { get; }

        public override string MetadataName
        {
            get
            {
                return this.Arity > 0
                    ? this.Name + "`" + Arity
                    : base.MetadataName;
            }
        }

        public ISymbol AssociatedSymbol { get; internal set; }

        public bool MightContainExtensionMethods => false;

        public bool IsComImport => false;

        public bool IsUnmanagedType => throw new NotImplementedException();

        public bool IsRefLikeType => Modifiers.IsRef;

        public INamedTypeSymbol NativeIntegerUnderlyingType => null;

        public INamedTypeSymbol TupleUnderlyingType => null;

        public bool IsSerializable => false;

        public bool IsFileLocal => Modifiers.IsFile;
    }
}

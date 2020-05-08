// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static INamedTypeSymbol NamedType(
            TypeKind typeKind,
            string name,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            ImmutableArray<ITypeSymbol> typeArguments = default,
            INamedTypeSymbol baseType = null,
            ImmutableArray<INamedTypeSymbol> interfaces = default,
            ImmutableArray<ISymbol> members = default,
            NullableAnnotation nullableAnnotation = default,
            ISymbol containingSymbol = null)
        {
            return new NamedTypeSymbol(
                CodeAnalysis.SpecialType.None,
                typeKind,
                attributes,
                declaredAccessibility,
                modifiers,
                name,
                typeArguments,
                baseType,
                interfaces,
                members,
                tupleElements: default,
                delegateInvokeMethod: null,
                nullableAnnotation,
                containingSymbol);
        }

        public static INamedTypeSymbol Class(
            string name,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            ImmutableArray<ITypeSymbol> typeArguments = default,
            INamedTypeSymbol baseType = null,
            ImmutableArray<INamedTypeSymbol> interfaces = default,
            ImmutableArray<ISymbol> members = default,
            NullableAnnotation nullableAnnotation = default,
            ISymbol containingSymbol = null)
        {
            return new NamedTypeSymbol(
                CodeAnalysis.SpecialType.None,
                TypeKind.Class,
                attributes,
                declaredAccessibility,
                modifiers,
                name,
                typeArguments,
                baseType,
                interfaces,
                members,
                tupleElements: default,
                delegateInvokeMethod: null,
                nullableAnnotation,
                containingSymbol);
        }

        public static INamedTypeSymbol Struct(
            string name,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            ImmutableArray<ITypeSymbol> typeArguments = default,
            INamedTypeSymbol baseType = null,
            ImmutableArray<INamedTypeSymbol> interfaces = default,
            ImmutableArray<ISymbol> members = default,
            NullableAnnotation nullableAnnotation = default,
            ISymbol containingSymbol = null)
        {

            return new NamedTypeSymbol(
                CodeAnalysis.SpecialType.None,
                TypeKind.Struct,
                attributes,
                declaredAccessibility,
                modifiers,
                name,
                typeArguments,
                baseType,
                interfaces,
                members,
                tupleElements: default,
                delegateInvokeMethod: null,
                nullableAnnotation,
                containingSymbol);
        }

        public static INamedTypeSymbol Interface(
            string name,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            ImmutableArray<ITypeSymbol> typeArguments = default,
            ImmutableArray<INamedTypeSymbol> interfaces = default,
            ImmutableArray<ISymbol> members = default,
            NullableAnnotation nullableAnnotation = default,
            ISymbol containingSymbol = null)
        {
            return new NamedTypeSymbol(
                CodeAnalysis.SpecialType.None,
                TypeKind.Struct,
                attributes,
                declaredAccessibility,
                modifiers,
                name,
                typeArguments,
                baseType: null,
                interfaces,
                members,
                tupleElements: default,
                delegateInvokeMethod: null,
                nullableAnnotation,
                containingSymbol);
        }

        public static INamedTypeSymbol Enum(
            string name,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            INamedTypeSymbol baseType = null,
            ImmutableArray<ISymbol> members = default,
            NullableAnnotation nullableAnnotation = default,
            ISymbol containingSymbol = null)
        {
            return new NamedTypeSymbol(
                CodeAnalysis.SpecialType.None,
                TypeKind.Enum,
                attributes,
                declaredAccessibility,
                modifiers,
                name,
                typeArguments: default,
                baseType,
                interfaces: default,
                members,
                tupleElements: default,
                delegateInvokeMethod: null,
                nullableAnnotation,
                containingSymbol);
        }

        public static INamedTypeSymbol Delegate(
            IMethodSymbol delegateInvokeMethod)
        {
            return new NamedTypeSymbol(
                CodeAnalysis.SpecialType.None,
                TypeKind.Delegate,
                attributes: default,
                declaredAccessibility: default,
                modifiers: default,
                name: null,
                typeArguments: default,
                baseType: null,
                interfaces: default,
                members: default,
                tupleElements: default,
                delegateInvokeMethod,
                nullableAnnotation: default,
                containingSymbol: null);
        }

        public static INamedTypeSymbol TupleType(
            ImmutableArray<IFieldSymbol> tupleElements,
            NullableAnnotation nullableAnnotation = NullableAnnotation.None)
        {
            return new NamedTypeSymbol(
                CodeAnalysis.SpecialType.None,
                TypeKind.Struct,
                attributes: default,
                declaredAccessibility: default,
                modifiers: default,
                name: null,
                typeArguments: default,
                baseType: null,
                interfaces: default,
                members: default,
                tupleElements,
                delegateInvokeMethod: null,
                nullableAnnotation,
                containingSymbol: null);
        }

        //public static INamedTypeSymbol DelegateType(ITypeSymbol type = null)
        //    => new DiscardSymbol(type);

        //public static INamedTypeSymbol AnonymousType(ITypeSymbol type = null)
        //    => new DiscardSymbol(type);

        public static INamedTypeSymbol SpecialType(
            SpecialType specialType,
            ImmutableArray<ITypeSymbol> typeArguments = default,
            NullableAnnotation nullableAnnotation = NullableAnnotation.None)
        {
            return new NamedTypeSymbol(
                specialType,
                typeKind: default,
                attributes: default,
                declaredAccessibility: default,
                modifiers: default,
                name: null,
                typeArguments,
                baseType: null,
                interfaces: default,
                members: default,
                tupleElements: default,
                delegateInvokeMethod: null,
                nullableAnnotation,
                containingSymbol: null);
        }

        public static INamedTypeSymbol With(
            this INamedTypeSymbol type,
            Optional<SpecialType> specialType = default,
            Optional<TypeKind> typeKind = default,
            Optional<ImmutableArray<AttributeData>> attributes = default,
            Optional<Accessibility> declaredAccessibility = default,
            Optional<SymbolModifiers> modifiers = default,
            Optional<string> name = default,
            Optional<ImmutableArray<ITypeSymbol>> typeArguments = default,
            Optional<INamedTypeSymbol> baseType = default,
            Optional<ImmutableArray<INamedTypeSymbol>> interfaces = default,
            Optional<ImmutableArray<ISymbol>> members = default,
            Optional<ImmutableArray<IFieldSymbol>> tupleElements = default,
            Optional<IMethodSymbol> delegateInvokeMethod = default,
            Optional<NullableAnnotation> nullableAnnotation = default,
            Optional<ISymbol> containingSymbol = default)
        {
            return new NamedTypeSymbol(
                specialType.GetValueOr(type.SpecialType),
                typeKind.GetValueOr(type.TypeKind),
                attributes.GetValueOr(type.GetAttributes()),
                declaredAccessibility.GetValueOr(type.DeclaredAccessibility),
                modifiers.GetValueOr(type.GetModifiers()),
                name.GetValueOr(type.Name),
                typeArguments.GetValueOr(type.TypeArguments),
                baseType.GetValueOr(type.BaseType),
                interfaces.GetValueOr(type.Interfaces),
                members.GetValueOr(type.GetMembers()),
                tupleElements.GetValueOr(type.TupleElements),
                delegateInvokeMethod.GetValueOr(type.DelegateInvokeMethod),
                nullableAnnotation.GetValueOr(type.NullableAnnotation),
                containingSymbol.GetValueOr(type.ContainingSymbol));
        }

        internal static INamedTypeSymbol GenerateValueTuple(
            ImmutableArray<IFieldSymbol> tupleElements, int start, int end)
        {
            var typeArguments = ArrayBuilder<ITypeSymbol>.GetInstance();

            // Break up the tuple into sets of 7 elements as that's the max ValueTuple can hold before recursing.
            for (int i = 0; i < 7; i++)
            {
                if (start < tupleElements.Length)
                    typeArguments.Add(tupleElements[start].Type);

                start++;
            }

            if (start < end)
                typeArguments.Add(GenerateValueTuple(tupleElements, start, end));

            return Struct(
                nameof(ValueTuple),
                typeArguments: typeArguments.ToImmutableAndFree(),
                containingSymbol: Namespace(
                    nameof(System),
                    containingSymbol: GlobalNamespace()));
        }

        internal static INamedTypeSymbol GenerateSystemType(SpecialType specialType)
        {
            switch (specialType)
            {
                case CodeAnalysis.SpecialType.System_Object: return GenerateSystemType(nameof(Object));
                case CodeAnalysis.SpecialType.System_Boolean: return GenerateSystemType(nameof(Boolean));
                case CodeAnalysis.SpecialType.System_Char: return GenerateSystemType(nameof(Char));
                case CodeAnalysis.SpecialType.System_SByte: return GenerateSystemType(nameof(SByte));
                case CodeAnalysis.SpecialType.System_Byte: return GenerateSystemType(nameof(Byte));
                case CodeAnalysis.SpecialType.System_Int16: return GenerateSystemType(nameof(Int16));
                case CodeAnalysis.SpecialType.System_UInt16: return GenerateSystemType(nameof(UInt16));
                case CodeAnalysis.SpecialType.System_Int32: return GenerateSystemType(nameof(Int32));
                case CodeAnalysis.SpecialType.System_UInt32: return GenerateSystemType(nameof(UInt32));
                case CodeAnalysis.SpecialType.System_Int64: return GenerateSystemType(nameof(Int64));
                case CodeAnalysis.SpecialType.System_UInt64: return GenerateSystemType(nameof(UInt64));
                case CodeAnalysis.SpecialType.System_Decimal: return GenerateSystemType(nameof(Decimal));
                case CodeAnalysis.SpecialType.System_Single: return GenerateSystemType(nameof(Single));
                case CodeAnalysis.SpecialType.System_Double: return GenerateSystemType(nameof(Double));
                case CodeAnalysis.SpecialType.System_String: return GenerateSystemType(nameof(String));
                case CodeAnalysis.SpecialType.System_DateTime: return GenerateSystemType(nameof(DateTime));
            }

            throw new NotImplementedException();

            static INamedTypeSymbol GenerateSystemType(string name)
            {
                return Class(
                    name,
                    containingSymbol: Namespace(
                        nameof(System),
                        containingSymbol: GlobalNamespace()));
            }
        }

        private class NamedTypeSymbol : TypeSymbol, INamedTypeSymbol
        {
            private readonly ImmutableArray<AttributeData> _attributes;
            private readonly ImmutableArray<ISymbol> _members;

            public NamedTypeSymbol(
                SpecialType specialType,
                TypeKind typeKind,
                ImmutableArray<AttributeData> attributes,
                Accessibility declaredAccessibility,
                SymbolModifiers modifiers,
                string name,
                ImmutableArray<ITypeSymbol> typeArguments,
                INamedTypeSymbol baseType,
                ImmutableArray<INamedTypeSymbol> interfaces,
                ImmutableArray<ISymbol> members,
                ImmutableArray<IFieldSymbol> tupleElements,
                IMethodSymbol delegateInvokeMethod,
                NullableAnnotation nullableAnnotation,
                ISymbol containingSymbol)
            {
                SpecialType = specialType;
                TypeKind = typeKind;
                _attributes = attributes;
                DeclaredAccessibility = declaredAccessibility;
                Modifiers = modifiers;
                Name = name;
                TypeArguments = typeArguments;
                BaseType = baseType;
                Interfaces = interfaces.NullToEmpty();
                TupleElements = tupleElements;
                DelegateInvokeMethod = delegateInvokeMethod;
                NullableAnnotation = nullableAnnotation;
                ContainingSymbol = containingSymbol;

                _members = members.NullToEmpty().SelectAsArray(
                    m => m is INamedTypeSymbol nt ? nt.With(containingSymbol: this) : m);
            }

            public override SymbolKind Kind => SymbolKind.NamedType;

            public IMethodSymbol DelegateInvokeMethod { get; }
            public ImmutableArray<IFieldSymbol> TupleElements { get; }
            public ImmutableArray<ITypeSymbol> TypeArguments { get; }

            public override Accessibility DeclaredAccessibility { get; }
            public override ImmutableArray<AttributeData> GetAttributes() => _attributes;
            public override ImmutableArray<INamedTypeSymbol> Interfaces { get; }
            public override ImmutableArray<ISymbol> GetMembers() => _members;
            public override INamedTypeSymbol BaseType { get; }
            public override ISymbol ContainingSymbol { get; }
            public override NullableAnnotation NullableAnnotation { get; }
            public override SpecialType SpecialType { get; }
            public override string Name { get; }
            public override SymbolModifiers Modifiers { get; }
            public override TypeKind TypeKind { get; }
            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitNamedType(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitNamedType(this);

            #region default implementation

            INamedTypeSymbol INamedTypeSymbol.OriginalDefinition => throw new NotImplementedException();
            public bool IsComImport => throw new NotImplementedException();
            public bool IsGenericType => throw new NotImplementedException();
            public bool IsImplicitClass => throw new NotImplementedException();
            public bool IsScriptClass => throw new NotImplementedException();
            public bool IsSerializable => throw new NotImplementedException();
            public bool IsUnboundGenericType => throw new NotImplementedException();
            public bool MightContainExtensionMethods => throw new NotImplementedException();
            public IEnumerable<string> MemberNames => throw new NotImplementedException();
            public ImmutableArray<CustomModifier> GetTypeArgumentCustomModifiers(int ordinal) => throw new NotImplementedException();
            public ImmutableArray<IMethodSymbol> Constructors => throw new NotImplementedException();
            public ImmutableArray<IMethodSymbol> InstanceConstructors => throw new NotImplementedException();
            public ImmutableArray<IMethodSymbol> StaticConstructors => throw new NotImplementedException();
            public ImmutableArray<ITypeParameterSymbol> TypeParameters => throw new NotImplementedException();
            public ImmutableArray<NullableAnnotation> TypeArgumentNullableAnnotations => throw new NotImplementedException();
            public INamedTypeSymbol Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<NullableAnnotation> typeArgumentNullableAnnotations) => throw new NotImplementedException();
            public INamedTypeSymbol Construct(params ITypeSymbol[] typeArguments) => throw new NotImplementedException();
            public INamedTypeSymbol ConstructedFrom => throw new NotImplementedException();
            public INamedTypeSymbol ConstructUnboundGenericType() => throw new NotImplementedException();
            public INamedTypeSymbol EnumUnderlyingType => throw new NotImplementedException();
            public INamedTypeSymbol NativeIntegerUnderlyingType => throw new NotImplementedException();
            public INamedTypeSymbol TupleUnderlyingType => throw new NotImplementedException();
            public int Arity => throw new NotImplementedException();
            public ISymbol AssociatedSymbol => throw new NotImplementedException();

            #endregion
        }
    }
}

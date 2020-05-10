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
                enumUnderlyingType: null,
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
                enumUnderlyingType: null,
                nullableAnnotation,
                containingSymbol);
        }

        public static INamedTypeSymbol Struct(
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
                enumUnderlyingType: null,
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
                TypeKind.Interface,
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
                enumUnderlyingType: null,
                nullableAnnotation,
                containingSymbol);
        }

        public static INamedTypeSymbol Enum(
            string name,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            ImmutableArray<ISymbol> members = default,
            NullableAnnotation nullableAnnotation = default,
            INamedTypeSymbol enumUnderlyingType = null,
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
                baseType: null,
                interfaces: default,
                members,
                tupleElements: default,
                delegateInvokeMethod: null,
                enumUnderlyingType,
                nullableAnnotation,
                containingSymbol);
        }

        public static INamedTypeSymbol Delegate(
            ITypeSymbol returnType,
            string name,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = default,
            SymbolModifiers modifiers = default,
            ImmutableArray<ITypeSymbol> typeArguments = default,
            ImmutableArray<IParameterSymbol> parameters = default)
        {
            return new NamedTypeSymbol(
                CodeAnalysis.SpecialType.None,
                TypeKind.Delegate,
                attributes,
                declaredAccessibility,
                modifiers,
                name,
                typeArguments,
                baseType: null,
                interfaces: default,
                members: default,
                tupleElements: default,
                delegateInvokeMethod: DelegateInvoke(returnType, parameters),
                enumUnderlyingType: null,
                nullableAnnotation: default,
                containingSymbol: null);
        }

        public static INamedTypeSymbol TupleType(
            ImmutableArray<IFieldSymbol> tupleElements = default,
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
                tupleElements.NullToEmpty(),
                delegateInvokeMethod: null,
                enumUnderlyingType: null,
                nullableAnnotation,
                containingSymbol: null);
        }

        public static INamedTypeSymbol WithSpecialType(this INamedTypeSymbol symbol, SpecialType specialType)
            => With(symbol, specialType: ToOptional(specialType));

        public static INamedTypeSymbol WithTypeKind(this INamedTypeSymbol symbol, TypeKind typeKind)
            => With(symbol, typeKind: ToOptional(typeKind));

        public static INamedTypeSymbol WithAttributes(this INamedTypeSymbol symbol, params AttributeData[] attributes)
            => WithAttributes(symbol, (IEnumerable<AttributeData>)attributes);

        public static INamedTypeSymbol WithAttributes(this INamedTypeSymbol symbol, IEnumerable<AttributeData> attributes)
            => WithAttributes(symbol, attributes.ToImmutableArray());

        public static INamedTypeSymbol WithAttributes(this INamedTypeSymbol symbol, ImmutableArray<AttributeData> attributes)
            => With(symbol, attributes: ToOptional(attributes));

        public static INamedTypeSymbol WithDeclaredAccessibility(this INamedTypeSymbol symbol, Accessibility declaredAccessibility)
            => With(symbol, declaredAccessibility: ToOptional(declaredAccessibility));

        public static INamedTypeSymbol WithModifiers(this INamedTypeSymbol symbol, SymbolModifiers modifiers)
            => With(symbol, modifiers: ToOptional(modifiers));

        public static INamedTypeSymbol WithName(this INamedTypeSymbol symbol, string name)
            => With(symbol, name: ToOptional(name));

        public static INamedTypeSymbol WithTypeArguments(this INamedTypeSymbol symbol, params ITypeSymbol[] typeArguments)
            => WithTypeArguments(symbol, (IEnumerable<ITypeSymbol>)typeArguments);

        public static INamedTypeSymbol WithTypeArguments(this INamedTypeSymbol symbol, IEnumerable<ITypeSymbol> typeArguments)
            => WithTypeArguments(symbol, typeArguments.ToImmutableArray());

        public static INamedTypeSymbol WithTypeArguments(this INamedTypeSymbol symbol, ImmutableArray<ITypeSymbol> typeArguments)
            => With(symbol, typeArguments: ToOptional(typeArguments));

        public static INamedTypeSymbol WithBaseType(this INamedTypeSymbol symbol, INamedTypeSymbol baseType)
            => With(symbol, baseType: ToOptional(baseType));

        public static INamedTypeSymbol WithInterfaces(this INamedTypeSymbol symbol, params INamedTypeSymbol[] interfaces)
            => WithInterfaces(symbol, (IEnumerable<INamedTypeSymbol>)interfaces);

        public static INamedTypeSymbol WithInterfaces(this INamedTypeSymbol symbol, IEnumerable<INamedTypeSymbol> interfaces)
            => WithInterfaces(symbol, interfaces.ToImmutableArray());

        public static INamedTypeSymbol WithInterfaces(this INamedTypeSymbol symbol, ImmutableArray<INamedTypeSymbol> interfaces)
            => With(symbol, interfaces: ToOptional(interfaces));

        public static INamedTypeSymbol WithMembers(this INamedTypeSymbol symbol, params ISymbol[] members)
            => WithMembers(symbol, (IEnumerable<ISymbol>)members);

        public static INamedTypeSymbol WithMembers(this INamedTypeSymbol symbol, IEnumerable<ISymbol> members)
            => WithMembers(symbol, members.ToImmutableArray());

        public static INamedTypeSymbol WithMembers(this INamedTypeSymbol symbol, ImmutableArray<ISymbol> members)
            => With(symbol, members: ToOptional(members));

        public static INamedTypeSymbol WithTupleElements(this INamedTypeSymbol symbol, params IFieldSymbol[] tupleElements)
            => WithTupleElements(symbol, (IEnumerable<IFieldSymbol>)tupleElements);

        public static INamedTypeSymbol WithTupleElements(this INamedTypeSymbol symbol, IEnumerable<IFieldSymbol> tupleElements)
            => WithTupleElements(symbol, tupleElements.ToImmutableArray());

        public static INamedTypeSymbol WithTupleElements(this INamedTypeSymbol symbol, ImmutableArray<IFieldSymbol> tupleElements)
            => With(symbol, tupleElements: ToOptional(tupleElements));

        public static INamedTypeSymbol WithDelegateInvokeMethod(this INamedTypeSymbol symbol, IMethodSymbol delegateInvokeMethod)
            => With(symbol, delegateInvokeMethod: ToOptional(delegateInvokeMethod));

        public static INamedTypeSymbol WithEnumUnderlyingType(this INamedTypeSymbol symbol, INamedTypeSymbol enumUnderlyingType)
            => With(symbol, enumUnderlyingType: ToOptional(enumUnderlyingType));

        private static INamedTypeSymbol WithNullableAnnotation(this INamedTypeSymbol arrayType, NullableAnnotation nullableAnnotation)
            => With(arrayType, nullableAnnotation: ToOptional(nullableAnnotation));

        public static INamedTypeSymbol WithContainingSymbol(this INamedTypeSymbol symbol, ISymbol containingSymbol)
            => With(symbol, containingSymbol: ToOptional(containingSymbol));

        private static INamedTypeSymbol With(
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
            Optional<INamedTypeSymbol> enumUnderlyingType = default,
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
                enumUnderlyingType.GetValueOr(type.EnumUnderlyingType),
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
                INamedTypeSymbol enumUnderlyingType,
                NullableAnnotation nullableAnnotation,
                ISymbol containingSymbol)
            {
                SpecialType = specialType;
                TypeKind = typeKind;
                _attributes = attributes.NullToEmpty();
                DeclaredAccessibility = declaredAccessibility;
                Modifiers = modifiers;
                Name = name;
                TypeArguments = typeArguments.NullToEmpty();
                BaseType = baseType;
                Interfaces = interfaces.NullToEmpty();
                TupleElements = tupleElements;
                DelegateInvokeMethod = delegateInvokeMethod;
                EnumUnderlyingType = enumUnderlyingType;
                NullableAnnotation = nullableAnnotation;

                ContainingSymbol = containingSymbol;

                _members = members.NullToEmpty().SelectAsArray(
                    s => s is INamedTypeSymbol n ? n.With(containingSymbol: this) : s);
            }

            public override SymbolKind Kind => SymbolKind.NamedType;

            public IMethodSymbol DelegateInvokeMethod { get; }
            public INamedTypeSymbol EnumUnderlyingType { get; }
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
            public INamedTypeSymbol NativeIntegerUnderlyingType => throw new NotImplementedException();
            public INamedTypeSymbol TupleUnderlyingType => throw new NotImplementedException();
            public int Arity => throw new NotImplementedException();
            public ISymbol AssociatedSymbol => throw new NotImplementedException();

            #endregion
        }
    }
}

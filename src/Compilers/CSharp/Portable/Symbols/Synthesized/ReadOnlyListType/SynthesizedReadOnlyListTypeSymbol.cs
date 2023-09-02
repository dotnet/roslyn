// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedReadOnlyListTypeSymbol : NamedTypeSymbol
    {
        private readonly ModuleSymbol _containingModule;
        private readonly ImmutableArray<NamedTypeSymbol> _interfaces;
        private readonly ImmutableArray<Symbol> _members;

        internal SynthesizedReadOnlyListTypeSymbol(SourceModuleSymbol containingModule, string name)
        {
            var compilation = containingModule.DeclaringCompilation;

            _containingModule = containingModule;
            Name = name;
            var typeParameter = new SynthesizedReadOnlyListTypeParameterSymbol(this);
            TypeParameters = ImmutableArray.Create<TypeParameterSymbol>(typeParameter);
            var typeArgs = GetTypeParametersAsTypeArguments();
            var arrayType = compilation.CreateArrayTypeSymbol(elementType: typeParameter);

            // PROTOTYPE: Test missing interfaces.
            var iEnumerable = compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
            var iEnumerableT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(typeArgs);
            var iReadOnlyCollectionT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IReadOnlyCollection_T).Construct(typeArgs);
            var iReadOnlyListT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IReadOnlyList_T).Construct(typeArgs);
            var iCollectionT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T).Construct(typeArgs);
            var iListT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IList_T).Construct(typeArgs);

            _interfaces = ImmutableArray.Create(
                iEnumerable,
                iEnumerableT,
                iReadOnlyCollectionT,
                iReadOnlyListT,
                iCollectionT,
                iListT);

            // PROTOTYPE: Test missing interface members.
            var membersBuilder = ArrayBuilder<Symbol>.GetInstance();
            membersBuilder.Add(
                new SynthesizedFieldSymbol(this, arrayType, "_items"));
            membersBuilder.Add(
                new SynthesizedReadOnlyListConstructor(this, arrayType));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerable__GetEnumerator),
                    generateGetEnumerator));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)getMember(iEnumerableT, compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IEnumerable_T__GetEnumerator)),
                    generateGetEnumeratorT));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    (PropertySymbol)getMember(iReadOnlyCollectionT, compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_IReadOnlyCollection_T__Count)),
                    generateCount));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    (PropertySymbol)getMember(iReadOnlyListT, compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_IReadOnlyList_T__Item)),
                    generateIndexer));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    (PropertySymbol)getMember(iCollectionT, compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_ICollection_T__Count)),
                    generateCount));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    (PropertySymbol)getMember(iCollectionT, compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_ICollection_T__IsReadOnly)),
                    generateIsReadOnly));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)getMember(iCollectionT, compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_ICollection_T__Add)),
                    generateNotSupportedException));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)getMember(iCollectionT, compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_ICollection_T__Clear)),
                    generateNotSupportedException));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)getMember(iCollectionT, compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_ICollection_T__Contains)),
                    generateNotSupportedException)); // PROTOTYPE: Should be supported.
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)getMember(iCollectionT, compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_ICollection_T__CopyTo)),
                    generateNotSupportedException)); // PROTOTYPE: Should be supported.
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)getMember(iCollectionT, compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_ICollection_T__Remove)),
                    generateNotSupportedException));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    (PropertySymbol)getMember(iListT, compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_IList_T__Item)),
                    generateIndexer,
                    generateNotSupportedException));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)getMember(iListT, compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_IList_T__IndexOf)),
                    generateNotSupportedException)); // PROTOTYPE: Should be supported.
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)getMember(iListT, compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_IList_T__Insert)),
                    generateNotSupportedException));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)getMember(iListT, compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_IList_T__RemoveAt)),
                    generateNotSupportedException));
            _members = membersBuilder.ToImmutableAndFree();

            // IEnumerable.GetEnumerator()
            static BoundStatement generateGetEnumerator(SyntheticBoundNodeFactory f, MethodSymbol method)
            {
                // PROTOTYPE: Test missing member.
                var getEnumerator = (MethodSymbol)method.DeclaringCompilation.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerable__GetEnumerator);
                var field = method.ContainingType.GetFieldsToEmit().Single();
                // return _items.GetEnumerator();
                return f.Return(
                    f.Call(
                        f.Field(f.This(), field),
                        getEnumerator));
            }

            // IEnumerable<T>.GetEnumerator()
            static BoundStatement generateGetEnumeratorT(SyntheticBoundNodeFactory f, MethodSymbol method)
            {
                // PROTOTYPE: Test missing member.
                var getEnumerator = (MethodSymbol)method.DeclaringCompilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IEnumerable_T__GetEnumerator);
                var field = method.ContainingType.GetFieldsToEmit().Single();
                // return _items.GetEnumerator();
                return f.Return(
                    f.Call(
                        f.Field(f.This(), field),
                        getEnumerator));
            }

            // IReadOnlyCollection<T>.Count, ICollection<T>.Count
            static BoundStatement generateCount(SyntheticBoundNodeFactory f, MethodSymbol method)
            {
                var field = method.ContainingType.GetFieldsToEmit().Single();
                // return _items.Length;
                return f.Return(
                    f.ArrayLength(
                        f.Field(f.This(), field)));
            }

            // ICollection<T>.IsReadOnly
            static BoundStatement generateIsReadOnly(SyntheticBoundNodeFactory f, MethodSymbol method)
            {
                // return true;
                return f.Return(f.Literal(true));
            }

            // IReadOnlyList<T>.this[], IList<T>.this[]
            static BoundStatement generateIndexer(SyntheticBoundNodeFactory f, MethodSymbol method)
            {
                var field = method.ContainingType.GetFieldsToEmit().Single();
                var parameter = method.Parameters[0];
                // return _items[index];
                return f.Return(
                    f.ArrayAccess(
                        f.Field(f.This(), field),
                        f.Parameter(parameter)));
            }

            static BoundStatement generateNotSupportedException(SyntheticBoundNodeFactory f, MethodSymbol method)
            {
                // PROTOTYPE: Test missing type and member.
                var constructor = (MethodSymbol)method.DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_NotSupportedException__ctor);
                // throw new System.NotSupportedException();
                return f.Throw(f.New(constructor));
            }

            // PROTOTYPE: Null-enable.
#nullable disable
            static Symbol getMember(NamedTypeSymbol container, Symbol? symbol)
            {
                return symbol is null ?
                    null :
                    symbol.SymbolAsMember(container);
            }
#nullable enable

            static void addProperty(ArrayBuilder<Symbol> builder, PropertySymbol property)
            {
                builder.Add(property);
                builder.AddIfNotNull(property.GetMethod);
                builder.AddIfNotNull(property.SetMethod);
            }
        }

        public override int Arity => 1;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters { get; }

        public override NamedTypeSymbol ConstructedFrom => this;

        public override bool MightContainExtensionMethods => false;

        public override string Name { get; }

        public override IEnumerable<string> MemberNames => GetMembers().Select(m => m.Name);

        public override Accessibility DeclaredAccessibility => Accessibility.Internal;

        public override bool IsSerializable => false;

        public override bool AreLocalsZeroed => true;

        public override TypeKind TypeKind => TypeKind.Class;

        public override bool IsRefLikeType => false;

        public override bool IsReadOnly => false;

        public override Symbol? ContainingSymbol => _containingModule.GlobalNamespace;

        internal override ModuleSymbol ContainingModule => _containingModule;

        public override AssemblySymbol ContainingAssembly => _containingModule.ContainingAssembly;

        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override bool IsStatic => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => true;

        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics => GetTypeParametersAsTypeArguments();

        internal override bool IsFileLocal => false;

        internal override FileIdentifier? AssociatedFileIdentifier => null;

        internal override bool MangleName => false;

        internal override bool HasDeclaredRequiredMembers => false;

        internal override bool HasCodeAnalysisEmbeddedAttribute => false;

        internal override bool IsInterpolatedStringHandlerType => false;

        internal override bool HasSpecialName => false;

        internal override bool IsComImport => false;

        internal override bool IsWindowsRuntimeImport => false;

        internal override bool ShouldAddWinRTMembers => false;

        internal override TypeLayout Layout => default;

        internal override CharSet MarshallingCharSet => DefaultMarshallingCharSet;

        internal override bool HasDeclarativeSecurity => false;

        internal override bool IsInterface => false;

        internal override NamedTypeSymbol? NativeIntegerUnderlyingType => null;

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => ContainingAssembly.GetSpecialType(SpecialType.System_Object);

        internal override bool IsRecord => false;

        internal override bool IsRecordStruct => false;

        internal override ObsoleteAttributeData? ObsoleteAttributeData => null;

        public override ImmutableArray<Symbol> GetMembers() => _members;

        public override ImmutableArray<Symbol> GetMembers(string name) => GetMembers().WhereAsArray(m => m.Name == name);

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity) => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name) => ImmutableArray<NamedTypeSymbol>.Empty;

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData) => throw ExceptionUtilities.Unreachable();

        internal override NamedTypeSymbol AsNativeInteger() => throw ExceptionUtilities.Unreachable();

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => ImmutableArray<string>.Empty;

        internal override AttributeUsageInfo GetAttributeUsageInfo() => default;

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved) => BaseTypeNoUseSiteDiagnostics;

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers() => GetMembersUnordered();

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name) => GetMembers(name);

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => _members.OfType<FieldSymbol>();

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => _interfaces;

        internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation() => SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>();

        internal override bool HasCollectionBuilderAttribute(out TypeSymbol? builderType, out string? methodName)
        {
            builderType = null;
            methodName = null;
            return false;
        }

        internal override bool HasInlineArrayAttribute(out int length)
        {
            length = 0;
            return false;
        }

        internal override bool HasPossibleWellKnownCloneMethod() => false;

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null) => _interfaces;

        internal override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls() => SpecializedCollections.EmptyEnumerable<(MethodSymbol Body, MethodSymbol Implemented)>();
    }
}

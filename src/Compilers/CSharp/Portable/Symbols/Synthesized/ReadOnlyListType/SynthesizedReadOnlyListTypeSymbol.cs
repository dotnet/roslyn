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
            var interfacesBuilder = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            interfacesBuilder.Add(compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable));
            interfacesBuilder.Add(compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(typeArgs));
            interfacesBuilder.Add(compilation.GetSpecialType(SpecialType.System_Collections_Generic_IReadOnlyCollection_T).Construct(typeArgs));
            interfacesBuilder.Add(compilation.GetSpecialType(SpecialType.System_Collections_Generic_IReadOnlyList_T).Construct(typeArgs));
            interfacesBuilder.Add(compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T).Construct(typeArgs));
            interfacesBuilder.Add(compilation.GetSpecialType(SpecialType.System_Collections_Generic_IList_T).Construct(typeArgs));
            _interfaces = interfacesBuilder.ToImmutableAndFree();

            // PROTOTYPE: Test missing interface members.
            var membersBuilder = ArrayBuilder<Symbol>.GetInstance();
            membersBuilder.Add(new SynthesizedFieldSymbol(this, arrayType, "_items"));
            membersBuilder.Add(new SynthesizedReadOnlyListConstructor(this, arrayType));
            membersBuilder.Add(new SynthesizedReadOnlyListGetEnumerator(this, (MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerable__GetEnumerator)));
            membersBuilder.Add(new SynthesizedReadOnlyListGetEnumeratorT(this, (MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IEnumerable_T__GetEnumerator)));
            // PROTOTYPE: Add these to WellKnownMember.
            addProperty(membersBuilder, new SynthesizedReadOnlyListCountProperty(this, (PropertySymbol)compilation.GetSpecialType(SpecialType.System_Collections_Generic_IReadOnlyCollection_T).Construct(typeArgs).GetMembers("Count").Single()));
            addProperty(membersBuilder, new SynthesizedReadOnlyListIndexerProperty(this, (PropertySymbol)compilation.GetSpecialType(SpecialType.System_Collections_Generic_IReadOnlyList_T).Construct(typeArgs).GetMembers("this[]").Single()));
            addProperty(membersBuilder, new SynthesizedReadOnlyListCountProperty(this, (PropertySymbol)compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T).Construct(typeArgs).GetMembers("Count").Single()));
            addProperty(membersBuilder, new SynthesizedReadOnlyListIsReadOnlyProperty(this, (PropertySymbol)compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T).Construct(typeArgs).GetMembers("IsReadOnly").Single()));
            membersBuilder.Add(new SynthesizedReadOnlyListNotSupportedMethod(this, (MethodSymbol)compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T).Construct(typeArgs).GetMembers("Add").Single()));
            membersBuilder.Add(new SynthesizedReadOnlyListNotSupportedMethod(this, (MethodSymbol)compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T).Construct(typeArgs).GetMembers("Clear").Single()));
            membersBuilder.Add(new SynthesizedReadOnlyListContainsMethod(this, (MethodSymbol)compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T).Construct(typeArgs).GetMembers("Contains").Single()));
            membersBuilder.Add(new SynthesizedReadOnlyListCopyToMethod(this, (MethodSymbol)compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T).Construct(typeArgs).GetMembers("CopyTo").Single()));
            membersBuilder.Add(new SynthesizedReadOnlyListNotSupportedMethod(this, (MethodSymbol)compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T).Construct(typeArgs).GetMembers("Remove").Single()));
            addProperty(membersBuilder, new SynthesizedReadOnlyListIndexerProperty(this, (PropertySymbol)compilation.GetSpecialType(SpecialType.System_Collections_Generic_IList_T).Construct(typeArgs).GetMembers("this[]").Single()));
            membersBuilder.Add(new SynthesizedReadOnlyListIndexOfMethod(this, (MethodSymbol)compilation.GetSpecialType(SpecialType.System_Collections_Generic_IList_T).Construct(typeArgs).GetMembers("IndexOf").Single()));
            membersBuilder.Add(new SynthesizedReadOnlyListNotSupportedMethod(this, (MethodSymbol)compilation.GetSpecialType(SpecialType.System_Collections_Generic_IList_T).Construct(typeArgs).GetMembers("Insert").Single()));
            membersBuilder.Add(new SynthesizedReadOnlyListNotSupportedMethod(this, (MethodSymbol)compilation.GetSpecialType(SpecialType.System_Collections_Generic_IList_T).Construct(typeArgs).GetMembers("RemoveAt").Single()));
            _members = membersBuilder.ToImmutableAndFree();

            static void addProperty(ArrayBuilder<Symbol> builder, PropertySymbol property)
            {
                builder.Add(property);
                if (property.GetMethod is { } getMethod)
                {
                    builder.Add(getMethod);
                }
                if (property.SetMethod is { } setMethod)
                {
                    builder.Add(setMethod);
                }
            }
        }

        public override int Arity => 1;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters { get; }

        public override NamedTypeSymbol ConstructedFrom => this;

        public override bool MightContainExtensionMethods => false;

        public override string Name { get; }

        public override IEnumerable<string> MemberNames => GetMembers().SelectAsArray(m => m.Name);

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

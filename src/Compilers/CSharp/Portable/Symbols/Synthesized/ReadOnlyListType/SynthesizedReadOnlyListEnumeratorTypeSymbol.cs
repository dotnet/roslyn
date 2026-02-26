// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedReadOnlyListEnumeratorTypeSymbol : NamedTypeSymbol
    {
        private readonly SynthesizedReadOnlyListTypeSymbol _containingType;
        private readonly ImmutableArray<NamedTypeSymbol> _interfaces;
        private readonly ImmutableArray<Symbol> _members;
        private readonly FieldSymbol _itemField;
        private readonly FieldSymbol _moveNextCalledField;

        public SynthesizedReadOnlyListEnumeratorTypeSymbol(SynthesizedReadOnlyListTypeSymbol containingType, SynthesizedReadOnlyListTypeParameterSymbol typeParameter)
        {
            _containingType = containingType;

            var compilation = containingType.DeclaringCompilation;
            var typeArgs = containingType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;

            _itemField = new SynthesizedFieldSymbol(this, typeParameter, "_item", isReadOnly: true);
            _moveNextCalledField = new SynthesizedFieldSymbol(this, compilation.GetSpecialType(SpecialType.System_Boolean), "_moveNextCalled", isReadOnly: false);

            var iDisposable = compilation.GetSpecialType(SpecialType.System_IDisposable);
            var iEnumerator = compilation.GetSpecialType(SpecialType.System_Collections_IEnumerator);
            var iEnumeratorT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerator_T).Construct(typeArgs);

            _interfaces = ImmutableArray.Create(
                iDisposable,
                iEnumerator,
                iEnumeratorT);

            var membersBuilder = ArrayBuilder<Symbol>.GetInstance();
            membersBuilder.Add(_itemField);
            membersBuilder.Add(_moveNextCalledField);
            membersBuilder.Add(
                new SynthesizedReadOnlyListEnumeratorConstructor(this, typeParameter));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    (PropertySymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__Current),
                    static (f, method, interfaceMethod) =>
                    {
                        var containingType = (SynthesizedReadOnlyListEnumeratorTypeSymbol)method.ContainingType;
                        var itemField = containingType._itemField;
                        var itemFieldReference = f.Field(f.This(), itemField);

                        Debug.Assert(method.ReturnType.IsObjectType());
                        Debug.Assert(itemFieldReference.Type.IsTypeParameter());

                        Conversion c = f.ClassifyEmitConversion(itemFieldReference, method.ReturnType);
                        Debug.Assert(c.IsImplicit);
                        Debug.Assert(c.IsBoxing);

                        // return (object)_item;
                        return f.Return(f.Convert(method.ReturnType, itemFieldReference, c));
                    }));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    ((PropertySymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IEnumerator_T__Current)).AsMember(iEnumeratorT),
                    static (f, method, interfaceMethod) =>
                    {
                        var containingType = (SynthesizedReadOnlyListEnumeratorTypeSymbol)method.ContainingType;
                        var itemField = containingType._itemField;
                        var itemFieldReference = f.Field(f.This(), itemField);
                        // return _item;
                        return f.Return(itemFieldReference);
                    }));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext)),
                    static (f, method, interfaceMethod) =>
                    {
                        var containingType = (SynthesizedReadOnlyListEnumeratorTypeSymbol)method.ContainingType;
                        var moveNextCalledField = containingType._moveNextCalledField;
                        var moveNextCalledFieldReference = f.Field(f.This(), moveNextCalledField);
                        // return _moveNextCalled ? false : (_moveNextCalled = true);
                        return f.Return(
                            f.Conditional(
                                moveNextCalledFieldReference,
                                f.Literal(false),
                                f.AssignmentExpression(
                                    moveNextCalledFieldReference,
                                    f.Literal(true)),
                                method.ReturnType));
                    }));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__Reset)),
                    static (f, method, interfaceMethod) =>
                    {
                        var containingType = (SynthesizedReadOnlyListEnumeratorTypeSymbol)method.ContainingType;
                        var moveNextCalledField = containingType._moveNextCalledField;
                        var moveNextCalledFieldReference = f.Field(f.This(), moveNextCalledField);
                        // _moveNextCalled = false;
                        // return;
                        return f.Block(f.Assignment(moveNextCalledFieldReference, f.Literal(false)), f.Return());
                    }));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose)),
                    static (f, method, interfaceMethod) => f.Return()));
            _members = membersBuilder.ToImmutableAndFree();
            return;

            static void addProperty(ArrayBuilder<Symbol> builder, PropertySymbol property)
            {
                Debug.Assert(property is { GetMethod: not null, SetMethod: null });
                builder.Add(property);
                builder.Add(property.GetMethod);
            }
        }

        public override int Arity => 0;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override NamedTypeSymbol ConstructedFrom => this;

        public override bool MightContainExtensions => false;

        public override string Name => "Enumerator";

        public override IEnumerable<string> MemberNames => GetMembers().Select(m => m.Name);

        public override Accessibility DeclaredAccessibility => Accessibility.Private;

        public override bool IsSerializable => false;

        public override bool AreLocalsZeroed => true;

        public override TypeKind TypeKind => TypeKind.Class;

        public override bool IsRefLikeType => false;

        internal override string? ExtensionGroupingName => null;

        internal sealed override string? ExtensionMarkerName => null;

        public override bool IsReadOnly => false;

        public override Symbol ContainingSymbol => _containingType;

        internal override ModuleSymbol ContainingModule => _containingType.ContainingModule;

        public override AssemblySymbol ContainingAssembly => _containingType.ContainingAssembly;

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

        internal override bool HasCompilerLoweringPreserveAttribute => false;

        internal override bool IsInterpolatedStringHandlerType => false;

        internal sealed override ParameterSymbol? ExtensionParameter => null;

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

        public override ImmutableArray<Symbol> GetMembers(string name) => GetMembers().WhereAsArray(static (m, name) => m.Name == name, name);

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity) => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name) => ImmutableArray<NamedTypeSymbol>.Empty;

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData) => throw ExceptionUtilities.Unreachable();

        internal override NamedTypeSymbol AsNativeInteger() => throw ExceptionUtilities.Unreachable();

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => ImmutableArray<string>.Empty;

        internal override AttributeUsageInfo GetAttributeUsageInfo() => default;

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved) => BaseTypeNoUseSiteDiagnostics;

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved) => _interfaces;

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers() => throw ExceptionUtilities.Unreachable();

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name) => throw ExceptionUtilities.Unreachable();

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => _members.OfType<FieldSymbol>();

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => _interfaces;

        internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation() => SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>();

        internal override bool GetGuidString(out string? guidString)
        {
            guidString = null;
            return false;
        }

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

        internal override bool HasAsyncMethodBuilderAttribute(out TypeSymbol? builderArgument)
        {
            builderArgument = null;
            return false;
        }

        internal override bool HasPossibleWellKnownCloneMethod() => false;

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null) => _interfaces;

        internal override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls() => SpecializedCollections.EmptyEnumerable<(MethodSymbol Body, MethodSymbol Implemented)>();
    }
}

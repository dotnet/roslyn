// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A synthesized type used during emit to allow temp locals of Span&lt;T&gt;
    /// of a specific length where the span storage is on the stack.
    /// <code>
    /// [InlineArray(N)] struct &lt;&gt;y__InlineArrayN&lt;T&gt; { private T _element0; }
    /// </code>
    /// </summary>
    internal sealed class SynthesizedInlineArrayTypeSymbol : NamedTypeSymbol
    {
        private readonly ModuleSymbol _containingModule;
        private readonly int _arrayLength;
        private readonly MethodSymbol _inlineArrayAttributeConstructor;
        private readonly ImmutableArray<FieldSymbol> _fields;

        internal SynthesizedInlineArrayTypeSymbol(SourceModuleSymbol containingModule, string name, int arrayLength, MethodSymbol inlineArrayAttributeConstructor)
        {
            Debug.Assert(arrayLength > 0);

            var typeParameter = new InlineArrayTypeParameterSymbol(this);
            var field = new SynthesizedFieldSymbol(this, typeParameter, "_element0");

            _containingModule = containingModule;
            _arrayLength = arrayLength;
            _inlineArrayAttributeConstructor = inlineArrayAttributeConstructor;
            _fields = ImmutableArray.Create<FieldSymbol>(field);
            Name = name;
            TypeParameters = ImmutableArray.Create<TypeParameterSymbol>(typeParameter);
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

        public override TypeKind TypeKind => TypeKind.Struct;

        public override bool IsRefLikeType => false;

        public override bool IsReadOnly => true;

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

        internal override bool MangleName => true;

        internal override bool HasDeclaredRequiredMembers => false;

        internal override bool HasCodeAnalysisEmbeddedAttribute => false;

        internal override bool GetGuidString(out string? guidString)
        {
            guidString = null;
            return false;
        }

        internal override bool IsInterpolatedStringHandlerType => false;

        internal override bool HasSpecialName => false;

        internal override bool IsComImport => false;

        internal override bool IsWindowsRuntimeImport => false;

        internal override bool ShouldAddWinRTMembers => false;

        internal override TypeLayout Layout => new TypeLayout(LayoutKind.Sequential, size: 1, alignment: 0);

        internal override CharSet MarshallingCharSet => DefaultMarshallingCharSet;

        internal override bool HasDeclarativeSecurity => false;

        internal override bool IsInterface => false;

        internal override NamedTypeSymbol? NativeIntegerUnderlyingType => null;

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => ContainingAssembly.GetSpecialType(SpecialType.System_ValueType);

        internal override bool IsRecord => false;

        internal override bool IsRecordStruct => false;

        internal override ObsoleteAttributeData? ObsoleteAttributeData => null;

        public override ImmutableArray<Symbol> GetMembers() => ImmutableArray<Symbol>.CastUp(_fields);

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

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers() => throw ExceptionUtilities.Unreachable();

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name) => throw ExceptionUtilities.Unreachable();

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => _fields;

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation() => SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>();

        internal override bool HasCollectionBuilderAttribute(out TypeSymbol? builderType, out string? methodName)
        {
            builderType = null;
            methodName = null;
            return false;
        }

        internal override bool HasInlineArrayAttribute(out int length)
        {
            length = _arrayLength;
            return true;
        }

        internal sealed override bool HasAsyncMethodBuilderAttribute(out TypeSymbol? builderArgument)
        {
            builderArgument = null;
            return false;
        }

        internal override bool HasPossibleWellKnownCloneMethod() => false;

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls() => SpecializedCollections.EmptyEnumerable<(MethodSymbol Body, MethodSymbol Implemented)>();

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = _containingModule.DeclaringCompilation;
            Debug.Assert(compilation is { });

            AddSynthesizedAttribute(
                ref attributes,
                SynthesizedAttributeData.Create(
                    moduleBuilder.Compilation,
                    _inlineArrayAttributeConstructor,
                    arguments: ImmutableArray.Create(new TypedConstant(compilation.GetSpecialType(SpecialType.System_Int32), TypedConstantKind.Primitive, _arrayLength)),
                    namedArguments: ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty));
        }

        private sealed class InlineArrayTypeParameterSymbol : TypeParameterSymbol
        {
            private readonly SynthesizedInlineArrayTypeSymbol _container;

            internal InlineArrayTypeParameterSymbol(SynthesizedInlineArrayTypeSymbol container)
            {
                _container = container;
            }

            public override string Name => "T";

            public override int Ordinal => 0;

            public override bool HasConstructorConstraint => false;

            public override TypeParameterKind TypeParameterKind => TypeParameterKind.Type;

            public override bool HasReferenceTypeConstraint => false;

            public override bool IsReferenceTypeFromConstraintTypes => false;

            public override bool HasNotNullConstraint => false;

            public override bool HasValueTypeConstraint => false;

            public override bool AllowsRefLikeType => false; // Span types do not support ref like type parameters for now

            public override bool IsValueTypeFromConstraintTypes => false;

            public override bool HasUnmanagedTypeConstraint => false;

            public override VarianceKind Variance => VarianceKind.None;

            public override Symbol ContainingSymbol => _container;

            public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;

            public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

            internal override bool? IsNotNullable => null;

            internal override bool? ReferenceTypeConstraintIsNullable => null;

            internal override void EnsureAllConstraintsAreResolved()
            {
            }

            internal override ImmutableArray<TypeWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress) => ImmutableArray<TypeWithAnnotations>.Empty;

            internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress) => ContainingAssembly.GetSpecialType(SpecialType.System_Object);

            internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress) => ContainingAssembly.GetSpecialType(SpecialType.System_Object);

            internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress) => ImmutableArray<NamedTypeSymbol>.Empty;
        }
    }
}

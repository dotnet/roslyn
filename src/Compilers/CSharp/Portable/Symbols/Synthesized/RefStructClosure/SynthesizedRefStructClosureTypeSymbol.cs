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
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A synthesized ref struct type generated when a lambda or anonymous method is converted to a
    /// type parameter constrained to a "function interface" (e.g. <c>System.IFunc&lt;T, TResult&gt;</c>
    /// or <c>System.IAction</c>) with <c>allows ref struct</c>. Instead of allocating a delegate, the
    /// compiler synthesizes this ref struct, which implements the function interface and whose
    /// <c>Invoke</c> method contains the lowered lambda body. See csharplang issue #10209.
    /// </summary>
    internal sealed class SynthesizedRefStructClosureTypeSymbol : NamedTypeSymbol
    {
        private readonly ModuleSymbol _containingModule;
        private readonly ImmutableArray<NamedTypeSymbol> _interfaces;
        private readonly ImmutableArray<Symbol> _members;
        private readonly SynthesizedRefStructClosureInvokeMethod _invokeMethod;

        internal SynthesizedRefStructClosureTypeSymbol(
            SourceModuleSymbol containingModule,
            string name,
            NamedTypeSymbol functionInterface,
            MethodSymbol interfaceInvokeMethod)
        {
            Debug.Assert(functionInterface.IsInterface);
            Debug.Assert((object)interfaceInvokeMethod.ContainingType == functionInterface ||
                         interfaceInvokeMethod.ContainingType.Equals(functionInterface, TypeCompareKind.ConsiderEverything));

            _containingModule = containingModule;
            Name = name;
            _interfaces = ImmutableArray.Create(functionInterface);

            _invokeMethod = new SynthesizedRefStructClosureInvokeMethod(this, interfaceInvokeMethod);
            _members = ImmutableArray.Create<Symbol>(_invokeMethod);
        }

        /// <summary>
        /// The single function interface this closure implements (e.g. <c>IFunc&lt;int, int&gt;</c>).
        /// </summary>
        internal NamedTypeSymbol FunctionInterface => _interfaces[0];

        /// <summary>
        /// The synthesized <c>Invoke</c> method holding the lowered lambda body.
        /// </summary>
        internal SynthesizedRefStructClosureInvokeMethod InvokeMethod => _invokeMethod;

        public override int Arity => 0;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override NamedTypeSymbol ConstructedFrom => this;

        public override bool MightContainExtensions => false;

        public override string Name { get; }

        public override IEnumerable<string> MemberNames => GetMembers().Select(m => m.Name);

        public override Accessibility DeclaredAccessibility => Accessibility.Internal;

        public override bool IsSerializable => false;

        public override bool AreLocalsZeroed => true;

        public override TypeKind TypeKind => TypeKind.Struct;

        public override bool IsRefLikeType => true;

        internal override string? ExtensionGroupingName => null;

        internal override string? ExtensionMarkerName => null;

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

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => ContainingAssembly.GetSpecialType(SpecialType.System_ValueType);

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

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);
            AddSynthesizedAttribute(ref attributes, DeclaringCompilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeIsByRefLikeAttribute(this));
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

        internal override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls()
            => SpecializedCollections.SingletonEnumerable((Body: (MethodSymbol)_invokeMethod, Implemented: _invokeMethod.ExplicitInterfaceImplementations[0]));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Cci;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedHotReloadExceptionSymbol : NamedTypeSymbol
    {
        public const string NamespaceName = "System.Runtime.CompilerServices";
        public const string TypeName = "HotReloadException";
        public const string CodeFieldName = "Code";

        private readonly NamedTypeSymbol _baseType;
        private readonly NamespaceSymbol _namespace;

        // constructor and field:
        private readonly ImmutableArray<Symbol> _members;

        public SynthesizedHotReloadExceptionSymbol(
            NamespaceSymbol containingNamespace,
            NamedTypeSymbol exceptionType,
            TypeSymbol stringType,
            TypeSymbol intType)
        {
            _namespace = containingNamespace;
            _baseType = exceptionType;

            _members =
            [
                new SynthesizedHotReloadExceptionConstructorSymbol(this, exceptionType, stringType, intType),
                new SynthesizedFieldSymbol(this, intType, CodeFieldName, DeclarationModifiers.Public, isReadOnly: true, isStatic: false),
            ];
        }

        public MethodSymbol Constructor
            => (MethodSymbol)_members[0];

        public FieldSymbol CodeField
            => (FieldSymbol)_members[1];

        public override ImmutableArray<Symbol> GetMembers()
           => _members;

        public override ImmutableArray<Symbol> GetMembers(string name)
            => name switch
            {
                WellKnownMemberNames.InstanceConstructorName => [Constructor],
                CodeFieldName => [CodeField],
                _ => []
            };

        public override IEnumerable<string> MemberNames
            => _members.Select(static m => m.Name);

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
            => [CodeField];

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => [];
        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name) => [];
        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity) => [];

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            AddSynthesizedAttribute(
                ref attributes,
                moduleBuilder.Compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
        }

        public override string Name => TypeName;
        public override int Arity => 0;
        public override ImmutableArray<TypeParameterSymbol> TypeParameters => [];
        public override bool IsImplicitlyDeclared => true;
        internal override ManagedKind GetManagedKind(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo) => ManagedKind.Managed;
        public override NamedTypeSymbol ConstructedFrom => this;
        public override bool MightContainExtensions => false;
        internal override bool HasDeclaredRequiredMembers => false;
        public override Accessibility DeclaredAccessibility => Accessibility.Internal;
        public override TypeKind TypeKind => TypeKind.Class;
        public override Symbol ContainingSymbol => _namespace;
        public override NamespaceSymbol ContainingNamespace => _namespace;
        public override ImmutableArray<Location> Locations => [];
        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => [];
        public override bool IsStatic => false;
        public override bool IsRefLikeType => false;

        internal override string? ExtensionGroupingName => null;
        internal override string? ExtensionMarkerName => null;

        public override bool IsReadOnly => false;
        public override bool IsAbstract => false;
        public override bool IsSealed => true;
        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics => [];
        internal override bool MangleName => false;
        internal sealed override bool IsFileLocal => false;
        internal sealed override FileIdentifier? AssociatedFileIdentifier => null;
        internal override bool HasCodeAnalysisEmbeddedAttribute => true;
        internal override bool HasCompilerLoweringPreserveAttribute => false;
        internal override bool IsInterpolatedStringHandlerType => false;
        internal sealed override ParameterSymbol? ExtensionParameter => null;
        internal override bool HasSpecialName => false;
        internal override bool IsComImport => false;
        internal override bool IsWindowsRuntimeImport => false;
        internal override bool ShouldAddWinRTMembers => false;
        public override bool IsSerializable => false;
        public sealed override bool AreLocalsZeroed => ContainingModule.AreLocalsZeroed;
        internal override TypeLayout Layout => default;
        internal override CharSet MarshallingCharSet => DefaultMarshallingCharSet;
        internal override bool HasDeclarativeSecurity => false;
        internal override bool IsInterface => false;
        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => _baseType;
        internal override ObsoleteAttributeData? ObsoleteAttributeData => null;
        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData) => throw ExceptionUtilities.Unreachable();
        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => [];
        internal override AttributeUsageInfo GetAttributeUsageInfo() => AttributeUsageInfo.Default;
        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved) => _baseType;
        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved) => [];
        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers() => GetMembers();
        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name) => GetMembers(name);
        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => [];
        internal override IEnumerable<SecurityAttribute>? GetSecurityInformation() => null;
        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null) => [];
        internal sealed override bool IsRecord => false;
        internal sealed override bool IsRecordStruct => false;
        internal sealed override bool HasPossibleWellKnownCloneMethod() => false;
        internal sealed override NamedTypeSymbol AsNativeInteger() => throw ExceptionUtilities.Unreachable();
        internal sealed override NamedTypeSymbol? NativeIntegerUnderlyingType => null;
        internal sealed override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls() => [];

        internal override bool GetGuidString([NotNullWhen(true)] out string? guidString)
        {
            guidString = null;
            return false;
        }

        internal sealed override bool HasInlineArrayAttribute(out int length)
        {
            length = 0;
            return false;
        }

        internal sealed override bool HasCollectionBuilderAttribute(out TypeSymbol? builderType, out string? methodName)
        {
            builderType = null;
            methodName = null;
            return false;
        }

        internal sealed override bool HasAsyncMethodBuilderAttribute(out TypeSymbol? builderArgument)
        {
            builderArgument = null;
            return false;
        }
    }
}

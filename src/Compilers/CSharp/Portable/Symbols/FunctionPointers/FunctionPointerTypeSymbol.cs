// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class FunctionPointerTypeSymbol : TypeSymbol
    {
        public static FunctionPointerTypeSymbol CreateFromSource(FunctionPointerTypeSyntax syntax, Binder typeBinder, BindingDiagnosticBag diagnostics, ConsList<TypeSymbol> basesBeingResolved, bool suppressUseSiteDiagnostics)
            => new FunctionPointerTypeSymbol(
                FunctionPointerMethodSymbol.CreateFromSource(
                    syntax,
                    typeBinder,
                    diagnostics,
                    basesBeingResolved,
                    suppressUseSiteDiagnostics));

        /// <summary>
        /// Creates a function pointer from individual parts. This method should only be used when diagnostics are not needed. This is
        /// intended for use in test code.
        /// </summary>
        public static FunctionPointerTypeSymbol CreateFromPartsForTests(
            CallingConvention callingConvention,
            TypeWithAnnotations returnType,
            ImmutableArray<CustomModifier> refCustomModifiers,
            RefKind returnRefKind,
            ImmutableArray<TypeWithAnnotations> parameterTypes,
            ImmutableArray<ImmutableArray<CustomModifier>> parameterRefCustomModifiers,
            ImmutableArray<RefKind> parameterRefKinds,
            CSharpCompilation compilation)
            => new FunctionPointerTypeSymbol(FunctionPointerMethodSymbol.CreateFromPartsForTest(callingConvention, returnType, refCustomModifiers, returnRefKind, parameterTypes, parameterRefCustomModifiers, parameterRefKinds, compilation));

        /// <summary>
        /// Creates a function pointer from individual parts. This method should only be used when diagnostics are not needed.
        /// </summary>
        public static FunctionPointerTypeSymbol CreateFromParts(
            CallingConvention callingConvention,
            ImmutableArray<CustomModifier> callingConventionModifiers,
            TypeWithAnnotations returnType,
            RefKind returnRefKind,
            ImmutableArray<TypeWithAnnotations> parameterTypes,
            ImmutableArray<RefKind> parameterRefKinds,
            CSharpCompilation compilation)
            => new FunctionPointerTypeSymbol(FunctionPointerMethodSymbol.CreateFromParts(callingConvention, callingConventionModifiers, returnType, returnRefKind, parameterTypes, parameterRefKinds, compilation));

        public static FunctionPointerTypeSymbol CreateFromMetadata(ModuleSymbol containingModule, Cci.CallingConvention callingConvention, ImmutableArray<ParamInfo<TypeSymbol>> retAndParamTypes)
            => new FunctionPointerTypeSymbol(
                FunctionPointerMethodSymbol.CreateFromMetadata(containingModule, callingConvention, retAndParamTypes));

        public FunctionPointerTypeSymbol SubstituteTypeSymbol(
            TypeWithAnnotations substitutedReturnType,
            ImmutableArray<TypeWithAnnotations> substitutedParameterTypes,
            ImmutableArray<CustomModifier> refCustomModifiers,
            ImmutableArray<ImmutableArray<CustomModifier>> paramRefCustomModifiers)
            => new FunctionPointerTypeSymbol(Signature.SubstituteParameterSymbols(substitutedReturnType, substitutedParameterTypes, refCustomModifiers, paramRefCustomModifiers));

        private FunctionPointerTypeSymbol(FunctionPointerMethodSymbol signature)
        {
            Signature = signature;
        }

        public FunctionPointerMethodSymbol Signature { get; }

        public override bool IsReferenceType => false;
        public override bool IsValueType => true;
        public override TypeKind TypeKind => TypeKind.FunctionPointer;
        public override bool IsRefLikeType => false;
        public override bool IsReadOnly => false;
        public override SymbolKind Kind => SymbolKind.FunctionPointerType;
        public override Symbol? ContainingSymbol => null;
        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;
        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;
        public override Accessibility DeclaredAccessibility => Accessibility.NotApplicable;
        public override bool IsStatic => false;
        public override bool IsAbstract => false;
        public override bool IsSealed => false;
        // Pointers do not support boxing, so they really have no base type.
        internal override NamedTypeSymbol? BaseTypeNoUseSiteDiagnostics => null;
        internal override ManagedKind GetManagedKind(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo) => ManagedKind.Unmanaged;
        internal override ObsoleteAttributeData? ObsoleteAttributeData => null;
        public override void Accept(CSharpSymbolVisitor visitor) => visitor.VisitFunctionPointerType(this);
        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor) => visitor.VisitFunctionPointerType(this);
        public override ImmutableArray<Symbol> GetMembers() => ImmutableArray<Symbol>.Empty;
        public override ImmutableArray<Symbol> GetMembers(string name) => ImmutableArray<Symbol>.Empty;
        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => ImmutableArray<NamedTypeSymbol>.Empty;
        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name) => ImmutableArray<NamedTypeSymbol>.Empty;
        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument a) => visitor.VisitFunctionPointerType(this, a);
        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override bool Equals(TypeSymbol t2, TypeCompareKind compareKind)
        {
            if (ReferenceEquals(this, t2))
            {
                return true;
            }

            if (!(t2 is FunctionPointerTypeSymbol other))
            {
                return false;
            }

            return Signature.Equals(other.Signature, compareKind);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(1, Signature.GetHashCode());
        }

        protected override ISymbol CreateISymbol()
        {
            return new PublicModel.FunctionPointerTypeSymbol(this, DefaultNullableAnnotation);
        }

        protected override ITypeSymbol CreateITypeSymbol(CodeAnalysis.NullableAnnotation nullableAnnotation)
        {
            Debug.Assert(nullableAnnotation != DefaultNullableAnnotation);
            return new PublicModel.FunctionPointerTypeSymbol(this, nullableAnnotation);
        }

        internal override void AddNullableTransforms(ArrayBuilder<byte> transforms)
        {
            Signature.AddNullableTransforms(transforms);
        }

        internal override bool ApplyNullableTransforms(byte defaultTransformFlag, ImmutableArray<byte> transforms, ref int position, out TypeSymbol result)
        {
            var newSignature = Signature.ApplyNullableTransforms(defaultTransformFlag, transforms, ref position);
            bool madeChanges = (object)Signature != newSignature;
            result = madeChanges ? new FunctionPointerTypeSymbol(newSignature) : this;
            return madeChanges;
        }

        internal override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            UseSiteInfo<AssemblySymbol> fromSignature = Signature.GetUseSiteInfo();

            if (fromSignature.DiagnosticInfo?.Code == (int)ErrorCode.ERR_BindToBogus && fromSignature.DiagnosticInfo.Arguments.AsSingleton() == (object)Signature)
            {
                return new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_BogusType, this));
            }

            return fromSignature;
        }

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo? result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return Signature.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes);
        }

        internal override TypeSymbol MergeEquivalentTypes(TypeSymbol other, VarianceKind variance)
        {
            Debug.Assert(this.Equals(other, TypeCompareKind.AllIgnoreOptions));
            var mergedSignature = Signature.MergeEquivalentTypes(((FunctionPointerTypeSymbol)other).Signature, variance);
            if ((object)mergedSignature != Signature)
            {
                return new FunctionPointerTypeSymbol(mergedSignature);
            }

            return this;
        }

        internal override TypeSymbol SetNullabilityForReferenceTypes(Func<TypeWithAnnotations, TypeWithAnnotations> transform)
        {
            var substitutedSignature = Signature.SetNullabilityForReferenceTypes(transform);
            if ((object)Signature != substitutedSignature)
            {
                return new FunctionPointerTypeSymbol(substitutedSignature);
            }

            return this;
        }

        /// <summary>
        /// For scenarios such as overriding with differing ref kinds (such as out vs in or ref)
        /// we need to compare function pointer parameters assuming that Ref matches RefReadonly/In
        /// and Out. This is done because you cannot overload on ref vs out vs in in regular method
        /// signatures, and we are disallowing similar overloads in source with function pointers.
        /// </summary>
        internal static bool RefKindEquals(TypeCompareKind compareKind, RefKind refKind1, RefKind refKind2)
            => (compareKind & TypeCompareKind.FunctionPointerRefMatchesOutInRefReadonly) != 0
               ? (refKind1 == RefKind.None) == (refKind2 == RefKind.None)
               : refKind1 == refKind2;

        /// <summary>
        /// For scenarios such as overriding with differing ref kinds (such as out vs in or ref)
        /// we need to compare function pointer parameters assuming that Ref matches RefReadonly/In
        /// and Out. For that reason, we must also ensure that GetHashCode returns equal hashcodes
        /// for types that only differ by the type of ref they have.
        /// </summary>
        internal static RefKind GetRefKindForHashCode(RefKind refKind)
            => refKind == RefKind.None ? RefKind.None : RefKind.Ref;

        /// <summary>
        /// Return true if the given type is valid as a calling convention modifier type.
        /// </summary>
        internal static bool IsCallingConventionModifier(NamedTypeSymbol modifierType)
        {
            Debug.Assert(modifierType.ContainingAssembly is not null || modifierType.IsErrorType());
            return (object?)modifierType.ContainingAssembly == modifierType.ContainingAssembly?.CorLibrary
                   && modifierType.Arity == 0
                   && modifierType.Name != "CallConv"
                   && modifierType.Name.StartsWith("CallConv", StringComparison.Ordinal)
                   && modifierType.IsCompilerServicesTopLevelType();
        }

        internal override bool IsRecord => false;

        internal override bool IsRecordStruct => false;

        internal override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls()
        {
            return SpecializedCollections.EmptyEnumerable<(MethodSymbol Body, MethodSymbol Implemented)>();
        }

        internal override bool HasInlineArrayAttribute(out int length)
        {
            length = 0;
            return false;
        }
    }
}

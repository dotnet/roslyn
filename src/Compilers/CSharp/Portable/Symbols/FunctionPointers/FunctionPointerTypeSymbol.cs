// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable enable

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
        public static FunctionPointerTypeSymbol CreateFromSource(FunctionPointerTypeSyntax syntax, Binder typeBinder, DiagnosticBag diagnostics, ConsList<TypeSymbol> basesBeingResolved, bool suppressUseSiteDiagnostics)
            => new FunctionPointerTypeSymbol(
                FunctionPointerMethodSymbol.CreateFromSource(
                    syntax,
                    typeBinder,
                    diagnostics,
                    basesBeingResolved,
                    suppressUseSiteDiagnostics));

        /// <summary>
        /// Creates a function pointer from individual parts. This method should only be used when diagnostics are not needed.
        /// </summary>
        public static FunctionPointerTypeSymbol CreateFromParts(
            TypeWithAnnotations returnType,
            RefKind returnRefKind,
            ImmutableArray<TypeWithAnnotations> parameterTypes,
            ImmutableArray<RefKind> parameterRefKinds,
            CSharpCompilation compilation)
            => new FunctionPointerTypeSymbol(FunctionPointerMethodSymbol.CreateFromParts(returnType, returnRefKind, parameterTypes, parameterRefKinds, compilation));

        public static FunctionPointerTypeSymbol CreateFromMetadata(Cci.CallingConvention callingConvention, ImmutableArray<ParamInfo<TypeSymbol>> retAndParamTypes)
            => new FunctionPointerTypeSymbol(
                FunctionPointerMethodSymbol.CreateFromMetadata(callingConvention, retAndParamTypes));

        public FunctionPointerTypeSymbol SubstituteTypeSymbol(
            TypeWithAnnotations substitutedReturnType,
            ImmutableArray<TypeWithAnnotations> substitutedParameterTypes,
            ImmutableArray<CustomModifier> refCustomModifiers,
            ImmutableArray<ImmutableArray<CustomModifier>> paramRefCustomModifiers)
            => new FunctionPointerTypeSymbol(Signature.SubstituteParameterSymbols(substitutedReturnType, substitutedParameterTypes, refCustomModifiers, paramRefCustomModifiers));

        public static (CallingConvention Convention, bool IsValid) GetCallingConvention(string convention) =>
            convention switch
            {
                "" => (CallingConvention.Default, true),
                "cdecl" => (CallingConvention.CDecl, true),
                "managed" => (CallingConvention.Default, true),
                "thiscall" => (CallingConvention.ThisCall, true),
                "stdcall" => (CallingConvention.Standard, true),
                _ => (CallingConvention.Default, false),
            };

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
        internal override ManagedKind GetManagedKind(ref HashSet<DiagnosticInfo>? useSiteDiagnostics) => ManagedKind.Unmanaged;
        internal override ObsoleteAttributeData? ObsoleteAttributeData => null;
        public override void Accept(CSharpSymbolVisitor visitor) => visitor.VisitFunctionPointerType(this);
        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor) => visitor.VisitFunctionPointerType(this);
        public override ImmutableArray<Symbol> GetMembers() => ImmutableArray<Symbol>.Empty;
        public override ImmutableArray<Symbol> GetMembers(string name) => ImmutableArray<Symbol>.Empty;
        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => ImmutableArray<NamedTypeSymbol>.Empty;
        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => ImmutableArray<NamedTypeSymbol>.Empty;
        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument a) => visitor.VisitFunctionPointerType(this, a);
        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override bool Equals(TypeSymbol t2, TypeCompareKind compareKind, IReadOnlyDictionary<TypeParameterSymbol, bool>? isValueTypeOverrideOpt = null)
        {
            if (ReferenceEquals(this, t2))
            {
                return true;
            }

            if (!(t2 is FunctionPointerTypeSymbol other))
            {
                return false;
            }

            return Signature.Equals(other.Signature, compareKind, isValueTypeOverrideOpt);
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

        internal override DiagnosticInfo? GetUseSiteDiagnostic()
        {
            DiagnosticInfo? fromSignature = Signature.GetUseSiteDiagnostic();

            if (fromSignature?.Code == (int)ErrorCode.ERR_BindToBogus && fromSignature.Arguments.AsSingleton() == (object)Signature)
            {
                return new CSDiagnosticInfo(ErrorCode.ERR_BogusType, this);
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
    }
}

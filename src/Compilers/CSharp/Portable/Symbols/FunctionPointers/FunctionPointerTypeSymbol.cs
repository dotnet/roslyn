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
    // PROTOTYPE(func-ptr): Support generic substitution and retargeting
    // PROTOTYPE(func-ptr): Match msvc's emitting of custom modifiers for calling convention
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

        public static FunctionPointerTypeSymbol CreateFromMetadata(Cci.CallingConvention callingConvention, ImmutableArray<ParamInfo<TypeSymbol>> retAndParamTypes)
            => new FunctionPointerTypeSymbol(
                FunctionPointerMethodSymbol.CreateFromMetadata(callingConvention, retAndParamTypes));

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
        public override SymbolKind Kind => SymbolKind.FunctionPointer;
        public override Symbol? ContainingSymbol => null;
        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;
        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;
        public override Accessibility DeclaredAccessibility => Accessibility.NotApplicable;
        public override bool IsStatic => false;
        public override bool IsAbstract => false;
        public override bool IsSealed => false;
        // Pointers do not support boxing, so they really have no base type.
        internal override NamedTypeSymbol? BaseTypeNoUseSiteDiagnostics => null;
        internal override ManagedKind ManagedKind => ManagedKind.Unmanaged;
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
            // PROTOTYPE(func-ptr): Implement
        }

        internal override bool ApplyNullableTransforms(byte defaultTransformFlag, ImmutableArray<byte> transforms, ref int position, out TypeSymbol result)
        {
            // PROTOTYPE(func-ptr): Implement
            result = this;
            return true;
        }

        internal override DiagnosticInfo? GetUseSiteDiagnostic()
        {
            return Signature.GetUseSiteDiagnostic();
        }

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo? result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return Signature.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes);
        }

        internal override TypeSymbol MergeEquivalentTypes(TypeSymbol other, VarianceKind variance)
        {
            // PROTOTYPE(func-ptr): Implement
            throw new NotImplementedException();
        }

        internal override TypeSymbol SetNullabilityForReferenceTypes(Func<TypeWithAnnotations, TypeWithAnnotations> transform)
        {
            // PROTOTYPE(func-ptr): Implement
            throw new NotImplementedException();
        }

    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    // Suppose we have class C<T> { void M<U>(T, U) {}} and additional class types X and Y.
    // C<> is a NamedTypeSymbol.
    // C<>.M<> is a MethodSymbol.
    // C<X> is a ConstructedTypeSymbol.
    // C<X>.M<> is a SubstitutedMethodSymbol. It has parameters of types X and U.
    // C<X>.M<Y> is a ConstructedMethodSymbol.
    internal class SubstitutedMethodSymbol : WrappedMethodSymbol
    {
        private readonly Symbol _containingSymbol;
        private readonly MethodSymbol _underlyingMethod;
        private readonly TypeMap _inputMap;
        private readonly MethodSymbol _constructedFrom;

        private TypeWithAnnotations.Boxed _lazyReturnType;
        private ImmutableArray<ParameterSymbol> _lazyParameters;
        private TypeMap _lazyMap;
        private ImmutableArray<TypeParameterSymbol> _lazyTypeParameters;

        //we want to compute these lazily since it may be expensive for the underlying symbol
        private ImmutableArray<MethodSymbol> _lazyExplicitInterfaceImplementations;
        private OverriddenOrHiddenMembersResult _lazyOverriddenOrHiddenMembers;

        private int _hashCode; // computed on demand

        internal SubstitutedMethodSymbol(NamedTypeSymbol containingSymbol, MethodSymbol originalDefinition)
            : this(containingSymbol, containingSymbol.TypeSubstitution, originalDefinition, constructedFrom: null)
        {
            Debug.Assert(containingSymbol is SubstitutedNamedTypeSymbol || containingSymbol is SubstitutedErrorTypeSymbol);
            Debug.Assert(TypeSymbol.Equals(originalDefinition.ContainingType, containingSymbol.OriginalDefinition, TypeCompareKind.ConsiderEverything2));
        }

        protected SubstitutedMethodSymbol(Symbol containingSymbol, TypeMap map, MethodSymbol originalDefinition, MethodSymbol constructedFrom)
        {
            Debug.Assert((object)originalDefinition != null);
            Debug.Assert(originalDefinition.IsDefinition);
            _containingSymbol = containingSymbol;
            _underlyingMethod = originalDefinition;
            _inputMap = map;
            if ((object)constructedFrom != null)
            {
                _constructedFrom = constructedFrom;
                Debug.Assert(ReferenceEquals(constructedFrom.ConstructedFrom, constructedFrom));
                _lazyTypeParameters = constructedFrom.TypeParameters;
                _lazyMap = map;
            }
            else
            {
                _constructedFrom = this;
            }
        }

        public override MethodSymbol UnderlyingMethod
        {
            get
            {
                return _underlyingMethod;
            }
        }

        public override MethodSymbol ConstructedFrom
        {
            get
            {
                return _constructedFrom;
            }
        }

        private TypeMap Map
        {
            get
            {
                EnsureMapAndTypeParameters();
                return _lazyMap;
            }
        }

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                EnsureMapAndTypeParameters();
                return _lazyTypeParameters;
            }
        }

        private void EnsureMapAndTypeParameters()
        {
            if (!RoslynImmutableInterlocked.VolatileRead(ref _lazyTypeParameters).IsDefault)
            {
                return;
            }

            ImmutableArray<TypeParameterSymbol> typeParameters;
            Debug.Assert(ReferenceEquals(_constructedFrom, this));

            // We're creating a new unconstructed Method from another; alpha-rename type parameters.
            var newMap = _inputMap.WithAlphaRename(this.OriginalDefinition, this, propagateAttributes: false, out typeParameters);

            var prevMap = Interlocked.CompareExchange(ref _lazyMap, newMap, null);
            if (prevMap != null)
            {
                // There is a race with another thread who has already set the map
                // need to ensure that typeParameters, matches the map
                typeParameters = prevMap.SubstituteTypeParameters(this.OriginalDefinition.TypeParameters);
            }

            ImmutableInterlocked.InterlockedCompareExchange(ref _lazyTypeParameters, typeParameters, default(ImmutableArray<TypeParameterSymbol>));
            Debug.Assert(_lazyTypeParameters != null);
        }

        public sealed override AssemblySymbol ContainingAssembly
        {
            get
            {
                return OriginalDefinition.ContainingAssembly;
            }
        }

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get
            {
                return GetTypeParametersAsTypeArguments();
            }
        }

        public sealed override MethodSymbol OriginalDefinition
        {
            get
            {
                return _underlyingMethod;
            }
        }

        internal sealed override MethodSymbol CallsiteReducedFromMethod
        {
            get
            {
                var method = OriginalDefinition.ReducedFrom;
                return ((object)method == null) ? null : method.Construct(this.TypeArgumentsWithAnnotations);
            }
        }

        public override TypeSymbol ReceiverType
        {
            get
            {
                var reduced = this.CallsiteReducedFromMethod;
                if ((object)reduced == null)
                {
                    return this.ContainingType;
                }

                return reduced.Parameters[0].Type;
            }
        }

        public override TypeWithAnnotations GetTypeInferredDuringReduction(TypeParameterSymbol reducedFromTypeParameter)
        {
            // This will throw if API shouldn't be supported or there is a problem with the argument.
            var notUsed = OriginalDefinition.GetTypeInferredDuringReduction(reducedFromTypeParameter);

            Debug.Assert(notUsed.Type is null && OriginalDefinition.ReducedFrom is not null);
            return this.TypeArgumentsWithAnnotations[reducedFromTypeParameter.Ordinal];
        }

        public sealed override MethodSymbol ReducedFrom
        {
            get
            {
                return OriginalDefinition.ReducedFrom;
            }
        }

        public sealed override Symbol ContainingSymbol
        {
            get
            {
                return _containingSymbol;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return _containingSymbol is NamedTypeSymbol nt ? nt : _containingSymbol.ContainingType;
            }
        }

        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.OriginalDefinition.GetAttributes();
        }

        public override ImmutableArray<CSharpAttributeData> GetReturnTypeAttributes()
        {
            return this.OriginalDefinition.GetReturnTypeAttributes();
        }

        internal sealed override UnmanagedCallersOnlyAttributeData GetUnmanagedCallersOnlyAttributeData(bool forceComplete)
            => this.OriginalDefinition.GetUnmanagedCallersOnlyAttributeData(forceComplete);

        internal sealed override bool HasSpecialNameAttribute => throw ExceptionUtilities.Unreachable();

        public sealed override Symbol AssociatedSymbol
        {
            get
            {
                Symbol underlying = OriginalDefinition.AssociatedSymbol;
                return ((object)underlying == null) ? null : underlying.SymbolAsMember(ContainingType);
            }
        }

        public sealed override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                if (_lazyReturnType == null)
                {
                    var returnType = Map.SubstituteType(OriginalDefinition.ReturnTypeWithAnnotations);
                    Interlocked.CompareExchange(ref _lazyReturnType, new TypeWithAnnotations.Boxed(returnType), null);
                }
                return _lazyReturnType.Value;
            }
        }

        public sealed override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                return Map.SubstituteCustomModifiers(OriginalDefinition.RefCustomModifiers);
            }
        }

        public sealed override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_lazyParameters.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyParameters, SubstituteParameters());
                }

                return _lazyParameters;
            }
        }

        internal sealed override bool IsExplicitInterfaceImplementation
        {
            get { return this.OriginalDefinition.IsExplicitInterfaceImplementation; }
        }

        public sealed override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (!ReferenceEquals(this.ConstructedFrom, this))
                {
                    return ImmutableArray<MethodSymbol>.Empty;
                }

                if (_lazyExplicitInterfaceImplementations.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(
                        ref _lazyExplicitInterfaceImplementations,
                        ExplicitInterfaceHelpers.SubstituteExplicitInterfaceImplementations(this.OriginalDefinition.ExplicitInterfaceImplementations, Map),
                        default(ImmutableArray<MethodSymbol>));
                }
                return _lazyExplicitInterfaceImplementations;
            }
        }

        internal sealed override OverriddenOrHiddenMembersResult OverriddenOrHiddenMembers
        {
            get
            {
                if (_lazyOverriddenOrHiddenMembers == null)
                {
                    // We need to compute the overridden or hidden members for this type, rather than applying
                    // our type map to those of the underlying type, because the substitution may have introduced
                    // ambiguities.
                    Interlocked.CompareExchange(ref _lazyOverriddenOrHiddenMembers, this.MakeOverriddenOrHiddenMembers(), null);
                }
                return _lazyOverriddenOrHiddenMembers;
            }
        }

        internal sealed override bool CallsAreOmitted(SyntaxTree syntaxTree)
        {
            return OriginalDefinition.CallsAreOmitted(syntaxTree);
        }

        internal sealed override TypeMap TypeSubstitution
        {
            get { return this.Map; }
        }

#nullable enable

        internal sealed override bool TryGetThisParameter(out ParameterSymbol? thisParameter)
        {
            // Required in EE scenarios.  Specifically, the EE binds in the context of a 
            // substituted method, whereas the core compiler always binds within the
            // context of an original definition.  
            // There should never be any reason to call this in normal compilation
            // scenarios, but the behavior should be sensible if it does occur.
            ParameterSymbol? originalThisParameter;
            if (!OriginalDefinition.TryGetThisParameter(out originalThisParameter))
            {
                thisParameter = null;
                return false;
            }

            thisParameter = originalThisParameter is not null
                ? new ThisParameterSymbol(this)
                : null;
            return true;
        }

#nullable disable

        internal override int TryGetOverloadResolutionPriority()
            => OriginalDefinition.TryGetOverloadResolutionPriority();

        private ImmutableArray<ParameterSymbol> SubstituteParameters()
        {
            var unsubstitutedParameters = OriginalDefinition.Parameters;
            int count = unsubstitutedParameters.Length;

            if (count == 0)
            {
                return ImmutableArray<ParameterSymbol>.Empty;
            }
            else
            {
                var substituted = ArrayBuilder<ParameterSymbol>.GetInstance(count);
                TypeMap map = Map;
                foreach (var p in unsubstitutedParameters)
                {
                    substituted.Add(new SubstitutedParameterSymbol(this, map, p));
                }

                return substituted.ToImmutableAndFree();
            }
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool IsNullableAnalysisEnabled() => throw ExceptionUtilities.Unreachable();

        private int ComputeHashCode()
        {
            int code = this.OriginalDefinition.GetHashCode();
            int containingHashCode;

            // If the containing type of the original definition is the same as our containing type
            // it's possible that we will compare equal to the original definition under certain conditions 
            // (e.g, ignoring nullability) and want to retain the same hashcode. As such, consider only
            // the original definition for the hashcode when we know equality is possible
            containingHashCode = _containingSymbol.GetHashCode();
            if (containingHashCode == this.OriginalDefinition.ContainingType.GetHashCode() &&
                wasConstructedForAnnotations(this))
            {
                return code;
            }

            code = Hash.Combine(containingHashCode, code);

            // Unconstructed methods may contain alpha-renamed type parameters while	
            // still be considered equal; we do not want to give a different hashcode to such types.	
            //	
            // Example:	
            //   Having original method A<U>.Goo<V>() we create two _unconstructed_ methods	
            //    A<int>.Goo<V'>	
            //    A<int>.Goo<V">     	
            //  Note that V' and V" are type parameters substituted via alpha-renaming of original V	
            //  These are different objects, but represent the same "type parameter at index 1"	
            //	
            //  In short - we are not interested in the type arguments of unconstructed methods.	
            if ((object)ConstructedFrom != (object)this)
            {
                foreach (var arg in this.TypeArgumentsWithAnnotations)
                {
                    code = Hash.Combine(arg.Type, code);
                }
            }

            // 0 means that hashcode is not initialized. 
            // in a case we really get 0 for the hashcode, tweak it by +1
            if (code == 0)
            {
                code++;
            }

            return code;

            static bool wasConstructedForAnnotations(SubstitutedMethodSymbol method)
            {
                var typeArguments = method.TypeArgumentsWithAnnotations;
                var typeParameters = method.OriginalDefinition.TypeParameters;

                for (int i = 0; i < typeArguments.Length; i++)
                {
                    if (!typeParameters[i].Equals(
                         typeArguments[i].Type,
                         TypeCompareKind.ConsiderEverything))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public sealed override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            MethodSymbol other = obj as MethodSymbol;
            if ((object)other == null) return false;

            if ((object)this.OriginalDefinition != (object)other.OriginalDefinition &&
                this.OriginalDefinition != other.OriginalDefinition)
            {
                return false;
            }

            // This checks if the methods have the same definition and the type parameters on the containing types have been
            // substituted in the same way.
            if (!TypeSymbol.Equals(this.ContainingType, other.ContainingType, compareKind)) return false;

            // If both are declarations, then we don't need to check type arguments
            // If exactly one is a declaration, then they re not equal
            bool selfIsDeclaration = (object)this == (object)this.ConstructedFrom;
            bool otherIsDeclaration = (object)other == (object)other.ConstructedFrom;
            // PERF: VSadov specifically replaced the short-circuited operators in changeset #24717.
            if (selfIsDeclaration | otherIsDeclaration)
            {
                return selfIsDeclaration & otherIsDeclaration;
            }

            // This checks if the type parameters on the method itself have been substituted in the same way.
            int arity = this.Arity;
            for (int i = 0; i < arity; i++)
            {
                if (!this.TypeArgumentsWithAnnotations[i].Equals(other.TypeArgumentsWithAnnotations[i], compareKind))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            int code = _hashCode;
            if (code == 0)
            {
                code = ComputeHashCode();
                _hashCode = code;
            }

            return code;
        }

        internal sealed override bool HasAsyncMethodBuilderAttribute(out TypeSymbol builderArgument)
        {
            return _underlyingMethod.HasAsyncMethodBuilderAttribute(out builderArgument);
        }

        internal sealed override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
            => throw ExceptionUtilities.Unreachable();
    }
}

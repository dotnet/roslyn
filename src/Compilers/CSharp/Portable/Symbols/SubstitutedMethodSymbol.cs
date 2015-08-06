// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    // Suppose we have class C<T> { void M<U>(T, U) {}} and additional class types X and Y.
    // C<> is a NamedTypeSymbol.
    // C<>.M<> is a MethodSymbol.
    // C<X> is a ConstructedTypeSymbol.
    // C<X>.M<> is a SubstitutedMethodSymbol. It has parameters of types X and U.
    // C<X>.M<Y> is a ConstructedMethodSymbol.
    internal class SubstitutedMethodSymbol : MethodSymbol
    {
        private readonly NamedTypeSymbol _containingType;
        protected readonly MethodSymbol originalDefinition;
        private readonly TypeMap _inputMap;
        private readonly MethodSymbol _constructedFrom;

        private TypeSymbol _lazyReturnType;
        private ImmutableArray<ParameterSymbol> _lazyParameters;
        private TypeMap _lazyMap;
        private ImmutableArray<TypeParameterSymbol> _lazyTypeParameters;

        //we want to compute these lazily since it may be expensive for the underlying symbol
        private ImmutableArray<MethodSymbol> _lazyExplicitInterfaceImplementations;
        private OverriddenOrHiddenMembersResult _lazyOverriddenOrHiddenMembers;

        private int _hashCode; // computed on demand

        internal SubstitutedMethodSymbol(SubstitutedNamedTypeSymbol containingSymbol, MethodSymbol originalDefinition)
            : this(containingSymbol, containingSymbol.TypeSubstitution, originalDefinition, constructedFrom: null)
        {
        }

        protected SubstitutedMethodSymbol(NamedTypeSymbol containingSymbol, TypeMap map, MethodSymbol originalDefinition, MethodSymbol constructedFrom)
        {
            Debug.Assert(originalDefinition.IsDefinition);
            _containingType = containingSymbol;
            this.originalDefinition = originalDefinition;
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

        public override MethodSymbol ConstructedFrom
        {
            get
            {
                return _constructedFrom;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return originalDefinition.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
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
            if (!_lazyTypeParameters.IsDefault)
            {
                return;
            }

            ImmutableArray<TypeParameterSymbol> typeParameters;
            Debug.Assert(ReferenceEquals(_constructedFrom, this));

            // We're creating a new unconstructed Method from another; alpha-rename type parameters.
            var newMap = _inputMap.WithAlphaRename(this.originalDefinition, this, out typeParameters);

            var prevMap = Interlocked.CompareExchange(ref _lazyMap, newMap, null);
            if (prevMap != null)
            {
                // There is a race with another thread who has already set the map
                // need to ensure that typeParameters, matches the map
                typeParameters = prevMap.SubstituteTypeParameters(this.originalDefinition.TypeParameters);
            }

            ImmutableInterlocked.InterlockedCompareExchange(ref _lazyTypeParameters, typeParameters, default(ImmutableArray<TypeParameterSymbol>));
            Debug.Assert(_lazyTypeParameters != null);
        }

        internal sealed override Microsoft.Cci.CallingConvention CallingConvention
        {
            get
            {
                return originalDefinition.CallingConvention;
            }
        }

        public sealed override int Arity
        {
            get
            {
                return originalDefinition.Arity;
            }
        }

        public sealed override string Name
        {
            get
            {
                return originalDefinition.Name;
            }
        }

        internal sealed override bool HasSpecialName
        {
            get
            {
                return originalDefinition.HasSpecialName;
            }
        }

        internal sealed override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get
            {
                return originalDefinition.ImplementationAttributes;
            }
        }

        internal sealed override bool RequiresSecurityObject
        {
            get
            {
                return originalDefinition.RequiresSecurityObject;
            }
        }

        public sealed override DllImportData GetDllImportData()
        {
            return originalDefinition.GetDllImportData();
        }

        internal sealed override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get { return originalDefinition.ReturnValueMarshallingInformation; }
        }

        internal sealed override bool HasDeclarativeSecurity
        {
            get { return originalDefinition.HasDeclarativeSecurity; }
        }

        internal sealed override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            return originalDefinition.GetSecurityInformation();
        }

        internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return originalDefinition.GetAppliedConditionalSymbols();
        }

        public sealed override AssemblySymbol ContainingAssembly
        {
            get
            {
                return originalDefinition.ContainingAssembly;
            }
        }

        public sealed override ImmutableArray<Location> Locations
        {
            get
            {
                return originalDefinition.Locations;
            }
        }

        public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return originalDefinition.DeclaringSyntaxReferences;
            }
        }

        public override ImmutableArray<TypeSymbol> TypeArguments
        {
            get
            {
                return TypeParameters.Cast<TypeParameterSymbol, TypeSymbol>();
            }
        }

        public sealed override MethodSymbol OriginalDefinition
        {
            get
            {
                return originalDefinition;
            }
        }

        public sealed override bool IsExtern
        {
            get
            {
                return originalDefinition.IsExtern;
            }
        }

        public sealed override bool IsSealed
        {
            get
            {
                return originalDefinition.IsSealed;
            }
        }

        public sealed override bool IsVirtual
        {
            get
            {
                return originalDefinition.IsVirtual;
            }
        }

        public sealed override bool IsAsync
        {
            get
            {
                return originalDefinition.IsAsync;
            }
        }

        public sealed override bool IsAbstract
        {
            get
            {
                return originalDefinition.IsAbstract;
            }
        }

        public sealed override bool IsOverride
        {
            get
            {
                return originalDefinition.IsOverride;
            }
        }

        public sealed override bool IsStatic
        {
            get
            {
                return originalDefinition.IsStatic;
            }
        }

        public sealed override bool IsExtensionMethod
        {
            get
            {
                return originalDefinition.IsExtensionMethod;
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return originalDefinition.ObsoleteAttributeData;
            }
        }

        internal sealed override MethodSymbol CallsiteReducedFromMethod
        {
            get
            {
                var method = originalDefinition.ReducedFrom;
                return ((object)method == null) ? null : method.Construct(this.TypeArguments);
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

        public override TypeSymbol GetTypeInferredDuringReduction(TypeParameterSymbol reducedFromTypeParameter)
        {
            // This will throw if API shouldn't be supported or there is a problem with the argument.
            var notUsed = originalDefinition.GetTypeInferredDuringReduction(reducedFromTypeParameter);

            Debug.Assert((object)notUsed == null && (object)originalDefinition.ReducedFrom != null);
            return this.TypeArguments[reducedFromTypeParameter.Ordinal];
        }

        public sealed override MethodSymbol ReducedFrom
        {
            get
            {
                return originalDefinition.ReducedFrom;
            }
        }

        public sealed override bool HidesBaseMethodsByName
        {
            get
            {
                return originalDefinition.HidesBaseMethodsByName;
            }
        }

        public sealed override Accessibility DeclaredAccessibility
        {
            get
            {
                return originalDefinition.DeclaredAccessibility;
            }
        }

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return originalDefinition.IsMetadataVirtual(ignoreInterfaceImplementationChanges);
        }

        internal override bool IsMetadataFinal
        {
            get
            {
                return originalDefinition.IsMetadataFinal;
            }
        }

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return originalDefinition.IsMetadataNewSlot(ignoreInterfaceImplementationChanges);
        }

        public sealed override Symbol ContainingSymbol
        {
            get
            {
                return _containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return _containingType;
            }
        }

        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.originalDefinition.GetAttributes();
        }

        public sealed override Symbol AssociatedSymbol
        {
            get
            {
                Symbol underlying = originalDefinition.AssociatedSymbol;
                return ((object)underlying == null) ? null : underlying.SymbolAsMember(ContainingType);
            }
        }

        public sealed override MethodKind MethodKind
        {
            get
            {
                return originalDefinition.MethodKind;
            }
        }

        public sealed override bool ReturnsVoid
        {
            get
            {
                return originalDefinition.ReturnsVoid;
            }
        }

        public sealed override bool IsGenericMethod
        {
            get
            {
                return originalDefinition.IsGenericMethod;
            }
        }

        public sealed override bool IsImplicitlyDeclared
        {
            get
            {
                return this.originalDefinition.IsImplicitlyDeclared;
            }
        }

        internal sealed override bool GenerateDebugInfo
        {
            get
            {
                return this.originalDefinition.GenerateDebugInfo;
            }
        }

        public sealed override bool IsVararg
        {
            get
            {
                return originalDefinition.IsVararg;
            }
        }

        public sealed override TypeSymbol ReturnType
        {
            get
            {
                var returnType = _lazyReturnType;
                if (returnType != null)
                {
                    return returnType;
                }

                returnType = Map.SubstituteType(originalDefinition.ReturnType).Type;
                return Interlocked.CompareExchange(ref _lazyReturnType, returnType, null) ?? returnType;
            }
        }

        public sealed override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
        {
            get
            {
                return Map.SubstituteCustomModifiers(originalDefinition.ReturnType, originalDefinition.ReturnTypeCustomModifiers);
            }
        }

        internal sealed override int ParameterCount
        {
            get { return this.originalDefinition.ParameterCount; }
        }

        public sealed override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_lazyParameters.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref _lazyParameters, SubstituteParameters(), default(ImmutableArray<ParameterSymbol>));
                }

                return _lazyParameters;
            }
        }

        internal sealed override bool IsExplicitInterfaceImplementation
        {
            get { return this.originalDefinition.IsExplicitInterfaceImplementation; }
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
                        ExplicitInterfaceHelpers.SubstituteExplicitInterfaceImplementations(this.originalDefinition.ExplicitInterfaceImplementations, Map),
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
            return originalDefinition.CallsAreOmitted(syntaxTree);
        }

        internal sealed override TypeMap TypeSubstitution
        {
            get { return this.Map; }
        }

        internal sealed override bool TryGetThisParameter(out ParameterSymbol thisParameter)
        {
            // Required in EE scenarios.  Specifically, the EE binds in the context of a 
            // substituted method, whereas the core compiler always binds within the
            // context of an original definition.  
            // There should never be any reason to call this in normal compilation
            // scenarios, but the behavior should be sensible if it does occur.
            ParameterSymbol originalThisParameter;
            if (!originalDefinition.TryGetThisParameter(out originalThisParameter))
            {
                thisParameter = null;
                return false;
            }

            thisParameter = (object)originalThisParameter != null
                ? new ThisParameterSymbol(this)
                : null;
            return true;
        }

        private ImmutableArray<ParameterSymbol> SubstituteParameters()
        {
            var unsubstitutedParameters = originalDefinition.Parameters;
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
            throw ExceptionUtilities.Unreachable;
        }

        private int ComputeHashCode()
        {
            int code = this.OriginalDefinition.GetHashCode();
            code = Hash.Combine(this.ContainingType, code);

            // Unconstructed method may contain alpha-renamed type parameters while
            // may still be considered equal, we do not want to give different hashcode to such types.
            //
            // Example:
            //   Having original method A<U>.Foo<V>() we create two _unconstructed_ methods
            //    A<int>.Foo<V'>
            //    A<int>.Foo<V">     
            //  Note that V' and V" are type parameters substituted via alpha-renaming of original V
            //  These are different objects, but represent the same "type parameter at index 1"
            //
            //  In short - we are not interested in the type arguments of unconstructed methods.
            if ((object)ConstructedFrom != (object)this)
            {
                foreach (var arg in this.TypeArguments)
                {
                    code = Hash.Combine(arg, code);
                }
            }

            return code;
        }

        public override bool Equals(object obj)
        {
            if ((object)this == obj) return true;

            SubstitutedMethodSymbol other = obj as SubstitutedMethodSymbol;
            if ((object)other == null) return false;

            if ((object)this.OriginalDefinition != (object)other.OriginalDefinition &&
                this.OriginalDefinition != other.OriginalDefinition)
            {
                return false;
            }

            // This checks if the methods have the same definition and the type parameters on the containing types have been
            // substituted in the same way.
            if (this.ContainingType != other.ContainingType) return false;

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
                if (this.TypeArguments[i] != other.TypeArguments[i])
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

                // 0 means that hashcode is not initialized. 
                // in a case we really get 0 for the hashcode, tweak it by +1
                if (code == 0)
                {
                    code++;
                }

                _hashCode = code;
            }

            return code;
        }
    }
}

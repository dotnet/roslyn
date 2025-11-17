// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class RewrittenMethodSymbol : WrappedMethodSymbol
    {
        protected readonly MethodSymbol _originalMethod;
        private readonly TypeMap _typeMap;
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private ImmutableArray<ParameterSymbol> _lazyParameters;

        protected RewrittenMethodSymbol(MethodSymbol originalMethod, TypeMap typeMap, ImmutableArray<TypeParameterSymbol> typeParametersToAlphaRename)
        {
            Debug.Assert(originalMethod.IsDefinition);
            Debug.Assert(originalMethod.ExplicitInterfaceImplementations.IsEmpty);

            _originalMethod = originalMethod;
            _typeMap = typeMap.WithAlphaRename(typeParametersToAlphaRename, this, propagateAttributes: true, out _typeParameters);
        }

        public TypeMap TypeMap => _typeMap;

        public sealed override MethodSymbol UnderlyingMethod => _originalMethod;

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return _typeParameters; }
        }

        public sealed override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get { return GetTypeParametersAsTypeArguments(); }
        }

        public sealed override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get { return _typeMap.SubstituteType(_originalMethod.ReturnTypeWithAnnotations); }
        }

        internal override TypeWithAnnotations IteratorElementTypeWithAnnotations
        {
            get
            {
                TypeWithAnnotations iteratorElementTypeWithAnnotations = _originalMethod.IteratorElementTypeWithAnnotations;

                if (iteratorElementTypeWithAnnotations.HasType)
                {
                    return _typeMap.SubstituteType(iteratorElementTypeWithAnnotations);
                }

                return iteratorElementTypeWithAnnotations;
            }
        }

        internal override bool IsIterator
        {
            get { return _originalMethod.IsIterator; }
        }

        internal sealed override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            return _originalMethod.CalculateLocalSyntaxOffset(localPosition, localTree);
        }

        internal sealed override bool IsNullableAnalysisEnabled() => throw ExceptionUtilities.Unreachable();

        public sealed override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

        internal sealed override UnmanagedCallersOnlyAttributeData? GetUnmanagedCallersOnlyAttributeData(bool forceComplete)
            => _originalMethod.GetUnmanagedCallersOnlyAttributeData(forceComplete);

        internal sealed override bool HasSpecialNameAttribute => throw ExceptionUtilities.Unreachable();

        public sealed override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return _typeMap.SubstituteCustomModifiers(_originalMethod.RefCustomModifiers); }
        }

        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _originalMethod.GetAttributes();
        }

        public sealed override ImmutableArray<CSharpAttributeData> GetReturnTypeAttributes()
        {
            return _originalMethod.GetReturnTypeAttributes();
        }

        internal sealed override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            return _originalMethod.GetUseSiteInfo();
        }

        public sealed override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_lazyParameters.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyParameters, this.MakeParameters());
                }
                return _lazyParameters;
            }
        }

        protected abstract ImmutableArray<ParameterSymbol> MakeParameters();

        internal sealed override bool HasAsyncMethodBuilderAttribute(out TypeSymbol? builderArgument)
        {
            return _originalMethod.HasAsyncMethodBuilderAttribute(out builderArgument);
        }

        protected class RewrittenMethodParameterSymbol : RewrittenMethodParameterSymbolBase
        {
            internal RewrittenMethodParameterSymbol(RewrittenMethodSymbol containingMethod, ParameterSymbol originalParameter)
                : base(containingMethod, originalParameter)
            {
            }

            internal sealed override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
            {
            }
        }

        protected abstract class RewrittenMethodParameterSymbolBase : RewrittenParameterSymbol
        {
            protected readonly RewrittenMethodSymbol _containingMethod;

            protected RewrittenMethodParameterSymbolBase(RewrittenMethodSymbol containingMethod, ParameterSymbol originalParameter) :
                base(originalParameter)
            {
                _containingMethod = containingMethod;
            }

            public sealed override Symbol ContainingSymbol
            {
                get { return _containingMethod; }
            }

            public override TypeWithAnnotations TypeWithAnnotations
            {
                get { return _containingMethod._typeMap.SubstituteType(this._underlyingParameter.TypeWithAnnotations); }
            }

            public override ImmutableArray<CustomModifier> RefCustomModifiers
            {
                get
                {
                    return _containingMethod._typeMap.SubstituteCustomModifiers(this._underlyingParameter.RefCustomModifiers);
                }
            }

            internal sealed override bool HasEnumeratorCancellationAttribute
            {
                get { return _underlyingParameter.HasEnumeratorCancellationAttribute; }
            }
        }
    }
}

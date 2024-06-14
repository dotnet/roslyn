// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class RewrittenMethodSymbol : WrappedMethodSymbol
    {
        protected readonly MethodSymbol _originalMethod;
        protected readonly TypeMap _typeMap;
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private ImmutableArray<ParameterSymbol> _lazyParameters;

        protected RewrittenMethodSymbol(MethodSymbol originalMethod, TypeMap typeMap)
        {
            Debug.Assert(originalMethod.IsDefinition);
            Debug.Assert(originalMethod.ExplicitInterfaceImplementations.IsEmpty);

            _originalMethod = originalMethod;

            // PROTOTYPE(roles): Are we creating type parameters with the right emit behavior? Attributes, etc.
            _typeMap = typeMap.WithAlphaRename(_originalMethod, this, out _typeParameters);
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

        internal sealed override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            return _originalMethod.CalculateLocalSyntaxOffset(localPosition, localTree);
        }

        internal sealed override bool IsNullableAnalysisEnabled() => throw ExceptionUtilities.Unreachable();

        public sealed override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

        internal sealed override UnmanagedCallersOnlyAttributeData? GetUnmanagedCallersOnlyAttributeData(bool forceComplete)
            => _originalMethod.GetUnmanagedCallersOnlyAttributeData(forceComplete);

        public sealed override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return _typeMap.SubstituteCustomModifiers(_originalMethod.RefCustomModifiers); }
        }

        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _originalMethod.GetAttributes();
        }

        internal sealed override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            return UnderlyingMethod.GetUseSiteInfo();
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
            // PROTOTYPE(roles): Test this code path
            return _originalMethod.HasAsyncMethodBuilderAttribute(out builderArgument);
        }

        protected class RewrittenMethodParameterSymbol : RewrittenParameterSymbol
        {
            protected readonly RewrittenMethodSymbol _containingMethod;

            public RewrittenMethodParameterSymbol(RewrittenMethodSymbol containingMethod, ParameterSymbol originalParameter) :
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
        }
    }
}

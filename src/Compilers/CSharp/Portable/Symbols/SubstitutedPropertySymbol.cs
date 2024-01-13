// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SubstitutedPropertySymbol : WrappedPropertySymbol
    {
        private readonly SubstitutedNamedTypeSymbol _containingType;

        private TypeWithAnnotations.Boxed _lazyType;
        private ImmutableArray<ParameterSymbol> _lazyParameters;

        internal SubstitutedPropertySymbol(SubstitutedNamedTypeSymbol containingType, PropertySymbol originalDefinition)
            : base(originalDefinition)
        {
            _containingType = containingType;
        }

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get
            {
                if (_lazyType == null)
                {
                    var type = _containingType.TypeSubstitution.SubstituteType(OriginalDefinition.TypeWithAnnotations);
                    Interlocked.CompareExchange(ref _lazyType, new TypeWithAnnotations.Boxed(type), null);
                }

                return _lazyType.Value;
            }
        }

        public override Symbol ContainingSymbol
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

        public override PropertySymbol OriginalDefinition
        {
            get
            {
                return _underlyingProperty;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return OriginalDefinition.GetAttributes();
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return _containingType.TypeSubstitution.SubstituteCustomModifiers(OriginalDefinition.RefCustomModifiers); }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
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

        public override MethodSymbol GetMethod
        {
            get
            {
                MethodSymbol originalGetMethod = OriginalDefinition.GetMethod;
                return (object)originalGetMethod == null ? null : originalGetMethod.AsMember(_containingType);
            }
        }

        public override MethodSymbol SetMethod
        {
            get
            {
                MethodSymbol originalSetMethod = OriginalDefinition.SetMethod;
                return (object)originalSetMethod == null ? null : originalSetMethod.AsMember(_containingType);
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return OriginalDefinition.IsExplicitInterfaceImplementation; }
        }

        //we want to compute this lazily since it may be expensive for the underlying symbol
        private ImmutableArray<PropertySymbol> _lazyExplicitInterfaceImplementations;

        private OverriddenOrHiddenMembersResult _lazyOverriddenOrHiddenMembers;

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (_lazyExplicitInterfaceImplementations.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(
                        ref _lazyExplicitInterfaceImplementations,
                        ExplicitInterfaceHelpers.SubstituteExplicitInterfaceImplementations(OriginalDefinition.ExplicitInterfaceImplementations, _containingType.TypeSubstitution),
                        default(ImmutableArray<PropertySymbol>));
                }
                return _lazyExplicitInterfaceImplementations;
            }
        }

        internal override bool MustCallMethodsDirectly
        {
            get { return OriginalDefinition.MustCallMethodsDirectly; }
        }

        internal override OverriddenOrHiddenMembersResult OverriddenOrHiddenMembers
        {
            get
            {
                if (_lazyOverriddenOrHiddenMembers == null)
                {
                    Interlocked.CompareExchange(ref _lazyOverriddenOrHiddenMembers, this.MakeOverriddenOrHiddenMembers(), null);
                }

                return _lazyOverriddenOrHiddenMembers;
            }
        }

        private ImmutableArray<ParameterSymbol> SubstituteParameters()
        {
            var unsubstitutedParameters = OriginalDefinition.Parameters;

            if (unsubstitutedParameters.IsEmpty)
            {
                return unsubstitutedParameters;
            }
            else
            {
                int count = unsubstitutedParameters.Length;
                var substituted = new ParameterSymbol[count];
                for (int i = 0; i < count; i++)
                {
                    substituted[i] = new SubstitutedParameterSymbol(this, _containingType.TypeSubstitution, unsubstitutedParameters[i]);
                }
                return substituted.AsImmutableOrNull();
            }
        }
    }
}

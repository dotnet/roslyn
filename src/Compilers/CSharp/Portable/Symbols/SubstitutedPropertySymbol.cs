// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SubstitutedPropertySymbol : PropertySymbol
    {
        private readonly PropertySymbol _originalDefinition;
        private readonly SubstitutedNamedTypeSymbol _containingType;

        private TypeSymbol _lazyType;
        private ImmutableArray<ParameterSymbol> _lazyParameters;

        internal SubstitutedPropertySymbol(SubstitutedNamedTypeSymbol containingType, PropertySymbol originalDefinition)
        {
            _containingType = containingType;
            _originalDefinition = originalDefinition;
        }

        internal override RefKind RefKind
        {
            get
            {
                return _originalDefinition.RefKind;
            }
        }

        public override TypeSymbol Type
        {
            get
            {
                if ((object)_lazyType == null)
                {
                    Interlocked.CompareExchange(ref _lazyType, _containingType.TypeSubstitution.SubstituteType(_originalDefinition.Type).Type, null);
                }

                return _lazyType;
            }
        }

        public override string Name
        {
            get
            {
                return _originalDefinition.Name;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return _originalDefinition.HasSpecialName;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _originalDefinition.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
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
                return _originalDefinition;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _originalDefinition.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _originalDefinition.DeclaringSyntaxReferences;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _originalDefinition.GetAttributes();
        }

        public override bool IsStatic
        {
            get
            {
                return _originalDefinition.IsStatic;
            }
        }

        public override bool IsExtern
        {
            get { return _originalDefinition.IsExtern; }
        }

        public override bool IsSealed
        {
            get { return _originalDefinition.IsSealed; }
        }

        public override bool IsAbstract
        {
            get { return _originalDefinition.IsAbstract; }
        }

        public override bool IsVirtual
        {
            get { return _originalDefinition.IsVirtual; }
        }

        public override bool IsOverride
        {
            get { return _originalDefinition.IsOverride; }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return _originalDefinition.IsImplicitlyDeclared; }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return _originalDefinition.ObsoleteAttributeData; }
        }

        public override bool IsIndexer
        {
            get { return _originalDefinition.IsIndexer; }
        }

        public override ImmutableArray<CustomModifier> TypeCustomModifiers
        {
            get { return _containingType.TypeSubstitution.SubstituteCustomModifiers(_originalDefinition.Type, _originalDefinition.TypeCustomModifiers); }
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
                MethodSymbol originalGetMethod = _originalDefinition.GetMethod;
                return (object)originalGetMethod == null ? null : originalGetMethod.AsMember(_containingType);
            }
        }

        public override MethodSymbol SetMethod
        {
            get
            {
                MethodSymbol originalSetMethod = _originalDefinition.SetMethod;
                return (object)originalSetMethod == null ? null : originalSetMethod.AsMember(_containingType);
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return _originalDefinition.IsExplicitInterfaceImplementation; }
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
                        ExplicitInterfaceHelpers.SubstituteExplicitInterfaceImplementations(_originalDefinition.ExplicitInterfaceImplementations, _containingType.TypeSubstitution),
                        default(ImmutableArray<PropertySymbol>));
                }
                return _lazyExplicitInterfaceImplementations;
            }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get { return _originalDefinition.CallingConvention; }
        }

        internal override bool MustCallMethodsDirectly
        {
            get { return _originalDefinition.MustCallMethodsDirectly; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return _originalDefinition.DeclaredAccessibility;
            }
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
            var unsubstitutedParameters = _originalDefinition.Parameters;

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

        public override string MetadataName
        {
            // We'll never emit this symbol, so it doesn't really
            // make sense for it to have a metadata name.  However, all
            // symbols have an implementation of MetadataName (since it
            // is virtual on Symbol) so we might as well define it in a
            // consistent way.

            get
            {
                return _originalDefinition.MetadataName;
            }
        }
    }
}

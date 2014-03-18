// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly PropertySymbol originalDefinition;
        private readonly SubstitutedNamedTypeSymbol containingType;

        private TypeSymbol lazyType;
        private ImmutableArray<ParameterSymbol> lazyParameters;

        internal SubstitutedPropertySymbol(SubstitutedNamedTypeSymbol containingType, PropertySymbol originalDefinition)
        {
            this.containingType = containingType;
            this.originalDefinition = originalDefinition;
        }

        public override TypeSymbol Type
        {
            get
            {
                if ((object)this.lazyType == null)
                {
                    Interlocked.CompareExchange(ref this.lazyType, containingType.TypeSubstitution.SubstituteType(originalDefinition.Type), null);
                }

                return this.lazyType;
            }
        }

        public override string Name
        {
            get
            {
                return this.originalDefinition.Name;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return originalDefinition.HasSpecialName;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return originalDefinition.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return this.containingType;
            }
        }

        public override PropertySymbol OriginalDefinition
        {
            get
            {
                return this.originalDefinition;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return this.originalDefinition.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return this.originalDefinition.DeclaringSyntaxReferences;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.originalDefinition.GetAttributes();
        }

        public override bool IsStatic
        {
            get
            {
                return this.originalDefinition.IsStatic;
            }
        }

        public override bool IsExtern
        {
            get { return this.originalDefinition.IsExtern; }
        }

        public override bool IsSealed
        {
            get { return this.originalDefinition.IsSealed; }
        }

        public override bool IsAbstract
        {
            get { return this.originalDefinition.IsAbstract; }
        }

        public override bool IsVirtual
        {
            get { return this.originalDefinition.IsVirtual; }
        }

        public override bool IsOverride
        {
            get { return this.originalDefinition.IsOverride; }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return this.originalDefinition.IsImplicitlyDeclared; }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return originalDefinition.ObsoleteAttributeData; }
        }

        public override bool IsIndexer
        {
            get { return this.originalDefinition.IsIndexer; }
        }

        public override ImmutableArray<CustomModifier> TypeCustomModifiers
        {
            get { return this.originalDefinition.TypeCustomModifiers; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (this.lazyParameters.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref this.lazyParameters, SubstituteParameters(), default(ImmutableArray<ParameterSymbol>));
                }

                return this.lazyParameters;
            }
        }


        public override MethodSymbol GetMethod
        {
            get
            {
                MethodSymbol originalGetMethod = this.originalDefinition.GetMethod;
                return (object)originalGetMethod == null ? null : originalGetMethod.AsMember(this.containingType);
            }
        }

        public override MethodSymbol SetMethod
        {
            get
            {
                MethodSymbol originalSetMethod = this.originalDefinition.SetMethod;
                return (object)originalSetMethod == null ? null : originalSetMethod.AsMember(this.containingType);
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return this.originalDefinition.IsExplicitInterfaceImplementation; }
        }

        //we want to compute this lazily since it may be expensive for the underlying symbol
        private ImmutableArray<PropertySymbol> lazyExplicitInterfaceImplementations;

        private OverriddenOrHiddenMembersResult lazyOverriddenOrHiddenMembers;

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (lazyExplicitInterfaceImplementations.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(
                        ref lazyExplicitInterfaceImplementations,
                        ExplicitInterfaceHelpers.SubstituteExplicitInterfaceImplementations(this.originalDefinition.ExplicitInterfaceImplementations, this.containingType.TypeSubstitution),
                        default(ImmutableArray<PropertySymbol>));
                }
                return lazyExplicitInterfaceImplementations;
            }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get { return this.originalDefinition.CallingConvention; }
        }

        internal override bool MustCallMethodsDirectly
        {
            get { return this.originalDefinition.MustCallMethodsDirectly; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return this.originalDefinition.DeclaredAccessibility;
            }
        }

        internal override OverriddenOrHiddenMembersResult OverriddenOrHiddenMembers
        {
            get
            {
                if (this.lazyOverriddenOrHiddenMembers == null)
                {
                    Interlocked.CompareExchange(ref this.lazyOverriddenOrHiddenMembers, this.MakeOverriddenOrHiddenMembers(), null);
                }
                return this.lazyOverriddenOrHiddenMembers;
            }
        }

        private ImmutableArray<ParameterSymbol> SubstituteParameters()
        {
            var unsubstitutedParameters = originalDefinition.Parameters;

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
                    substituted[i] = new SubstitutedParameterSymbol(this, this.containingType.TypeSubstitution, unsubstitutedParameters[i]);
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
                return originalDefinition.MetadataName;
            }
        }
    }
}

// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SubstitutedEventSymbol : EventSymbol
    {
        private readonly EventSymbol originalDefinition;
        private readonly SubstitutedNamedTypeSymbol containingType;

        private TypeSymbol lazyType;

        internal SubstitutedEventSymbol(SubstitutedNamedTypeSymbol containingType, EventSymbol originalDefinition)
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

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.originalDefinition.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        internal override bool HasSpecialName
        {
            get { return this.originalDefinition.HasSpecialName; }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.containingType;
            }
        }

        public override EventSymbol OriginalDefinition
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

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return originalDefinition.ObsoleteAttributeData; }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return this.originalDefinition.IsImplicitlyDeclared;
            }
        }

        public override MethodSymbol AddMethod
        {
            get
            {
                MethodSymbol originalAddMethod = this.originalDefinition.AddMethod;
                return (object)originalAddMethod == null ? null : originalAddMethod.AsMember(this.containingType);
            }
        }

        public override MethodSymbol RemoveMethod
        {
            get
            {
                MethodSymbol originalRemoveMethod = this.originalDefinition.RemoveMethod;
                return (object)originalRemoveMethod == null ? null : originalRemoveMethod.AsMember(this.containingType);
            }
        }

        internal override FieldSymbol AssociatedField
        {
            get
            {
                FieldSymbol originalAssociatedField = this.originalDefinition.AssociatedField;
                return (object)originalAssociatedField == null ? null : originalAssociatedField.AsMember(this.containingType);
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return this.originalDefinition.IsExplicitInterfaceImplementation; }
        }

        //we want to compute this lazily since it may be expensive for the underlying symbol
        private ImmutableArray<EventSymbol> lazyExplicitInterfaceImplementations;

        private OverriddenOrHiddenMembersResult lazyOverriddenOrHiddenMembers;

        public override ImmutableArray<EventSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (lazyExplicitInterfaceImplementations.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(
                        ref lazyExplicitInterfaceImplementations,
                        ExplicitInterfaceHelpers.SubstituteExplicitInterfaceImplementations(this.originalDefinition.ExplicitInterfaceImplementations, this.containingType.TypeSubstitution),
                        default(ImmutableArray<EventSymbol>));
                }
                return lazyExplicitInterfaceImplementations;
            }
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

        public override bool IsWindowsRuntimeEvent
        {
            get
            {
                // A substituted event computes overriding and interface implementation separately
                // from the original definition, in case the type has changed.  However, is should
                // never be the case that providing type arguments changes a WinRT event to a 
                // non-WinRT event or vice versa, so we'll delegate to the original definition.
                return this.originalDefinition.IsWindowsRuntimeEvent;
            }
        }
    }
}

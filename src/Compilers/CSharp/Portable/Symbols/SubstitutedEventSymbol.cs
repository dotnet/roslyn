// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly EventSymbol _originalDefinition;
        private readonly SubstitutedNamedTypeSymbol _containingType;

        private TypeSymbol _lazyType;

        internal SubstitutedEventSymbol(SubstitutedNamedTypeSymbol containingType, EventSymbol originalDefinition)
        {
            _containingType = containingType;
            _originalDefinition = originalDefinition;
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

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _originalDefinition.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        internal override bool HasSpecialName
        {
            get { return _originalDefinition.HasSpecialName; }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingType;
            }
        }

        public override EventSymbol OriginalDefinition
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

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return _originalDefinition.ObsoleteAttributeData; }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return _originalDefinition.IsImplicitlyDeclared;
            }
        }

        public override MethodSymbol AddMethod
        {
            get
            {
                MethodSymbol originalAddMethod = _originalDefinition.AddMethod;
                return (object)originalAddMethod == null ? null : originalAddMethod.AsMember(_containingType);
            }
        }

        public override MethodSymbol RemoveMethod
        {
            get
            {
                MethodSymbol originalRemoveMethod = _originalDefinition.RemoveMethod;
                return (object)originalRemoveMethod == null ? null : originalRemoveMethod.AsMember(_containingType);
            }
        }

        internal override FieldSymbol AssociatedField
        {
            get
            {
                FieldSymbol originalAssociatedField = _originalDefinition.AssociatedField;
                return (object)originalAssociatedField == null ? null : originalAssociatedField.AsMember(_containingType);
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return _originalDefinition.IsExplicitInterfaceImplementation; }
        }

        //we want to compute this lazily since it may be expensive for the underlying symbol
        private ImmutableArray<EventSymbol> _lazyExplicitInterfaceImplementations;

        private OverriddenOrHiddenMembersResult _lazyOverriddenOrHiddenMembers;

        public override ImmutableArray<EventSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (_lazyExplicitInterfaceImplementations.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(
                        ref _lazyExplicitInterfaceImplementations,
                        ExplicitInterfaceHelpers.SubstituteExplicitInterfaceImplementations(_originalDefinition.ExplicitInterfaceImplementations, _containingType.TypeSubstitution),
                        default(ImmutableArray<EventSymbol>));
                }
                return _lazyExplicitInterfaceImplementations;
            }
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

        public override bool IsWindowsRuntimeEvent
        {
            get
            {
                // A substituted event computes overriding and interface implementation separately
                // from the original definition, in case the type has changed.  However, is should
                // never be the case that providing type arguments changes a WinRT event to a 
                // non-WinRT event or vice versa, so we'll delegate to the original definition.
                return _originalDefinition.IsWindowsRuntimeEvent;
            }
        }
    }
}

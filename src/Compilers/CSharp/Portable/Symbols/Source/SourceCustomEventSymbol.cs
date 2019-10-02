// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// This class represents an event declared in source with explicit accessors
    /// (i.e. not a field-like event).
    /// </summary>
    internal sealed class SourceCustomEventSymbol : SourceEventSymbol
    {
        private readonly TypeWithAnnotations _type;
        private readonly string _name;
        private readonly SourceEventAccessorSymbol? _addMethod;
        private readonly SourceEventAccessorSymbol? _removeMethod;
        private readonly TypeSymbol _explicitInterfaceType;
        private readonly ImmutableArray<EventSymbol> _explicitInterfaceImplementations;

        internal SourceCustomEventSymbol(SourceMemberContainerTypeSymbol containingType, Binder binder, EventDeclarationSyntax syntax, DiagnosticBag diagnostics) :
            base(containingType, syntax, syntax.Modifiers, isFieldLike: false,
                 interfaceSpecifierSyntaxOpt: syntax.ExplicitInterfaceSpecifier,
                 nameTokenSyntax: syntax.Identifier, diagnostics: diagnostics)
        {
            ExplicitInterfaceSpecifierSyntax? interfaceSpecifier = syntax.ExplicitInterfaceSpecifier;
            SyntaxToken nameToken = syntax.Identifier;
            bool isExplicitInterfaceImplementation = interfaceSpecifier != null;

            string aliasQualifierOpt;
            _name = ExplicitInterfaceHelpers.GetMemberNameAndInterfaceSymbol(binder, interfaceSpecifier, nameToken.ValueText, diagnostics, out _explicitInterfaceType, out aliasQualifierOpt);

            _type = BindEventType(binder, syntax.Type, diagnostics);

            var explicitlyImplementedEvent = this.FindExplicitlyImplementedEvent(_explicitInterfaceType, nameToken.ValueText, interfaceSpecifier, diagnostics);
            this.FindExplicitlyImplementedMemberVerification(explicitlyImplementedEvent, diagnostics);

            // The runtime will not treat the accessors of this event as overrides or implementations
            // of those of another event unless both the signatures and the custom modifiers match.
            // Hence, in the case of overrides and *explicit* implementations, we need to copy the custom
            // modifiers that are in the signatures of the overridden/implemented event accessors.
            // (From source, we know that there can only be one overridden/implemented event, so there
            // are no conflicts.)  This is unnecessary for implicit implementations because, if the custom
            // modifiers don't match, we'll insert bridge methods for the accessors (explicit implementations 
            // that delegate to the implicit implementations) with the correct custom modifiers
            // (see SourceMemberContainerTypeSymbol.SynthesizeInterfaceMemberImplementation).

            // Note: we're checking if the syntax indicates explicit implementation rather,
            // than if explicitInterfaceType is null because we don't want to look for an
            // overridden event if this is supposed to be an explicit implementation.
            if (!isExplicitInterfaceImplementation)
            {
                // If this event is an override, we may need to copy custom modifiers from
                // the overridden event (so that the runtime will recognize it as an override).
                // We check for this case here, while we can still modify the parameters and
                // return type without losing the appearance of immutability.
                if (this.IsOverride)
                {
                    EventSymbol? overriddenEvent = this.OverriddenEvent;
                    if ((object?)overriddenEvent != null)
                    {
                        CopyEventCustomModifiers(overriddenEvent, ref _type, ContainingAssembly);
                    }
                }
            }
            else if ((object)explicitlyImplementedEvent != null)
            {
                CopyEventCustomModifiers(explicitlyImplementedEvent, ref _type, ContainingAssembly);
            }

            AccessorDeclarationSyntax? addSyntax = null;
            AccessorDeclarationSyntax? removeSyntax = null;

            if (syntax.AccessorList != null)
            {
                foreach (AccessorDeclarationSyntax accessor in syntax.AccessorList.Accessors)
                {
                    bool checkBody = false;

                    switch (accessor.Kind())
                    {
                        case SyntaxKind.AddAccessorDeclaration:
                            if (addSyntax == null)
                            {
                                addSyntax = accessor;
                                checkBody = true;
                            }
                            else
                            {
                                diagnostics.Add(ErrorCode.ERR_DuplicateAccessor, accessor.Keyword.GetLocation());
                            }
                            break;
                        case SyntaxKind.RemoveAccessorDeclaration:
                            if (removeSyntax == null)
                            {
                                removeSyntax = accessor;
                                checkBody = true;
                            }
                            else
                            {
                                diagnostics.Add(ErrorCode.ERR_DuplicateAccessor, accessor.Keyword.GetLocation());
                            }
                            break;
                        case SyntaxKind.GetAccessorDeclaration:
                        case SyntaxKind.SetAccessorDeclaration:
                            diagnostics.Add(ErrorCode.ERR_AddOrRemoveExpected, accessor.Keyword.GetLocation());
                            break;

                        case SyntaxKind.UnknownAccessorDeclaration:
                            // Don't need to handle UnknownAccessorDeclaration.  An error will have 
                            // already been produced for it in the parser.
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(accessor.Kind());
                    }

                    if (checkBody && !IsAbstract && accessor.Body == null && accessor.ExpressionBody == null && accessor.SemicolonToken.Kind() == SyntaxKind.SemicolonToken)
                    {
                        diagnostics.Add(ErrorCode.ERR_AddRemoveMustHaveBody, accessor.SemicolonToken.GetLocation());
                    }
                }

                if (IsAbstract)
                {
                    if (!syntax.AccessorList.OpenBraceToken.IsMissing)
                    {
                        diagnostics.Add(ErrorCode.ERR_AbstractEventHasAccessors, syntax.AccessorList.OpenBraceToken.GetLocation(), this);
                    }
                }
                else if ((addSyntax == null || removeSyntax == null) && (!syntax.AccessorList.OpenBraceToken.IsMissing || !isExplicitInterfaceImplementation))
                {
                    diagnostics.Add(ErrorCode.ERR_EventNeedsBothAccessors, this.Locations[0], this);
                }
            }
            else if (isExplicitInterfaceImplementation && !IsAbstract)
            {
                diagnostics.Add(ErrorCode.ERR_ExplicitEventFieldImpl, this.Locations[0]);
            }

            if (isExplicitInterfaceImplementation && IsAbstract && syntax.AccessorList == null)
            {
                Debug.Assert(containingType.IsInterface);

                Binder.CheckFeatureAvailability(syntax, MessageID.IDS_DefaultInterfaceImplementation, diagnostics, this.Locations[0]);

                if (!ContainingAssembly.RuntimeSupportsDefaultInterfaceImplementation)
                {
                    diagnostics.Add(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, this.Locations[0]);
                }

                _addMethod = new SynthesizedEventAccessorSymbol(this, isAdder: true, explicitlyImplementedEvent, aliasQualifierOpt);
                _removeMethod = new SynthesizedEventAccessorSymbol(this, isAdder: false, explicitlyImplementedEvent, aliasQualifierOpt);
            }
            else
            {
                _addMethod = CreateAccessorSymbol(addSyntax, explicitlyImplementedEvent, aliasQualifierOpt, diagnostics);
                _removeMethod = CreateAccessorSymbol(removeSyntax, explicitlyImplementedEvent, aliasQualifierOpt, diagnostics);
            }

            _explicitInterfaceImplementations =
                (object?)explicitlyImplementedEvent == null ?
                    ImmutableArray<EventSymbol>.Empty :
                    ImmutableArray.Create<EventSymbol>(explicitlyImplementedEvent);
        }

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get { return _type; }
        }

        public override string Name
        {
            get { return _name; }
        }

        public override MethodSymbol? AddMethod
        {
            get { return _addMethod; }
        }

        public override MethodSymbol? RemoveMethod
        {
            get { return _removeMethod; }
        }

        protected override AttributeLocation AllowedAttributeLocations
        {
            get { return AttributeLocation.Event; }
        }

        private ExplicitInterfaceSpecifierSyntax? ExplicitInterfaceSpecifier
        {
            get { return ((EventDeclarationSyntax)this.CSharpSyntaxNode).ExplicitInterfaceSpecifier; }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return this.ExplicitInterfaceSpecifier != null; }
        }

        public override ImmutableArray<EventSymbol> ExplicitInterfaceImplementations
        {
            get { return _explicitInterfaceImplementations; }
        }

        internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, DiagnosticBag diagnostics)
        {
            base.AfterAddingTypeMembersChecks(conversions, diagnostics);

            if ((object)_explicitInterfaceType != null)
            {
                var explicitInterfaceSpecifier = this.ExplicitInterfaceSpecifier;
                RoslynDebug.Assert(explicitInterfaceSpecifier != null);
                _explicitInterfaceType.CheckAllConstraints(DeclaringCompilation, conversions, new SourceLocation(explicitInterfaceSpecifier.Name), diagnostics);
            }

            if (!_explicitInterfaceImplementations.IsEmpty)
            {
                // Note: we delayed nullable-related checks that could pull on NonNullTypes
                EventSymbol explicitlyImplementedEvent = _explicitInterfaceImplementations[0];
                TypeSymbol.CheckNullableReferenceTypeMismatchOnImplementingMember(this.ContainingType, this, explicitlyImplementedEvent, isExplicit: true, diagnostics);
            }
        }

        [return: NotNullIfNotNull(parameterName: "syntaxOpt")]
        private SourceCustomEventAccessorSymbol? CreateAccessorSymbol(AccessorDeclarationSyntax? syntaxOpt,
            EventSymbol? explicitlyImplementedEventOpt, string? aliasQualifierOpt, DiagnosticBag diagnostics)
        {
            if (syntaxOpt == null)
            {
                return null;
            }

            return new SourceCustomEventAccessorSymbol(this, syntaxOpt, explicitlyImplementedEventOpt, aliasQualifierOpt, diagnostics);
        }
    }
}

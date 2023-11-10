// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
        private readonly string _name;
        private readonly SourceEventAccessorSymbol? _addMethod;
        private readonly SourceEventAccessorSymbol? _removeMethod;

        private SymbolCompletionState _state;
        private TypeWithAnnotations _lazyType;
        private ExplicitInterfaceMemberInfo? _lazyExplicitInterfaceMemberInfo = ExplicitInterfaceMemberInfo.Uninitialized;
        private ImmutableArray<EventSymbol> _lazyExplicitInterfaceImplementations;

        internal SourceCustomEventSymbol(SourceMemberContainerTypeSymbol containingType, EventDeclarationSyntax syntax, BindingDiagnosticBag diagnostics) :
            base(containingType, syntax, syntax.Modifiers, isFieldLike: false,
                 interfaceSpecifierSyntaxOpt: syntax.ExplicitInterfaceSpecifier,
                 nameTokenSyntax: syntax.Identifier, diagnostics: diagnostics)
        {
            ExplicitInterfaceSpecifierSyntax? interfaceSpecifier = syntax.ExplicitInterfaceSpecifier;
            SyntaxToken nameToken = syntax.Identifier;
            bool isExplicitInterfaceImplementation = interfaceSpecifier != null;

            _name = ExplicitInterfaceHelpers.GetMemberName(interfaceSpecifier, nameToken.ValueText);

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
                        case SyntaxKind.InitAccessorDeclaration:
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
                    diagnostics.Add(ErrorCode.ERR_EventNeedsBothAccessors, this.GetFirstLocation(), this);
                }
            }
            else if (isExplicitInterfaceImplementation && !IsAbstract)
            {
                diagnostics.Add(ErrorCode.ERR_ExplicitEventFieldImpl, this.GetFirstLocation());
            }

            if (isExplicitInterfaceImplementation && IsAbstract && syntax.AccessorList == null)
            {
                Debug.Assert(containingType.IsInterface);

                Binder.CheckFeatureAvailability(syntax, MessageID.IDS_DefaultInterfaceImplementation, diagnostics, this.GetFirstLocation());

                if (!ContainingAssembly.RuntimeSupportsDefaultInterfaceImplementation)
                {
                    diagnostics.Add(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, this.GetFirstLocation());
                }

                _addMethod = new SynthesizedEventAccessorSymbol(this, isAdder: true, isExpressionBodied: false);
                _removeMethod = new SynthesizedEventAccessorSymbol(this, isAdder: false, isExpressionBodied: false);
            }
            else
            {
                _addMethod = CreateAccessorSymbol(DeclaringCompilation, addSyntax, diagnostics);
                _removeMethod = CreateAccessorSymbol(DeclaringCompilation, removeSyntax, diagnostics);
            }
        }

        private void EnsureSignatureGuarded(BindingDiagnosticBag diagnostics)
        {
            var syntax = (EventDeclarationSyntax)CSharpSyntaxNode;
            var binder = DeclaringCompilation.GetBinder(syntax);

            _lazyType = BindEventType(binder, syntax.Type, diagnostics);

            ExplicitInterfaceSpecifierSyntax? explicitInterfaceSpecifier = syntax.ExplicitInterfaceSpecifier;
            _lazyExplicitInterfaceMemberInfo = ExplicitInterfaceHelpers.GetMemberInfo(explicitInterfaceSpecifier, syntax.Identifier.ValueText, binder, diagnostics);

            TypeSymbol? explicitInterfaceType = _lazyExplicitInterfaceMemberInfo?.ExplicitInterfaceType;
            bool isExplicitInterfaceImplementation = explicitInterfaceSpecifier != null;

            var explicitlyImplementedEvent = this.FindExplicitlyImplementedEvent(explicitInterfaceType, syntax.Identifier.ValueText, explicitInterfaceSpecifier, diagnostics);
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
                        CopyEventCustomModifiers(overriddenEvent, ref _lazyType, ContainingAssembly);
                    }
                }
            }
            else if ((object)explicitlyImplementedEvent != null)
            {
                CopyEventCustomModifiers(explicitlyImplementedEvent, ref _lazyType, ContainingAssembly);
            }

            _lazyExplicitInterfaceImplementations =
                (object?)explicitlyImplementedEvent == null ?
                    ImmutableArray<EventSymbol>.Empty :
                    ImmutableArray.Create<EventSymbol>(explicitlyImplementedEvent);
        }

        private void EnsureSignature()
        {
            if (!_state.HasComplete(CompletionPart.FinishPropertyEnsureSignature))
            {
                lock (SyntaxReference)
                {
                    if (_state.NotePartComplete(CompletionPart.StartPropertyEnsureSignature))
                    {
                        var diagnostics = BindingDiagnosticBag.GetInstance();
                        EnsureSignatureGuarded(diagnostics);
                        AddDeclarationDiagnostics(diagnostics);
                        diagnostics.Free();
                    }
                }
            }
        }

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get
            {
                EnsureSignature();
                return _lazyType;
            }
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

        public override ExplicitInterfaceSpecifierSyntax? ExplicitInterfaceSpecifier
        {
            get { return ((EventDeclarationSyntax)this.CSharpSyntaxNode).ExplicitInterfaceSpecifier; }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return this.ExplicitInterfaceSpecifier != null; }
        }

        public override ImmutableArray<EventSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                EnsureSignature();
                return _lazyExplicitInterfaceImplementations;
            }
        }

        internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, BindingDiagnosticBag diagnostics)
        {
            EnsureSignature();

            base.AfterAddingTypeMembersChecks(conversions, diagnostics);

            if (_lazyExplicitInterfaceMemberInfo?.ExplicitInterfaceType is { } explicitInterfaceType)
            {
                var explicitInterfaceSpecifier = this.ExplicitInterfaceSpecifier;
                RoslynDebug.Assert(explicitInterfaceSpecifier != null);
                explicitInterfaceType.CheckAllConstraints(DeclaringCompilation, conversions, new SourceLocation(explicitInterfaceSpecifier.Name), diagnostics);
            }

            if (!_lazyExplicitInterfaceImplementations.IsEmpty)
            {
                // Note: we delayed nullable-related checks that could pull on NonNullTypes
                EventSymbol explicitlyImplementedEvent = _lazyExplicitInterfaceImplementations[0];
                TypeSymbol.CheckModifierMismatchOnImplementingMember(this.ContainingType, this, explicitlyImplementedEvent, isExplicit: true, diagnostics);
            }
        }

        [return: NotNullIfNotNull(parameterName: nameof(syntaxOpt))]
        private SourceCustomEventAccessorSymbol? CreateAccessorSymbol(CSharpCompilation compilation, AccessorDeclarationSyntax? syntaxOpt,
            BindingDiagnosticBag diagnostics)
        {
            if (syntaxOpt == null)
            {
                return null;
            }

            return new SourceCustomEventAccessorSymbol(this, syntaxOpt, isNullableAnalysisEnabled: compilation.IsNullableAnalysisEnabledIn(syntaxOpt), diagnostics);
        }
    }
}

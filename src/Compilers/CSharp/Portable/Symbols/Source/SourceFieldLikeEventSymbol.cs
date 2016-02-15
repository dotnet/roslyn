// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// This class represents an event declared in source without explicit accessors.
    /// It implicitly has thread safe accessors and an associated field (of the same
    /// name), unless it does not have an initializer and is either extern or inside
    /// an interface, in which case it only has accessors.
    /// </summary>
    internal sealed class SourceFieldLikeEventSymbol : SourceEventSymbol
    {
        private readonly string _name;
        private readonly TypeSymbolWithAnnotations _type;
        private readonly SourceEventFieldSymbol _associatedField;
        private readonly SynthesizedFieldLikeEventAccessorSymbol _addMethod;
        private readonly SynthesizedFieldLikeEventAccessorSymbol _removeMethod;

        internal SourceFieldLikeEventSymbol(SourceMemberContainerTypeSymbol containingType, Binder binder, SyntaxTokenList modifiers, VariableDeclaratorSyntax declaratorSyntax, DiagnosticBag diagnostics)
            : base(containingType, declaratorSyntax, modifiers, null, declaratorSyntax.Identifier, diagnostics)
        {
            _name = declaratorSyntax.Identifier.ValueText;


            var declaratorDiagnostics = DiagnosticBag.GetInstance();
            var declarationSyntax = (VariableDeclarationSyntax)declaratorSyntax.Parent;
            _type = BindEventType(binder, declarationSyntax.Type, declaratorDiagnostics);

            // The runtime will not treat the accessors of this event as overrides or implementations
            // of those of another event unless both the signatures and the custom modifiers match.
            // Hence, in the case of overrides and *explicit* implementations (not possible for field-like
            // events), we need to copy the custom modifiers that are in the signatures of the 
            // overridden/implemented event accessors. (From source, we know that there can only be one 
            // overridden/implemented event, so there are no conflicts.)  This is unnecessary for implicit 
            // implementations because, if the custom modifiers don't match, we'll insert bridge methods 
            // for the accessors (explicit implementations that delegate to the implicit implementations) 
            // with the correct custom modifiers (see SourceNamedTypeSymbol.ImplementInterfaceMember).

            // If this event is an override, we may need to copy custom modifiers from
            // the overridden event (so that the runtime will recognize it as an override).
            // We check for this case here, while we can still modify the parameters and
            // return type without losing the appearance of immutability.
            if (this.IsOverride)
            {
                EventSymbol overriddenEvent = this.OverriddenEvent;
                if ((object)overriddenEvent != null)
                {
                    CopyEventCustomModifiers(overriddenEvent, ref _type, ContainingAssembly);
                }
            }

            bool hasInitializer = declaratorSyntax.Initializer != null;
            bool inInterfaceType = containingType.IsInterfaceType();

            if (hasInitializer)
            {
                if (inInterfaceType)
                {
                    diagnostics.Add(ErrorCode.ERR_InterfaceEventInitializer, this.Locations[0], this);
                }
                else if (this.IsAbstract)
                {
                    diagnostics.Add(ErrorCode.ERR_AbstractEventInitializer, this.Locations[0], this);
                }
            }

            // NOTE: if there's an initializer in source, we'd better create a backing field, regardless of
            // whether or not the initializer is legal.
            if (hasInitializer || !(inInterfaceType || this.IsExtern || this.IsAbstract))
            {
                _associatedField = MakeAssociatedField(declaratorSyntax);
                // Don't initialize this.type - we'll just use the type of the field (which is lazy and handles var)
            }

            // Accessors will assume that Type is available.
            _addMethod = new SynthesizedFieldLikeEventAccessorSymbol(this, isAdder: true);
            _removeMethod = new SynthesizedFieldLikeEventAccessorSymbol(this, isAdder: false);

            if (declarationSyntax.Variables[0] == declaratorSyntax)
            {
                // Don't report these diagnostics for every declarator in this declaration.
                diagnostics.AddRange(declaratorDiagnostics);
            }

            declaratorDiagnostics.Free();
        }

        /// <summary>
        /// Backing field for field-like event. Will be null if the event
        /// has no initializer and is either extern or inside an interface.
        /// </summary>
        internal override FieldSymbol AssociatedField
        {
            get { return _associatedField; }
        }

        public override string Name
        {
            get { return _name; }
        }

        public override TypeSymbolWithAnnotations Type
        {
            get { return _type; }
        }

        public override MethodSymbol AddMethod
        {
            get { return _addMethod; }
        }

        public override MethodSymbol RemoveMethod
        {
            get { return _removeMethod; }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return false; }
        }

        protected override AttributeLocation AllowedAttributeLocations
        {
            get
            {
                return (object)_associatedField != null ?
                    AttributeLocation.Event | AttributeLocation.Method | AttributeLocation.Field :
                    AttributeLocation.Event | AttributeLocation.Method;
            }
        }

        public override ImmutableArray<EventSymbol> ExplicitInterfaceImplementations
        {
            get { return ImmutableArray<EventSymbol>.Empty; }
        }

        private SourceEventFieldSymbol MakeAssociatedField(VariableDeclaratorSyntax declaratorSyntax)
        {
            DiagnosticBag discardedDiagnostics = DiagnosticBag.GetInstance();
            var field = new SourceEventFieldSymbol(this, declaratorSyntax, discardedDiagnostics);
            discardedDiagnostics.Free();

            Debug.Assert(field.Name == _name);
            return field;
        }

        internal override void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
        {
            if ((object)this.AssociatedField != null)
            {
                this.AssociatedField.ForceComplete(locationOpt, cancellationToken);
            }

            base.ForceComplete(locationOpt, cancellationToken);
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Event accessor that has been synthesized for a field-like event declared in source.
    /// </summary>
    /// <remarks>
    /// Associated with <see cref="SourceFieldLikeEventSymbol"/>.
    /// </remarks>
    internal sealed class SynthesizedFieldLikeEventAccessorSymbol : SourceEventAccessorSymbol
    {
        // Since we don't have a syntax reference, we'll have to use another object for locking.
        private readonly object _methodChecksLockObject = new object();

        private readonly string _name;

        internal SynthesizedFieldLikeEventAccessorSymbol(SourceFieldLikeEventSymbol @event, bool isAdder)
            : base(@event, null, @event.Locations)
        {
            this.MakeFlags(
                isAdder ? MethodKind.EventAdd : MethodKind.EventRemove,
                @event.Modifiers,
                returnsVoid: false, // until we learn otherwise (in LazyMethodChecks).
                isExtensionMethod: false,
                isMetadataVirtualIgnoringModifiers: false);

            _name = GetOverriddenAccessorName(@event, isAdder) ??
                SourceEventSymbol.GetAccessorName(@event.Name, isAdder);
        }

        public override string Name
        {
            get { return _name; }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        internal override bool GenerateDebugInfo
        {
            get { return false; }
        }

        protected override SourceMemberMethodSymbol BoundAttributesSource
        {
            get
            {
                return this.MethodKind == MethodKind.EventAdd
                    ? (SourceMemberMethodSymbol)this.AssociatedEvent.RemoveMethod
                    : null;
            }
        }

        protected override IAttributeTargetSymbol AttributeOwner
        {
            get
            {
                // attributes for this accessor are specified on the associated event:
                return AssociatedEvent;
            }
        }

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            return OneOrMany.Create(this.AssociatedEvent.AttributeDeclarationSyntaxList);
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
        }

        protected override object MethodChecksLockObject
        {
            get { return _methodChecksLockObject; }
        }

        internal override MethodImplAttributes ImplementationAttributes
        {
            get
            {
                MethodImplAttributes result = base.ImplementationAttributes;

                if (!IsAbstract && !AssociatedEvent.IsWindowsRuntimeEvent && !ContainingType.IsStructType() &&
                    (object)DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_Threading_Interlocked__CompareExchange_T) == null)
                {
                    // Under these conditions, this method needs to be synchronized.
                    result |= MethodImplAttributes.Synchronized;
                }

                return result;
            }
        }
    }
}

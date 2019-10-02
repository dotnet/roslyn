// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A delegate field associated with a <see cref="SourceFieldLikeEventSymbol"/>.
    /// </summary>
    /// <remarks>
    /// SourceFieldSymbol takes care of the initializer (plus "var" in the interactive case).
    /// </remarks>
    internal sealed class SourceEventFieldSymbol : SourceMemberFieldSymbolFromDeclarator
    {
        private readonly SourceEventSymbol _associatedEvent;

        internal SourceEventFieldSymbol(SourceEventSymbol associatedEvent, VariableDeclaratorSyntax declaratorSyntax, DiagnosticBag discardedDiagnostics)
            : base(associatedEvent.containingType,
                   declaratorSyntax,
                   (associatedEvent.Modifiers & (~DeclarationModifiers.AccessibilityMask)) | DeclarationModifiers.Private,
                   modifierErrors: true,
                   diagnostics: discardedDiagnostics)
        {
            _associatedEvent = associatedEvent;
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return true;
            }
        }

        protected override IAttributeTargetSymbol AttributeOwner
        {
            get
            {
                return _associatedEvent;
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                return _associatedEvent;
            }
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));

            // Dev11 doesn't synthesize this attribute, the debugger has a knowledge 
            // of special name C# compiler uses for backing fields, which is not desirable.
            AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDebuggerBrowsableNeverAttribute());
        }
    }
}

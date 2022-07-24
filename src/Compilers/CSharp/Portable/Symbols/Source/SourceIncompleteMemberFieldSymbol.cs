// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// This symbol is used for incomplete members, for example, `public class A { public B }`
    /// </summary>
    internal sealed class SourceIncompleteMemberFieldSymbol : SourceMemberFieldSymbol
    {
        private readonly IncompleteMemberSyntax _node;
        private readonly TypeWithAnnotations _fieldType;
        private readonly RefKind _refKind;

        public SourceIncompleteMemberFieldSymbol(SourceMemberContainerTypeSymbol containingType, IncompleteMemberSyntax node, TypeWithAnnotations fieldType, RefKind refKind)
            : base(containingType, MakeModifiers(containingType, node.GetFirstToken(), node.Modifiers, isRefField: refKind != RefKind.None, BindingDiagnosticBag.Discarded, out _), "", node.GetReference(), node.Location)
        {
            _node = node;
            _fieldType = fieldType;
            _refKind = refKind;
            state.NotePartComplete(CompletionPart.Type);
        }

        public override bool HasInitializer => false;

        public override RefKind RefKind => _refKind;

        protected override TypeSyntax? TypeSyntax => _node.Type;

        protected override SyntaxTokenList ModifiersTokenList => _node.Modifiers;

        protected override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList => _node.AttributeLists;

        protected override ConstantValue? MakeConstantValue(HashSet<SourceFieldSymbolWithSyntaxReference> dependencies, bool earlyDecodingWellKnownAttributes, BindingDiagnosticBag diagnostics)
        {
            return null;
        }

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return _fieldType;
        }
    }
}

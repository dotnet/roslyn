// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a constant field of an enum.
    /// </summary>
    internal abstract class SourceEnumConstantSymbol : SourceFieldSymbolWithSyntaxReference
    {
        public static SourceEnumConstantSymbol CreateExplicitValuedConstant(
            SourceMemberContainerTypeSymbol containingEnum,
            EnumMemberDeclarationSyntax syntax,
            DiagnosticBag diagnostics)
        {
            var initializer = syntax.EqualsValue;
            RoslynDebug.Assert(initializer != null);
            return new ExplicitValuedEnumConstantSymbol(containingEnum, syntax, initializer, diagnostics);
        }

        public static SourceEnumConstantSymbol CreateImplicitValuedConstant(
            SourceMemberContainerTypeSymbol containingEnum,
            EnumMemberDeclarationSyntax syntax,
            SourceEnumConstantSymbol otherConstant,
            int otherConstantOffset,
            DiagnosticBag diagnostics)
        {
            if ((object)otherConstant == null)
            {
                Debug.Assert(otherConstantOffset == 0);
                return new ZeroValuedEnumConstantSymbol(containingEnum, syntax, diagnostics);
            }
            else
            {
                Debug.Assert(otherConstantOffset > 0);
                return new ImplicitValuedEnumConstantSymbol(containingEnum, syntax, otherConstant, (uint)otherConstantOffset, diagnostics);
            }
        }

        protected SourceEnumConstantSymbol(SourceMemberContainerTypeSymbol containingEnum, EnumMemberDeclarationSyntax syntax, DiagnosticBag diagnostics)
            : base(containingEnum, syntax.Identifier.ValueText, syntax.GetReference(), syntax.Identifier.GetLocation())
        {
            if (this.Name == WellKnownMemberNames.EnumBackingFieldName)
            {
                diagnostics.Add(ErrorCode.ERR_ReservedEnumerator, this.ErrorLocation, WellKnownMemberNames.EnumBackingFieldName);
            }
        }

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return TypeWithAnnotations.Create(this.ContainingType);
        }

        public override Symbol? AssociatedSymbol
        {
            get
            {
                return null;
            }
        }

        protected sealed override DeclarationModifiers Modifiers
        {
            get
            {
                return DeclarationModifiers.Const | DeclarationModifiers.Static | DeclarationModifiers.Public;
            }
        }

        public new EnumMemberDeclarationSyntax SyntaxNode
        {
            get
            {
                return (EnumMemberDeclarationSyntax)base.SyntaxNode;
            }
        }

        protected override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
        {
            get
            {
                if (this.containingType.AnyMemberHasAttributes)
                {
                    return this.SyntaxNode.AttributeLists;
                }

                return default(SyntaxList<AttributeListSyntax>);
            }
        }

        internal sealed override void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var incompletePart = state.NextIncompletePart;
                switch (incompletePart)
                {
                    case CompletionPart.Attributes:
                        GetAttributes();
                        break;

                    case CompletionPart.Type:
                        state.NotePartComplete(CompletionPart.Type);
                        break;

                    case CompletionPart.FixedSize:
                        Debug.Assert(!this.IsFixedSizeBuffer);
                        state.NotePartComplete(CompletionPart.FixedSize);
                        break;

                    case CompletionPart.ConstantValue:
                        GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false);
                        break;

                    case CompletionPart.None:
                        return;

                    default:
                        // any other values are completion parts intended for other kinds of symbols
                        state.NotePartComplete(CompletionPart.All & ~CompletionPart.FieldSymbolAll);
                        break;
                }

                state.SpinWaitComplete(incompletePart, cancellationToken);
            }
        }

        private sealed class ZeroValuedEnumConstantSymbol : SourceEnumConstantSymbol
        {
            public ZeroValuedEnumConstantSymbol(
                SourceMemberContainerTypeSymbol containingEnum,
                EnumMemberDeclarationSyntax syntax,
                DiagnosticBag diagnostics)
                : base(containingEnum, syntax, diagnostics)
            {
            }

            protected override ConstantValue MakeConstantValue(HashSet<SourceFieldSymbolWithSyntaxReference> dependencies, bool earlyDecodingWellKnownAttributes, DiagnosticBag diagnostics)
            {
                var constantType = this.ContainingType.EnumUnderlyingType.SpecialType;
                return Microsoft.CodeAnalysis.ConstantValue.Default(constantType);
            }
        }

        private sealed class ExplicitValuedEnumConstantSymbol : SourceEnumConstantSymbol
        {
            private readonly SyntaxReference _equalsValueNodeRef;

            public ExplicitValuedEnumConstantSymbol(
                SourceMemberContainerTypeSymbol containingEnum,
                EnumMemberDeclarationSyntax syntax,
                EqualsValueClauseSyntax initializer,
                DiagnosticBag diagnostics) :
                base(containingEnum, syntax, diagnostics)
            {
                _equalsValueNodeRef = initializer.GetReference();
            }

            protected override ConstantValue MakeConstantValue(HashSet<SourceFieldSymbolWithSyntaxReference> dependencies, bool earlyDecodingWellKnownAttributes, DiagnosticBag diagnostics)
            {
                return ConstantValueUtils.EvaluateFieldConstant(this, (EqualsValueClauseSyntax)_equalsValueNodeRef.GetSyntax(), dependencies, earlyDecodingWellKnownAttributes, diagnostics);
            }
        }

        private sealed class ImplicitValuedEnumConstantSymbol : SourceEnumConstantSymbol
        {
            private readonly SourceEnumConstantSymbol _otherConstant;
            private readonly uint _otherConstantOffset;

            public ImplicitValuedEnumConstantSymbol(
                SourceMemberContainerTypeSymbol containingEnum,
                EnumMemberDeclarationSyntax syntax,
                SourceEnumConstantSymbol otherConstant,
                uint otherConstantOffset,
                DiagnosticBag diagnostics) :
                base(containingEnum, syntax, diagnostics)
            {
                RoslynDebug.Assert((object)otherConstant != null);
                Debug.Assert(otherConstantOffset > 0);

                _otherConstant = otherConstant;
                _otherConstantOffset = otherConstantOffset;
            }

            protected override ConstantValue MakeConstantValue(HashSet<SourceFieldSymbolWithSyntaxReference> dependencies, bool earlyDecodingWellKnownAttributes, DiagnosticBag diagnostics)
            {
                var otherValue = _otherConstant.GetConstantValue(new ConstantFieldsInProgress(this, dependencies), earlyDecodingWellKnownAttributes);
                // Value may be Unset if there are dependencies
                // that must be evaluated first.
                if (otherValue == Microsoft.CodeAnalysis.ConstantValue.Unset)
                {
                    return Microsoft.CodeAnalysis.ConstantValue.Unset;
                }
#nullable disable // Can 'otherValue' be null?
                if (otherValue.IsBad)
#nullable enable
                {
                    return Microsoft.CodeAnalysis.ConstantValue.Bad;
                }
                ConstantValue value;
                var overflowKind = EnumConstantHelper.OffsetValue(otherValue, _otherConstantOffset, out value);
                if (overflowKind == EnumOverflowKind.OverflowReport)
                {
                    // Report an error if the value is immediately
                    // outside the range, but not otherwise.
                    diagnostics.Add(ErrorCode.ERR_EnumeratorOverflow, this.Locations[0], this);
                }
                return value;
            }
        }
    }
}

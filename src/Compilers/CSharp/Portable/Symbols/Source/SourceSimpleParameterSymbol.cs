// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A source parameter that has no default value, no attributes,
    /// and is not params.
    /// </summary>
    internal sealed class SourceSimpleParameterSymbol : SourceParameterSymbol
    {
        public SourceSimpleParameterSymbol(
            Symbol owner,
            TypeWithAnnotations parameterType,
            int ordinal,
            RefKind refKind,
            string name,
            ImmutableArray<Location> locations)
            : base(owner, parameterType, ordinal, refKind, name, locations)
        {
        }

        internal override ConstantValue ExplicitDefaultConstantValue
        {
            get { return null; }
        }

        public override bool IsNullChecked
        {
            get
            {
                var node = this.GetNonNullSyntaxNode();
                if (node is ParameterSyntax param)
                {
                    return param.ExclamationToken.Kind() == SyntaxKind.ExclamationToken;
                }
                return false;
            }
        }

        internal override bool IsMetadataOptional
        {
            get { return false; }
        }

        public override bool IsParams
        {
            get { return false; }
        }

        internal override bool HasDefaultArgumentSyntax
        {
            get { return false; }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        internal override SyntaxReference SyntaxReference
        {
            get { return null; }
        }

        internal override bool IsExtensionMethodThis
        {
            get { return false; }
        }

        internal override bool IsIDispatchConstant
        {
            get { return false; }
        }

        internal override bool IsIUnknownConstant
        {
            get { return false; }
        }

        internal override bool IsCallerFilePath
        {
            get { return false; }
        }

        internal override bool IsCallerLineNumber
        {
            get { return false; }
        }

        internal override bool IsCallerMemberName
        {
            get { return false; }
        }

        internal override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            get { return FlowAnalysisAnnotations.None; }
        }

        internal override ImmutableHashSet<string> NotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get { return null; }
        }

        internal override bool HasOptionalAttribute
        {
            get { return false; }
        }

        internal override SyntaxList<AttributeListSyntax> AttributeDeclarationList
        {
            get { return default(SyntaxList<AttributeListSyntax>); }
        }

        internal override CustomAttributesBag<CSharpAttributeData> GetAttributesBag()
        {
            state.NotePartComplete(CompletionPart.Attributes);
            return CustomAttributesBag<CSharpAttributeData>.Empty;
        }

        internal override ConstantValue DefaultValueFromAttributes
        {
            get { return ConstantValue.NotAvailable; }
        }
    }
}

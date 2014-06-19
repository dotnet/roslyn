// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceLabelSymbol : LabelSymbol
    {
        private readonly MethodSymbol containingMethod;
        private readonly SyntaxNodeOrToken identifierNodeOrToken;

        /// <summary>
        /// Switch case labels have a constant expression associated with them.
        /// </summary>
        private readonly ConstantValue switchCaseLabelConstant;

        public SourceLabelSymbol(
            MethodSymbol containingMethod,
            SyntaxNodeOrToken identifierNodeOrToken,
            ConstantValue switchCaseLabelConstant = null)
            : base(identifierNodeOrToken.IsToken ? identifierNodeOrToken.AsToken().ValueText : identifierNodeOrToken.ToString())
        {
            this.containingMethod = containingMethod;
            this.identifierNodeOrToken = identifierNodeOrToken;
            this.switchCaseLabelConstant = switchCaseLabelConstant;
        }

        public SourceLabelSymbol(
            MethodSymbol containingMethod,
            ConstantValue switchCaseLabelConstant = null)
            : base(switchCaseLabelConstant.ToString())
        {
            this.containingMethod = containingMethod;
            this.identifierNodeOrToken = default(SyntaxToken);
            this.switchCaseLabelConstant = switchCaseLabelConstant;
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return identifierNodeOrToken.IsToken && identifierNodeOrToken.Parent == null
                    ? ImmutableArray<Location>.Empty
                    : ImmutableArray.Create<Location>(identifierNodeOrToken.GetLocation());
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                CSharpSyntaxNode node = null;

                if (identifierNodeOrToken.IsToken)
                {
                    if (identifierNodeOrToken.Parent != null)
                        node = identifierNodeOrToken.Parent.FirstAncestorOrSelf<LabeledStatementSyntax>();
                }
                else
                {
                    node = identifierNodeOrToken.AsNode().FirstAncestorOrSelf<SwitchLabelSyntax>();
                }

                return node == null ? ImmutableArray<SyntaxReference>.Empty : ImmutableArray.Create<SyntaxReference>(node.GetReference());
            }
        }

        public override MethodSymbol ContainingMethod
        {
            get
            {
                return containingMethod;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return containingMethod;
            }
        }

        // Get the identifier node or token that defined this label symbol. This is useful for robustly
        // checking if a label symbol actually matches a particular definition, even in the presence
        // of duplicates.
        internal override SyntaxNodeOrToken IdentifierNodeOrToken
        {
            get
            {
                return identifierNodeOrToken;
            }
        }

        /// <summary>
        /// If the label is a switch case label, returns the associated constant value with
        /// case expression, otherwise returns null.
        /// </summary>
        public ConstantValue SwitchCaseLabelConstant
        {
            get
            {
                return this.switchCaseLabelConstant;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == (object)this)
            {
                return true;
            }

            var symbol = obj as SourceLabelSymbol;
            return (object)symbol != null
                && symbol.identifierNodeOrToken.CSharpKind() != SyntaxKind.None
                && symbol.identifierNodeOrToken.Equals(this.identifierNodeOrToken)
                && Equals(symbol.containingMethod, this.containingMethod);
        }

        public override int GetHashCode()
        {
            return this.identifierNodeOrToken.GetHashCode();
        }
    }
}
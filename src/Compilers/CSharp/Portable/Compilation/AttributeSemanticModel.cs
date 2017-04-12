// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binding for an attribute.  Represents the result of binding an attribute constructor and
    /// the positional and named arguments.
    /// </summary>
    internal sealed class AttributeSemanticModel : MemberSemanticModel
    {
        private readonly AliasSymbol _aliasOpt;

        private AttributeSemanticModel(CSharpCompilation compilation, AttributeSyntax syntax, NamedTypeSymbol attributeType, AliasSymbol aliasOpt, Binder rootBinder, SyntaxTreeSemanticModel parentSemanticModelOpt = null, int speculatedPosition = 0)
            : base(compilation, syntax, attributeType, new ExecutableCodeBinder(syntax, rootBinder.ContainingMember(), rootBinder), parentSemanticModelOpt, speculatedPosition)
        {
            Debug.Assert(syntax != null);
            _aliasOpt = aliasOpt;
        }

        /// <summary>
        /// Creates an AttributeSemanticModel that allows asking semantic questions about an attribute node.
        /// </summary>
        public static AttributeSemanticModel Create(CSharpCompilation compilation, AttributeSyntax syntax, NamedTypeSymbol attributeType, AliasSymbol aliasOpt, Binder rootBinder)
        {
            return new AttributeSemanticModel(compilation, syntax, attributeType, aliasOpt, rootBinder);
        }

        /// <summary>
        /// Creates a speculative AttributeSemanticModel that allows asking semantic questions about an attribute node that did not appear in the original source code.
        /// </summary>
        public static AttributeSemanticModel CreateSpeculative(SyntaxTreeSemanticModel parentSemanticModel, AttributeSyntax syntax, NamedTypeSymbol attributeType, AliasSymbol aliasOpt, Binder rootBinder, int position)
        {
            Debug.Assert(parentSemanticModel != null);
            Debug.Assert(rootBinder != null);
            Debug.Assert(rootBinder.IsSemanticModelBinder);

            return new AttributeSemanticModel(parentSemanticModel.Compilation, syntax, attributeType, aliasOpt, rootBinder, parentSemanticModel, position);
        }

        private NamedTypeSymbol AttributeType
        {
            get
            {
                return (NamedTypeSymbol)MemberSymbol;
            }
        }

        internal protected override CSharpSyntaxNode GetBindableSyntaxNode(CSharpSyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.Attribute:
                    return node;

                case SyntaxKind.AttributeArgument:
                    // Try to walk up to the AttributeSyntax
                    var parent = node.Parent;
                    if (parent != null)
                    {
                        parent = parent.Parent;
                        if (parent != null)
                        {
                            return parent;
                        }
                    }
                    break;
            }

            return base.GetBindableSyntaxNode(node);
        }

        internal override BoundNode Bind(Binder binder, CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            if (node.Kind() == SyntaxKind.Attribute)
            {
                var attribute = (AttributeSyntax)node;
                return binder.BindAttribute(attribute, AttributeType, diagnostics);
            }
            else if (SyntaxFacts.IsAttributeName(node))
            {
                return new BoundTypeExpression((NameSyntax)node, _aliasOpt, inferredType: false, type: AttributeType);
            }
            else
            {
                return base.Bind(binder, node, diagnostics);
            }
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ConstructorInitializerSyntax constructorInitializer, out SemanticModel speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, EqualsValueClauseSyntax initializer, out SemanticModel speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ArrowExpressionClauseSyntax expressionBody, out SemanticModel speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, StatementSyntax statement, out SemanticModel speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, BaseMethodDeclarationSyntax method, out SemanticModel speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, AccessorDeclarationSyntax accessor, out SemanticModel speculativeModel)
        {
            speculativeModel = null;
            return false;
        }
    }
}

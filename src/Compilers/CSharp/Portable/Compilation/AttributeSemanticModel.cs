// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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
        private readonly Symbol? _attributeTarget;

        internal AttributeSemanticModel(
            AttributeSyntax syntax,
            NamedTypeSymbol attributeType,
            Symbol? attributeTarget,
            AliasSymbol aliasOpt,
            Binder rootBinder,
            PublicSemanticModel containingPublicSemanticModel,
            ImmutableDictionary<Symbol, Symbol>? parentRemappedSymbolsOpt = null)
            : base(syntax, attributeType, new ExecutableCodeBinder(syntax, rootBinder.ContainingMember(), rootBinder), containingPublicSemanticModel, parentRemappedSymbolsOpt)
        {
            Debug.Assert(syntax != null);
            _aliasOpt = aliasOpt;
            _attributeTarget = attributeTarget;
        }

        /// <summary>
        /// Creates an AttributeSemanticModel that allows asking semantic questions about an attribute node.
        /// </summary>
        public static AttributeSemanticModel Create(PublicSemanticModel containingSemanticModel, AttributeSyntax syntax, NamedTypeSymbol attributeType, AliasSymbol aliasOpt, Symbol? attributeTarget, Binder rootBinder, ImmutableDictionary<Symbol, Symbol>? parentRemappedSymbolsOpt)
        {
            rootBinder = attributeTarget is null ? rootBinder : new ContextualAttributeBinder(rootBinder, attributeTarget);
            return new AttributeSemanticModel(syntax, attributeType, attributeTarget, aliasOpt, rootBinder, containingSemanticModel, parentRemappedSymbolsOpt: parentRemappedSymbolsOpt);
        }

        /// <summary>
        /// Creates a speculative AttributeSemanticModel that allows asking semantic questions about an attribute node that did not appear in the original source code.
        /// </summary>
        public static SpeculativeSemanticModelWithMemberModel CreateSpeculative(SyntaxTreeSemanticModel parentSemanticModel, AttributeSyntax syntax, NamedTypeSymbol attributeType, AliasSymbol aliasOpt, Binder rootBinder, ImmutableDictionary<Symbol, Symbol> parentRemappedSymbolsOpt, int position)
        {
            return new SpeculativeSemanticModelWithMemberModel(parentSemanticModel, position, syntax, attributeType, aliasOpt, rootBinder, parentRemappedSymbolsOpt);
        }

        private NamedTypeSymbol AttributeType
        {
            get
            {
                return (NamedTypeSymbol)MemberSymbol;
            }
        }

        protected internal override CSharpSyntaxNode GetBindableSyntaxNode(CSharpSyntaxNode node)
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

        internal override BoundNode Bind(Binder binder, CSharpSyntaxNode node, BindingDiagnosticBag diagnostics)
        {
            if (node.Kind() == SyntaxKind.Attribute)
            {
                var attribute = (AttributeSyntax)node;
                return binder.BindAttribute(attribute, AttributeType, attributedMember: ContextualAttributeBinder.GetAttributedMember(_attributeTarget), diagnostics);
            }
            else if (SyntaxFacts.IsAttributeName(node))
            {
                return new BoundTypeExpression((NameSyntax)node, _aliasOpt, type: AttributeType);
            }
            else
            {
                return base.Bind(binder, node, diagnostics);
            }
        }

        protected override BoundNode RewriteNullableBoundNodesWithSnapshots(
            BoundNode boundRoot,
            Binder binder,
            DiagnosticBag diagnostics,
            bool createSnapshots,
            out NullableWalker.SnapshotManager? snapshotManager,
            ref ImmutableDictionary<Symbol, Symbol>? remappedSymbols)
        {
            return NullableWalker.AnalyzeAndRewrite(Compilation, symbol: null, boundRoot, binder, initialState: null, diagnostics, createSnapshots, out snapshotManager, ref remappedSymbols);
        }

        protected override void AnalyzeBoundNodeNullability(BoundNode boundRoot, Binder binder, DiagnosticBag diagnostics, bool createSnapshots)
        {
            NullableWalker.AnalyzeWithoutRewrite(Compilation, symbol: null, boundRoot, binder, diagnostics, createSnapshots);
        }

        protected override bool IsNullableAnalysisEnabled()
        {
            return IsNullableAnalysisEnabledIn(Compilation, (AttributeSyntax)Root);
        }

        internal static bool IsNullableAnalysisEnabledIn(CSharpCompilation compilation, AttributeSyntax syntax)
        {
            return compilation.IsNullableAnalysisEnabledIn(syntax);
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ConstructorInitializerSyntax constructorInitializer, out PublicSemanticModel? speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, PrimaryConstructorBaseTypeSyntax constructorInitializer, out PublicSemanticModel? speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, EqualsValueClauseSyntax initializer, out PublicSemanticModel? speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ArrowExpressionClauseSyntax expressionBody, out PublicSemanticModel? speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, StatementSyntax statement, out PublicSemanticModel? speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, BaseMethodDeclarationSyntax method, out PublicSemanticModel? speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, AccessorDeclarationSyntax accessor, out PublicSemanticModel? speculativeModel)
        {
            speculativeModel = null;
            return false;
        }
    }
}

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

        private AttributeSemanticModel(
            AttributeSyntax syntax,
            NamedTypeSymbol attributeType,
            AliasSymbol aliasOpt,
            Binder rootBinder,
            SyntaxTreeSemanticModel? containingSemanticModelOpt = null,
            SyntaxTreeSemanticModel? parentSemanticModelOpt = null,
            ImmutableDictionary<Symbol, Symbol>? parentRemappedSymbolsOpt = null,
            int speculatedPosition = 0)
            : base(syntax, attributeType, new ExecutableCodeBinder(syntax, rootBinder.ContainingMember(), rootBinder), containingSemanticModelOpt, parentSemanticModelOpt, snapshotManagerOpt: null, parentRemappedSymbolsOpt: parentRemappedSymbolsOpt, speculatedPosition)
        {
            Debug.Assert(syntax != null);
            _aliasOpt = aliasOpt;
        }

        /// <summary>
        /// Creates an AttributeSemanticModel that allows asking semantic questions about an attribute node.
        /// </summary>
        public static AttributeSemanticModel Create(SyntaxTreeSemanticModel containingSemanticModel, AttributeSyntax syntax, NamedTypeSymbol attributeType, AliasSymbol aliasOpt, Binder rootBinder, ImmutableDictionary<Symbol, Symbol> parentRemappedSymbolsOpt)
        {
            return new AttributeSemanticModel(syntax, attributeType, aliasOpt, rootBinder, containingSemanticModel, parentRemappedSymbolsOpt: parentRemappedSymbolsOpt);
        }

        /// <summary>
        /// Creates a speculative AttributeSemanticModel that allows asking semantic questions about an attribute node that did not appear in the original source code.
        /// </summary>
        public static AttributeSemanticModel CreateSpeculative(SyntaxTreeSemanticModel parentSemanticModel, AttributeSyntax syntax, NamedTypeSymbol attributeType, AliasSymbol aliasOpt, Binder rootBinder, ImmutableDictionary<Symbol, Symbol> parentRemappedSymbolsOpt, int position)
        {
            Debug.Assert(parentSemanticModel != null);
            Debug.Assert(rootBinder != null);
            Debug.Assert(rootBinder.IsSemanticModelBinder);

            return new AttributeSemanticModel(syntax, attributeType, aliasOpt, rootBinder, parentSemanticModelOpt: parentSemanticModel, parentRemappedSymbolsOpt: parentRemappedSymbolsOpt, speculatedPosition: position);
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
                // note: we should find the attributed member before binding the attribute as part of https://github.com/dotnet/roslyn/issues/53618
                return binder.BindAttribute(attribute, AttributeType, attributedMember: null, diagnostics);
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

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ConstructorInitializerSyntax constructorInitializer, out SemanticModel? speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, PrimaryConstructorBaseTypeSyntax constructorInitializer, out SemanticModel? speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, EqualsValueClauseSyntax initializer, out SemanticModel? speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ArrowExpressionClauseSyntax expressionBody, out SemanticModel? speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, StatementSyntax statement, out SemanticModel? speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, BaseMethodDeclarationSyntax method, out SemanticModel? speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, AccessorDeclarationSyntax accessor, out SemanticModel? speculativeModel)
        {
            speculativeModel = null;
            return false;
        }
    }
}

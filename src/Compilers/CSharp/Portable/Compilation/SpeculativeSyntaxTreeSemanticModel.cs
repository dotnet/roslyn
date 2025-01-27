// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Allows asking semantic questions about a tree of syntax nodes that did not appear in the original source code.
    /// Typically, an instance is obtained by a call to SemanticModel.TryGetSpeculativeSemanticModel. 
    /// </summary>
    internal class SpeculativeSyntaxTreeSemanticModel : SyntaxTreeSemanticModel
    {
        private readonly SyntaxTreeSemanticModel _parentSemanticModel;
        private readonly CSharpSyntaxNode _root;
        private readonly Binder _rootBinder;
        private readonly int _position;
        private readonly SpeculativeBindingOption _bindingOption;

        public static SpeculativeSyntaxTreeSemanticModel Create(SyntaxTreeSemanticModel parentSemanticModel, TypeSyntax root, Binder rootBinder, int position, SpeculativeBindingOption bindingOption)
        {
            return CreateCore(parentSemanticModel, root, rootBinder, position, bindingOption);
        }

        public static SpeculativeSyntaxTreeSemanticModel Create(SyntaxTreeSemanticModel parentSemanticModel, CrefSyntax root, Binder rootBinder, int position)
        {
            return CreateCore(parentSemanticModel, root, rootBinder, position, bindingOption: SpeculativeBindingOption.BindAsTypeOrNamespace);
        }

        private static SpeculativeSyntaxTreeSemanticModel CreateCore(SyntaxTreeSemanticModel parentSemanticModel, CSharpSyntaxNode root, Binder rootBinder, int position, SpeculativeBindingOption bindingOption)
        {
            Debug.Assert(parentSemanticModel is SyntaxTreeSemanticModel);
            Debug.Assert(root != null);
            Debug.Assert(root is TypeSyntax || root is CrefSyntax);
            Debug.Assert(rootBinder != null);
            Debug.Assert(rootBinder.IsSemanticModelBinder);

            var speculativeModel = new SpeculativeSyntaxTreeSemanticModel(parentSemanticModel, root, rootBinder, position, bindingOption);
            return speculativeModel;
        }

        private SpeculativeSyntaxTreeSemanticModel(SyntaxTreeSemanticModel parentSemanticModel, CSharpSyntaxNode root, Binder rootBinder, int position, SpeculativeBindingOption bindingOption)
            : base(parentSemanticModel.Compilation, parentSemanticModel.SyntaxTree, root.SyntaxTree, parentSemanticModel.Options)
        {
            _parentSemanticModel = parentSemanticModel;
            _root = root;
            _rootBinder = rootBinder;
            _position = position;
            _bindingOption = bindingOption;
        }

        public override bool IsSpeculativeSemanticModel
        {
            get
            {
                return true;
            }
        }

        public override int OriginalPositionForSpeculation
        {
            get
            {
                return _position;
            }
        }

        public override CSharpSemanticModel ParentModel
        {
            get
            {
                return _parentSemanticModel;
            }
        }

        internal override CSharpSyntaxNode Root
        {
            get
            {
                return _root;
            }
        }

        internal override BoundNode Bind(Binder binder, CSharpSyntaxNode node, BindingDiagnosticBag diagnostics)
        {
            return _parentSemanticModel.Bind(binder, node, diagnostics);
        }

        internal override Binder GetEnclosingBinderInternal(int position)
        {
            return _rootBinder;
        }

        private SpeculativeBindingOption GetSpeculativeBindingOption(ExpressionSyntax node)
        {
            if (SyntaxFacts.IsInNamespaceOrTypeContext(node))
            {
                return SpeculativeBindingOption.BindAsTypeOrNamespace;
            }

            return _bindingOption;
        }

        internal override SymbolInfo GetSymbolInfoWorker(CSharpSyntaxNode node, SymbolInfoOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var cref = node as CrefSyntax;
            if (cref != null)
            {
                return _parentSemanticModel.GetSpeculativeSymbolInfo(_position, cref, options);
            }

            var expression = (ExpressionSyntax)node;

            if ((options & SymbolInfoOptions.PreserveAliases) != 0)
            {
                var aliasSymbol = _parentSemanticModel.GetSpeculativeAliasInfo(_position, expression, this.GetSpeculativeBindingOption(expression));
                return new SymbolInfo(aliasSymbol);
            }

            return _parentSemanticModel.GetSpeculativeSymbolInfo(_position, expression, this.GetSpeculativeBindingOption(expression));
        }

        internal override CSharpTypeInfo GetTypeInfoWorker(CSharpSyntaxNode node, CancellationToken cancellationToken = default(CancellationToken))
        {
            var expression = (ExpressionSyntax)node;
            return _parentSemanticModel.GetSpeculativeTypeInfoWorker(_position, expression, this.GetSpeculativeBindingOption(expression));
        }
    }
}

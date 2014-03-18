// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly CSharpSemanticModel parentSemanticModel;
        private readonly CSharpSyntaxNode root;
        private readonly Binder rootBinder;
        private readonly int position;
        private readonly SpeculativeBindingOption bindingOption;

        public static SpeculativeSyntaxTreeSemanticModel Create(CSharpSemanticModel parentSemanticModel, TypeSyntax root, Binder rootBinder, int position, SpeculativeBindingOption bindingOption)
        {
            return CreateCore(parentSemanticModel, root, rootBinder, position, bindingOption);
        }

        public static SpeculativeSyntaxTreeSemanticModel Create(CSharpSemanticModel parentSemanticModel, CrefSyntax root, Binder rootBinder, int position)
        {
            return CreateCore(parentSemanticModel, root, rootBinder, position, bindingOption: SpeculativeBindingOption.BindAsTypeOrNamespace);
        }

        private static SpeculativeSyntaxTreeSemanticModel CreateCore(CSharpSemanticModel parentSemanticModel, CSharpSyntaxNode root, Binder rootBinder, int position, SpeculativeBindingOption bindingOption)
        {
            Debug.Assert(parentSemanticModel is SyntaxTreeSemanticModel);
            Debug.Assert(root != null);
            Debug.Assert(root is TypeSyntax || root is CrefSyntax);
            Debug.Assert(rootBinder != null);
            Debug.Assert(rootBinder.IsSemanticModelBinder);

            var speculativeModel = new SpeculativeSyntaxTreeSemanticModel(parentSemanticModel, root, rootBinder, position, bindingOption);
            return speculativeModel;
        }

        private SpeculativeSyntaxTreeSemanticModel(CSharpSemanticModel parentSemanticModel, CSharpSyntaxNode root, Binder rootBinder, int position, SpeculativeBindingOption bindingOption)
            : base(parentSemanticModel.Compilation, parentSemanticModel.SyntaxTree, root.SyntaxTree)
        {
            this.parentSemanticModel = parentSemanticModel;
            this.root = root;
            this.rootBinder = rootBinder;
            this.position = position;
            this.bindingOption = bindingOption;
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
                return this.position;
            }
        }

        public override CSharpSemanticModel ParentModel
        {
            get
            {
                return this.parentSemanticModel;
            }
        }

        internal override CSharpSyntaxNode Root
        {
            get
            {
                return this.root;
            }
        }

        internal override BoundNode Bind(Binder binder, CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            return parentSemanticModel.Bind(binder, node, diagnostics);
        }

        internal override Binder GetEnclosingBinderInternal(int position)
        {
            return this.rootBinder;
        }

        private SpeculativeBindingOption GetSpeculativeBindingOption(ExpressionSyntax node)
        {
            if (SyntaxFacts.IsInNamespaceOrTypeContext(node))
            {
                return SpeculativeBindingOption.BindAsTypeOrNamespace;
            }

            return this.bindingOption;
        }

        internal override SymbolInfo GetSymbolInfoWorker(CSharpSyntaxNode node, SymbolInfoOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var cref = node as CrefSyntax;
            if (cref != null)
            {
                return parentSemanticModel.GetSpeculativeSymbolInfo(position, cref, options);
            }

            var expression = (ExpressionSyntax)node;

            if ((options & SymbolInfoOptions.PreserveAliases) != 0)
            {
                var aliasSymbol = (AliasSymbol)parentSemanticModel.GetSpeculativeAliasInfo(position, expression, this.GetSpeculativeBindingOption(expression));
                return new SymbolInfo(aliasSymbol, ImmutableArray<ISymbol>.Empty, CandidateReason.None);
            }

            return parentSemanticModel.GetSpeculativeSymbolInfo(position, expression, this.GetSpeculativeBindingOption(expression));
        }

        internal override CSharpTypeInfo GetTypeInfoWorker(CSharpSyntaxNode node, CancellationToken cancellationToken = default(CancellationToken))
        {
            var expression = (ExpressionSyntax)node;
            return parentSemanticModel.GetSpeculativeTypeInfoWorker(position, expression, this.GetSpeculativeBindingOption(expression));
        }
    }
}
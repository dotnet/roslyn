// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class MethodBodySemanticModel : MemberSemanticModel
    {
        private MethodBodySemanticModel(CSharpCompilation compilation, Symbol owner, Binder rootBinder, CSharpSyntaxNode syntax, SyntaxTreeSemanticModel parentSemanticModelOpt = null, int speculatedPosition = 0)
            : base(compilation, syntax, owner, rootBinder, parentSemanticModelOpt, speculatedPosition)
        {
            Debug.Assert((object)owner != null);
            Debug.Assert(owner.Kind == SymbolKind.Method);
            Debug.Assert(syntax != null);
            Debug.Assert(syntax.Kind() != SyntaxKind.CompilationUnit);
        }

        /// <summary>
        /// Creates a SemanticModel that creates and owns the ExecutableCodeBinder for the method of which it is a model.
        /// </summary>
        internal static MethodBodySemanticModel Create(CSharpCompilation compilation, MethodSymbol owner, Binder rootBinder, CSharpSyntaxNode syntax)
        {
            var executableCodeBinder = new ExecutableCodeBinder(syntax, owner, rootBinder);
            return new MethodBodySemanticModel(compilation, owner, executableCodeBinder, syntax);
        }

        internal override BoundNode Bind(Binder binder, CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            if (node.Kind() == SyntaxKind.ArrowExpressionClause)
            {
                return binder.BindExpressionBodyAsBlock((ArrowExpressionClauseSyntax)node, diagnostics);
            }
            return base.Bind(binder, node, diagnostics);
        }

        internal override BoundNode GetBoundRoot()
        {
            CSharpSyntaxNode root = this.Root;
            if (root.Kind() == SyntaxKind.ArrowExpressionClause)
            {
                root = ((ArrowExpressionClauseSyntax)root).Expression;
                return GetUpperBoundNode(GetBindableSyntaxNode(root));
            }
            return base.GetBoundRoot();
        }

        /// <summary>
        /// Creates a speculative SemanticModel for a method body that did not appear in the original source code.
        /// </summary>
        internal static MethodBodySemanticModel CreateSpeculative(SyntaxTreeSemanticModel parentSemanticModel, MethodSymbol owner, StatementSyntax syntax, Binder rootBinder, int position)
        {
            Debug.Assert(parentSemanticModel != null);
            Debug.Assert(syntax != null);
            Debug.Assert(rootBinder != null);
            Debug.Assert(rootBinder.IsSemanticModelBinder);

            return new MethodBodySemanticModel(parentSemanticModel.Compilation, owner, rootBinder, syntax, parentSemanticModel, position);
        }

        /// <summary>
        /// Creates a speculative SemanticModel for an expression body that did not appear in the original source code.
        /// </summary>
        internal static MethodBodySemanticModel CreateSpeculative(SyntaxTreeSemanticModel parentSemanticModel, MethodSymbol owner, ArrowExpressionClauseSyntax syntax, Binder rootBinder, int position)
        {
            Debug.Assert(parentSemanticModel != null);
            Debug.Assert(syntax != null);
            Debug.Assert(rootBinder != null);
            Debug.Assert(rootBinder.IsSemanticModelBinder);

            return new MethodBodySemanticModel(parentSemanticModel.Compilation, owner, rootBinder, syntax, parentSemanticModel, position);
        }

        internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, BaseMethodDeclarationSyntax method, out SemanticModel speculativeModel)
        {
            // CONSIDER: Do we want to ensure that speculated method and the original method have identical signatures?
            return GetSpeculativeSemanticModelForMethodBody(parentModel, position, method.Body, out speculativeModel);
        }

        private bool GetSpeculativeSemanticModelForMethodBody(SyntaxTreeSemanticModel parentModel, int position, BlockSyntax body, out SemanticModel speculativeModel)
        {
            position = CheckAndAdjustPosition(position);

            var methodSymbol = (MethodSymbol)this.MemberSymbol;
            var executablebinder = new ExecutableCodeBinder(body, methodSymbol, this.RootBinder.Next); // Strip off ExecutableCodeBinder (see ctor).
            var blockBinder = executablebinder.GetBinder(body).WithAdditionalFlags(GetSemanticModelBinderFlags());
            speculativeModel = CreateSpeculative(parentModel, methodSymbol, body, blockBinder, position);
            return true;
        }

        internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, AccessorDeclarationSyntax accessor, out SemanticModel speculativeModel)
        {
            return GetSpeculativeSemanticModelForMethodBody(parentModel, position, accessor.Body, out speculativeModel);
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, StatementSyntax statement, out SemanticModel speculativeModel)
        {
            position = CheckAndAdjustPosition(position);

            var binder = this.GetEnclosingBinder(position);
            if (binder == null)
            {
                speculativeModel = null;
                return false;
            }

            var methodSymbol = (MethodSymbol)this.MemberSymbol;
            binder = new ExecutableCodeBinder(statement, methodSymbol, binder);

            // local declaration statements need to be wrapped in a block so the local gets seen 
            if (!statement.IsKind(SyntaxKind.Block))
            {
                binder = new BlockBinder(binder, new SyntaxList<StatementSyntax>(statement));
            }

            speculativeModel = CreateSpeculative(parentModel, methodSymbol, statement, binder, position);
            return true;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ArrowExpressionClauseSyntax expressionBody, out SemanticModel speculativeModel)
        {
            position = CheckAndAdjustPosition(position);

            var binder = this.GetEnclosingBinder(position);
            if (binder == null)
            {
                speculativeModel = null;
                return false;
            }

            var methodSymbol = (MethodSymbol)this.MemberSymbol;
            binder = new ExecutableCodeBinder(expressionBody, methodSymbol, binder);

            speculativeModel = CreateSpeculative(parentModel, methodSymbol, expressionBody, binder, position);
            return true;
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
    }
}

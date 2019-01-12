// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class MethodBodySemanticModel : MemberSemanticModel
    {
        private MethodBodySemanticModel(
            Symbol owner,
            Binder rootBinder,
            CSharpSyntaxNode syntax,
            SyntaxTreeSemanticModel containingSemanticModelOpt = null,
            SyntaxTreeSemanticModel parentSemanticModelOpt = null,
            int speculatedPosition = 0)
            : base(syntax, owner, rootBinder, containingSemanticModelOpt, parentSemanticModelOpt, speculatedPosition)
        {
            Debug.Assert((object)owner != null);
            Debug.Assert(owner.Kind == SymbolKind.Method);
            Debug.Assert(syntax != null);
            Debug.Assert(syntax.Kind() != SyntaxKind.CompilationUnit);
        }

        /// <summary>
        /// Creates a SemanticModel for the method.
        /// </summary>
        internal static MethodBodySemanticModel Create(SyntaxTreeSemanticModel containingSemanticModel, MethodSymbol owner, ExecutableCodeBinder executableCodeBinder,
                                                       CSharpSyntaxNode syntax, BoundNode boundNode = null)
        {
            Debug.Assert(containingSemanticModel != null);
            var result = new MethodBodySemanticModel(owner, executableCodeBinder, syntax, containingSemanticModel);

            if (boundNode != null)
            {
                result.UnguardedAddBoundTreeForStandaloneSyntax(syntax, boundNode);
            }

            return result;
        }

        internal override BoundNode Bind(Binder binder, CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ArrowExpressionClause:
                    return binder.BindExpressionBodyAsBlock((ArrowExpressionClauseSyntax)node, diagnostics);

                case SyntaxKind.BaseConstructorInitializer:
                case SyntaxKind.ThisConstructorInitializer:
                    return binder.BindConstructorInitializer((ConstructorInitializerSyntax)node, diagnostics);

                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    return binder.BindMethodBody(node, diagnostics);
            }

            return base.Bind(binder, node, diagnostics);
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

            return new MethodBodySemanticModel(owner, rootBinder, syntax, parentSemanticModelOpt: parentSemanticModel, speculatedPosition: position);
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

            return new MethodBodySemanticModel(owner, rootBinder, syntax, parentSemanticModelOpt: parentSemanticModel, speculatedPosition: position);
        }

        /// <summary>
        /// Creates a speculative SemanticModel for a constructor initializer that did not appear in the original source code.
        /// </summary>
        internal static MethodBodySemanticModel CreateSpeculative(SyntaxTreeSemanticModel parentSemanticModel, MethodSymbol owner, ConstructorInitializerSyntax syntax, Binder rootBinder, int position)
        {
            Debug.Assert(parentSemanticModel != null);
            Debug.Assert(syntax != null);
            Debug.Assert(rootBinder != null);
            Debug.Assert(rootBinder.IsSemanticModelBinder);

            return new MethodBodySemanticModel(owner, rootBinder, syntax, parentSemanticModelOpt: parentSemanticModel, speculatedPosition: position);
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

            // Strip off ExecutableCodeBinder (see ctor).
            Binder binder = this.RootBinder;

            do
            {
                if (binder is ExecutableCodeBinder)
                {
                    binder = binder.Next;
                    break;
                }

                binder = binder.Next;
            }
            while (binder != null);

            Debug.Assert(binder != null);

            var executablebinder = new ExecutableCodeBinder(body, methodSymbol, binder ?? this.RootBinder);
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
            if ((MemberSymbol as MethodSymbol)?.MethodKind == MethodKind.Constructor)
            {
                var binder = this.GetEnclosingBinder(position);
                if (binder != null)
                {
                    var methodSymbol = (MethodSymbol)this.MemberSymbol;
                    binder = new ExecutableCodeBinder(constructorInitializer, methodSymbol, binder);
                    speculativeModel = CreateSpeculative(parentModel, methodSymbol, constructorInitializer, binder, position);
                    return true;
                }
            }

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

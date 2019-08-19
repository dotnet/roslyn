// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class MethodBodySemanticModel : MemberSemanticModel
    {
#nullable enable
        /// <summary>
        /// Initial state for a MethodBodySemanticModel. Shared between here and the <see cref="MethodCompiler"/>. Used to make a <see cref="MethodBodySemanticModel"/>
        /// with the required syntax and optional precalculated starting state for the model.
        /// </summary>
        internal readonly struct InitialState
        {
            internal readonly CSharpSyntaxNode Syntax;
            internal readonly BoundNode? Body;
            internal readonly ExecutableCodeBinder? Binder;
            internal readonly NullableWalker.SnapshotManager? SnapshotManager;

            internal InitialState(CSharpSyntaxNode syntax, BoundNode? bodyOpt = null, ExecutableCodeBinder? binder = null, NullableWalker.SnapshotManager? snapshotManager = null)
            {
                Syntax = syntax;
                Body = bodyOpt;
                Binder = binder;
                SnapshotManager = snapshotManager;
            }
        }
#nullable restore

        private MethodBodySemanticModel(
            Symbol owner,
            Binder rootBinder,
            CSharpSyntaxNode syntax,
            SyntaxTreeSemanticModel containingSemanticModelOpt = null,
            SyntaxTreeSemanticModel parentSemanticModelOpt = null,
            NullableWalker.SnapshotManager snapshotManagerOpt = null,
            int speculatedPosition = 0)
            : base(syntax, owner, rootBinder, containingSemanticModelOpt, parentSemanticModelOpt, snapshotManagerOpt, speculatedPosition)
        {
            Debug.Assert((object)owner != null);
            Debug.Assert(owner.Kind == SymbolKind.Method);
            Debug.Assert(syntax != null);
            Debug.Assert(syntax.Kind() != SyntaxKind.CompilationUnit);
        }

        /// <summary>
        /// Creates a SemanticModel for the method.
        /// </summary>
        internal static MethodBodySemanticModel Create(SyntaxTreeSemanticModel containingSemanticModel, MethodSymbol owner, InitialState initialState)
        {
            Debug.Assert(containingSemanticModel != null);
            var result = new MethodBodySemanticModel(owner, initialState.Binder, initialState.Syntax, containingSemanticModel);

            if (initialState.Body != null)
            {
                result.UnguardedAddBoundTreeForStandaloneSyntax(initialState.Syntax, initialState.Body, initialState.SnapshotManager);
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
        internal static MethodBodySemanticModel CreateSpeculative(SyntaxTreeSemanticModel parentSemanticModel, MethodSymbol owner, StatementSyntax syntax, Binder rootBinder, NullableWalker.SnapshotManager snapshotManagerOpt, int position)
        {
            Debug.Assert(parentSemanticModel != null);
            Debug.Assert(syntax != null);
            Debug.Assert(rootBinder != null);
            Debug.Assert(rootBinder.IsSemanticModelBinder);

            return new MethodBodySemanticModel(owner, rootBinder, syntax, parentSemanticModelOpt: parentSemanticModel, snapshotManagerOpt: snapshotManagerOpt, speculatedPosition: position);
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
            // We don't pass the snapshot manager along here, because we're speculating about an entirely new body and it should not
            // be influenced by any existing code in the body.
            speculativeModel = CreateSpeculative(parentModel, methodSymbol, body, blockBinder, snapshotManagerOpt: null, position);
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
            speculativeModel = CreateSpeculative(parentModel, methodSymbol, statement, binder, GetSnapshotManager(), position);
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

        protected override BoundNode RewriteNullableBoundNodesWithSnapshots(BoundNode boundRoot, Binder binder, DiagnosticBag diagnostics, bool createSnapshots, out NullableWalker.SnapshotManager snapshotManager)
        {
            return NullableWalker.AnalyzeAndRewrite(Compilation, MemberSymbol, boundRoot, binder, diagnostics, createSnapshots, out snapshotManager);
        }
    }
}

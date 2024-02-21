// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class MethodBodySemanticModel : MemberSemanticModel
    {
        /// <summary>
        /// Initial state for a MethodBodySemanticModel. Shared between here and the <see cref="MethodCompiler"/>. Used to make a <see cref="MethodBodySemanticModel"/>
        /// with the required syntax and optional precalculated starting state for the model.
        /// </summary>
        internal readonly struct InitialState
        {
            internal readonly CSharpSyntaxNode Syntax;
            internal readonly BoundNode? Body;
            internal readonly Binder? Binder;
            internal readonly NullableWalker.SnapshotManager? SnapshotManager;
            internal readonly ImmutableDictionary<Symbol, Symbol>? RemappedSymbols;

            internal InitialState(
                CSharpSyntaxNode syntax,
                BoundNode? bodyOpt = null,
                Binder? binder = null,
                NullableWalker.SnapshotManager? snapshotManager = null,
                ImmutableDictionary<Symbol, Symbol>? remappedSymbols = null)
            {
                Syntax = syntax;
                Body = bodyOpt;
                Binder = binder;
                SnapshotManager = snapshotManager;
                RemappedSymbols = remappedSymbols;
            }
        }
#nullable disable

        internal MethodBodySemanticModel(
            MethodSymbol owner,
            Binder rootBinder,
            CSharpSyntaxNode syntax,
            PublicSemanticModel containingPublicSemanticModel,
            ImmutableDictionary<Symbol, Symbol> parentRemappedSymbolsOpt = null)
            : base(syntax, owner, rootBinder, containingPublicSemanticModel, parentRemappedSymbolsOpt)
        {
            Debug.Assert((object)owner != null);
            Debug.Assert(owner.Kind == SymbolKind.Method);
            Debug.Assert(syntax != null);
            Debug.Assert(parentRemappedSymbolsOpt is null || IsSpeculativeSemanticModel);
            Debug.Assert((syntax.Kind() == SyntaxKind.CompilationUnit) == (!IsSpeculativeSemanticModel && owner is SynthesizedSimpleProgramEntryPointSymbol));
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
                result.UnguardedAddBoundTreeForStandaloneSyntax(initialState.Syntax, initialState.Body, initialState.SnapshotManager, initialState.RemappedSymbols);
            }

            return result;
        }

        internal override BoundNode Bind(Binder binder, CSharpSyntaxNode node, BindingDiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ArrowExpressionClause:
                    return binder.BindExpressionBodyAsBlock((ArrowExpressionClauseSyntax)node, diagnostics);

                case SyntaxKind.BaseConstructorInitializer:
                case SyntaxKind.ThisConstructorInitializer:
                    return binder.BindConstructorInitializer((ConstructorInitializerSyntax)node, diagnostics);

                case SyntaxKind.PrimaryConstructorBaseType:
                    return binder.BindConstructorInitializer((PrimaryConstructorBaseTypeSyntax)node, diagnostics);

                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.InitAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.CompilationUnit:
                case SyntaxKind.RecordDeclaration:
                case SyntaxKind.ClassDeclaration:
                    return binder.BindMethodBody(node, diagnostics);
            }

            return base.Bind(binder, node, diagnostics);
        }

        /// <summary>
        /// Creates a speculative SemanticModel for a method body that did not appear in the original source code.
        /// </summary>
        internal static SpeculativeSemanticModelWithMemberModel CreateSpeculative(
            SyntaxTreeSemanticModel parentSemanticModel,
            MethodSymbol owner,
            StatementSyntax syntax,
            Binder rootBinder,
            NullableWalker.SnapshotManager snapshotManagerOpt,
            ImmutableDictionary<Symbol, Symbol> parentRemappedSymbolsOpt,
            int position)
        {
            return CreateSpeculativeForNode(parentSemanticModel, owner, syntax, rootBinder, snapshotManagerOpt, parentRemappedSymbolsOpt, position);
        }

        private static SpeculativeSemanticModelWithMemberModel CreateSpeculativeForNode(
            SyntaxTreeSemanticModel parentSemanticModel,
            MethodSymbol owner,
            CSharpSyntaxNode syntax,
            Binder rootBinder,
            NullableWalker.SnapshotManager snapshotManagerOpt,
            ImmutableDictionary<Symbol, Symbol> parentRemappedSymbolsOpt,
            int position)
        {
            return new SpeculativeSemanticModelWithMemberModel(parentSemanticModel, position, owner, syntax, rootBinder, parentRemappedSymbolsOpt, snapshotManagerOpt);
        }

        /// <summary>
        /// Creates a speculative SemanticModel for an expression body that did not appear in the original source code.
        /// </summary>
        internal static SpeculativeSemanticModelWithMemberModel CreateSpeculative(SyntaxTreeSemanticModel parentSemanticModel, MethodSymbol owner, ArrowExpressionClauseSyntax syntax, Binder rootBinder, int position)
        {
            return CreateSpeculativeForNode(parentSemanticModel, owner, syntax, rootBinder, snapshotManagerOpt: null, parentRemappedSymbolsOpt: null, position);
        }

        /// <summary>
        /// Creates a speculative SemanticModel for a constructor initializer that did not appear in the original source code.
        /// </summary>
        internal static SpeculativeSemanticModelWithMemberModel CreateSpeculative(SyntaxTreeSemanticModel parentSemanticModel, MethodSymbol owner, ConstructorInitializerSyntax syntax, Binder rootBinder, int position)
        {
            return CreateSpeculativeForNode(parentSemanticModel, owner, syntax, rootBinder, snapshotManagerOpt: null, parentRemappedSymbolsOpt: null, position);
        }

        /// <summary>
        /// Creates a speculative SemanticModel for a constructor initializer that did not appear in the original source code.
        /// </summary>
        internal static SpeculativeSemanticModelWithMemberModel CreateSpeculative(SyntaxTreeSemanticModel parentSemanticModel, MethodSymbol owner, PrimaryConstructorBaseTypeSyntax syntax, Binder rootBinder, int position)
        {
            return CreateSpeculativeForNode(parentSemanticModel, owner, syntax, rootBinder, snapshotManagerOpt: null, parentRemappedSymbolsOpt: null, position);
        }

        internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, BaseMethodDeclarationSyntax method, out PublicSemanticModel speculativeModel)
        {
            // CONSIDER: Do we want to ensure that speculated method and the original method have identical signatures?
            return GetSpeculativeSemanticModelForMethodBody(parentModel, position, method.Body, out speculativeModel);
        }

        private bool GetSpeculativeSemanticModelForMethodBody(SyntaxTreeSemanticModel parentModel, int position, BlockSyntax body, out PublicSemanticModel speculativeModel)
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

            Binder executablebinder = new WithNullableContextBinder(SyntaxTree, position, binder ?? this.RootBinder);
            executablebinder = new ExecutableCodeBinder(body, methodSymbol, executablebinder);
            var blockBinder = executablebinder.GetBinder(body).WithAdditionalFlags(GetSemanticModelBinderFlags());
            // We don't pass the snapshot manager along here, because we're speculating about an entirely new body and it should not
            // be influenced by any existing code in the body.
            speculativeModel = CreateSpeculative(parentModel, methodSymbol, body, blockBinder, snapshotManagerOpt: null, parentRemappedSymbolsOpt: null, position);
            return true;
        }

        internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, AccessorDeclarationSyntax accessor, out PublicSemanticModel speculativeModel)
        {
            return GetSpeculativeSemanticModelForMethodBody(parentModel, position, accessor.Body, out speculativeModel);
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, StatementSyntax statement, out PublicSemanticModel speculativeModel)
        {
            position = CheckAndAdjustPosition(position);

            var binder = this.GetEnclosingBinder(position);
            if (binder == null)
            {
                speculativeModel = null;
                return false;
            }

            var methodSymbol = (MethodSymbol)this.MemberSymbol;
            binder = new WithNullableContextBinder(SyntaxTree, position, binder);
            binder = new ExecutableCodeBinder(statement, methodSymbol, binder);
            speculativeModel = CreateSpeculative(parentModel, methodSymbol, statement, binder, GetSnapshotManager(), GetRemappedSymbols(), position);
            return true;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ArrowExpressionClauseSyntax expressionBody, out PublicSemanticModel speculativeModel)
        {
            position = CheckAndAdjustPosition(position);

            var binder = this.GetEnclosingBinder(position);
            if (binder == null)
            {
                speculativeModel = null;
                return false;
            }

            var methodSymbol = (MethodSymbol)this.MemberSymbol;
            binder = new WithNullableContextBinder(SyntaxTree, position, binder);
            binder = new ExecutableCodeBinder(expressionBody, methodSymbol, binder);

            speculativeModel = CreateSpeculative(parentModel, methodSymbol, expressionBody, binder, position);
            return true;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ConstructorInitializerSyntax constructorInitializer, out PublicSemanticModel speculativeModel)
        {
            if (MemberSymbol is MethodSymbol methodSymbol && methodSymbol.MethodKind == MethodKind.Constructor &&
                Root.FindToken(position).Parent?.AncestorsAndSelf().OfType<ConstructorInitializerSyntax>().FirstOrDefault()?.Parent == Root)
            {
                var binder = this.GetEnclosingBinder(position);
                if (binder != null)
                {
                    binder = new WithNullableContextBinder(SyntaxTree, position, binder);
                    binder = new ExecutableCodeBinder(constructorInitializer, methodSymbol, binder);
                    speculativeModel = CreateSpeculative(parentModel, methodSymbol, constructorInitializer, binder, position);
                    return true;
                }
            }

            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, PrimaryConstructorBaseTypeSyntax constructorInitializer, out PublicSemanticModel speculativeModel)
        {
            if (MemberSymbol is SynthesizedPrimaryConstructor primaryCtor &&
                primaryCtor.GetSyntax() is TypeDeclarationSyntax typeDecl)
            {
                Debug.Assert(typeDecl.Kind() is (SyntaxKind.RecordDeclaration or SyntaxKind.ClassDeclaration));
                if (Root.FindToken(position).Parent?.AncestorsAndSelf().OfType<PrimaryConstructorBaseTypeSyntax>().FirstOrDefault() == typeDecl.PrimaryConstructorBaseTypeIfClass)
                {
                    var binder = this.GetEnclosingBinder(position);
                    if (binder != null)
                    {
                        binder = new WithNullableContextBinder(SyntaxTree, position, binder);
                        binder = new ExecutableCodeBinder(constructorInitializer, primaryCtor, binder);
                        speculativeModel = CreateSpeculative(parentModel, primaryCtor, constructorInitializer, binder, position);
                        return true;
                    }
                }
            }

            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, EqualsValueClauseSyntax initializer, out PublicSemanticModel speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        protected override BoundNode RewriteNullableBoundNodesWithSnapshots(
            BoundNode boundRoot,
            Binder binder,
            DiagnosticBag diagnostics,
            bool createSnapshots,
            out NullableWalker.SnapshotManager snapshotManager,
            ref ImmutableDictionary<Symbol, Symbol> remappedSymbols)
        {
            var afterInitializersState = NullableWalker.GetAfterInitializersState(Compilation, MemberSymbol, boundRoot);
            return NullableWalker.AnalyzeAndRewrite(Compilation, MemberSymbol, boundRoot, binder, afterInitializersState, diagnostics, createSnapshots, out snapshotManager, ref remappedSymbols);
        }

        protected override void AnalyzeBoundNodeNullability(BoundNode boundRoot, Binder binder, DiagnosticBag diagnostics, bool createSnapshots)
        {
            NullableWalker.AnalyzeWithoutRewrite(Compilation, MemberSymbol, boundRoot, binder, diagnostics, createSnapshots);
        }

        protected override bool IsNullableAnalysisEnabled()
        {
            return Compilation.IsNullableAnalysisEnabledIn((MethodSymbol)MemberSymbol);
        }
    }
}

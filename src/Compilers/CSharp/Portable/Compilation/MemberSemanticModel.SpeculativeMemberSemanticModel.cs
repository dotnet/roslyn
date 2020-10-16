﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class MemberSemanticModel
    {
        /// <summary>
        /// Allows asking semantic questions about a TypeSyntax (or its descendants) within a member, that did not appear in the original source code.
        /// Typically, an instance is obtained by a call to SemanticModel.TryGetSpeculativeSemanticModel. 
        /// </summary>
        private sealed class SpeculativeMemberSemanticModel : MemberSemanticModel
        {
            /// <summary>
            /// Creates a speculative SemanticModel for a TypeSyntax node at a position within an existing MemberSemanticModel.
            /// </summary>
            public SpeculativeMemberSemanticModel(SyntaxTreeSemanticModel parentSemanticModel, Symbol owner, TypeSyntax root, Binder rootBinder, NullableWalker.SnapshotManager snapshotManagerOpt, ImmutableDictionary<Symbol, Symbol> parentRemappedSymbolsOpt, int position)
                : base(root, owner, rootBinder, containingSemanticModelOpt: null, parentSemanticModelOpt: parentSemanticModel, snapshotManagerOpt, parentRemappedSymbolsOpt, speculatedPosition: position)
            {
            }

            protected override NullableWalker.SnapshotManager GetSnapshotManager()
            {
                // In this override, current nullability state cannot influence anything of speculatively bound expressions.
                return _parentSnapshotManagerOpt;
            }

            protected override BoundNode RewriteNullableBoundNodesWithSnapshots(
                BoundNode boundRoot,
                Binder binder,
                DiagnosticBag diagnostics,
                bool createSnapshots,
                out NullableWalker.SnapshotManager snapshotManager,
                ref ImmutableDictionary<Symbol, Symbol> remappedSymbols)
            {
                Debug.Assert(boundRoot.Syntax is TypeSyntax);
                return NullableWalker.AnalyzeAndRewrite(Compilation, MemberSymbol as MethodSymbol, boundRoot, binder, initialState: null, diagnostics, createSnapshots: false, out snapshotManager, ref remappedSymbols);
            }

#if DEBUG
            protected override void AnalyzeBoundNodeNullability(BoundNode boundRoot, Binder binder, DiagnosticBag diagnostics, bool createSnapshots)
            {
                NullableWalker.AnalyzeWithoutRewrite(Compilation, MemberSymbol as MethodSymbol, boundRoot, binder, diagnostics, createSnapshots);
            }
#endif

            internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ConstructorInitializerSyntax constructorInitializer, out SemanticModel speculativeModel)
            {
                throw ExceptionUtilities.Unreachable;
            }

            internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, PrimaryConstructorBaseTypeSyntax constructorInitializer, out SemanticModel speculativeModel)
            {
                throw ExceptionUtilities.Unreachable;
            }

            internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, EqualsValueClauseSyntax initializer, out SemanticModel speculativeModel)
            {
                throw ExceptionUtilities.Unreachable;
            }

            internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ArrowExpressionClauseSyntax expressionBody, out SemanticModel speculativeModel)
            {
                throw ExceptionUtilities.Unreachable;
            }

            internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, StatementSyntax statement, out SemanticModel speculativeModel)
            {
                throw ExceptionUtilities.Unreachable;
            }

            internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, BaseMethodDeclarationSyntax method, out SemanticModel speculativeModel)
            {
                throw ExceptionUtilities.Unreachable;
            }

            internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, AccessorDeclarationSyntax accessor, out SemanticModel speculativeModel)
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}

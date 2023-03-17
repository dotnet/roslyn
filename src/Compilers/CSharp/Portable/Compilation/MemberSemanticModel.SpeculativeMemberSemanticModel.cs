// Licensed to the .NET Foundation under one or more agreements.
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
        internal sealed class SpeculativeMemberSemanticModel : MemberSemanticModel
        {
            /// <summary>
            /// Creates a speculative SemanticModel for a TypeSyntax node at a position within an existing MemberSemanticModel.
            /// </summary>
            public SpeculativeMemberSemanticModel(
                PublicSemanticModel containingPublicSemanticModel,
                Symbol owner,
                TypeSyntax root,
                Binder rootBinder,
                ImmutableDictionary<Symbol, Symbol> parentRemappedSymbolsOpt)
                : base(root, owner, rootBinder, containingPublicSemanticModel: containingPublicSemanticModel, parentRemappedSymbolsOpt)
            {
                Debug.Assert(containingPublicSemanticModel is not null);
            }

            protected override NullableWalker.SnapshotManager GetSnapshotManager()
            {
                // In this override, current nullability state cannot influence anything of speculatively bound expressions.
                return ((SpeculativeSemanticModelWithMemberModel)_containingPublicSemanticModel).ParentSnapshotManagerOpt;
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

            protected override void AnalyzeBoundNodeNullability(BoundNode boundRoot, Binder binder, DiagnosticBag diagnostics, bool createSnapshots)
            {
                NullableWalker.AnalyzeWithoutRewrite(Compilation, MemberSymbol as MethodSymbol, boundRoot, binder, diagnostics, createSnapshots);
            }

            protected override bool IsNullableAnalysisEnabled()
            {
                return ((SyntaxTreeSemanticModel)_containingPublicSemanticModel.ParentModel).IsNullableAnalysisEnabledAtSpeculativePosition(_containingPublicSemanticModel.OriginalPositionForSpeculation, Root);
            }

            internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ConstructorInitializerSyntax constructorInitializer, out PublicSemanticModel speculativeModel)
            {
                throw ExceptionUtilities.Unreachable();
            }

            internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, PrimaryConstructorBaseTypeSyntax constructorInitializer, out PublicSemanticModel speculativeModel)
            {
                throw ExceptionUtilities.Unreachable();
            }

            internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, EqualsValueClauseSyntax initializer, out PublicSemanticModel speculativeModel)
            {
                throw ExceptionUtilities.Unreachable();
            }

            internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ArrowExpressionClauseSyntax expressionBody, out PublicSemanticModel speculativeModel)
            {
                throw ExceptionUtilities.Unreachable();
            }

            internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, StatementSyntax statement, out PublicSemanticModel speculativeModel)
            {
                throw ExceptionUtilities.Unreachable();
            }

            internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, BaseMethodDeclarationSyntax method, out PublicSemanticModel speculativeModel)
            {
                throw ExceptionUtilities.Unreachable();
            }

            internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, AccessorDeclarationSyntax accessor, out PublicSemanticModel speculativeModel)
            {
                throw ExceptionUtilities.Unreachable();
            }
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            public SpeculativeMemberSemanticModel(SyntaxTreeSemanticModel parentSemanticModel, Symbol owner, TypeSyntax root, Binder rootBinder, int position)
                : base(root, owner, rootBinder, containingSemanticModelOpt: null, parentSemanticModelOpt: parentSemanticModel, speculatedPosition: position)
            {
            }

            protected override BoundNode RewriteNullableBoundNodes(BoundNode boundRoot, Conversions conversions, DiagnosticBag diagnostics)
            {
                // https://github.com/dotnet/roslyn/issues/35037: Speculative models are going to have to do something more advanced
                // here. They will need to run nullable analysis up to the point that is being speculated on, and
                // then take that state and run analysis on the statement or expression being speculated on.
                // Currently, it will return incorrect info because it's just running analysis on the speculated
                // part.
                return NullableWalker.AnalyzeAndRewrite(Compilation, MemberSymbol as MethodSymbol, boundRoot, conversions, diagnostics);
            }

            internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ConstructorInitializerSyntax constructorInitializer, out SemanticModel speculativeModel)
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

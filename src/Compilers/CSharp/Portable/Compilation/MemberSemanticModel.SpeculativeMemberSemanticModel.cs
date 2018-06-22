// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
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

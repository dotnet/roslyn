// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A base class for visiting all variable declarators.
    /// </summary>
    internal abstract class LocalVariableDeclaratorsVisitor : CSharpSyntaxWalker
    {
        protected abstract void VisitFixedStatementDeclarations(FixedStatementSyntax node);
        protected abstract void VisitForEachStatementDeclarations(ForEachStatementSyntax node);
        protected abstract void VisitLockStatementDeclarations(LockStatementSyntax node);
        protected abstract void VisitUsingStatementDeclarations(UsingStatementSyntax node);

        public sealed override void VisitFixedStatement(FixedStatementSyntax node)
        {
            this.VisitFixedStatementDeclarations(node);
            this.Visit(node.Statement);
        }

        public sealed override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            this.VisitForEachStatementDeclarations(node);
            base.VisitForEachStatement(node);
        }

        public sealed override void VisitLockStatement(LockStatementSyntax node)
        {
            this.VisitLockStatementDeclarations(node);
            base.VisitLockStatement(node);
        }

        public sealed override void VisitUsingStatement(UsingStatementSyntax node)
        {
            this.VisitUsingStatementDeclarations(node);
            base.VisitUsingStatement(node);
        }

        public abstract override void VisitVariableDeclarator(VariableDeclaratorSyntax node);
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    internal sealed class LocalVariableDeclaratorsCollector : CSharpSyntaxWalker
    {
        private readonly ArrayBuilder<SyntaxNode> _builder;

        private LocalVariableDeclaratorsCollector(ArrayBuilder<SyntaxNode> builder)
        {
            _builder = builder;
        }

        internal static ImmutableArray<SyntaxNode> GetDeclarators(SourceMethodSymbol method)
        {
            var builder = ArrayBuilder<SyntaxNode>.GetInstance();
            var visitor = new LocalVariableDeclaratorsCollector(builder);
            visitor.Visit(method.BodySyntax);
            return builder.ToImmutableAndFree();
        }

        public sealed override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            _builder.Add(node);
            base.VisitForEachStatement(node);
        }

        public sealed override void VisitLockStatement(LockStatementSyntax node)
        {
            _builder.Add(node);
            base.VisitLockStatement(node);
        }

        public sealed override void VisitUsingStatement(UsingStatementSyntax node)
        {
            if (node.Expression != null)
            {
                _builder.Add(node);
            }

            base.VisitUsingStatement(node);
        }

        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            _builder.Add(node);
            base.VisitSwitchStatement(node);
        }

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            _builder.Add(node);
            base.VisitIfStatement(node);
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            _builder.Add(node);
            base.VisitWhileStatement(node);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            _builder.Add(node);
            base.VisitDoStatement(node);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            if (node.Condition != null)
            {
                _builder.Add(node);
            }

            base.VisitForStatement(node);
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            _builder.Add(node);
            base.VisitVariableDeclarator(node);
        }

        public override void VisitCatchDeclaration(CatchDeclarationSyntax node)
        {
            _builder.Add(node);
            base.VisitCatchDeclaration(node);
        }
    }
}

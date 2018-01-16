// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class AwaitInAsyncMethodChecker : CSharpSyntaxWalker
    {
        internal static void Check(SyntaxNode syntax, Location location, DiagnosticBag diagnostics)
        {
            var awaitFinder = new AwaitInAsyncMethodChecker();

            switch (syntax.Kind())
            {
                case SyntaxKind.ParenthesizedLambdaExpression:
                    syntax = ((ParenthesizedLambdaExpressionSyntax)syntax).Body;
                    break;
                case SyntaxKind.SimpleLambdaExpression:
                    syntax = ((SimpleLambdaExpressionSyntax)syntax).Body;
                    break;
                case SyntaxKind.LocalFunctionStatement:
                    var local = (LocalFunctionStatementSyntax)syntax;
                    syntax = (SyntaxNode)local.Body ?? local.ExpressionBody;
                    break;
            }

            awaitFinder.Visit(syntax);
            if (!awaitFinder.FoundAwait)
            {
                diagnostics.Add(ErrorCode.WRN_AsyncLacksAwaits, location);
            }
        }

        public bool FoundAwait { get; private set; } = false;

        public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            // Any awaits found in a local function do not count towards the current method
        }

        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            // Any awaits found in a lambda do not count towards the current method
        }

        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            // Any awaits found in a lambda do not count towards the current method
        }

        public override void VisitAwaitExpression(AwaitExpressionSyntax node)
        {
            FoundAwait = true;
        }

        public override void VisitUsingStatement(UsingStatementSyntax node)
        {
            if (node.AwaitKeyword != default)
            {
                FoundAwait = true;
                return;
            }

            base.VisitUsingStatement(node);
        }
    }
}

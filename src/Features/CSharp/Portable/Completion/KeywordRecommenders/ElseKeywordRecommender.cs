// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class ElseKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public ElseKeywordRecommender()
            : base(SyntaxKind.ElseKeyword, isValidInPreprocessorContext: true)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.IsPreProcessorKeywordContext)
            {
                return true;
            }

            var token = context.TargetToken;

            var statement = token.GetAncestor<StatementSyntax>();
            var ifStatement = statement.GetAncestorOrThis<IfStatementSyntax>();

            if (statement == null || ifStatement == null)
            {
                return false;
            }

            // cases:
            //   if (foo)
            //     Console.WriteLine();
            //   |
            //   if (foo)
            //     Console.WriteLine();
            //   e|
            if (token.IsKind(SyntaxKind.SemicolonToken) && ifStatement.Statement.GetLastToken(includeSkipped: true) == token)
            {
                return true;
            }

            // if (foo) {
            //     Console.WriteLine();
            //   } |
            //   if (foo) {
            //     Console.WriteLine();
            //   } e|
            if (token.IsKind(SyntaxKind.CloseBraceToken) && ifStatement.Statement is BlockSyntax && token == ((BlockSyntax)ifStatement.Statement).CloseBraceToken)
            {
                return true;
            }

            return false;
        }
    }
}

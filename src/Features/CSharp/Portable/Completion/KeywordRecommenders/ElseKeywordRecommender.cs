// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
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

            // We have to consider all ancestor if statements of the last token until we find a match for this 'else':
            // while (true)
            //     if (true)
            //         while (true)
            //             if (true)
            //                 Console.WriteLine();
            //             else
            //                 Console.WriteLine();
            //     $$
            foreach (var ifStatement in token.GetAncestors<IfStatementSyntax>())
            {
                if (ifStatement.Statement.GetLastToken(includeSkipped: true, includeZeroWidth: true) == token)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

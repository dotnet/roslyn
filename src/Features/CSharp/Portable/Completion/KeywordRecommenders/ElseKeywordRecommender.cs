// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                // If there's a missing token at the end of the statement, it's incomplete and we do not offer 'else'.
                // context.TargetToken does not include zero width so in that case these will never be equal.
                if (ifStatement.Statement.GetLastToken(includeZeroWidth: true) == token)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

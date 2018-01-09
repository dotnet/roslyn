// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class WhenKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public WhenKeywordRecommender()
            : base(SyntaxKind.WhenKeyword, isValidInPreprocessorContext: true)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.IsCatchFilterContext)
            {
                return true;
            }

            // case int i $$
            if (IsIdentifierInCasePattern(context.LeftToken.Parent))
            {
                return true;
            }

            // case int i w$$
            if (IsIdentifierInCasePattern(context.LeftToken.GetPreviousToken().Parent))
            {
                return true;
            }

            return false;
        }

        private static bool IsIdentifierInCasePattern(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.SingleVariableDesignation)
                && node.Parent.IsKind(SyntaxKind.DeclarationPattern)
                && node.Parent.Parent.IsKind(SyntaxKind.CasePatternSwitchLabel);
        }
    }
}

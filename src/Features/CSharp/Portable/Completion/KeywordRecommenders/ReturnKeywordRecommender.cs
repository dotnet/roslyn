// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class ReturnKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public ReturnKeywordRecommender()
            : base(SyntaxKind.ReturnKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                context.IsStatementContext ||
                context.TargetToken.IsAfterYieldKeyword() ||
                IsAttributeContext(context, cancellationToken);
        }

        private static bool IsAttributeContext(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                context.IsMemberAttributeContext(SyntaxKindSet.ClassInterfaceStructTypeDeclarations, cancellationToken) ||
                (context.SyntaxTree.IsInteractiveOrScript() && context.IsTypeAttributeContext(cancellationToken));
        }
    }
}

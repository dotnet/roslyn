﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

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
                (context.SyntaxTree.IsScript() && context.IsTypeAttributeContext(cancellationToken));
        }
    }
}

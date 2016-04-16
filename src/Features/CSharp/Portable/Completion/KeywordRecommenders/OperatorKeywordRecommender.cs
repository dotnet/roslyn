// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class OperatorKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        private static readonly ISet<SyntaxKind> s_validMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
            {
                SyntaxKind.StaticKeyword,
                SyntaxKind.PublicKeyword,
                SyntaxKind.ExternKeyword,
            };

        public OperatorKeywordRecommender()
            : base(SyntaxKind.OperatorKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            // cases:
            //   public static implicit |
            //   public static explicit |
            var token = context.TargetToken;

            return
                token.Kind() == SyntaxKind.ImplicitKeyword ||
                token.Kind() == SyntaxKind.ExplicitKeyword;
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class FieldKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        // interfaces don't have members that you can put a [field:] attribute on
        private static readonly ISet<SyntaxKind> s_validTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.StructDeclaration,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.EnumDeclaration,
        };

        public FieldKeywordRecommender()
            : base(SyntaxKind.FieldKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return context.IsMemberAttributeContext(s_validTypeDeclarations, cancellationToken);
        }
    }
}

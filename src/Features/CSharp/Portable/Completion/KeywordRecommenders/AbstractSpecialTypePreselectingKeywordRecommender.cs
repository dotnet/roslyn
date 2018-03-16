// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal abstract class AbstractSpecialTypePreselectingKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public AbstractSpecialTypePreselectingKeywordRecommender(
            SyntaxKind keywordKind, 
            bool isValidInPreprocessorContext = false, 
            bool shouldFormatOnCommit = false) 
            : base(keywordKind, isValidInPreprocessorContext, shouldFormatOnCommit)
        {
        }

        protected abstract SpecialType SpecialType { get; }

        protected override bool ShouldPreselect(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return context.InferredTypes.Any(t => t.SpecialType == this.SpecialType);
        }
    }
}

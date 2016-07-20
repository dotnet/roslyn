using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

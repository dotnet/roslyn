// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class OutKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public OutKeywordRecommender()
            : base(SyntaxKind.OutKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;

            // TODO(cyrusn): lambda/anon methods can have out/ref parameters
            return
                context.TargetToken.IsTypeParameterVarianceContext() ||
                syntaxTree.IsParameterModifierContext(position, context.LeftToken, cancellationToken) ||
                syntaxTree.IsAnonymousMethodParameterModifierContext(position, context.LeftToken, cancellationToken) ||
                syntaxTree.IsPossibleLambdaParameterModifierContext(position, context.LeftToken, cancellationToken) ||
                context.TargetToken.IsConstructorOrMethodParameterArgumentContext() ||
                context.TargetToken.IsXmlCrefParameterModifierContext();
        }
    }
}

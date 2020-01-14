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
                IsOutParameterModifierContext(position, context) ||
                syntaxTree.IsAnonymousMethodParameterModifierContext(position, context.LeftToken) ||
                syntaxTree.IsPossibleLambdaParameterModifierContext(position, context.LeftToken) ||
                context.TargetToken.IsConstructorOrMethodParameterArgumentContext() ||
                context.TargetToken.IsXmlCrefParameterModifierContext();
        }

        private static bool IsOutParameterModifierContext(int position, CSharpSyntaxContext context)
        {
            return context.SyntaxTree.IsParameterModifierContext(
                       position, context.LeftToken, includeOperators: false, out _, out var previousModifier) &&
                   previousModifier == SyntaxKind.None;
        }
    }
}

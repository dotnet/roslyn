﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class UndefKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public UndefKeywordRecommender()
            : base(SyntaxKind.UndefKeyword, isValidInPreprocessorContext: true)
        {
        }

        protected override async Task<bool> IsValidContextAsync(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;
            return
                context.IsPreProcessorKeywordContext &&
                await syntaxTree.IsBeforeFirstTokenAsync(position, cancellationToken).ConfigureAwait(false);
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class ReplaceKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public ReplaceKeywordRecommender() :
            base(SyntaxKind.ReplaceKeyword, isValidInPreprocessorContext: false)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return InMemberDeclarationContext(position, context, cancellationToken);
        }

        private static bool InMemberDeclarationContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return context.IsGlobalStatementContext
                || context.SyntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken)
                || context.IsMemberDeclarationContext(
                    validModifiers: SyntaxKindSet.AllMemberModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassStructTypeDeclarations,
                    canBePartial: false,
                    cancellationToken: cancellationToken);
        }
    }
}

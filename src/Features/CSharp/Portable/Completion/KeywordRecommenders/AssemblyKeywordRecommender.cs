// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class AssemblyKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public AssemblyKeywordRecommender()
            : base(SyntaxKind.AssemblyKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var token = context.TargetToken;

            if (token.Kind() != SyntaxKind.OpenBracketToken)
            {
                return false;
            }

            if (token.Parent.Kind() == SyntaxKind.AttributeList)
            {
                var attributeList = token.Parent;
                var previousSyntax = attributeList.Parent;
                return previousSyntax is CompilationUnitSyntax || previousSyntax.Parent is CompilationUnitSyntax;
            }

            return true;
        }
    }
}

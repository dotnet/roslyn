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

            // Note that we pass the token.SpanStart to IsTypeDeclarationContext below. This is a bit subtle,
            // but we want to be sure that the attribute itself (i.e. the open square bracket, '[') is in a
            // type declaration context.
            if (token.Kind() != SyntaxKind.OpenBracketToken)
                return false;
            if (token.Parent.Kind() == SyntaxKind.AttributeList)
            {
                var previousSyntax = token.GetPreviousToken().Parent;
                return previousSyntax == null;
            }
            var nextSyntax = token.GetNextToken().Parent;
            var currentSyntax = token.Parent;
            return nextSyntax is NamespaceDeclarationSyntax || currentSyntax is CompilationUnitSyntax;
        }
    }
}

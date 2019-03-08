﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                var parentSyntax = attributeList.Parent;
                switch (parentSyntax)
                {
                    case CompilationUnitSyntax _:
                    // The case where the parent of attributeList is (Class/Interface/Enum/Struct)DeclarationSyntax, like:
                    // [$$
                    // class Goo {
                    // for these cases is necessary check if they Parent is CompilationUnitSyntax
                    case BaseTypeDeclarationSyntax baseType when baseType.Parent is CompilationUnitSyntax:
                    // The case where the parent of attributeList is IncompleteMemberSyntax(See test: ), like:
                    // [$$
                    // for that case is necessary check if they Parent is CompilationUnitSyntax
                    case IncompleteMemberSyntax incompleteMember when incompleteMember.Parent is CompilationUnitSyntax:
                        return true;
                    default:
                        return false;
                }
            }

            var skippedTokensTriviaSyntax = token.Parent;
            // This case happens when:
            // [$$
            // namespace Goo {
            return skippedTokensTriviaSyntax is SkippedTokensTriviaSyntax;
        }
    }
}

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

            if (token.Kind() == SyntaxKind.OpenBracketToken &&
                token.Parent.Kind() == SyntaxKind.AttributeList)
            {
                var attributeList = token.Parent;
                var parentSyntax = attributeList.Parent;
                switch (parentSyntax)
                {
                    case CompilationUnitSyntax _:
                    case NamespaceDeclarationSyntax _:
                    case BaseTypeDeclarationSyntax { Parent: CompilationUnitSyntax _ } baseType:
                    // The case where the parent of attributeList is IncompleteMemberSyntax(See test: ), like:
                    // [$$
                    // for that case is necessary check if they Parent is CompilationUnitSyntax
                    case IncompleteMemberSyntax incompleteMember when incompleteMember.Parent is CompilationUnitSyntax:
                        return true;
                }
            }

            return false;
        }
    }
}

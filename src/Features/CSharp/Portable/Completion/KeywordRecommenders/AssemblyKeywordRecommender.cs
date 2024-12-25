// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

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
            token.GetRequiredParent().Kind() == SyntaxKind.AttributeList)
        {
            var attributeList = token.GetRequiredParent();
            var parentSyntax = attributeList.Parent;
            switch (parentSyntax)
            {
                case CompilationUnitSyntax:
                case BaseNamespaceDeclarationSyntax:
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
            }
        }

        return false;
    }
}

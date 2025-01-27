// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class TypeVarKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    public TypeVarKeywordRecommender()
        : base(SyntaxKind.TypeVarKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        var token = context.TargetToken;

        if (token.Kind() == SyntaxKind.OpenBracketToken &&
            token.Parent.IsKind(SyntaxKind.AttributeList))
        {
            var typeParameters = token.GetAncestor<TypeParameterListSyntax>();
            var type = typeParameters.GetAncestorOrThis<TypeDeclarationSyntax>();

            if (type != null && type.TypeParameterList == typeParameters)
            {
                return true;
            }

            var @delegate = typeParameters.GetAncestorOrThis<DelegateDeclarationSyntax>();
            if (@delegate != null && @delegate.TypeParameterList == typeParameters)
            {
                return true;
            }

            var method = typeParameters.GetAncestorOrThis<MethodDeclarationSyntax>();
            if (method != null && method.TypeParameterList == typeParameters)
            {
                return true;
            }
        }

        return false;
    }
}

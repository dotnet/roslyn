// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class ModuleKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    public ModuleKeywordRecommender()
        : base(SyntaxKind.ModuleKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsTypeAttributeContext(cancellationToken))
        {
            var token = context.LeftToken;
            var type = token.GetAncestor<MemberDeclarationSyntax>();

            return type == null || type.IsParentKind(SyntaxKind.CompilationUnit);
        }

        return false;
    }
}

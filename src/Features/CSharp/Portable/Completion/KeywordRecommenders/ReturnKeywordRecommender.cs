// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class ReturnKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    public ReturnKeywordRecommender()
        : base(SyntaxKind.ReturnKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return
            context.IsStatementContext ||
            context.IsRegularTopLevelStatementsContext() ||
            context.TargetToken.IsAfterYieldKeyword() ||
            IsAttributeContext(context, cancellationToken);
    }

    private static bool IsAttributeContext(CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return
            context.IsMemberAttributeContext(SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations, cancellationToken) ||
            (context.SyntaxTree.IsScript() && context.IsTypeAttributeContext(cancellationToken)) ||
            context.IsStatementAttributeContext() ||
            IsAccessorAttributeContext();

        bool IsAccessorAttributeContext()
        {
            var token = context.TargetToken;
            return token.Kind() == SyntaxKind.OpenBracketToken &&
                token.Parent is AttributeListSyntax &&
                token.Parent.Parent is AccessorDeclarationSyntax;
        }
    }
}

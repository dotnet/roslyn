// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal sealed class MethodKeywordRecommender() : AbstractSyntacticSingleKeywordRecommender(SyntaxKind.MethodKeyword)
{
    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsMemberAttributeContext(SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations, cancellationToken))
            return true;

        var token = context.TargetToken;

        if (token.Kind() == SyntaxKind.OpenBracketToken)
        {
            return token.Parent is AttributeListSyntax
            {
                Parent: PropertyDeclarationSyntax
                    or EventDeclarationSyntax
                    or AccessorDeclarationSyntax
                    or LocalFunctionStatementSyntax
                    or TypeDeclarationSyntax(kind: SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration) { ParameterList: not null }
            };
        }

        return false;
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateEnumMember;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateEnumMember
{
    [ExportLanguageService(typeof(IGenerateEnumMemberService), LanguageNames.CSharp), Shared]
    internal partial class CSharpGenerateEnumMemberService :
        AbstractGenerateEnumMemberService<CSharpGenerateEnumMemberService, SimpleNameSyntax, ExpressionSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpGenerateEnumMemberService()
        {
        }

        protected override bool IsIdentifierNameGeneration(SyntaxNode node)
            => node is IdentifierNameSyntax;

        protected override bool TryInitializeIdentifierNameState(
            SemanticDocument document, SimpleNameSyntax identifierName, CancellationToken cancellationToken,
            out SyntaxToken identifierToken, out ExpressionSyntax simpleNameOrMemberAccessExpression)
        {
            identifierToken = identifierName.Identifier;
            if (identifierToken.ValueText != string.Empty &&
                !identifierName.IsVar)
            {
                simpleNameOrMemberAccessExpression = identifierName.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == identifierName
                    ? (ExpressionSyntax)memberAccess
                    : identifierName;

                // If we're being invoked, then don't offer this, offer generate method instead.
                // Note: we could offer to generate a field with a delegate type.  However, that's
                // very esoteric and probably not what most users want.
                if (simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.InvocationExpression) ||
                    simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.ObjectCreationExpression) ||
                    simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.GotoStatement) ||
                    simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.AliasQualifiedName))
                {
                    return false;
                }

                return true;
            }

            identifierToken = default;
            simpleNameOrMemberAccessExpression = null;
            return false;
        }
    }
}

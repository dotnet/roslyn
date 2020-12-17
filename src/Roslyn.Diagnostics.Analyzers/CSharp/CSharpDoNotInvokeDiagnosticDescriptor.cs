// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpDiagnosticDescriptorAccessAnalyzer : DiagnosticDescriptorAccessAnalyzer<SyntaxKind, MemberAccessExpressionSyntax>
    {
        protected override SyntaxKind SimpleMemberAccessExpressionKind => SyntaxKind.SimpleMemberAccessExpression;

        protected override SyntaxNode GetLeftOfMemberAccess(MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Expression;
        }

        protected override SyntaxNode GetRightOfMemberAccess(MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name;
        }

        protected override bool IsThisOrBaseOrMeOrMyBaseExpression(SyntaxNode node)
        {
            return node.Kind() switch
            {
                SyntaxKind.ThisExpression or SyntaxKind.BaseExpression => true,
                _ => false,
            };
        }
    }
}

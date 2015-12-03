// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.QualifyMemberAccess;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.QualifyMemberAccess
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpQualifyMemberAccessDiagnosticAnalyzer : QualifyMemberAccessDiagnosticAnalyzerBase<SyntaxKind>
    {
        private static readonly ImmutableArray<SyntaxKind> s_kindsOfInterest = ImmutableArray.Create(SyntaxKind.IdentifierName);

        protected override string GetLanguageName()
        {
            return LanguageNames.CSharp;
        }

        protected override ImmutableArray<SyntaxKind> GetSupportedSyntaxKinds()
        {
            return s_kindsOfInterest;
        }

        protected override bool IsCandidate(SyntaxNode node)
        {
            return (node.Parent as MemberAccessExpressionSyntax)?.Expression.Kind() != SyntaxKind.ThisExpression;
        }
    }
}

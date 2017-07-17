// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.QualifyMemberAccess;

namespace Microsoft.CodeAnalysis.CSharp.QualifyMemberAccess
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpQualifyMemberAccessDiagnosticAnalyzer : AbstractQualifyMemberAccessDiagnosticAnalyzer<SyntaxKind>
    {
        protected override string GetLanguageName()
            => LanguageNames.CSharp;

        protected override bool IsAlreadyQualifiedMemberAccess(SyntaxNode node)
            => node.IsKind(SyntaxKind.ThisExpression);

        // If the member is already qualified with `base.`, it cannot be further qualified.
        protected override bool CanMemberAccessBeQualified(SyntaxNode node)
            => !node.IsKind(SyntaxKind.BaseExpression);
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.QualifyMemberAccess;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.QualifyMemberAccess
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpQualifyMemberAccessDiagnosticAnalyzer : QualifyMemberAccessDiagnosticAnalyzerBase<SyntaxKind>
    {
        private static readonly ImmutableArray<SyntaxKind> s_kindsOfInterest = ImmutableArray.Create(SyntaxKind.IdentifierName);

        protected override ImmutableArray<SyntaxKind> GetSupportedSyntaxKinds()
        {
            return s_kindsOfInterest;
        }

        protected override bool IsAlreadyQualifiedMemberAccess(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.ThisExpression);
        }
    }
}

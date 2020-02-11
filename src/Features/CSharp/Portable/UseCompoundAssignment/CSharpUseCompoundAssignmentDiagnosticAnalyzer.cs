﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UseCompoundAssignment;

namespace Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseCompoundAssignmentDiagnosticAnalyzer
        : AbstractUseCompoundAssignmentDiagnosticAnalyzer<SyntaxKind, AssignmentExpressionSyntax, BinaryExpressionSyntax>
    {
        public CSharpUseCompoundAssignmentDiagnosticAnalyzer()
            : base(CSharpSyntaxFactsService.Instance, Utilities.Kinds)
        {
        }

        protected override SyntaxKind GetKind(int rawKind)
            => (SyntaxKind)rawKind;

        protected override SyntaxKind GetAnalysisKind()
            => SyntaxKind.SimpleAssignmentExpression;

        protected override bool IsSupported(SyntaxKind assignmentKind, ParseOptions options)
            => assignmentKind != SyntaxKind.CoalesceExpression ||
            ((CSharpParseOptions)options).LanguageVersion >= LanguageVersion.CSharp8;
    }
}

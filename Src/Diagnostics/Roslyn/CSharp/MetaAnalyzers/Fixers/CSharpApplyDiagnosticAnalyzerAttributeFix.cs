// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Diagnostics.CodeFixes;

namespace Roslyn.Diagnostics.Analyzers.CSharp.Reliability
{
    [ExportCodeFixProvider(RoslynDiagnosticIds.MissingDiagnosticAnalyzerAttributeRuleId, LanguageNames.CSharp), Shared]
    public sealed class CSharpApplyDiagnosticAnalyzerAttributeFix : ApplyDiagnosticAnalyzerAttributeFix
    {
        protected override SyntaxNode ParseExpression(string expression)
        {
            return SyntaxFactory.ParseExpression(expression).WithAdditionalAnnotations(Simplifier.Annotation);
        }
    }
}

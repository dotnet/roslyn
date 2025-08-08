// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.Fixers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpApplyDiagnosticAnalyzerAttributeFix)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    public sealed class CSharpApplyDiagnosticAnalyzerAttributeFix() : ApplyDiagnosticAnalyzerAttributeFix
    {
        protected override SyntaxNode ParseExpression(string expression)
        {
            return SyntaxFactory.ParseExpression(expression).WithAdditionalAnnotations(Simplifier.Annotation);
        }
    }
}

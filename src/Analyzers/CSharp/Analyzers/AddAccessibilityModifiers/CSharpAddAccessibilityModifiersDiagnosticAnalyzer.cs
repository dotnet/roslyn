// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.AddOrRemoveAccessibilityModifiers;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.CSharp.AddOrRemoveAccessibilityModifiers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpAddOrRemoveAccessibilityModifiersDiagnosticAnalyzer
    : AbstractAddOrRemoveAccessibilityModifiersDiagnosticAnalyzer<CompilationUnitSyntax>
{
    protected override IAccessibilityFacts AccessibilityFacts => CSharpAccessibilityFacts.Instance;
    protected override IAddOrRemoveAccessibilityModifiers AddOrRemoveAccessibilityModifiers => CSharpAddOrRemoveAccessibilityModifiers.Instance;

    protected override void ProcessCompilationUnit(
        SyntaxTreeAnalysisContext context,
        CodeStyleOption2<AccessibilityModifiersRequired> option, CompilationUnitSyntax compilationUnit)
    {
        ProcessMembers(context, option, compilationUnit.Members);
    }

    private void ProcessMembers(
        SyntaxTreeAnalysisContext context,
        CodeStyleOption2<AccessibilityModifiersRequired> option,
        SyntaxList<MemberDeclarationSyntax> members)
    {
        foreach (var memberDeclaration in members)
            ProcessMemberDeclaration(context, option, memberDeclaration);
    }

    private void ProcessMemberDeclaration(
        SyntaxTreeAnalysisContext context,
        CodeStyleOption2<AccessibilityModifiersRequired> option, MemberDeclarationSyntax member)
    {
        if (!context.ShouldAnalyzeSpan(member.Span))
            return;

        if (member is BaseNamespaceDeclarationSyntax namespaceDeclaration)
            ProcessMembers(context, option, namespaceDeclaration.Members);

        // If we have a class or struct, recurse inwards.
        if (member is TypeDeclarationSyntax typeDeclaration)
            ProcessMembers(context, option, typeDeclaration.Members);

        CheckMemberAndReportDiagnostic(context, option, member);
    }
}

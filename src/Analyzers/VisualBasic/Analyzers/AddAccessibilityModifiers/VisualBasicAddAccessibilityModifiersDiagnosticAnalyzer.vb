' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.AddOrRemoveAccessibilityModifiers
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddOrRemoveAccessibilityModifiers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicAddOrRemoveAccessibilityModifiersDiagnosticAnalyzer
        Inherits AbstractAddOrRemoveAccessibilityModifiersDiagnosticAnalyzer(Of CompilationUnitSyntax)

        Protected Overrides ReadOnly Property AccessibilityFacts As IAccessibilityFacts = VisualBasicAccessibilityFacts.Instance
        Protected Overrides ReadOnly Property AddOrRemoveAccessibilityModifiers As IAddOrRemoveAccessibilityModifiers = VisualBasicAddOrRemoveAccessibilityModifiers.Instance

        Protected Overrides Sub ProcessCompilationUnit(
                context As SyntaxTreeAnalysisContext,
                [option] As CodeStyleOption2(Of AccessibilityModifiersRequired),
                compilationUnit As CompilationUnitSyntax)

            ProcessMembers(context, [option], compilationUnit.Members)
        End Sub

        Private Sub ProcessMembers(
                context As SyntaxTreeAnalysisContext,
                [option] As CodeStyleOption2(Of AccessibilityModifiersRequired),
                members As SyntaxList(Of StatementSyntax))

            For Each member In members
                ProcessMember(context, [option], member)
            Next
        End Sub

        Private Sub ProcessMember(
                context As SyntaxTreeAnalysisContext,
                [option] As CodeStyleOption2(Of AccessibilityModifiersRequired),
                member As StatementSyntax)

            If Not context.ShouldAnalyzeSpan(member.Span) Then
                Return
            End If

            If member.Kind() = SyntaxKind.NamespaceBlock Then
                Dim namespaceBlock = DirectCast(member, NamespaceBlockSyntax)
                ProcessMembers(context, [option], namespaceBlock.Members)
            End If

            ' If we have a class or struct or module, recurse inwards.
            If member.IsKind(SyntaxKind.ClassBlock) OrElse
               member.IsKind(SyntaxKind.StructureBlock) OrElse
               member.IsKind(SyntaxKind.ModuleBlock) Then

                Dim typeBlock = DirectCast(member, TypeBlockSyntax)
                ProcessMembers(context, [option], typeBlock.Members)
            End If

            CheckMemberAndReportDiagnostic(context, [option], member)
        End Sub
    End Class
End Namespace

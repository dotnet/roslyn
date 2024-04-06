' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.AddAccessibilityModifiers
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.AddAccessibilityModifiers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicAddAccessibilityModifiersDiagnosticAnalyzer
        Inherits AbstractAddAccessibilityModifiersDiagnosticAnalyzer(Of CompilationUnitSyntax)

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

            Dim name As SyntaxToken = Nothing
            Dim modifiersAdded As Boolean = False
            If Not VisualBasicAddAccessibilityModifiers.Instance.ShouldUpdateAccessibilityModifier(VisualBasicAccessibilityFacts.Instance, member, [option].Value, name, modifiersAdded) Then
                Return
            End If

            ' Have an issue to flag, either add or remove. Report issue to user.
            Dim additionalLocations = ImmutableArray.Create(member.GetLocation())
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                name.GetLocation(),
                [option].Notification,
                context.Options,
                additionalLocations:=additionalLocations,
                If(modifiersAdded, ModifiersAddedProperties, Nothing)))
        End Sub
    End Class
End Namespace

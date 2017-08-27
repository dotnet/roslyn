﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.AddAccessibilityModifiers
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddAccessibilityModifiers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicAddAccessibilityModifiersDiagnosticAnalyzer
        Inherits AbstractAddAccessibilityModifiersDiagnosticAnalyzer(Of CompilationUnitSyntax)

        Protected Overrides Sub ProcessCompilationUnit(
                context As SyntaxTreeAnalysisContext, generator As SyntaxGenerator,
                [option] As CodeStyleOption(Of AccessibilityModifiersRequired), compilationUnit As CompilationUnitSyntax)

            ProcessMembers(context, generator, [option], compilationUnit.Members)
        End Sub

        Private Sub ProcessMembers(context As SyntaxTreeAnalysisContext, generator As SyntaxGenerator,
                                   [option] As CodeStyleOption(Of AccessibilityModifiersRequired), members As SyntaxList(Of StatementSyntax))
            For Each member In members
                ProcessMember(context, generator, [option], member)
            Next
        End Sub

        Private Sub ProcessMember(context As SyntaxTreeAnalysisContext, generator As SyntaxGenerator,
                              [option] As CodeStyleOption(Of AccessibilityModifiersRequired), member As StatementSyntax)


            If member.Kind() = SyntaxKind.NamespaceBlock Then
                Dim namespaceBlock = DirectCast(member, NamespaceBlockSyntax)
                ProcessMembers(context, generator, [option], namespaceBlock.Members)
            End If

            ' If we have a class or struct or module, recurse inwards.
            If member.IsKind(SyntaxKind.ClassBlock) OrElse
               member.IsKind(SyntaxKind.StructureBlock) OrElse
               member.IsKind(SyntaxKind.ModuleBlock) Then

                Dim typeBlock = DirectCast(member, TypeBlockSyntax)
                ProcessMembers(context, generator, [option], typeBlock.Members)
            End If

            ' Have to have a name to report the issue on.
            Dim name = member.GetNameToken()
            If name.Kind() = SyntaxKind.None Then
                Return
            End If

            ' Certain members never have accessibility. Don't bother reporting on them.
            If Not generator.CanHaveAccessibility(member) Then
                Return
            End If

            ' If they already have accessibility, no need to report anything.
            Dim Accessibility = generator.GetAccessibility(member)
            If Accessibility <> Accessibility.NotApplicable Then
                Return
            End If

            ' Missing accessibility.  Report issue to user.
            Dim additionalLocations = ImmutableArray.Create(member.GetLocation())
            context.ReportDiagnostic(Diagnostic.Create(
                CreateDescriptorWithSeverity([option].Notification.Value),
                name.GetLocation(),
                additionalLocations:=additionalLocations))
        End Sub
    End Class
End Namespace

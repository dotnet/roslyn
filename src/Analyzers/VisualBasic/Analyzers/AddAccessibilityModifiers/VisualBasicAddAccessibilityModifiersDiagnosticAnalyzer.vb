' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.AddAccessibilityModifiers
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.AddAccessibilityModifiers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicAddAccessibilityModifiersDiagnosticAnalyzer
        Inherits AbstractAddAccessibilityModifiersDiagnosticAnalyzer(Of CompilationUnitSyntax)

        Private Shared ReadOnly Property SyntaxFacts As VisualBasicSyntaxFacts = VisualBasicSyntaxFacts.Instance

        Protected Overrides Sub ProcessCompilationUnit(
                context As SyntaxTreeAnalysisContext,
                [option] As CodeStyleOption2(Of AccessibilityModifiersRequired), compilationUnit As CompilationUnitSyntax)

            ProcessMembers(context, [option], compilationUnit.Members)
        End Sub

        Private Sub ProcessMembers(context As SyntaxTreeAnalysisContext,
                                   [option] As CodeStyleOption2(Of AccessibilityModifiersRequired), members As SyntaxList(Of StatementSyntax))
            For Each member In members
                ProcessMember(context, [option], member)
            Next
        End Sub

        Private Sub ProcessMember(context As SyntaxTreeAnalysisContext,
                              [option] As CodeStyleOption2(Of AccessibilityModifiersRequired), member As StatementSyntax)

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

            ' Have to have a name to report the issue on.
            Dim name = member.GetNameToken()
            If name.Kind() = SyntaxKind.None Then
                Return
            End If

            ' Certain members never have accessibility. Don't bother reporting on them.
            If Not SyntaxFacts.CanHaveAccessibility(member) Then
                Return
            End If

            ' This analyzer bases all of its decisions on the accessibility
            Dim Accessibility = SyntaxFacts.GetAccessibility(member)

            ' Omit will flag any accesibility values that exist and are default
            ' The other options will remove or ignore accessibility
            Dim isOmit = [option].Value = AccessibilityModifiersRequired.OmitIfDefault

            If isOmit Then
                If Accessibility = Accessibility.NotApplicable Then
                    ' Accessibility modifier already missing.  nothing we need to do.
                    Return
                End If

                If Not MatchesDefaultAccessibility(Accessibility, member) Then
                    ' Explicit accessibility was different than the default accessibility.
                    ' We have to keep this here.
                    Return
                End If

            Else ' Require all, flag missing modidifers
                If Accessibility <> Accessibility.NotApplicable Then
                    Return
                End If
            End If

            ' Have an issue to flag, either add or remove. Report issue to user.
            Dim additionalLocations = ImmutableArray.Create(member.GetLocation())
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                name.GetLocation(),
                [option].Notification.Severity,
                additionalLocations:=additionalLocations,
                properties:=Nothing))
        End Sub

        Private Shared Function MatchesDefaultAccessibility(accessibility As Accessibility, member As StatementSyntax) As Boolean
            ' Top level items in a namespace or file
            If member.IsParentKind(SyntaxKind.CompilationUnit) OrElse
               member.IsParentKind(SyntaxKind.NamespaceBlock) Then
                ' default is Friend
                Return accessibility = Accessibility.Friend
            End If

            ' default for const and field in a class is private
            If member.IsParentKind(SyntaxKind.ClassBlock) OrElse
               member.IsParentKind(SyntaxKind.ModuleBlock) Then
                If member.IsKind(SyntaxKind.FieldDeclaration) Then
                    Return accessibility = Accessibility.Private
                End If
            End If

            ' Everything else has a default of public
            Return accessibility = Accessibility.Public
        End Function
    End Class
End Namespace

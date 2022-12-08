' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.AddAccessibilityModifiers
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddAccessibilityModifiers
    Friend Class VisualBasicAddAccessibilityModifiers
        Inherits AbstractAddAccessibilityModifiers(Of StatementSyntax)

        Public Shared ReadOnly Instance As New VisualBasicAddAccessibilityModifiers()

        Protected Sub New()
        End Sub

        Public Overrides Function ShouldUpdateAccessibilityModifier(
                accessibilityFacts As CodeAnalysis.LanguageService.IAccessibilityFacts,
                member As StatementSyntax,
                [option] As AccessibilityModifiersRequired,
                ByRef name As SyntaxToken,
                ByRef modifiedAdded As Boolean) As Boolean

            ' Have to have a name to report the issue on.
            name = member.GetNameToken()
            If name.Kind() = SyntaxKind.None Then
                Return False
            End If

            ' Certain members never have accessibility. Don't bother reporting on them.
            If Not accessibilityFacts.CanHaveAccessibility(member) Then
                Return False
            End If

            ' This analyzer bases all of its decisions on the accessibility
            Dim Accessibility = accessibilityFacts.GetAccessibility(member)

            ' Omit will flag any accesibility values that exist and are default
            ' The other options will remove or ignore accessibility
            Dim isOmit = [option] = AccessibilityModifiersRequired.OmitIfDefault
            modifiedAdded = Not isOmit

            If isOmit Then
                If Accessibility = Accessibility.NotApplicable Then
                    ' Accessibility modifier already missing.  nothing we need to do.
                    Return False
                End If

                If Not MatchesDefaultAccessibility(Accessibility, member) Then
                    ' Explicit accessibility was different than the default accessibility.
                    ' We have to keep this here.
                    Return False
                End If

            Else ' Require all, flag missing modidifers
                If Accessibility <> Accessibility.NotApplicable Then
                    Return False
                End If
            End If

            Return True
        End Function

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

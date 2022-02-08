' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.RemoveUnusedMembers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedMembers
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RemoveUnusedMembers), [Shared]>
    Friend Class VisualBasicRemoveUnusedMembersCodeFixProvider
        Inherits AbstractRemoveUnusedMembersCodeFixProvider(Of FieldDeclarationSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        ''' <summary>
        ''' This method adjusts the <paramref name="declarators"/> to remove based on whether or not all variable declarators
        ''' within a field declaration should be removed,
        ''' i.e. if all the fields declared within a field declaration are unused,
        ''' we can remove the entire field declaration instead of individual variable declarators.
        ''' </summary>
        Protected Overrides Sub AdjustAndAddAppropriateDeclaratorsToRemove(fieldDeclarators As HashSet(Of FieldDeclarationSyntax), declarators As HashSet(Of SyntaxNode))
            For Each variableDeclarator In fieldDeclarators.SelectMany(Function(f) f.Declarators)
                AdjustAndAddAppropriateDeclaratorsToRemove(
                    parentDeclaration:=variableDeclarator,
                    childDeclarators:=variableDeclarator.Names,
                    declarators:=declarators)
            Next

            For Each fieldDeclarator In fieldDeclarators
                AdjustAndAddAppropriateDeclaratorsToRemove(
                    parentDeclaration:=fieldDeclarator,
                    childDeclarators:=fieldDeclarator.Declarators,
                    declarators:=declarators)
            Next
        End Sub
    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.RemoveUnusedMembers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedMembers
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RemoveUnusedMembers), [Shared]>
    Friend Class VisualBasicRemoveUnusedMembersCodeFixProvider
        Inherits AbstractRemoveUnusedMembersCodeFixProvider(Of FieldDeclarationSyntax)

        Protected Overrides Sub AdjustDeclarators(fieldDeclarators As HashSet(Of FieldDeclarationSyntax), declarators As HashSet(Of SyntaxNode))
            For Each variableDeclarator In fieldDeclarators.SelectMany(Function(f) f.Declarators)
                AdjustChildDeclarators(
                    parentDeclaration:=variableDeclarator,
                    childDeclarators:=variableDeclarator.Names,
                    declarators:=declarators)
            Next

            For Each fieldDeclarator In fieldDeclarators
                AdjustChildDeclarators(
                    parentDeclaration:=fieldDeclarator,
                    childDeclarators:=fieldDeclarator.Declarators,
                    declarators:=declarators)
            Next
        End Sub
    End Class
End Namespace

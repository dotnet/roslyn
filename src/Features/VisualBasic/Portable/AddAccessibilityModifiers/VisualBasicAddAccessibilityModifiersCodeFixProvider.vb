' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.AddAccessibilityModifiers
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddAccessibilityModifiers
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicAddAccessibilityModifiersCodeFixProvider
        Inherits AbstractAddAccessibilityModifiersCodeFixProvider

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function MapToDeclarator(declaration As SyntaxNode) As SyntaxNode
            If TypeOf declaration Is FieldDeclarationSyntax Then
                Return DirectCast(declaration, FieldDeclarationSyntax).Declarators(0).Names(0)
            End If

            Return declaration
        End Function
    End Class
End Namespace

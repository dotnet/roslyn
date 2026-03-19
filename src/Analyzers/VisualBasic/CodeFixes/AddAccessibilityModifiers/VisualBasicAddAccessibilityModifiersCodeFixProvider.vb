' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.AddOrRemoveAccessibilityModifiers
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddOrRemoveAccessibilityModifiers
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddOrRemoveAccessibilityModifiers), [Shared]>
    Friend NotInheritable Class VisualBasicAddOrRemoveAccessibilityModifiersCodeFixProvider
        Inherits AbstractAddOrRemoveAccessibilityModifiersCodeFixProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
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

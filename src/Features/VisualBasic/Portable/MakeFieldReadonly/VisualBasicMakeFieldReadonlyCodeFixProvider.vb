' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.MakeFieldReadonly
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.MakeFieldReadonly
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.MakeFieldReadonly), [Shared]>
    Friend Class VisualBasicMakeFieldReadonlyCodeFixProvider
        Inherits AbstractMakeFieldReadonlyCodeFixProvider(Of FieldDeclarationSyntax, ModifiedIdentifierSyntax)

        Friend Overrides Function GetVariableDeclaratorCount(declaration As FieldDeclarationSyntax) As Integer
            Return declaration.Declarators.Count
        End Function

        Friend Overrides Function GetInitializerNode(declaration As ModifiedIdentifierSyntax) As SyntaxNode
            Return CType(declaration.Parent, VariableDeclaratorSyntax).Initializer.Value
        End Function
    End Class
End Namespace

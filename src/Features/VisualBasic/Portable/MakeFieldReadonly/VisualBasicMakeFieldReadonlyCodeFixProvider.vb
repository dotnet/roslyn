' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.MakeFieldReadonly
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.MakeFieldReadonly
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.MakeFieldReadonly), [Shared]>
    Friend Class VisualBasicMakeFieldReadonlyCodeFixProvider
        Inherits AbstractMakeFieldReadonlyCodeFixProvider(Of ModifiedIdentifierSyntax, FieldDeclarationSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function GetVariableDeclarators(declaration As FieldDeclarationSyntax) As ImmutableList(Of ModifiedIdentifierSyntax)
            Return declaration.Declarators.SelectMany(Function(d) d.Names).ToImmutableListOrEmpty()
        End Function

        Protected Overrides Function GetInitializerNode(declaration As ModifiedIdentifierSyntax) As SyntaxNode
            Dim initializer = CType(declaration.Parent, VariableDeclaratorSyntax).Initializer?.Value
            If initializer Is Nothing Then
                initializer = TryCast(CType(declaration.Parent, VariableDeclaratorSyntax).AsClause, AsNewClauseSyntax)?.NewExpression
            End If

            Return initializer
        End Function
    End Class
End Namespace

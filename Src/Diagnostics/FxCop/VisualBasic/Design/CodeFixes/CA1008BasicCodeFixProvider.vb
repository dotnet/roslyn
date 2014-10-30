' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Design
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Design
    ' <summary>
    ' CA1008: Enums should have zero value
    ' </summary>
    <ExportCodeFixProvider("CA1008", LanguageNames.VisualBasic), [Shared]>
    Public Class CA1008BasicCodeFixProvider
        Inherits CA1008CodeFixProviderBase

        Friend Overrides Function GetFieldInitializer(field As IFieldSymbol) As SyntaxNode
            If field.DeclaringSyntaxReferences.Length = 0 Then
                Return Nothing
            End If

            Dim syntax = field.DeclaringSyntaxReferences.First().GetSyntax()
            Dim enumMemberSyntax = TryCast(syntax, EnumMemberDeclarationSyntax)
            Return If(enumMemberSyntax Is Nothing, Nothing, enumMemberSyntax.Initializer)
        End Function

        Friend Overrides Function CreateConstantValueInitializer(constantValueExpression As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.EqualsValue(DirectCast(constantValueExpression, ExpressionSyntax))
        End Function

        Protected Overrides Function GetParentNodeOrSelfToFix(nodeToFix As SyntaxNode) As SyntaxNode
            If nodeToFix.IsKind(SyntaxKind.EnumStatement) And nodeToFix.Parent IsNot Nothing Then
                Return nodeToFix.Parent
            End If

            Return nodeToFix
        End Function
    End Class
End Namespace
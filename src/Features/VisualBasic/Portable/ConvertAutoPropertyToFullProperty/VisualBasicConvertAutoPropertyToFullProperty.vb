﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertAutoPropertyToFullProperty
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertAutoPropertyToFullProperty
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicConvertAutoPropertyToFullPropertyCodeRefactoringProvider)), [Shared]>
    Friend Class VisualBasicConvertAutoPropertyToFullPropertyCodeRefactoringProvider
        Inherits AbstractConvertAutoPropertyToFullPropertyCodeRefactoringProvider(Of PropertyStatementSyntax, TypeBlockSyntax)

        Private Const Underscore As String = "_"

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        ''' <summary>
        ''' In VB, auto properties have an implicit backing field that is named using the property 
        ''' name preceded by an underscore. We will use this as the field name so we don't mess up 
        ''' any existing references to this field.
        ''' </summary>
        Friend Overrides Function GetFieldNameAsync(document As Document, propertySymbol As IPropertySymbol, cancellationToken As CancellationToken) As Task(Of String)
            Return Task.FromResult(Underscore + propertySymbol.Name)
        End Function

        Friend Overrides Function GetNewAccessors(options As DocumentOptionSet, propertyNode As SyntaxNode,
            fieldName As String, generator As SyntaxGenerator) _
            As (newGetAccessor As SyntaxNode, newSetAccessor As SyntaxNode)

            Dim returnStatement = New SyntaxList(Of StatementSyntax)(DirectCast(generator.ReturnStatement(
                generator.IdentifierName(fieldName)), StatementSyntax))
            Dim getAccessor As SyntaxNode = SyntaxFactory.GetAccessorBlock(
                SyntaxFactory.GetAccessorStatement(),
                returnStatement)

            Dim propertySyntax = DirectCast(propertyNode, PropertyStatementSyntax)

            Dim setAccessor As SyntaxNode
            If IsReadOnly(propertySyntax) Then
                setAccessor = Nothing
            Else
                Dim setStatement = New SyntaxList(Of StatementSyntax)(DirectCast(generator.ExpressionStatement(
                    generator.AssignmentStatement(generator.IdentifierName(fieldName),
                    generator.IdentifierName("Value"))), StatementSyntax))
                setAccessor = SyntaxFactory.SetAccessorBlock(
                    SyntaxFactory.SetAccessorStatement(),
                    setStatement)
            End If

            Return (getAccessor, setAccessor)
        End Function

        Private Shared Function IsReadOnly(propertySyntax As PropertyStatementSyntax) As Boolean
            Dim modifiers = propertySyntax.GetModifiers()
            For Each modifier In modifiers
                If modifier.IsKind(SyntaxKind.ReadOnlyKeyword) Then
                    Return True
                End If
            Next

            Return False
        End Function

        Friend Overrides Function GetPropertyWithoutInitializer(propertyNode As SyntaxNode) As SyntaxNode
            Return DirectCast(propertyNode, PropertyStatementSyntax).WithInitializer(Nothing)
        End Function

        Friend Overrides Function GetInitializerValue(propertyNode As SyntaxNode) As SyntaxNode
            Return DirectCast(propertyNode, PropertyStatementSyntax).Initializer?.Value
        End Function

        Friend Overrides Function ConvertPropertyToExpressionBodyIfDesired(options As DocumentOptionSet, propertyNode As SyntaxNode) As SyntaxNode
            Return propertyNode
        End Function

        Friend Overrides Function GetTypeBlock(syntaxNode As SyntaxNode) As SyntaxNode
            Return DirectCast(syntaxNode, TypeStatementSyntax).Parent
        End Function
    End Class
End Namespace

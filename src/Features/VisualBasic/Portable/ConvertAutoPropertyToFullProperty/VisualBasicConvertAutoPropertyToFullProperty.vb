' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.ConvertAutoPropertyToFullProperty
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertAutoPropertyToFullProperty
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ConvertAutoPropertyToFullProperty), [Shared]>
    Friend Class VisualBasicConvertAutoPropertyToFullPropertyCodeRefactoringProvider
        Inherits AbstractConvertAutoPropertyToFullPropertyCodeRefactoringProvider(Of PropertyStatementSyntax, TypeBlockSyntax, VisualBasicCodeGenerationContextInfo)

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
        Protected Overrides Function GetFieldNameAsync(document As Document, propertySymbol As IPropertySymbol, fallbackOptions As NamingStylePreferencesProvider, cancellationToken As CancellationToken) As Task(Of String)
            Return Task.FromResult(Underscore + propertySymbol.Name)
        End Function

        Protected Overrides Function GetNewAccessors(
                info As VisualBasicCodeGenerationContextInfo,
                propertyNode As SyntaxNode,
                fieldName As String,
                generator As SyntaxGenerator,
                cancellationToken As CancellationToken) As (newGetAccessor As SyntaxNode, newSetAccessor As SyntaxNode)

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

        Protected Overrides Function GetPropertyWithoutInitializer(propertyNode As SyntaxNode) As SyntaxNode
            Return DirectCast(propertyNode, PropertyStatementSyntax).WithInitializer(Nothing)
        End Function

        Protected Overrides Function GetInitializerValue(propertyNode As SyntaxNode) As SyntaxNode
            Return DirectCast(propertyNode, PropertyStatementSyntax).Initializer?.Value
        End Function

        Protected Overrides Function ConvertPropertyToExpressionBodyIfDesired(info As VisualBasicCodeGenerationContextInfo, propertyNode As SyntaxNode) As SyntaxNode
            Return propertyNode
        End Function

        Protected Overrides Function GetTypeBlock(syntaxNode As SyntaxNode) As SyntaxNode
            Return DirectCast(syntaxNode, TypeStatementSyntax).Parent
        End Function
    End Class
End Namespace

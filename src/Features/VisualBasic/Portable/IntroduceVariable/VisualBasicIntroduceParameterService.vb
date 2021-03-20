' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.IntroduceVariable
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicIntroduceParameterService
        Inherits AbstractIntroduceParameterService(Of VisualBasicIntroduceParameterService, ExpressionSyntax, InvocationExpressionSyntax, IdentifierNameSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function AddArgumentToArgumentList(invocationArguments As SeparatedSyntaxList(Of SyntaxNode),
                                                               newArgumentExpression As SyntaxNode,
                                                               insertionIndex As Integer,
                                                               name As String,
                                                               named As Boolean) As SeparatedSyntaxList(Of SyntaxNode)
            Dim argument As ArgumentSyntax
            If named Then
                Dim identifierName = SyntaxFactory.IdentifierName(name)
                Dim nameColon = SyntaxFactory.NameColonEquals(identifierName)
                argument = SyntaxFactory.SimpleArgument(nameColon, DirectCast(newArgumentExpression.WithAdditionalAnnotations(Simplifier.Annotation), ExpressionSyntax))
            Else
                argument = SyntaxFactory.SimpleArgument(DirectCast(newArgumentExpression.WithAdditionalAnnotations(Simplifier.Annotation), ExpressionSyntax))
            End If

            Return invocationArguments.Insert(insertionIndex, argument)
        End Function

        Protected Overrides Function GenerateExpressionFromOptionalParameter(parameterSymbol As IParameterSymbol) As SyntaxNode
            Return GenerateExpression(parameterSymbol.Type, parameterSymbol.ExplicitDefaultValue, canUseFieldReference:=True)
        End Function

        Protected Overrides Function AddExpressionArgumentToArgumentList(arguments As ImmutableArray(Of SyntaxNode), expression As SyntaxNode) As ImmutableArray(Of SyntaxNode)
            Dim newArgument = SyntaxFactory.SimpleArgument(DirectCast(expression, ExpressionSyntax))
            Return arguments.Add(newArgument)
        End Function

        Protected Overrides Function IsMethodDeclaration(node As SyntaxNode) As Boolean
            Select Case node.Kind()
                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.FunctionLambdaHeader,
                     SyntaxKind.SubLambdaHeader
                    Return True
            End Select

            Return False
        End Function

        Protected Overrides Function GetParameterList(document As SemanticDocument, parameterList As SyntaxNode, cancellationToken As CancellationToken) As List(Of IParameterSymbol)
            Dim semanticModel = document.SemanticModel
            Dim parameterSyntaxList = DirectCast(parameterList, ParameterListSyntax).Parameters
            Dim parameterSymbolList = New List(Of IParameterSymbol)

            For Each parameter In parameterSyntaxList
                Dim symbolInfo = semanticModel.GetDeclaredSymbol(parameter, cancellationToken)
                parameterSymbolList.Add(symbolInfo)
            Next

            Return parameterSymbolList
        End Function
    End Class
End Namespace

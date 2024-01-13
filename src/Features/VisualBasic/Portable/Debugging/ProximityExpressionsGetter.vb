' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Debugging
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Debugging
    <ExportLanguageService(GetType(IProximityExpressionsService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicProximityExpressionsService
        Implements IProximityExpressionsService

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Async Function GetProximityExpressionsAsync(
            document As Document,
            position As Integer,
            cancellationToken As CancellationToken) As Task(Of IList(Of String)) Implements IProximityExpressionsService.GetProximityExpressionsAsync

            Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Return [Do](tree, position, cancellationToken)
        End Function

        ' Internal for testing purposes
        Friend Shared Function [Do](syntaxTree As SyntaxTree,
                                    position As Integer) As IList(Of String)
            Return [Do](syntaxTree, position, Nothing)
        End Function

        Private Shared Function [Do](syntaxTree As SyntaxTree,
                                    position As Integer,
                                    cancellationToken As CancellationToken) As IList(Of String)
            Dim worker = New Worker(syntaxTree, position)
            Return worker.Do(cancellationToken)
        End Function

        Private Shared Sub AddRelevantExpressions(
            statement As StatementSyntax,
            expressions As IList(Of ExpressionSyntax),
            includeDeclarations As Boolean)

            Dim collector As New RelevantExpressionsCollector(includeDeclarations, expressions)
            collector.Visit(statement)
        End Sub

        Public Async Function IsValidAsync(
            document As Document,
            position As Integer,
            expressionValue As String,
            cancellationToken As CancellationToken) As Task(Of Boolean) Implements IProximityExpressionsService.IsValidAsync

            Dim expression = SyntaxFactory.ParseExpression(expressionValue)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim token = root.FindToken(position)

            ' The debugger stops on "End Sub" and we want to see locals and/or parameters at that point, 
            ' so we will have to diverge from the compiler model of considering those tokens outside the
            ' scope of the method.
            Dim parentEndBlock = TryCast(token.Parent, EndBlockStatementSyntax)
            If parentEndBlock IsNot Nothing Then
                token = parentEndBlock.EndKeyword.GetPreviousToken()
            End If

            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim info = semanticModel.GetSpeculativeSymbolInfo(token.SpanStart, expression, SpeculativeBindingOption.BindAsExpression)
            If info.Symbol Is Nothing Then
                Return False
            End If

            ' We seem to have bound successfully.  However, if it bound to a local, then make
            ' sure that that local isn't after the statement that we're currently looking at.  
            If info.Symbol.Kind = SymbolKind.Local Then
                Dim statement = info.Symbol.Locations.First().FindToken(cancellationToken).GetAncestor(Of StatementSyntax)()
                If statement IsNot Nothing AndAlso position < statement.SpanStart Then
                    Return False
                End If
            End If

            Return True
        End Function
    End Class
End Namespace

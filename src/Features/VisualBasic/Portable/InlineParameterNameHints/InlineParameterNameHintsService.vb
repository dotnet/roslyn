' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.InlineParameterNameHints
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InlineParameterNameHints

    <ExportLanguageService(GetType(IInlineParameterNameHintsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class InlineParameterNameHintsService
        Implements IInlineParameterNameHintsService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Async Function GetInlineParameterNameHintsAsync(document As Document, textSpan As TextSpan, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of InlineParameterHint)) Implements IInlineParameterNameHintsService.GetInlineParameterNameHintsAsync
            Dim tree = Await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim root = Await tree.GetRootAsync(cancellationToken).ConfigureAwait(False)
            Dim spans = New List(Of InlineParameterHint)

            Dim semanticModel = Await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim nodes = root.DescendantNodes()

            For Each node In nodes
                If TypeOf node Is SimpleArgumentSyntax Then
                    Dim simpleArgument = TryCast(node, SimpleArgumentSyntax)
                    If Not simpleArgument.IsNamed AndAlso simpleArgument.NameColonEquals Is Nothing AndAlso IsExpressionWithNoName(simpleArgument.Expression) Then
                        Dim param = simpleArgument.DetermineParameter(semanticModel, False, cancellationToken)
                        If param IsNot Nothing AndAlso param.Name.Length > 0 Then
                            spans.Add(New InlineParameterHint(param.Name, simpleArgument.Span.Start))
                        End If
                    End If
                End If
            Next
            Return spans
            'Throw New NotImplementedException()
        End Function

        Private Function IsExpressionWithNoName(arg As ExpressionSyntax) As Boolean
            If TypeOf arg Is LiteralExpressionSyntax Then
                'We want to adorn literals no matter what
                Return True
            End If

            If TypeOf arg Is ObjectCreationExpressionSyntax Then
                ' We want to adorn object invocations that exist as arguments because they are Not declared anywhere
                ' else in the file
                ' Example: testMethod(^ New Object()); should show the adornment at the caret  
                Return True
            End If

            If TypeOf arg Is PredefinedCastExpressionSyntax Then
                Dim cast = TryCast(arg, PredefinedCastExpressionSyntax)
                ' Recurse until we find a literal
                ' If so, then we should add the adornment
                Return IsExpressionWithNoName(cast.Expression)
            End If

            If TypeOf arg Is UnaryExpressionSyntax Then
                Dim negation = TryCast(arg, UnaryExpressionSyntax)
                ' Recurse until we find a literal
                ' If so, then we should add the adornment
                Return IsExpressionWithNoName(negation.Operand)
            End If

            Return False

        End Function
    End Class
End Namespace

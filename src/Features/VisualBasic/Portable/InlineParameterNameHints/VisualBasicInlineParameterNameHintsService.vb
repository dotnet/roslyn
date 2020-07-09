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
    Friend Class VisualBasicInlineParameterNameHintsService
        Inherits AbstractInlineParameterNameHintsService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function AddAllParameterNameHintLocations(semanticModel As SemanticModel, nodes As IEnumerable(Of SyntaxNode), cancellationToken As CancellationToken) As IEnumerable(Of InlineParameterHint)
            Dim spans = New List(Of InlineParameterHint)
            For Each node In nodes
                cancellationToken.ThrowIfCancellationRequested()
                Dim simpleArgument = TryCast(node, SimpleArgumentSyntax)
                If Not simpleArgument Is Nothing Then
                    If Not simpleArgument.IsNamed AndAlso simpleArgument.NameColonEquals Is Nothing AndAlso IsExpressionWithNoName(simpleArgument.Expression) Then
                        Dim param = simpleArgument.DetermineParameter(semanticModel, allowParamArray:=False, cancellationToken)
                        If param IsNot Nothing AndAlso param.Name.Length > 0 Then
                            spans.Add(New InlineParameterHint(param.Name, simpleArgument.Span.Start))
                        End If
                    End If
                End If
            Next

            Return spans
        End Function

        Private Function IsExpressionWithNoName(arg As ExpressionSyntax) As Boolean
            If TypeOf arg Is LiteralExpressionSyntax Then
                ' We want to adorn literals no matter what
                Return True
            End If

            If TypeOf arg Is ObjectCreationExpressionSyntax Then
                ' We want to adorn object invocations that exist as arguments because they are Not declared anywhere
                ' else in the file
                ' Example: testMethod(^ New Object()); should show the adornment at the caret  
                Return True
            End If

            If TypeOf arg Is PredefinedCastExpressionSyntax Then
                Dim cast = DirectCast(arg, PredefinedCastExpressionSyntax)
                ' Recurse until we find a literal
                ' If so, then we should add the adornment
                Return IsExpressionWithNoName(cast.Expression)
            End If

            If TypeOf arg Is TryCastExpressionSyntax Then
                Dim cast = DirectCast(arg, TryCastExpressionSyntax)
                ' Recurse until we find a literal
                ' If so, then we should add the adornment
                Return IsExpressionWithNoName(cast.Expression)
            End If

            If TypeOf arg Is CTypeExpressionSyntax Then
                Dim cast = DirectCast(arg, CTypeExpressionSyntax)
                ' Recurse until we find a literal
                ' If so, then we should add the adornment
                Return IsExpressionWithNoName(cast.Expression)
            End If

            If TypeOf arg Is DirectCastExpressionSyntax Then
                Dim cast = DirectCast(arg, DirectCastExpressionSyntax)
                ' Recurse until we find a literal
                ' If so, then we should add the adornment
                Return IsExpressionWithNoName(cast.Expression)
            End If

            If TypeOf arg Is UnaryExpressionSyntax Then
                Dim negation = DirectCast(arg, UnaryExpressionSyntax)
                ' Recurse until we find a literal
                ' If so, then we should add the adornment
                Return IsExpressionWithNoName(negation.Operand)
            End If

            Return False
        End Function
    End Class
End Namespace

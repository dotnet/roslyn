' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.InlineHints
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InlineParameterNameHints
    <ExportLanguageService(GetType(IInlineParameterNameHintsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicInlineParameterNameHintsService
        Inherits AbstractInlineParameterNameHintsService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Sub AddAllParameterNameHintLocations(
                semanticModel As SemanticModel,
                node As SyntaxNode,
                addHint As Action(Of InlineParameterHint),
                cancellationToken As CancellationToken)

            Dim simpleArgument = TryCast(node, SimpleArgumentSyntax)
            If simpleArgument?.Expression IsNot Nothing Then
                If Not simpleArgument.IsNamed AndAlso simpleArgument.NameColonEquals Is Nothing Then
                    Dim param = simpleArgument.DetermineParameter(semanticModel, allowParamArray:=False, cancellationToken)
                    If Not String.IsNullOrEmpty(param?.Name) Then
                        addHint(New InlineParameterHint(param.GetSymbolKey(cancellationToken), param.Name, simpleArgument.Span.Start, GetKind(simpleArgument.Expression)))
                    End If
                End If
            End If
        End Sub

        Private Function GetKind(arg As ExpressionSyntax) As InlineParameterHintKind
            If TypeOf arg Is LiteralExpressionSyntax OrElse
               TypeOf arg Is InterpolatedStringExpressionSyntax Then
                Return InlineParameterHintKind.Literal
            End If

            If TypeOf arg Is ObjectCreationExpressionSyntax Then
                Return InlineParameterHintKind.ObjectCreation
            End If

            Dim predefinedCast = TryCast(arg, PredefinedCastExpressionSyntax)
            If predefinedCast IsNot Nothing Then
                ' Recurse until we find a literal
                ' If so, then we should add the adornment
                Return GetKind(predefinedCast.Expression)
            End If

            Dim cast = TryCast(arg, CastExpressionSyntax)
            If cast IsNot Nothing Then
                ' Recurse until we find a literal
                ' If so, then we should add the adornment
                Return GetKind(cast.Expression)
            End If

            Dim unary = TryCast(arg, UnaryExpressionSyntax)
            If unary IsNot Nothing Then
                ' Recurse until we find a literal
                ' If so, then we should add the adornment
                Return GetKind(unary.Operand)
            End If

            Return InlineParameterHintKind.Other
        End Function
    End Class
End Namespace

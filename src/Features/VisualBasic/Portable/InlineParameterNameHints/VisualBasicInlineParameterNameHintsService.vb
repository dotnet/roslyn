' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.InlineHints
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
                hideForParametersThatDifferBySuffix As Boolean,
                hideForParametersThatMatchMethodIntent As Boolean,
                CancellationToken As CancellationToken)

            Dim argument = TryCast(node, SimpleArgumentSyntax)
            If argument?.Expression Is Nothing Then
                Return
            End If

            If argument.IsNamed OrElse argument.NameColonEquals IsNot Nothing Then
                Return
            End If

            Dim parameter = argument.DetermineParameter(semanticModel, allowParamArray:=False, CancellationToken)
            If String.IsNullOrEmpty(parameter?.Name) Then
                Return
            End If

            If hideForParametersThatMatchMethodIntent AndAlso MatchesMethodIntent(argument, parameter) Then
                Return
            End If

            addHint(New InlineParameterHint(parameter.GetSymbolKey(CancellationToken), parameter.Name, argument.Span.Start, GetKind(argument.Expression)))
        End Sub

        Private Overloads Shared Function MatchesMethodIntent(argument As ArgumentSyntax, parameter As IParameterSymbol) As Boolean
            ' Methods Like `SetColor(color: "y")` `FromResult(result: "x")` `Enable/DisablePolling(bool)` don't need
            ' parameter names to improve clarity.  The parameter Is clear from the context of the method name.
            Dim argumentList = TryCast(argument.Parent, ArgumentListSyntax)
            If argumentList Is Nothing Then
                Return False
            End If

            If argumentList.Arguments(0) IsNot argument Then
                Return False
            End If

            Dim invocationExpression = TryCast(argumentList.Parent, InvocationExpressionSyntax)
            If invocationExpression Is Nothing Then
                Return False
            End If

            Dim rightMostName = invocationExpression.Expression.GetRightmostName()
            If rightMostName Is Nothing Then
                Return False
            End If

            Return AbstractInlineParameterNameHintsService.MatchesMethodIntent(rightMostName.Identifier.ValueText, parameter)
        End Function

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

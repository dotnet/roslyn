' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.InlineHints
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InlineHints
    <ExportLanguageService(GetType(IInlineParameterNameHintsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicInlineParameterNameHintsService
        Inherits AbstractInlineParameterNameHintsService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Sub AddAllParameterNameHintLocations(
                semanticModel As SemanticModel,
                syntaxFacts As ISyntaxFactsService,
                node As SyntaxNode,
                buffer As ArrayBuilder(Of (position As Integer, argument As SyntaxNode, parameter As IParameterSymbol, kind As HintKind)),
                cancellationToken As CancellationToken)

            Dim argumentList = TryCast(node, ArgumentListSyntax)
            If argumentList Is Nothing Then
                Return
            End If

            For Each arg In argumentList.Arguments
                Dim argument = TryCast(arg, SimpleArgumentSyntax)
                If argument Is Nothing Then
                    Continue For
                End If

                If argument?.Expression Is Nothing Then
                    Continue For
                End If

                If argument.IsNamed OrElse argument.NameColonEquals IsNot Nothing Then
                    Continue For
                End If

                Dim parameter = argument.DetermineParameter(semanticModel, allowParamArray:=False, cancellationToken:=cancellationToken)
                If String.IsNullOrEmpty(parameter?.Name) Then
                    Continue For
                End If

                Dim argumentIdentifier = GetIdentifierNameFromArgument(argument, syntaxFacts)
                buffer.Add((argument.Span.Start, argument, parameter, GetKind(argument.Expression)))
            Next
        End Sub

        Private Shared Function GetKind(arg As ExpressionSyntax) As HintKind
            If TypeOf arg Is LiteralExpressionSyntax OrElse
               TypeOf arg Is InterpolatedStringExpressionSyntax Then
                Return HintKind.Literal
            End If

            If TypeOf arg Is ObjectCreationExpressionSyntax Then
                Return HintKind.ObjectCreation
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

            Return HintKind.Other
        End Function

        Protected Overrides Function IsIndexer(node As SyntaxNode, parameter As IParameterSymbol) As Boolean
            Dim propertySymbol = TryCast(parameter.ContainingSymbol, IPropertySymbol)
            Return propertySymbol IsNot Nothing AndAlso propertySymbol.IsDefault
        End Function

        Protected Overrides Function GetReplacementText(parameterName As String) As String
            Return parameterName & ":="
        End Function
    End Class
End Namespace

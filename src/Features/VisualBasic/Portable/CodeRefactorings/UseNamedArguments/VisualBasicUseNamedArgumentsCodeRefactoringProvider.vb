' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.UseNamedArguments
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.UseNamedArguments
    <ExtensionOrder(After:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicUseNamedArgumentsCodeRefactoringProvider)), [Shared]>
    Friend Class VisualBasicUseNamedArgumentsCodeRefactoringProvider
        Inherits AbstractUseNamedArgumentsCodeRefactoringProvider(Of ArgumentSyntax)

        Protected Overrides Function TryGetOrSynthesizeNamedArguments(arguments As SeparatedSyntaxList(Of ArgumentSyntax), parameters As ImmutableArray(Of IParameterSymbol), ByRef namedArguments As ArgumentSyntax(), ByRef hasLiteral As Boolean) As Boolean
            Dim result = ArrayBuilder(Of ArgumentSyntax).GetInstance(arguments.Count)
            Try
                ' An argument list with omitted arguments may or may not be already fixed
                Dim alreadyFixed = True
                For index = 0 To arguments.Count - 1
                    Dim argument = arguments(index)
                    Dim parameter = parameters(index)
                    If argument.IsNamed Then
                        result.Add(argument)
                    ElseIf argument.IsOmitted Then
                        Continue For
                    ElseIf parameter.IsParams Then
                        ' Named arguments cannot match a ParamArray parameter
                        Return False
                    Else
                        alreadyFixed = False
                        If Not hasLiteral AndAlso IsLiteral(argument) Then
                            hasLiteral = True
                        End If
                        Dim simpleArgument = DirectCast(argument, SimpleArgumentSyntax)
                        result.Add(simpleArgument.WithNameColonEquals(NameColonEquals(IdentifierName(parameter.Name))).WithTriviaFrom(simpleArgument.Expression))
                    End If
                Next
                If alreadyFixed Then
                    Return False
                Else
                    namedArguments = result.ToArray()
                    Return True
                End If
            Finally
                result.Free()
            End Try
        End Function

        Protected Overrides Function IsCandidate(node As SyntaxNode) As Boolean
            Select Case node.Kind()
                Case SyntaxKind.InvocationExpression
                    Dim invocation = DirectCast(node, InvocationExpressionSyntax)
                    Return invocation.Expression IsNot Nothing

                Case SyntaxKind.ConditionalAccessExpression
                    Dim conditional = DirectCast(node, ConditionalAccessExpressionSyntax)
                    Dim invocation = TryCast(conditional.WhenNotNull, InvocationExpressionSyntax)
                    Return invocation IsNot Nothing AndAlso invocation.Expression Is Nothing

                Case SyntaxKind.ObjectCreationExpression
                    Return True

                Case Else
                    Return False
            End Select
        End Function

        Protected Overrides Function ReplaceArgumentList(node As SyntaxNode, argumentList As SeparatedSyntaxList(Of ArgumentSyntax)) As SyntaxNode
            Select Case node.Kind()
                Case SyntaxKind.InvocationExpression
                    Dim invocation = DirectCast(node, InvocationExpressionSyntax)
                    Return invocation.WithArgumentList(invocation.ArgumentList.WithArguments(argumentList))

                Case SyntaxKind.ObjectCreationExpression
                    Dim objectCreation = DirectCast(node, ObjectCreationExpressionSyntax)
                    Return objectCreation.WithArgumentList(objectCreation.ArgumentList.WithArguments(argumentList))

                Case SyntaxKind.ConditionalAccessExpression
                    Dim conditional = DirectCast(node, ConditionalAccessExpressionSyntax)
                    Dim invocation = DirectCast(conditional.WhenNotNull, InvocationExpressionSyntax)
                    Return conditional.WithWhenNotNull(invocation.WithArgumentList(invocation.ArgumentList.WithArguments(argumentList)))

                Case Else
                    Return Nothing
            End Select
        End Function

        Protected Overrides Function GetArguments(node As SyntaxNode, ByRef targetNode As SyntaxNode) As SeparatedSyntaxList(Of ArgumentSyntax)
            targetNode = node
            Select Case node.Kind()
                Case SyntaxKind.ConditionalAccessExpression
                    Dim conditional = DirectCast(node, ConditionalAccessExpressionSyntax)
                    Dim invocation = DirectCast(conditional.WhenNotNull, InvocationExpressionSyntax)
                    targetNode = invocation
                    Return invocation.ArgumentList.Arguments

                Case SyntaxKind.InvocationExpression
                    Dim invocation = DirectCast(node, InvocationExpressionSyntax)
                    Return invocation.ArgumentList.Arguments

                Case SyntaxKind.ObjectCreationExpression
                    Dim objectCreation = DirectCast(node, ObjectCreationExpressionSyntax)
                    Return objectCreation.ArgumentList.Arguments

                Case Else
                    Return Nothing

            End Select
        End Function

        Protected Overrides Function IsLiteral(argument As ArgumentSyntax) As Boolean
            Return If(argument.GetExpression()?.IsAnyLiteralExpression(), False)
        End Function
    End Class
End Namespace

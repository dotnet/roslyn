' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.AddNamedArguments
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.AddNamedArguments
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicAddNamedArgumentsCodeRefactoringProvider)), [Shared]>
    Friend Class VisualBasicAddNamedArgumentsCodeRefactoringProvider
        Inherits AbstractAddNamedArgumentsCodeRefactoringProvider(Of ArgumentSyntax)

        Protected Overrides Function TryGetOrSynthesizeNamedArguments(arguments As SeparatedSyntaxList(Of ArgumentSyntax), parameters As ImmutableArray(Of IParameterSymbol), ByRef namedArguments As ArgumentSyntax(), ByRef hasLiteral As Boolean) As Boolean
            ' An argument list with omitted arguments may or may not be already fixed
            Dim alreadyFixed = True
            Dim result = ArrayBuilder(Of ArgumentSyntax).GetInstance(arguments.Count)
            For index = 0 To arguments.Count - 1
                Dim argument = arguments(index)
                Dim parameter = parameters(index)
                If argument.IsNamed Then
                    result.Add(argument)
                ElseIf argument.IsOmitted Then
                    Continue For
                ElseIf parameter.IsParams Then
                    ' Named arguments cannot match a ParamArray parameter
                    result.Free()
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
                result.Free()
                Return False
            Else
                namedArguments = result.ToArrayAndFree()
                Return True
            End If
        End Function

        Protected Overrides Function GetTargetNode(node As SyntaxNode) As SyntaxNode
            If node.IsKind(SyntaxKind.ConditionalAccessExpression) Then
                Return DirectCast(node, ConditionalAccessExpressionSyntax).WhenNotNull
            End If
            Return node
        End Function

        Protected Overrides Function IsCandidate(node As SyntaxNode) As Boolean
            Select Case node.Kind()
                Case SyntaxKind.InvocationExpression
                    ' If this is an indexer which is inside a conditional access, go up the tree
                    Dim invocation = DirectCast(node, InvocationExpressionSyntax)
                    Return invocation.Expression IsNot Nothing

                Case SyntaxKind.ConditionalAccessExpression
                    ' If this is not an indexer inside a conditional access, go up the tree
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

        Protected Overrides Function TryGetArguments(node As SyntaxNode, semanticModel As SemanticModel, ByRef arguments As SeparatedSyntaxList(Of ArgumentSyntax)) As Boolean
            Select Case node.Kind()
                Case SyntaxKind.ConditionalAccessExpression
                    Dim conditional = DirectCast(node, ConditionalAccessExpressionSyntax)
                    If IsArray(semanticModel, conditional.Expression) Then
                        Return False
                    End If

                    Dim invocation = DirectCast(conditional.WhenNotNull, InvocationExpressionSyntax)
                    arguments = invocation.ArgumentList.Arguments
                    Return True

                Case SyntaxKind.InvocationExpression
                    Dim invocation = DirectCast(node, InvocationExpressionSyntax)
                    If IsArray(semanticModel, invocation.Expression) Then
                        Return False
                    End If

                    arguments = invocation.ArgumentList.Arguments
                    Return True

                Case SyntaxKind.ObjectCreationExpression
                    Dim objectCreation = DirectCast(node, ObjectCreationExpressionSyntax)
                    arguments = objectCreation.ArgumentList.Arguments
                    Return True

                Case Else
                    Return False
            End Select
        End Function

        Protected Overrides Function IsLiteral(argument As ArgumentSyntax) As Boolean
            Return If(argument.GetExpression()?.IsAnyLiteralExpression(), False)
        End Function
    End Class
End Namespace

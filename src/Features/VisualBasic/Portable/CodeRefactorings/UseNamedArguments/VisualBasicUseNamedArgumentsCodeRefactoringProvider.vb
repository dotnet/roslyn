' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Composition
Imports System.Collections.Immutable
Imports System.Linq
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.UseNamedArguments
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.UseNamedArguments
    <ExtensionOrder(After:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicUseNamedArgumentsCodeRefactoringProvider)), [Shared]>
    Friend Class VisualBasicUseNamedArgumentsCodeRefactoringProvider
        Inherits AbstractUseNamedArgumentsCodeRefactoringProvider

        Protected Overrides Function IsCandidate(node As SyntaxNode) As Boolean
            Return node.IsKind(SyntaxKind.SimpleArgument)
        End Function

        Protected Overrides Function IsPositionalArgument(node As SyntaxNode) As Boolean
            Dim argument = DirectCast(node, SimpleArgumentSyntax)
            Return argument.NameColonEquals Is Nothing
        End Function

        Protected Overrides Function IsLegalToAddNamedArguments(parameters As ImmutableArray(Of IParameterSymbol), argumentCount As Integer) As Boolean
            Return Not parameters.LastOrDefault().IsParams OrElse parameters.Length > argumentCount
        End Function

        Protected Overrides Function GetArgumentListIndexAndCount(node As SyntaxNode) As ValueTuple(Of Integer, Integer)
            Dim argumentList = DirectCast(node.Parent, ArgumentListSyntax)
            Return ValueTuple.Create(argumentList.Arguments.IndexOf(DirectCast(node, SimpleArgumentSyntax)), argumentList.Arguments.Count)
        End Function

        Protected Overrides Function GetReceiver(argument As SyntaxNode) As SyntaxNode
            If argument.Parent?.Parent?.IsKind(SyntaxKind.Attribute) = True Then
                Return Nothing
            End If
            Return argument.Parent.Parent
        End Function

        Private Shared Iterator Function GetNamedArguments(parameters As ImmutableArray(Of IParameterSymbol),
                                                           argumentList As ArgumentListSyntax, index As Integer) As IEnumerable(Of SyntaxNode)
            Dim arguments = argumentList.Arguments
            For i As Integer = 0 To arguments.Count - 1
                Dim argument = DirectCast(arguments(i), ArgumentSyntax)
                If i < index Then
                    Yield argument
                ElseIf argument.IsNamed Then
                    Yield argument
                ElseIf argument.IsOmitted Then
                    Continue For
                Else
                    Dim parameter = parameters(i)
                    Dim simpleArgument = DirectCast(argument, SimpleArgumentSyntax)
                    Yield simpleArgument.WithNameColonEquals(SyntaxFactory.NameColonEquals(SyntaxFactory.IdentifierName(parameter.Name))).WithTriviaFrom(argument)
                End If
            Next
        End Function

        Protected Overrides Function GetOrSynthesizeNamedArguments(parameters As ImmutableArray(Of IParameterSymbol),
                                                                   argumentList As SyntaxNode, index As Integer) As SyntaxNode
            Dim argumentListSyntax = DirectCast(argumentList, ArgumentListSyntax)
            Dim namedArguments = GetNamedArguments(parameters, argumentListSyntax, index)
            Return argumentListSyntax.WithArguments(SyntaxFactory.SeparatedList(namedArguments))
        End Function
    End Class
End Namespace

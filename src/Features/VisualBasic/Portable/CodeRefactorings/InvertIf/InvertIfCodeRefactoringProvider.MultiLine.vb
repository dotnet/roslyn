' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InvertIf
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InvertIf), [Shared]>
    Friend NotInheritable Class VisualBasicInvertMultiLineIfCodeRefactoringProvider
        Inherits VisualBasicInvertIfCodeRefactoringProvider(Of MultiLineIfBlockSyntax)

        Protected Overrides Function GetInvertedIfNode(
            ifNode As MultiLineIfBlockSyntax,
            negatedExpression As ExpressionSyntax) As MultiLineIfBlockSyntax

            Dim ifPart = ifNode
            Dim elseBlock = ifNode.ElseBlock

            Dim ifStatement = ifNode.IfStatement

            Dim ifLeadingTrivia = ifNode.GetLeadingTrivia()
            Dim endifTrailingTrivia = ifNode.EndIfStatement.GetTrailingTrivia()
            Dim elseBlockLeadingTrivia = elseBlock.GetLeadingTrivia()
            Dim endifLeadingTrivia = ifNode.EndIfStatement.GetLeadingTrivia()

            ifNode = ifNode.Update(
                    ifStatement:=ifStatement.WithCondition(negatedExpression),
                    statements:=elseBlock.Statements,
                    elseIfBlocks:=Nothing,
                    elseBlock:=elseBlock.WithStatements(ifPart.Statements).WithLeadingTrivia(endifLeadingTrivia),
                    endIfStatement:=ifNode.EndIfStatement.WithTrailingTrivia(endifTrailingTrivia).WithLeadingTrivia(elseBlockLeadingTrivia))

            Return ifNode.WithLeadingTrivia(ifLeadingTrivia)
        End Function

        Protected Overrides Function GetHeaderSpan(ifNode As MultiLineIfBlockSyntax) As TextSpan
            Return TextSpan.FromBounds(
                    ifNode.IfStatement.IfKeyword.SpanStart,
                    ifNode.IfStatement.Condition.Span.End)
        End Function

        Protected Overrides Function IsElseless(ifNode As MultiLineIfBlockSyntax) As Boolean
            Return ifNode.ElseBlock Is Nothing
        End Function

        Protected Overrides Function CanInvert(ifNode As MultiLineIfBlockSyntax) As Boolean
            Return ifNode.ElseIfBlocks.IsEmpty
        End Function

        Protected Overrides Function GetCondition(ifNode As MultiLineIfBlockSyntax) As SyntaxNode
            Return ifNode.IfStatement.Condition
        End Function
    End Class
End Namespace

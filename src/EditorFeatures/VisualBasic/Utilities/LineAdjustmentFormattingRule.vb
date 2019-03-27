' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities
    Friend NotInheritable Class LineAdjustmentFormattingRule
        Inherits CompatAbstractFormattingRule

        Public Shared ReadOnly Instance As New LineAdjustmentFormattingRule()

        Private Sub New()
        End Sub

        Public Overrides Function GetAdjustNewLinesOperationSlow(previousToken As SyntaxToken, currentToken As SyntaxToken, optionSet As OptionSet, ByRef nextOperation As NextGetAdjustNewLinesOperation) As AdjustNewLinesOperation
            If Not CommonFormattingHelpers.HasAnyWhitespaceElasticTrivia(previousToken, currentToken) Then
                Return nextOperation.Invoke()
            End If

            Dim previous = CType(previousToken, SyntaxToken)
            Dim current = CType(currentToken, SyntaxToken)

            ' case: insert blank line in empty method body.
            If current.Kind = SyntaxKind.EndKeyword Then

                If (current.Parent.Kind = SyntaxKind.EndSubStatement AndAlso
                    current.Parent.Parent.IsKind(SyntaxKind.ConstructorBlock, SyntaxKind.SubBlock)) OrElse
                   (current.Parent.Kind = SyntaxKind.EndFunctionStatement AndAlso
                    current.Parent.Parent.Kind = SyntaxKind.FunctionBlock) AndAlso
                   Not DirectCast(current.Parent.Parent, MethodBlockSyntax).Statements.Any() Then

                    Return FormattingOperations.CreateAdjustNewLinesOperation(2, AdjustNewLinesOption.ForceLines)
                End If
            End If

            ' Introduce Line operation between 2 AttributeList
            If currentToken.Kind = SyntaxKind.LessThanToken AndAlso currentToken.Parent IsNot Nothing AndAlso TypeOf currentToken.Parent Is AttributeListSyntax AndAlso
               previousToken.Kind = SyntaxKind.GreaterThanToken AndAlso previousToken.Parent IsNot Nothing AndAlso TypeOf previousToken.Parent Is AttributeListSyntax Then
                Return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
            End If

            Return nextOperation.Invoke()
        End Function
    End Class
End Namespace

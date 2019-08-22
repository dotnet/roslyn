' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Partial Friend Class VisualBasicMethodExtractor
        Inherits MethodExtractor

        Private Class VisualBasicAnalyzer
            Inherits Analyzer

            Private Shared ReadOnly s_nonNoisySyntaxKindSet As HashSet(Of Integer) = New HashSet(Of Integer) From {SyntaxKind.WhitespaceTrivia, SyntaxKind.EndOfLineTrivia}

            Public Shared Function AnalyzeResultAsync(currentSelectionResult As SelectionResult, cancellationToken As CancellationToken) As Task(Of AnalyzerResult)
                Dim analyzer = New VisualBasicAnalyzer(currentSelectionResult, cancellationToken)
                Return analyzer.AnalyzeAsync(cancellationToken)
            End Function

            Public Sub New(currentSelectionResult As SelectionResult, cancellationToken As CancellationToken)
                MyBase.New(currentSelectionResult, cancellationToken)
            End Sub

            Protected Overrides Function CreateFromSymbol(
                compilation As Compilation, symbol As ISymbol,
                type As ITypeSymbol, style As VariableStyle, requiresDeclarationExpressionRewrite As Boolean) As VariableInfo
                If symbol.IsFunctionValue() AndAlso style.ParameterStyle.DeclarationBehavior <> DeclarationBehavior.None Then
                    Contract.ThrowIfFalse(style.ParameterStyle.DeclarationBehavior = DeclarationBehavior.MoveIn OrElse style.ParameterStyle.DeclarationBehavior = DeclarationBehavior.SplitIn)
                    style = AlwaysReturn(style)
                End If

                Return CreateFromSymbolCommon(Of LocalDeclarationStatementSyntax)(compilation, symbol, type, style, s_nonNoisySyntaxKindSet)
            End Function

            Protected Overrides Function GetIndexOfVariableInfoToUseAsReturnValue(variableInfo As IList(Of VariableInfo)) As Integer
                ' in VB, only byRef exist, not out or ref distinction like C#
                Dim numberOfByRefParameters = 0

                Dim byRefSymbolIndex As Integer = -1

                For i As Integer = 0 To variableInfo.Count - 1
                    Dim variable = variableInfo(i)

                    ' there should be no-one set as return value yet
                    Contract.ThrowIfTrue(variable.UseAsReturnValue)

                    If Not variable.CanBeUsedAsReturnValue Then
                        Continue For
                    End If

                    ' check modifier
                    If variable.ParameterModifier = ParameterBehavior.Ref OrElse
                       variable.ParameterModifier = ParameterBehavior.Out Then
                        numberOfByRefParameters += 1
                        byRefSymbolIndex = i
                    End If
                Next i

                ' if there is only one "byRef", that will be converted to return statement.
                If numberOfByRefParameters = 1 Then
                    Return byRefSymbolIndex
                End If

                Return -1
            End Function

            Protected Overrides Function GetRangeVariableType(semanticModel As SemanticModel, symbol As IRangeVariableSymbol) As ITypeSymbol
                Dim info = semanticModel.GetSpeculativeTypeInfo(Me.SelectionResult.FinalSpan.Start, SyntaxFactory.ParseName(symbol.Name), SpeculativeBindingOption.BindAsExpression)
                If info.Type.IsErrorType() Then
                    Return Nothing
                End If

                Return If(info.ConvertedType.IsObjectType(), info.ConvertedType, info.Type)
            End Function

            Protected Overrides Function GetFlowAnalysisNodeRange() As Tuple(Of SyntaxNode, SyntaxNode)
                Dim vbSelectionResult = DirectCast(Me.SelectionResult, VisualBasicSelectionResult)

                Dim firstStatement = vbSelectionResult.GetFirstStatement()
                Dim lastStatement = vbSelectionResult.GetLastStatement()

                ' single statement case
                If firstStatement Is lastStatement OrElse
                   firstStatement.Span.Contains(lastStatement.Span) Then
                    Return New Tuple(Of SyntaxNode, SyntaxNode)(firstStatement, firstStatement)
                End If

                ' multiple statement case
                Dim firstUnderContainer = vbSelectionResult.GetFirstStatementUnderContainer()
                Dim lastUnderContainer = vbSelectionResult.GetLastStatementUnderContainer()
                Return New Tuple(Of SyntaxNode, SyntaxNode)(firstUnderContainer, lastUnderContainer)
            End Function

            Protected Overrides Function ContainsReturnStatementInSelectedCode(jumpOutOfRegionStatements As IEnumerable(Of SyntaxNode)) As Boolean
                Return jumpOutOfRegionStatements.Where(Function(n) TypeOf n Is ReturnStatementSyntax OrElse TypeOf n Is ExitStatementSyntax).Any()
            End Function

            Protected Overrides Function ReadOnlyFieldAllowed() As Boolean
                Dim methodBlock = Me.SelectionResult.GetContainingScopeOf(Of MethodBlockBaseSyntax)()
                If methodBlock Is Nothing Then
                    Return True
                End If

                Return Not TypeOf methodBlock.BlockStatement Is SubNewStatementSyntax
            End Function
        End Class
    End Class
End Namespace

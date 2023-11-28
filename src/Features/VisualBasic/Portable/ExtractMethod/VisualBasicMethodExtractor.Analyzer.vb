' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Partial Friend Class VisualBasicMethodExtractor
        Private Class VisualBasicAnalyzer
            Inherits Analyzer

            Private Shared ReadOnly s_nonNoisySyntaxKindSet As HashSet(Of Integer) = New HashSet(Of Integer) From {SyntaxKind.WhitespaceTrivia, SyntaxKind.EndOfLineTrivia}

            Public Shared Function AnalyzeResult(currentSelectionResult As VisualBasicSelectionResult, cancellationToken As CancellationToken) As AnalyzerResult
                Dim analyzer = New VisualBasicAnalyzer(currentSelectionResult, cancellationToken)
                Return analyzer.Analyze()
            End Function

            Public Sub New(currentSelectionResult As VisualBasicSelectionResult, cancellationToken As CancellationToken)
                MyBase.New(currentSelectionResult, localFunction:=False, cancellationToken)
            End Sub

            Protected Overrides ReadOnly Property TreatOutAsRef As Boolean = True

            Protected Overrides Function CreateFromSymbol(
                compilation As Compilation, symbol As ISymbol,
                type As ITypeSymbol, style As VariableStyle, requiresDeclarationExpressionRewrite As Boolean) As VariableInfo
                If symbol.IsFunctionValue() AndAlso style.ParameterStyle.DeclarationBehavior <> DeclarationBehavior.None Then
                    Contract.ThrowIfFalse(style.ParameterStyle.DeclarationBehavior = DeclarationBehavior.MoveIn OrElse style.ParameterStyle.DeclarationBehavior = DeclarationBehavior.SplitIn)
                    style = AlwaysReturn(style)
                End If

                Return CreateFromSymbolCommon(Of LocalDeclarationStatementSyntax)(compilation, symbol, type, style, s_nonNoisySyntaxKindSet)
            End Function

            Protected Overrides Function GetRangeVariableType(semanticModel As SemanticModel, symbol As IRangeVariableSymbol) As ITypeSymbol
                Dim info = semanticModel.GetSpeculativeTypeInfo(Me.SelectionResult.FinalSpan.Start, SyntaxFactory.ParseName(symbol.Name), SpeculativeBindingOption.BindAsExpression)
                If info.Type.IsErrorType() Then
                    Return Nothing
                End If

                Return If(info.ConvertedType.IsObjectType(), info.ConvertedType, info.Type)
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

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Partial Friend NotInheritable Class VisualBasicExtractMethodService
        Partial Friend Class VisualBasicMethodExtractor
            Private Class VisualBasicAnalyzer
                Inherits Analyzer

                Public Sub New(currentSelectionResult As SelectionResult, cancellationToken As CancellationToken)
                    MyBase.New(currentSelectionResult, localFunction:=False, cancellationToken)
                End Sub

                Protected Overrides ReadOnly Property TreatOutAsRef As Boolean = True

                Protected Overrides Function IsInPrimaryConstructorBaseType() As Boolean
                    Return False
                End Function

                Protected Overrides Function GetRangeVariableType(symbol As IRangeVariableSymbol) As ITypeSymbol
                    Dim info = Me.SemanticModel.GetSpeculativeTypeInfo(Me.SelectionResult.FinalSpan.Start, SyntaxFactory.ParseName(symbol.Name), SpeculativeBindingOption.BindAsExpression)
                    If info.Type.IsErrorType() Then
                        Return Nothing
                    End If

                    Return If(info.ConvertedType.IsObjectType(), info.ConvertedType, info.Type)
                End Function

                Protected Overrides Function ReadOnlyFieldAllowed() As Boolean
                    Dim methodBlock = Me.SelectionResult.GetContainingScopeOf(Of MethodBlockBaseSyntax)()
                    If methodBlock Is Nothing Then
                        Return True
                    End If

                    Return TypeOf methodBlock.BlockStatement IsNot SubNewStatementSyntax
                End Function

                Protected Overrides Function GetStatementFlowControlInformation(
                        controlFlowAnalysis As ControlFlowAnalysis) As ExtractMethodFlowControlInformation
                    ' We do not currently support converting code with advanced flow control constructs in VB. So just
                    ' provide basic information that produces consistent behavior with how extract method has always
                    ' worked in VB.
                    Return ExtractMethodFlowControlInformation.Create(
                        Me.SemanticModel.Compilation,
                        supportsComplexFlowControl:=False,
                        breakStatementCount:=0,
                        continueStatementCount:=0,
                        returnStatementCount:=controlFlowAnalysis.ExitPoints.Count(Function(n) TypeOf n Is ReturnStatementSyntax OrElse TypeOf n Is ExitStatementSyntax),
                        endPointIsReachable:=controlFlowAnalysis.EndPointIsReachable)
                End Function
            End Class
        End Class
    End Class
End Namespace

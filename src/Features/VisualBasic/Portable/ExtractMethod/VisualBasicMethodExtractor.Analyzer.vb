' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
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

                Protected Overrides Function ContainsReturnStatementInSelectedCode(exitPoints As ImmutableArray(Of SyntaxNode)) As Boolean
                    Return exitPoints.Any(Function(n) TypeOf n Is ReturnStatementSyntax OrElse TypeOf n Is ExitStatementSyntax)
                End Function

                Protected Overrides Function ReadOnlyFieldAllowed() As Boolean
                    Dim methodBlock = Me.SelectionResult.GetContainingScopeOf(Of MethodBlockBaseSyntax)()
                    If methodBlock Is Nothing Then
                        Return True
                    End If

                    Return TypeOf methodBlock.BlockStatement IsNot SubNewStatementSyntax
                End Function
            End Class
        End Class
    End Class
End Namespace

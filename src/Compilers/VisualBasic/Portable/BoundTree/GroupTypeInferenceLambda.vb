' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class GroupTypeInferenceLambda

        Public Function InferLambdaReturnType(delegateParams As ImmutableArray(Of ParameterSymbol), diagnostics As BindingDiagnosticBag) As TypeSymbol
            ' Return type of the lambda must be an Anonymous Type corresponding to the following initializer:
            '   New With {key .$VB$ItAnonymous = <delegates's second parameter> }
            If delegateParams.Length <> 2 Then
                Return Nothing
            Else
                Return Compilation.AnonymousTypeManager.ConstructAnonymousTypeSymbol(
                                            New AnonymousTypeDescriptor(
                                                ImmutableArray.Create(New AnonymousTypeField(GeneratedNameConstants.ItAnonymous,
                                                                       delegateParams(1).Type,
                                                                       Syntax.QueryClauseKeywordOrRangeVariableIdentifier.GetLocation(),
                                                                       True)),
                                                Syntax.QueryClauseKeywordOrRangeVariableIdentifier.GetLocation(),
                                                True),
                                            diagnostics)
            End If
        End Function

    End Class

End Namespace

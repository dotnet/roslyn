' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class GroupTypeInferenceLambda

        Public Function InferLambdaReturnType(delegateParams As ImmutableArray(Of ParameterSymbol)) As TypeSymbol
            ' Return type of the lambda must be an Anonymous Type corresponding to the following initializer:
            '   New With {key .$VB$ItAnonymous = <delegates's second parameter> }
            If delegateParams.Length <> 2 Then
                Return Nothing
            Else
                Return Compilation.AnonymousTypeManager.ConstructAnonymousTypeSymbol(
                                            New AnonymousTypeDescriptor(
                                                ImmutableArray.Create(New AnonymousTypeField(StringConstants.ItAnonymous,
                                                                       delegateParams(1).Type,
                                                                       Syntax.QueryClauseKeywordOrRangeVariableIdentifier.GetLocation(),
                                                                       True)),
                                                Syntax.QueryClauseKeywordOrRangeVariableIdentifier.GetLocation(),
                                                True))
            End If
        End Function

    End Class

End Namespace

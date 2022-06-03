' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Enum SynthesizedLambdaKind
        UserDefined
        DelegateRelaxationStub
        LateBoundAddressOfLambda

        ' query lambdas:
        FilterConditionQueryLambda ' where, take while, skip while conditions
        OrderingQueryLambda
        AggregationQueryLambda
        AggregateQueryLambda
        FromOrAggregateVariableQueryLambda
        LetVariableQueryLambda
        SelectQueryLambda
        GroupByItemsQueryLambda
        GroupByKeysQueryLambda
        JoinLeftQueryLambda
        JoinRightQueryLambda

        ' non-user code lambdas:
        JoinNonUserCodeQueryLambda
        AggregateNonUserCodeQueryLambda
        FromNonUserCodeQueryLambda
        GroupNonUserCodeQueryLambda ' group join, group by
        ConversionNonUserCodeQueryLambda
    End Enum

    Friend Module SynthesizedLambdaKindExtensions
        <Extension>
        Friend Function IsQueryLambda(kind As SynthesizedLambdaKind) As Boolean
            Return kind >= SynthesizedLambdaKind.FilterConditionQueryLambda AndAlso
                   kind <= SynthesizedLambdaKind.ConversionNonUserCodeQueryLambda
        End Function
    End Module
End Namespace

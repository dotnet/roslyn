' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit
    Friend Class VisualBasicEncVariableSlotAllocator
        Inherits EncVariableSlotAllocator

        Public Sub New(symbolMap As SymbolMatcher,
                       syntaxMapOpt As Func(Of SyntaxNode, SyntaxNode),
                       previousTopLevelMethod As IMethodSymbolInternal,
                       methodId As DebugId,
                       previousLocals As ImmutableArray(Of EncLocalInfo),
                       lambdaMapOpt As IReadOnlyDictionary(Of Integer, KeyValuePair(Of DebugId, Integer)),
                       closureMapOpt As IReadOnlyDictionary(Of Integer, DebugId),
                       stateMachineTypeNameOpt As String,
                       hoistedLocalSlotCount As Integer,
                       hoistedLocalSlotsOpt As IReadOnlyDictionary(Of EncHoistedLocalInfo, Integer),
                       awaiterCount As Integer,
                       awaiterMapOpt As IReadOnlyDictionary(Of ITypeReference, Integer))
            MyBase.New(symbolMap, syntaxMapOpt, previousTopLevelMethod, methodId,
                       previousLocals, lambdaMapOpt, closureMapOpt, stateMachineTypeNameOpt,
                       hoistedLocalSlotCount, hoistedLocalSlotsOpt, awaiterCount, awaiterMapOpt)
        End Sub

        Protected Overrides Function GetLambda(lambdaOrLambdaBodySyntax As SyntaxNode) As SyntaxNode
            Return LambdaUtilities.GetLambda(lambdaOrLambdaBodySyntax)
        End Function

        Protected Overrides Function TryGetCorrespondingLambdaBody(previousLambdaSyntax As SyntaxNode, lambdaOrLambdaBodySyntax As SyntaxNode) As SyntaxNode
            Return LambdaUtilities.GetCorrespondingLambdaBody(lambdaOrLambdaBodySyntax, previousLambdaSyntax)
        End Function
    End Class
End Namespace
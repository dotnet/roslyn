' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class Rewriter

        Public Shared Function LowerBodyOrInitializer(
            method As MethodSymbol,
            body As BoundBlock,
            previousSubmissionFields As SynthesizedSubmissionFields,
            compilationState As TypeCompilationState,
            diagnostics As DiagnosticBag,
            <Out> ByRef statemachineTypeOpt As StateMachineTypeSymbol,
            <Out> ByRef variableSlotAllocatorOpt As VariableSlotAllocator,
            isBodySynthesized As Boolean) As BoundBlock

            Debug.Assert(Not body.HasErrors)

            ' performs node-specific lowering.
            Dim sawLambdas As Boolean
            Dim symbolsCapturedWithoutCopyCtor As ISet(Of Symbol) = Nothing
            Dim rewrittenNodes As HashSet(Of BoundNode) = Nothing

            Dim loweredBody = LocalRewriter.Rewrite(body,
                                                         method,
                                                         compilationState,
                                                         previousSubmissionFields,
                                                         diagnostics:=diagnostics,
                                                         rewrittenNodes:=rewrittenNodes,
                                                         hasLambdas:=sawLambdas,
                                                         symbolsCapturedWithoutCopyCtor:=symbolsCapturedWithoutCopyCtor,
                                                         flags:=LocalRewriter.RewritingFlags.Default,
                                                         currentMethod:=Nothing)

            If loweredBody.HasErrors Then
                Return loweredBody
            End If

#If DEBUG Then
            For Each node In rewrittenNodes.ToArray
                If node.Kind = BoundKind.Literal Then
                    rewrittenNodes.Remove(node)
                End If
            Next
#End If

            ' Lowers lambda expressions into expressions that construct delegates.    
            Dim bodyWithoutLambdas = loweredBody
            If sawLambdas Then
                bodyWithoutLambdas = LambdaRewriter.Rewrite(loweredBody,
                                                            method,
                                                            compilationState,
                                                            If(symbolsCapturedWithoutCopyCtor, SpecializedCollections.EmptySet(Of Symbol)),
                                                            diagnostics,
                                                            rewrittenNodes)
            End If

            If bodyWithoutLambdas.HasErrors Then
                Return bodyWithoutLambdas
            End If

            If compilationState.ModuleBuilderOpt IsNot Nothing Then
                variableSlotAllocatorOpt = compilationState.ModuleBuilderOpt.TryCreateVariableSlotAllocator(method)
            End If

            Return RewriteIteratorAndAsync(bodyWithoutLambdas, method, compilationState, diagnostics, variableSlotAllocatorOpt, statemachineTypeOpt)
        End Function

        Friend Shared Function RewriteIteratorAndAsync(bodyWithoutLambdas As BoundBlock,
                                                       method As MethodSymbol,
                                                       compilationState As TypeCompilationState,
                                                       diagnostics As DiagnosticBag,
                                                       slotAllocatorOpt As VariableSlotAllocator,
                                                       <Out> ByRef stateMachineTypeOpt As StateMachineTypeSymbol) As BoundBlock

            Dim iteratorStateMachine As IteratorStateMachine = Nothing
            Dim bodyWithoutIterators = IteratorRewriter.Rewrite(bodyWithoutLambdas,
                                                                method,
                                                                slotAllocatorOpt,
                                                                compilationState,
                                                                diagnostics,
                                                                iteratorStateMachine)

            If bodyWithoutIterators.HasErrors Then
                Return bodyWithoutIterators
            End If

            Dim asyncStateMachine As AsyncStateMachine = Nothing
            Dim bodyWithoutAsync = AsyncRewriter.Rewrite(bodyWithoutIterators,
                                                         method,
                                                         slotAllocatorOpt,
                                                         compilationState,
                                                         diagnostics,
                                                         asyncStateMachine)

            Debug.Assert(iteratorStateMachine Is Nothing OrElse asyncStateMachine Is Nothing)
            stateMachineTypeOpt = If(iteratorStateMachine, DirectCast(asyncStateMachine, StateMachineTypeSymbol))

            Return bodyWithoutAsync
        End Function
    End Class
End Namespace


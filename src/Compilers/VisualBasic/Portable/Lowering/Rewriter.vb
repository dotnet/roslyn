' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class Rewriter

        Public Shared Function LowerBodyOrInitializer(
            method As MethodSymbol,
            methodOrdinal As Integer,
            body As BoundBlock,
            previousSubmissionFields As SynthesizedSubmissionFields,
            compilationState As TypeCompilationState,
            diagnostics As DiagnosticBag,
            ByRef lazyVariableSlotAllocator As VariableSlotAllocator,
            lambdaDebugInfoBuilder As ArrayBuilder(Of LambdaDebugInfo),
            closureDebugInfoBuilder As ArrayBuilder(Of ClosureDebugInfo),
            ByRef delegateRelaxationIdDispenser As Integer,
            <Out> ByRef stateMachineTypeOpt As StateMachineTypeSymbol,
            allowOmissionOfConditionalCalls As Boolean,
            isBodySynthesized As Boolean) As BoundBlock

            Debug.Assert(Not body.HasErrors)
            Debug.Assert(compilationState.ModuleBuilderOpt IsNot Nothing)

            ' performs node-specific lowering.
            Dim sawLambdas As Boolean
            Dim symbolsCapturedWithoutCopyCtor As ISet(Of Symbol) = Nothing
            Dim rewrittenNodes As HashSet(Of BoundNode) = Nothing
            Dim flags = If(allowOmissionOfConditionalCalls, LocalRewriter.RewritingFlags.AllowOmissionOfConditionalCalls, LocalRewriter.RewritingFlags.Default)
            Dim localDiagnostics = DiagnosticBag.GetInstance()

            Try
                Dim loweredBody = LocalRewriter.Rewrite(body,
                                                    method,
                                                    compilationState,
                                                    previousSubmissionFields,
                                                    localDiagnostics,
                                                    rewrittenNodes,
                                                    sawLambdas,
                                                    symbolsCapturedWithoutCopyCtor,
                                                    flags,
                                                    currentMethod:=Nothing)

                If loweredBody.HasErrors OrElse localDiagnostics.HasAnyErrors Then
                    diagnostics.AddRangeAndFree(localDiagnostics)
                    Return loweredBody
                End If

#If DEBUG Then
                For Each node In rewrittenNodes.ToArray
                    If node.Kind = BoundKind.Literal Then
                        rewrittenNodes.Remove(node)
                    End If
                Next
#End If

                If lazyVariableSlotAllocator Is Nothing Then
                    ' synthesized lambda methods are handled in LambdaRewriter.RewriteLambdaAsMethod
                    Debug.Assert(TypeOf method IsNot SynthesizedLambdaMethod)
                    lazyVariableSlotAllocator = compilationState.ModuleBuilderOpt.TryCreateVariableSlotAllocator(method, method)
                End If

                ' Lowers lambda expressions into expressions that construct delegates.    
                Dim bodyWithoutLambdas = loweredBody
                If sawLambdas Then
                    bodyWithoutLambdas = LambdaRewriter.Rewrite(loweredBody,
                                                            method,
                                                            methodOrdinal,
                                                            lambdaDebugInfoBuilder,
                                                            closureDebugInfoBuilder,
                                                            delegateRelaxationIdDispenser,
                                                            lazyVariableSlotAllocator,
                                                            compilationState,
                                                            If(symbolsCapturedWithoutCopyCtor, SpecializedCollections.EmptySet(Of Symbol)),
                                                            localDiagnostics,
                                                            rewrittenNodes)
                End If

                If bodyWithoutLambdas.HasErrors OrElse localDiagnostics.HasAnyErrors Then
                    diagnostics.AddRangeAndFree(localDiagnostics)
                    Return bodyWithoutLambdas
                End If

                Dim bodyWithoutIteratorAndAsync = RewriteIteratorAndAsync(bodyWithoutLambdas, method, methodOrdinal, compilationState, localDiagnostics, lazyVariableSlotAllocator, stateMachineTypeOpt)

                diagnostics.AddRangeAndFree(localDiagnostics)

                Return bodyWithoutIteratorAndAsync

            Catch ex As BoundTreeVisitor.CancelledByStackGuardException
                diagnostics.AddRangeAndFree(localDiagnostics)
                ex.AddAnError(diagnostics)
                Return New BoundBlock(body.Syntax, body.StatementListSyntax, body.Locals, body.Statements, hasErrors:=True)
            End Try
        End Function

        Friend Shared Function RewriteIteratorAndAsync(bodyWithoutLambdas As BoundBlock,
                                                       method As MethodSymbol,
                                                       methodOrdinal As Integer,
                                                       compilationState As TypeCompilationState,
                                                       diagnostics As DiagnosticBag,
                                                       slotAllocatorOpt As VariableSlotAllocator,
                                                       <Out> ByRef stateMachineTypeOpt As StateMachineTypeSymbol) As BoundBlock

            Debug.Assert(compilationState.ModuleBuilderOpt IsNot Nothing)

            Dim iteratorStateMachine As IteratorStateMachine = Nothing
            Dim bodyWithoutIterators = IteratorRewriter.Rewrite(bodyWithoutLambdas,
                                                                method,
                                                                methodOrdinal,
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
                                                         methodOrdinal,
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


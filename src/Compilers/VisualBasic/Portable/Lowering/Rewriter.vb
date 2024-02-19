' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics.CodeAnalysis
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class Rewriter

        Public Shared Function LowerBodyOrInitializer(
            method As MethodSymbol,
            methodOrdinal As Integer,
            body As BoundBlock,
            previousSubmissionFields As SynthesizedSubmissionFields,
            compilationState As TypeCompilationState,
            instrumentations As MethodInstrumentation,
            <Out> ByRef codeCoverageSpans As ImmutableArray(Of SourceSpan),
            debugDocumentProvider As DebugDocumentProvider,
            diagnostics As BindingDiagnosticBag,
            ByRef lazyVariableSlotAllocator As VariableSlotAllocator,
            lambdaDebugInfoBuilder As ArrayBuilder(Of EncLambdaInfo),
            lambdaRuntimeRudeEditsBuilder As ArrayBuilder(Of LambdaRuntimeRudeEditInfo),
            closureDebugInfoBuilder As ArrayBuilder(Of EncClosureInfo),
            stateMachineStateDebugInfoBuilder As ArrayBuilder(Of StateMachineStateDebugInfo),
            ByRef delegateRelaxationIdDispenser As Integer,
            <Out> ByRef stateMachineTypeOpt As StateMachineTypeSymbol,
            allowOmissionOfConditionalCalls As Boolean,
            isBodySynthesized As Boolean) As BoundBlock

            Debug.Assert(Not body.HasErrors)
            Debug.Assert(compilationState.ModuleBuilderOpt IsNot Nothing)
            Debug.Assert(diagnostics.AccumulatesDiagnostics)

            ' performs node-specific lowering.
            Dim sawLambdas As Boolean
            Dim symbolsCapturedWithoutCopyCtor As ISet(Of Symbol) = Nothing
            Dim rewrittenNodes As HashSet(Of BoundNode) = Nothing
            Dim flags = If(allowOmissionOfConditionalCalls, LocalRewriter.RewritingFlags.AllowOmissionOfConditionalCalls, LocalRewriter.RewritingFlags.Default)
            Dim localDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics)
            Debug.Assert(localDiagnostics.AccumulatesDiagnostics)

            Try
                Dim codeCoverageInstrumenter As CodeCoverageInstrumenter =
                    If(Not isBodySynthesized AndAlso instrumentations.Kinds.Contains(InstrumentationKind.TestCoverage),
                        CodeCoverageInstrumenter.TryCreate(method, body, New SyntheticBoundNodeFactory(method, method, body.Syntax, compilationState, diagnostics), diagnostics, debugDocumentProvider, Instrumenter.NoOp),
                        Nothing)

                ' We don't want IL to differ based upon whether we write the PDB to a file/stream or not.
                ' Presence of sequence points in the tree affects final IL, therefore, we always generate them.
                Dim loweredBody = LocalRewriter.Rewrite(body,
                                                    method,
                                                    compilationState,
                                                    previousSubmissionFields,
                                                    localDiagnostics,
                                                    rewrittenNodes,
                                                    sawLambdas,
                                                    symbolsCapturedWithoutCopyCtor,
                                                    flags,
                                                    If(codeCoverageInstrumenter IsNot Nothing, New DebugInfoInjector(codeCoverageInstrumenter), DebugInfoInjector.Singleton),
                                                    currentMethod:=Nothing)

                codeCoverageSpans = If(codeCoverageInstrumenter IsNot Nothing, codeCoverageInstrumenter.DynamicAnalysisSpans, ImmutableArray(Of SourceSpan).Empty)

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
                    lazyVariableSlotAllocator = compilationState.ModuleBuilderOpt.TryCreateVariableSlotAllocator(method, method, diagnostics.DiagnosticBag)
                End If

                ' Lowers lambda expressions into expressions that construct delegates.    
                Dim bodyWithoutLambdas = loweredBody
                If sawLambdas Then
                    bodyWithoutLambdas = LambdaRewriter.Rewrite(loweredBody,
                                                            method,
                                                            methodOrdinal,
                                                            lambdaDebugInfoBuilder,
                                                            lambdaRuntimeRudeEditsBuilder,
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

                Dim bodyWithoutIteratorAndAsync = RewriteIteratorAndAsync(bodyWithoutLambdas, method, methodOrdinal, compilationState, localDiagnostics, stateMachineStateDebugInfoBuilder, lazyVariableSlotAllocator, stateMachineTypeOpt)

                diagnostics.AddRangeAndFree(localDiagnostics)

                Return bodyWithoutIteratorAndAsync

            Catch ex As BoundTreeVisitor.CancelledByStackGuardException
                diagnostics.AddRangeAndFree(localDiagnostics)
                ex.AddAnError(diagnostics)
                Return New BoundBlock(body.Syntax, body.StatementListSyntax, body.Locals, body.Statements, hasErrors:=True)
            End Try
        End Function

        <SuppressMessage("Style", "VSTHRD200:Use ""Async"" suffix for async methods", Justification:="'Async' refers to the language feature here.")>
        Friend Shared Function RewriteIteratorAndAsync(bodyWithoutLambdas As BoundBlock,
                                                       method As MethodSymbol,
                                                       methodOrdinal As Integer,
                                                       compilationState As TypeCompilationState,
                                                       diagnostics As BindingDiagnosticBag,
                                                       stateMachineStateDebugInfoBuilder As ArrayBuilder(Of StateMachineStateDebugInfo),
                                                       slotAllocatorOpt As VariableSlotAllocator,
                                                       <Out> ByRef stateMachineTypeOpt As StateMachineTypeSymbol) As BoundBlock

            Debug.Assert(compilationState.ModuleBuilderOpt IsNot Nothing)

            Dim iteratorStateMachine As IteratorStateMachine = Nothing
            Dim bodyWithoutIterators = IteratorRewriter.Rewrite(bodyWithoutLambdas,
                                                                method,
                                                                methodOrdinal,
                                                                stateMachineStateDebugInfoBuilder,
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
                                                         stateMachineStateDebugInfoBuilder,
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


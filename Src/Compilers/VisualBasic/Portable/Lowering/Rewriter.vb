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
            variableSlotAllocatorOpt As VariableSlotAllocator,
            Optional isBodySynthesized As Boolean = False) As BoundBlock

            Debug.Assert(Not body.HasErrors)

            ' performs node-specific lowering.
            Dim hasLambdas As Boolean
            Dim symbolsCapturedWithoutCopyCtor As ISet(Of Symbol) = Nothing
            Dim rewrittenNodes As HashSet(Of BoundNode) = Nothing

            Dim locallyRewritten = LocalRewriter.Rewrite(body,
                                                         method,
                                                         compilationState,
                                                         previousSubmissionFields,
                                                         diagnostics:=diagnostics,
                                                         rewrittenNodes:=rewrittenNodes,
                                                         hasLambdas:=hasLambdas,
                                                         symbolsCapturedWithoutCopyCtor:=symbolsCapturedWithoutCopyCtor,
                                                         flags:=LocalRewriter.RewritingFlags.Default,
                                                         currentMethod:=Nothing)

            If locallyRewritten.HasErrors Then
                Return locallyRewritten
            End If

#If DEBUG Then
            For Each node In rewrittenNodes.ToArray
                If node.Kind = BoundKind.Literal Then
                    rewrittenNodes.Remove(node)
                End If
            Next
#End If

            ' Lowers lambda expressions into expressions that construct delegates.    
            Dim lambdaRewritten = locallyRewritten
            If hasLambdas Then
                lambdaRewritten = LambdaRewriter.Rewrite(locallyRewritten,
                                                         method,
                                                         compilationState,
                                                         If(symbolsCapturedWithoutCopyCtor, SpecializedCollections.EmptySet(Of Symbol)),
                                                         diagnostics,
                                                         rewrittenNodes)
            End If

            If lambdaRewritten.HasErrors Then
                Return lambdaRewritten
            End If

            ' Rewrite Iterator methods
            Dim iteratorRewritten = IteratorRewriter.Rewrite(lambdaRewritten,
                                                             method,
                                                             variableSlotAllocatorOpt,
                                                             compilationState,
                                                             diagnostics)

            If iteratorRewritten.HasErrors Then
                Return iteratorRewritten
            End If

            ' Rewrite Async methods
            Dim asyncRewritten = AsyncRewriter.Rewrite(iteratorRewritten,
                                                       method,
                                                       variableSlotAllocatorOpt,
                                                       compilationState,
                                                       diagnostics)

            Return asyncRewritten
        End Function

    End Class
End Namespace


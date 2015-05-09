' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.ExpressionEvaluator

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend Structure VisualBasicMetadataContext

        Friend ReadOnly MetadataBlocks As ImmutableArray(Of MetadataBlock)
        Friend ReadOnly Compilation As VisualBasicCompilation
        Friend ReadOnly EvaluationContext As EvaluationContext

        Friend Sub New(metadataBlocks As ImmutableArray(Of MetadataBlock), compilation As VisualBasicCompilation)
            Me.MetadataBlocks = metadataBlocks
            Me.Compilation = compilation
            Me.EvaluationContext = Nothing
        End Sub

        Friend Sub New(metadataBlocks As ImmutableArray(Of MetadataBlock), evaluationContext As EvaluationContext)
            Me.MetadataBlocks = metadataBlocks
            Me.Compilation = evaluationContext.Compilation
            Me.EvaluationContext = evaluationContext
        End Sub

        Friend Function Matches(metadataBlocks As ImmutableArray(Of MetadataBlock)) As Boolean
            Return Not Me.MetadataBlocks.IsDefault AndAlso
                Me.MetadataBlocks.SequenceEqual(metadataBlocks)
        End Function

    End Structure

End Namespace

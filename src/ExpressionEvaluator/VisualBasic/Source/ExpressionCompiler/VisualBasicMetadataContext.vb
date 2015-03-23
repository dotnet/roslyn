' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.ExpressionEvaluator

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend Structure VisualBasicMetadataContext

        Friend ReadOnly MetadataBlocks As ImmutableArray(Of MetadataBlock)
        Friend ReadOnly Compilation As VisualBasicCompilation
        Friend ReadOnly EvaluationContext As EvaluationContext
        Friend ReadOnly ModuleVersionId As Guid

        Friend Sub New(metadataBlocks As ImmutableArray(Of MetadataBlock), compilation As VisualBasicCompilation, moduleVersionId As Guid)
            Me.MetadataBlocks = metadataBlocks
            Me.Compilation = compilation
            Me.EvaluationContext = Nothing
            Me.ModuleVersionId = moduleVersionId
        End Sub

        Friend Sub New(evaluationContext As EvaluationContext)
            Me.MetadataBlocks = evaluationContext.MetadataBlocks
            Me.Compilation = evaluationContext.Compilation
            Me.EvaluationContext = evaluationContext
            Me.ModuleVersionId = evaluationContext.ModuleVersionId
        End Sub

        Friend Function Matches(metadataBlocks As ImmutableArray(Of MetadataBlock), moduleVersionId As Guid) As Boolean
            Return Not Me.MetadataBlocks.IsDefault AndAlso
                Me.ModuleVersionId = moduleVersionId AndAlso
                Me.MetadataBlocks.SequenceEqual(metadataBlocks)
        End Function

    End Structure

End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.ExpressionEvaluator

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class VisualBasicMetadataContext : Inherits MetadataContext

        Friend ReadOnly Compilation As VisualBasicCompilation
        Friend ReadOnly EvaluationContext As EvaluationContext

        Friend Sub New(metadataBlocks As ImmutableArray(Of MetadataBlock))
            MyBase.New(metadataBlocks)

            Me.Compilation = metadataBlocks.ToCompilation()
        End Sub

        Friend Sub New(evaluationContext As EvaluationContext)
            MyBase.New(evaluationContext.MetadataBlocks)

            Me.Compilation = evaluationContext.Compilation
            Me.EvaluationContext = evaluationContext
        End Sub

    End Class

End Namespace

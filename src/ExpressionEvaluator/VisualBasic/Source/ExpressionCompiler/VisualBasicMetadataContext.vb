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

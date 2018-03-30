' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.ExpressionEvaluator

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    ' Remove this class and use MetadataContext(Of VisualBasicCompilation, EvaluationContext) directly.
    Friend NotInheritable Class VisualBasicMetadataContext
        Inherits MetadataContext(Of VisualBasicCompilation, EvaluationContext)

        Friend Sub New(compilation As VisualBasicCompilation, Optional evaluationContext As EvaluationContext = Nothing)
            MyBase.New(compilation, evaluationContext)
        End Sub

        ' TODO: Remove metadataBlocks parameter.
        Friend Sub New(metadataBlocks As ImmutableArray(Of MetadataBlock), evaluationContext As EvaluationContext)
            MyBase.New(evaluationContext.Compilation, evaluationContext)
        End Sub

    End Class

End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend Structure VisualBasicMetadataContext

        Friend ReadOnly Compilation As VisualBasicCompilation
        Friend ReadOnly EvaluationContext As EvaluationContext

        Friend Sub New(compilation As VisualBasicCompilation, Optional evaluationContext As EvaluationContext = Nothing)
            Me.Compilation = compilation
            Me.EvaluationContext = evaluationContext
        End Sub

    End Structure

End Namespace

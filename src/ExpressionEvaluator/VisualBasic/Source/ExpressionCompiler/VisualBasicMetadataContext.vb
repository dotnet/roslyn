' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

Imports System.Threading
Imports Roslyn.Services.Simplification

Namespace Roslyn.Services.VisualBasic.Simplification
    Partial Friend Class VisualBasicLambdaParameterSimplificationService
        Inherits AbstractVisualBasicExpressionSimplificationService

        Protected Overrides Function CreateExpressionRewriter(cancellationToken As CancellationToken) As IExpressionRewriter
            Return New Rewriter(cancellationToken)
        End Function
    End Class
End Namespace

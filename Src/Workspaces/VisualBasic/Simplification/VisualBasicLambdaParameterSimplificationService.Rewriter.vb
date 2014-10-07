Imports System.Threading
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Simplification

Namespace Roslyn.Services.VisualBasic.Simplification
    Partial Friend Class VisualBasicLambdaParameterSimplificationService
        Private Class Rewriter
            Inherits AbstractExpressionRewriter

            Public Sub New(cancellationToken As CancellationToken)
                MyBase.New(cancellationToken)
            End Sub
        End Class
    End Class
End Namespace

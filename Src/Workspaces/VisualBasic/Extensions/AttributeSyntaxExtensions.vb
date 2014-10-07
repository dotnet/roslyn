Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module AttributeSyntaxExtensions

        <Extension>
        Public Function MakeSemanticallyExplicit(
            expression As AttributeSyntax,
            document As Document,
            Optional cancellationToken As CancellationToken = Nothing
        ) As AttributeSyntax
            Return DirectCast(Simplifier.Expand(document, expression, cancellationToken), AttributeSyntax)
        End Function

    End Module
End Namespace

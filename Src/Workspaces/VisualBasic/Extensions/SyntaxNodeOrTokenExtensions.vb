Imports System.Runtime.CompilerServices
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.VisualBasic


Namespace Roslyn.Services.VisualBasic.Extensions
    Friend Module SyntaxNodeOrTokenExtensions
        <Extension()>
        Private Function GetAncestors(Of T As SyntaxNode)(token As SyntaxNodeOrToken) As IEnumerable(Of T)
            If token.IsToken Then
                Return token.AsToken().GetAncestors(Of T)()
            Else
                Return token.AsNode().GetAncestors(Of T)()
            End If
        End Function

        <Extension()>
        Private Function MatchesKind(node As SyntaxNodeOrToken, ParamArray kinds As SyntaxKind()) As Boolean
            Return kinds.Contains(node.Kind)
        End Function
    End Module
End Namespace
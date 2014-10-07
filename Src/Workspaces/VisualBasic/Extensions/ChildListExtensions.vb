Imports System.Runtime.CompilerServices
Imports Roslyn.Compilers.VisualBasic

Namespace Roslyn.Services.VisualBasic.Extensions
    Friend Module ChildListExtensions
        <Extension()>
        Friend Function AsNodes(childList As ChildSyntaxList) As IEnumerable(Of SyntaxNode)
            Return From child In childList
                   Where child.IsNode
                   Select child.AsNode()
        End Function

        <Extension()>
        Friend Function AsTokens(childList As ChildSyntaxList) As IEnumerable(Of SyntaxToken)
            Return From child In childList
                   Where child.IsToken
                   Select child.AsToken()
        End Function
    End Module
End Namespace
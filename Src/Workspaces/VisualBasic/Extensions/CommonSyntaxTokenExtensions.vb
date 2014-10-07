Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Common

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Module CommonSyntaxTokenExtensions
#If REMOVE Then
        <Extension()>
        Public Function IsKindOrHasMatchingText(token As SyntaxToken, kind As SyntaxKind) As Boolean
            Return CType(token, SyntaxToken).IsKindOrHasMatchingText(kind)
        End Function

        <Extension()>
        Public Function HasMatchingText(token As SyntaxToken, kind As SyntaxKind) As Boolean
            Return CType(token, SyntaxToken).HasMatchingText(kind)
        End Function

        <Extension()>
        Public Function IsParentKind(token As SyntaxToken, kind As SyntaxKind) As Boolean
            Return CType(token, SyntaxToken).IsParentKind(kind)
        End Function

        <Extension()>
        Public Function MatchesKind(token As SyntaxToken, kind As SyntaxKind) As Boolean
            Return CType(token, SyntaxToken).MatchesKind(kind)
        End Function

        <Extension()>
        Public Function MatchesKind(token As SyntaxToken, kind1 As SyntaxKind, kind2 As SyntaxKind) As Boolean
            Return CType(token, SyntaxToken).MatchesKind(kind1, kind2)
        End Function

        <Extension()>
        Public Function MatchesKind(token As SyntaxToken, ParamArray kinds As SyntaxKind()) As Boolean
            Return CType(token, SyntaxToken).MatchesKind(kinds)
        End Function
#End If
    End Module
End Namespace
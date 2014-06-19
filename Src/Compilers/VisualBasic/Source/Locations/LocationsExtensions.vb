Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Module LocationsExtensions

        <Extension()>
        Public Function GetSyntaxTree(location As Location) As SyntaxTree
            Dim tree As SyntaxTree = location.SourceTree
            If tree Is Nothing AndAlso location.Kind = LocationKind.None Then
                Dim embedded = TryCast(location, EmbeddedTreeLocation)
                If embedded IsNot Nothing Then
                    tree = embedded.EmbeddedKind.GetEmbeddedTree()
                End If
            End If
            Return tree
        End Function

    End Module

End Namespace

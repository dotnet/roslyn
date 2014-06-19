Imports System.Collections.Concurrent
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ' The NameGenerator class generates unique names, with numbered
    ' suffixes 1, 2, 3, ...
    Friend Class NameGenerator
        ' This dictionary remembers how many names we have given out.
        Private _nameCount As ConcurrentDictionary(Of String, Integer)

        Public Function GenerateName(baseName As String) As String
            If _nameCount Is Nothing Then
                Interlocked.CompareExchange(_nameCount, New ConcurrentDictionary(Of String, Integer), Nothing)
            End If
            Dim labelCount As Integer = _nameCount.AddOrUpdate(baseName, 1, Function(name, oldValue) oldValue + 1)
            Return baseName & labelCount.ToString()
        End Function
    End Class
End Namespace
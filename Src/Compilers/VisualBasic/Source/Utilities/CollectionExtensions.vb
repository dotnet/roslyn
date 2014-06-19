Imports System.Collections.Generic
Imports System.Linq.Enumerable
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Module CollectionExtensions

        <Extension()> _
        Friend Function ToDictionary(Of K, T)(data As IEnumerable(Of T), keySelector As Func(Of T, K), comparer As IEqualityComparer(Of K)) As Dictionary(Of K, ReadOnlyArray(Of T))
            Dim dictionary As New Dictionary(Of K, ReadOnlyArray(Of T))(comparer)
            Dim groups As IEnumerable(Of IGrouping(Of K, T)) = data.GroupBy(keySelector, comparer)
            Dim grouping As IGrouping(Of K, T)
            For Each grouping In groups
                Dim items As ReadOnlyArray(Of T) = grouping.AsReadOnly()
                dictionary.Add(grouping.Key, items)
            Next
            Return dictionary
        End Function
    End Module
End Namespace
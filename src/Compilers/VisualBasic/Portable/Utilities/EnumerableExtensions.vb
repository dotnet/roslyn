' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic
    Module EnumerableExtensions
        <Extension>
        Public Iterator Function Concat(Of T)(source As IEnumerable(Of T), value As T) As IEnumerable(Of T)
            For Each v In source
                Yield v
            Next

            Yield value
        End Function
    End Module
End Namespace

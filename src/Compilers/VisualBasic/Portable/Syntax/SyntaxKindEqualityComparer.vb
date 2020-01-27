' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Public Class SyntaxFacts

        Private NotInheritable Class SyntaxKindEqualityComparer
            Implements IEqualityComparer(Of SyntaxKind)

            Public Overloads Function Equals(x As SyntaxKind, y As SyntaxKind) As Boolean Implements IEqualityComparer(Of SyntaxKind).Equals
                Return x = y
            End Function

            Public Overloads Function GetHashCode(obj As SyntaxKind) As Integer Implements IEqualityComparer(Of SyntaxKind).GetHashCode
                Return obj
            End Function
        End Class

        ''' <summary>
        ''' A custom equality comparer for <see cref="SyntaxKind"/>
        ''' </summary>
        ''' <remarks>
        ''' PERF: The framework specializes EqualityComparer for enums, but only if the underlying type is System.Int32
        ''' Since SyntaxKind's underlying type is System.UInt16, ObjectEqualityComparer will be chosen instead.
        ''' </remarks>
        Public Shared ReadOnly Property EqualityComparer As IEqualityComparer(Of SyntaxKind) = New SyntaxKindEqualityComparer
    End Class
End Namespace

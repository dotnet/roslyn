' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities
    Friend Class DirectiveSyntaxEqualityComparer
        Implements IEqualityComparer(Of DirectiveTriviaSyntax)

        Public Shared ReadOnly Instance As New DirectiveSyntaxEqualityComparer

        Private Sub New()
        End Sub

        Public Shadows Function Equals(x As DirectiveTriviaSyntax, y As DirectiveTriviaSyntax) As Boolean Implements IEqualityComparer(Of DirectiveTriviaSyntax).Equals
            Return x.SpanStart = y.SpanStart
        End Function

        Public Shadows Function GetHashCode(obj As DirectiveTriviaSyntax) As Integer Implements IEqualityComparer(Of DirectiveTriviaSyntax).GetHashCode
            Return obj.SpanStart
        End Function
    End Class
End Namespace

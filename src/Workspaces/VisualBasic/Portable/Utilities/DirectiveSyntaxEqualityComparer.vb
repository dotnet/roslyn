' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
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

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundInterpolatedStringExpression

        Public ReadOnly Property HasInterpolations As Boolean
            Get
                ' $""
                ' $"TEXT"
                ' $"TEXT{INTERPOLATION}..."
                ' $"{INTERPOLATION}"
                ' $"{INTERPOLATION}TEXT..."
                ' The parser will never produce two adjacent text elements so in for non-synthetic trees this should only need
                ' to examine the first two elements at most.
                For Each item In Contents
                    If item.Kind = BoundKind.Interpolation Then Return True
                Next

                Return False
            End Get
        End Property

        Public ReadOnly Property IsEmpty() As Boolean
            Get
                Return Contents.Length = 0
            End Get
        End Property

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(Type.SpecialType = SpecialType.System_String)
            Debug.Assert(Not Contents.Where(Function(content) content.Kind <> BoundKind.Interpolation AndAlso content.Kind <> BoundKind.Literal).Any())
        End Sub
#End If

    End Class
End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundQueryableSource

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(RangeVariables.Length = 1)
        End Sub
#End If

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return Source.ExpressionSymbol
            End Get
        End Property

        Public Overrides ReadOnly Property ResultKind As LookupResultKind
            Get
                Return Source.ResultKind
            End Get
        End Property
    End Class

End Namespace

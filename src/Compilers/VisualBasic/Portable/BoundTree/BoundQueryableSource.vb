' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundQueryableSource

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

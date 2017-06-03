' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    <DebuggerDisplay("{GetDebuggerDisplay(), nq}")>
    Friend Partial Class BoundLiteral
        Public Overrides ReadOnly Property ConstantValueOpt As ConstantValue
            Get
                Return Me.Value
            End Get
        End Property

#If DEBUG Then
        Private Function GetDebuggerDisplay() As String
            Return Me.Value.ToString
        End Function
        Private Sub Validate()
            ValidateConstantValue()
        End Sub
#End If

    End Class

End Namespace

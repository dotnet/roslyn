' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundLValuePlaceholderBase
        Inherits BoundValuePlaceholderBase

        Public NotOverridable Overrides ReadOnly Property IsLValue As Boolean
            Get
                Return True
            End Get
        End Property

        Protected NotOverridable Overrides Function MakeRValueImpl() As BoundExpression
            Return New BoundLValueToRValueWrapper(Me.Syntax, Me, Me.Type).MakeCompilerGenerated() ' This is a compiler generated node
        End Function

    End Class

End Namespace

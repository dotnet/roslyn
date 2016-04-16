' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundLValuePlaceholderBase
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

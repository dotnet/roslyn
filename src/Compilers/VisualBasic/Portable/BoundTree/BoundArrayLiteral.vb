' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundArrayLiteral
        Inherits BoundExpression

        Public ReadOnly Property IsEmptyArrayLiteral As Boolean
            Get
                Return InferredType.Rank = 1 AndAlso Initializer.Initializers.Length = 0
            End Get
        End Property

    End Class

End Namespace

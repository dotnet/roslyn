' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundArrayLiteral
        Inherits BoundExpression

        Public ReadOnly Property IsEmptyArrayLiteral As Boolean
            Get
                Return InferredType.Rank = 1 AndAlso Initializer.Initializers.Length = 0
            End Get
        End Property

    End Class

End Namespace

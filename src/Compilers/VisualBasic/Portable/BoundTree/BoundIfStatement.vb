' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class BoundIfStatement
        Implements IBoundConditional

        Private ReadOnly Property IBoundConditional_WhenTrue As BoundNode Implements IBoundConditional.WhenTrue
            Get
                Return Me.Consequence
            End Get
        End Property

        Private ReadOnly Property IBoundConditional_WhenFalseOpt As BoundNode Implements IBoundConditional.WhenFalseOpt
            Get
                Return Me.AlternativeOpt
            End Get
        End Property

        Private ReadOnly Property IBoundConditional_Condition As BoundExpression Implements IBoundConditional.Condition
            Get
                Return Me.Condition
            End Get
        End Property
    End Class
End Namespace

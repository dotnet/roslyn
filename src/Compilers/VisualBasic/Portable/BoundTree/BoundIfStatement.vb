' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

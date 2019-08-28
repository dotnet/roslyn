' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class BoundWhileStatement
        Implements IBoundConditionalLoop

        Private ReadOnly Property IBoundConditionalLoop_Condition As BoundExpression Implements IBoundConditionalLoop.Condition
            Get
                Return Condition
            End Get
        End Property

        Private ReadOnly Property IBoundConditionalLoop_IgnoredCondition As BoundExpression Implements IBoundConditionalLoop.IgnoredCondition
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IBoundConditionalLoop_Body As BoundNode Implements IBoundConditionalLoop.Body
            Get
                Return Body
            End Get
        End Property
    End Class
End Namespace

' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Class BoundTypeOrValueExpression

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                ' Although BoundTypeOrValueExpression nodes are typically removed from the bound tree, they
                ' still persist as the receiver of a method group, and the semantic model can walk to them.
                ' If the semantic model does walk to them, it means that it is being treated as a type, not
                ' an expression.
                Return Me.Type
            End Get
        End Property
    End Class
End Namespace

﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Partial Class BoundWithStatement
        Inherits BoundStatement

        ''' <summary> Returns the placeholder used in this With statement to 
        ''' substitute the expression in initial binding </summary>
        Friend ReadOnly Property ExpressionPlaceholder As BoundValuePlaceholderBase
            Get
                Return Binder.ExpressionPlaceholder
            End Get
        End Property

        ''' <summary>
        ''' A draft version of initializers which will be used in this With statement. 
        ''' Initializers are expressions which are used to capture expression in the current
        ''' With statement; they can be empty in some cases like if the expression is a local 
        ''' variable of value type.
        ''' 
        ''' Note, the initializers returned by this property are 'draft' because they are 
        ''' generated based on initial bound tree, the real initializers will be generated 
        ''' in lowering based on lowered expression form.
        ''' </summary>
        Friend ReadOnly Property DraftInitializers As ImmutableArray(Of BoundExpression)
            Get
                Return Binder.DraftInitializers
            End Get
        End Property

        ''' <summary>
        ''' A draft version of placeholder substitute which will be used in this With statement. 
        ''' 
        ''' Note, the placeholder substitute returned by this property is 'draft' because it is
        ''' generated based on initial bound tree, the real substitute will be generated in lowering 
        ''' based on lowered expression form.
        ''' </summary>
        Friend ReadOnly Property DraftPlaceholderSubstitute As BoundExpression
            Get
                Return Binder.DraftPlaceholderSubstitute
            End Get
        End Property
    End Class

End Namespace

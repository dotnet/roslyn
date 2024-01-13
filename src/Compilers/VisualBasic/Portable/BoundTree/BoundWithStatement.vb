' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class BoundWithStatement
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

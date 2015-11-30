﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Public Class ArgumentSyntax

        ''' <summary>
        ''' Gets a value indicating whether this argument is a named argument.
        ''' </summary>
        ''' <returns>True if this argument is a named argument; otherwise false.</returns>
        Public MustOverride ReadOnly Property IsNamed As Boolean

        ''' <summary>
        ''' Gets a value indicating whether this argument is an omitted argument.
        ''' </summary>
        ''' <returns>True if this argument is an omitted argument; otherwise false.</returns>
        Public ReadOnly Property IsOmitted As Boolean
            Get
                Return Kind = SyntaxKind.OmittedArgument
            End Get
        End Property

        ''' <summary>
        ''' Gets the expression of this argument, if any.
        ''' </summary>
        ''' <returns>The expression of this argument if it is a simple argument; otherwise null.</returns>
        Public MustOverride Function GetExpression() As ExpressionSyntax

    End Class

    Partial Public Class SimpleArgumentSyntax

        Public NotOverridable Overrides ReadOnly Property IsNamed As Boolean
            Get
                Return NameColonEquals IsNot Nothing
            End Get
        End Property

        <ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)>
        Public NotOverridable Overrides Function GetExpression() As ExpressionSyntax
            Return Expression
        End Function

    End Class

    Partial Public Class OmittedArgumentSyntax

        Public NotOverridable Overrides ReadOnly Property IsNamed As Boolean
            Get
                Return False
            End Get
        End Property

        <ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)>
        Public NotOverridable Overrides Function GetExpression() As ExpressionSyntax
            Return Nothing
        End Function

    End Class

    Partial Public Class RangeArgumentSyntax

        Public NotOverridable Overrides ReadOnly Property IsNamed As Boolean
            Get
                Return False
            End Get
        End Property

        <ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)>
        Public NotOverridable Overrides Function GetExpression() As ExpressionSyntax
            Return UpperBound
        End Function

    End Class

End Namespace

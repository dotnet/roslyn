' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundSequencePointExpression

#Disable Warning IDE0051 ' Remove unused private members - https://github.com/dotnet/roslyn/issues/61776
#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(Type Is _Expression.Type)
        End Sub
#End If
#Enable Warning IDE0051 ' Remove unused private members

        Public Overrides ReadOnly Property IsLValue As Boolean
            Get
                Return Me.Expression.IsLValue
            End Get
        End Property

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            Return MakeRValue()
        End Function

        Public Shadows Function MakeRValue() As BoundSequencePointExpression
            If Expression.IsLValue Then
                Return Update(Expression.MakeRValue(), Type)
            End If

            Return Me
        End Function

    End Class

End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Holds all information needed to rewrite a bound using block node.
    ''' </summary>
    Friend NotInheritable Class UsingInfo

        ''' <summary>
        ''' A dictionary holding a placeholder, a conversion from placeholder to IDisposable and a condition if placeholder IsNot nothing
        ''' per type.
        ''' </summary>
        Public ReadOnly PlaceholderInfo As Dictionary(Of TypeSymbol, ValueTuple(Of BoundRValuePlaceholder, BoundExpression, BoundExpression))

        ''' <summary>
        ''' Syntax node for the using block.
        ''' </summary>
        Public ReadOnly UsingStatementSyntax As UsingBlockSyntax

        ''' <summary>
        ''' Initializes a new instance of the <see cref="UsingInfo" /> class.
        ''' </summary>
        ''' <param name="usingStatementSyntax">The syntax node for the using block</param>
        ''' <param name="placeholderInfo">A dictionary holding a placeholder, a conversion from placeholder to IDisposable and 
        ''' a condition if placeholder IsNot nothing per type.</param>
        Public Sub New(
            usingStatementSyntax As UsingBlockSyntax,
            placeholderInfo As Dictionary(Of TypeSymbol, ValueTuple(Of BoundRValuePlaceholder, BoundExpression, BoundExpression))
        )
            Me.PlaceholderInfo = placeholderInfo
            Me.UsingStatementSyntax = usingStatementSyntax
        End Sub
    End Class
End Namespace

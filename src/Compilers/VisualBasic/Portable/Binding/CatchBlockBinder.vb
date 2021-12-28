' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Binder used to bind Catch blocks. 
    ''' It hosts the control variable (if one is declared) 
    ''' and inherits BlockBaseBinder since there are no Exit/Continue for catch blocks. 
    ''' </summary>
    Friend NotInheritable Class CatchBlockBinder
        Inherits BlockBaseBinder

        Private ReadOnly _syntax As CatchBlockSyntax
        Private _locals As ImmutableArray(Of LocalSymbol) = Nothing

        Public Sub New(enclosing As Binder, syntax As CatchBlockSyntax)
            MyBase.New(enclosing)

            Debug.Assert(syntax IsNot Nothing)
            _syntax = syntax
        End Sub

        Friend Overrides ReadOnly Property Locals As ImmutableArray(Of LocalSymbol)
            Get
                If _locals.IsDefault Then
                    ImmutableInterlocked.InterlockedCompareExchange(_locals, BuildLocals(), Nothing)
                End If

                Return _locals
            End Get
        End Property

        ' Build a read only array of all the local variables declared By the Catch declaration statement.
        ' There can only be 0 or 1 variable.
        Private Function BuildLocals() As ImmutableArray(Of LocalSymbol)
            Dim catchStatement = _syntax.CatchStatement

            Dim asClauseOptSyntax = catchStatement.AsClause

            ' catch variables cannot be declared without As clause
            ' missing As means that we need to bind to something already declared.
            If asClauseOptSyntax IsNot Nothing Then
                Debug.Assert(catchStatement.IdentifierName IsNot Nothing)

                Dim localVar = LocalSymbol.Create(Me.ContainingMember,
                                               Me,
                                               catchStatement.IdentifierName.Identifier,
                                               Nothing,
                                               asClauseOptSyntax,
                                               Nothing,
                                               LocalDeclarationKind.Catch)

                Return ImmutableArray.Create(localVar)
            End If

            Return ImmutableArray(Of LocalSymbol).Empty
        End Function

    End Class
End Namespace


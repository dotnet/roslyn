' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Binder used to bind using blocks. 
    ''' It hosts the variables declared in the resource list (if they are declared).
    ''' </summary>
    Friend NotInheritable Class UsingBlockBinder
        Inherits BlockBaseBinder

        Private ReadOnly _syntax As UsingBlockSyntax
        Private _locals As ImmutableArray(Of LocalSymbol) = Nothing

        Public Sub New(enclosing As Binder, syntax As UsingBlockSyntax)
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

        ' Build a read only array of all the local variables declared By the Using statement.
        ' there can be none or multiple variable declarators with multiple variable names
        Private Function BuildLocals() As ImmutableArray(Of LocalSymbol)
            Dim variableDeclarators = _syntax.UsingStatement.Variables

            If variableDeclarators.Count > 0 Then
                Dim localsBuilder = ArrayBuilder(Of LocalSymbol).GetInstance

                For Each variableDeclarator In variableDeclarators
                    Dim isNotAsNewAndHasInitializer = variableDeclarator.Initializer IsNot Nothing AndAlso
                                                      (variableDeclarator.AsClause Is Nothing OrElse variableDeclarator.AsClause.Kind <> SyntaxKind.AsNewClause)

                    Dim names = variableDeclarator.Names
                    For i = 0 To names.Count - 1
                        Dim name = names(i)

                        ' we'll simply reuse the logic for local variables here (incl. local type inference)

                        ' if this is not an AsNew declaration with multiple names and an initializer (error case), then only use
                        ' the initializer for the last variable.
                        localsBuilder.Add(LocalSymbol.Create(Me.ContainingMember, Me,
                                                             name.Identifier, name, variableDeclarator.AsClause,
                                                             If(isNotAsNewAndHasInitializer AndAlso i = names.Count - 1,
                                                                variableDeclarator.Initializer,
                                                                Nothing),
                                                             LocalDeclarationKind.Using))
                    Next
                Next

                Return localsBuilder.ToImmutableAndFree
            End If

            Return ImmutableArray(Of LocalSymbol).Empty
        End Function
    End Class
End Namespace


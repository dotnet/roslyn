' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class StatementListBinder
        Inherits BlockBaseBinder

        Private ReadOnly _statementList As SyntaxList(Of StatementSyntax)
        Private _locals As ImmutableArray(Of LocalSymbol) = Nothing

        Public Sub New(containing As Binder,
                       statementList As SyntaxList(Of StatementSyntax))
            MyBase.New(containing)
            _statementList = statementList
        End Sub

        Friend Overrides ReadOnly Property Locals As ImmutableArray(Of LocalSymbol)
            Get
                If _locals.IsDefault Then
                    ImmutableInterlocked.InterlockedCompareExchange(_locals, BuildLocals(), Nothing)
                End If

                Return _locals
            End Get
        End Property

        ' Build a read only array of all the local variables declared in this statement list.
        Private Function BuildLocals() As ImmutableArray(Of LocalSymbol)
            Dim locals As ArrayBuilder(Of LocalSymbol) = Nothing

            For Each statement In _statementList
                If statement.Kind = SyntaxKind.LocalDeclarationStatement Then
                    If locals Is Nothing Then
                        locals = ArrayBuilder(Of LocalSymbol).GetInstance()
                    End If

                    Dim localDeclSyntax = DirectCast(statement, LocalDeclarationStatementSyntax)

                    Dim declarationKind As LocalDeclarationKind = LocalDeclarationKind.Variable

                    For Each modifier In localDeclSyntax.Modifiers
                        Select Case modifier.Kind
                            Case SyntaxKind.ConstKeyword
                                declarationKind = LocalDeclarationKind.Constant
                                Exit For

                            Case SyntaxKind.StaticKeyword
                                declarationKind = LocalDeclarationKind.Static
                        End Select
                    Next

                    For Each declarator As VariableDeclaratorSyntax In localDeclSyntax.Declarators
                        Dim asClauseOptSyntax = declarator.AsClause
                        Dim isNotAsNewAndHasInitializer = declarator.Initializer IsNot Nothing AndAlso
                                                      (declarator.AsClause Is Nothing OrElse declarator.AsClause.Kind <> SyntaxKind.AsNewClause)

                        Dim names = declarator.Names
                        For i = 0 To names.Count - 1
                            Dim modifiedIdentifier = names(i)

                            ' if this is not an AsNew declaration with multiple names and an initializer (error case), then only use
                            ' the initializer for the last variable.
                            Dim localVar = LocalSymbol.Create(Me.ContainingMember, Me,
                                                             modifiedIdentifier.Identifier, modifiedIdentifier, asClauseOptSyntax,
                                                             If(isNotAsNewAndHasInitializer AndAlso i = names.Count - 1,
                                                                declarator.Initializer,
                                                                Nothing),
                                                             declarationKind)
                            locals.Add(localVar)
                        Next
                    Next
                End If
            Next

            If locals IsNot Nothing Then
                Return locals.ToImmutableAndFree()
            Else
                Return ImmutableArray(Of LocalSymbol).Empty
            End If
        End Function
    End Class
End Namespace


Imports System.Collections.Immutable
'-----------------------------------------------------------------------------
' Copyright (c) Microsoft Corporation. All rights reserved.
'-----------------------------------------------------------------------------

Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Binder used to bind Case blocks. 
    ''' It hosts the variables which capture type checks. 
    ''' </summary>
    Friend NotInheritable Class CaseBlockBinder
        Inherits BlockBaseBinder

        Private ReadOnly _syntax As CaseBlockSyntax
        Private _locals As ImmutableArray(Of LocalSymbol) = Nothing

        Public Sub New(enclosing As Binder, syntax As CaseBlockSyntax)
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

        ' Build a read only array of all the local variables declared By the Case clauses.
        Private Function BuildLocals() As ImmutableArray(Of LocalSymbol)
            Dim caseStatement = _syntax.Begin

            If caseStatement.Cases.Count = 0 Then Return ImmutableArray(Of LocalSymbol).Empty

            Dim localsBuilder = ArrayBuilder(Of LocalSymbol).GetInstance()

            For Each clause In caseStatement.Cases
                If clause.Kind <> SyntaxKind.CaseTypeClause Then Continue For

                Dim typeClause = DirectCast(clause, CaseTypeClauseSyntax)

                localsBuilder.Add(LocalSymbol.Create(Me.ContainingMember,
                                                     Me,
                                                     typeClause.Identifier,
                                                     Nothing,
                                                     typeClause.AsClause,
                                                     Nothing,
                                                     LocalSymbol.LocalDeclarationKind.Case))
            Next

            Return localsBuilder.ToImmutableAndFree()
        End Function

    End Class
End Namespace


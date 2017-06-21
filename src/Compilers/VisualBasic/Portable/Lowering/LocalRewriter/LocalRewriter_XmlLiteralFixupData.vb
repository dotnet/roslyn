' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class LocalRewriter

        Private Structure XmlLiteralFixupData
            Public Structure LocalWithInitialization
                Public ReadOnly Local As LocalSymbol
                Public ReadOnly Initialization As BoundExpression

                Public Sub New(local As LocalSymbol, initialization As BoundExpression)
                    Me.Local = local
                    Me.Initialization = initialization
                End Sub
            End Structure

            Private _locals As ArrayBuilder(Of LocalWithInitialization)

            Public Sub AddLocal(local As LocalSymbol, initialization As BoundExpression)
                If Me._locals Is Nothing Then
                    Me._locals = ArrayBuilder(Of LocalWithInitialization).GetInstance
                End If
                Me._locals.Add(New LocalWithInitialization(local, initialization))
            End Sub

            Public ReadOnly Property IsEmpty As Boolean
                Get
                    Return Me._locals Is Nothing
                End Get
            End Property

            Public Function MaterializeAndFree() As ImmutableArray(Of LocalWithInitialization)
                Debug.Assert(Not Me.IsEmpty)
                Dim materialized = Me._locals.ToImmutableAndFree
                Me._locals = Nothing
                Return materialized
            End Function
        End Structure

    End Class

End Namespace


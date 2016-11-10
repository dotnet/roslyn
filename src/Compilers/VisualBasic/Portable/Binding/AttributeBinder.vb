' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Binder used for attributes
    ''' </summary>
    Friend NotInheritable Class AttributeBinder
        Inherits Binder

        ''' <summary> Root syntax node </summary>
        Private ReadOnly _root As VisualBasicSyntaxNode

        Public Sub New(containingBinder As Binder, tree As SyntaxTree, Optional node As VisualBasicSyntaxNode = Nothing)
            MyBase.New(containingBinder, tree)

            _root = node
        End Sub

        ''' <summary> Field or property declaration statement syntax node </summary>
        Friend ReadOnly Property Root As VisualBasicSyntaxNode
            Get
                Return _root
            End Get
        End Property

        ''' <summary>
        ''' Some nodes have special binder's for their contents 
        ''' </summary>
        Public Overrides Function GetBinder(node As SyntaxNode) As Binder
            Return Nothing
        End Function

        Friend Overrides ReadOnly Property IsDefaultInstancePropertyAllowed As Boolean
            Get
                Return False
            End Get
        End Property
    End Class

End Namespace


' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Binder used for field, auto property initializations and parameter default values.
    ''' </summary>
    Friend NotInheritable Class DeclarationInitializerBinder
        Inherits Binder

        ''' <summary>
        ''' Backing field for the ContainingMember property
        ''' </summary>
        Private ReadOnly _symbol As Symbol

        ''' <summary> Root syntax node </summary>
        Private ReadOnly _root As VisualBasicSyntaxNode

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DeclarationInitializerBinder"/> class.
        ''' </summary>
        ''' <param name="symbol">The field, property or parameter symbol with an initializer or default value.</param>
        ''' <param name="next">The next binder.</param>
        ''' <param name="root">Root syntax node</param>
        Public Sub New(symbol As Symbol, [next] As Binder, root As VisualBasicSyntaxNode)
            MyBase.New([next])
            Debug.Assert((symbol.Kind = SymbolKind.Field) OrElse (symbol.Kind = SymbolKind.Property) OrElse (symbol.Kind = SymbolKind.Parameter))
            Me._symbol = symbol
            Debug.Assert(root IsNot Nothing)
            _root = root
        End Sub

        ''' <summary>
        ''' The member containing the binding context. 
        ''' This property is the main reason for this binder, because the binding context for an initialization 
        ''' needs to be the field or property symbol.
        ''' </summary>
        Public Overrides ReadOnly Property ContainingMember As Symbol
            Get
                Return Me._symbol
            End Get
        End Property

        ''' <summary> Field or property declaration statement syntax node </summary>
        Friend ReadOnly Property Root As VisualBasicSyntaxNode
            Get
                Return _root
            End Get
        End Property

        ' Initializers are expressions and so contain no descendant binders, except for lambda bodies, which are handled
        ' elsewhere (in MemberSemanticModel).
        Public Overrides Function GetBinder(node As VisualBasicSyntaxNode) As Binder
            Return Nothing
        End Function

        Public Overrides Function GetBinder(stmts As SyntaxList(Of StatementSyntax)) As Binder
            Return Nothing
        End Function
    End Class

End Namespace

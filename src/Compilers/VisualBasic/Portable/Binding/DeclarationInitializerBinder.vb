' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
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

        ''' <summary>
        ''' Backing field for the AdditionalContainingMembers property
        ''' </summary>
        Private ReadOnly _additionalSymbols As ImmutableArray(Of Symbol)

        ''' <summary> Root syntax node </summary>
        Private ReadOnly _root As VisualBasicSyntaxNode

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DeclarationInitializerBinder"/> class.
        ''' </summary>
        ''' <param name="symbol">The field, property or parameter symbol with an initializer or default value.</param>
        ''' <param name="additionalSymbols">Additional field for property symbols initialized with the initializer.</param>
        ''' <param name="next">The next binder.</param>
        ''' <param name="root">Root syntax node</param>
        Public Sub New(symbol As Symbol, additionalSymbols As ImmutableArray(Of Symbol), [next] As Binder, root As VisualBasicSyntaxNode)
            MyBase.New([next])
            Debug.Assert((symbol.Kind = SymbolKind.Field) OrElse (symbol.Kind = SymbolKind.Property) OrElse (symbol.Kind = SymbolKind.Parameter))
            Debug.Assert(additionalSymbols.All(Function(s) s.Kind = symbol.Kind AndAlso (s.Kind = SymbolKind.Field OrElse s.Kind = SymbolKind.Property)))
            Debug.Assert(symbol.Kind <> SymbolKind.Parameter OrElse additionalSymbols.IsEmpty)
            Me._symbol = symbol
            Me._additionalSymbols = additionalSymbols
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

        ''' <summary>
        ''' Additional members associated with the binding context
        ''' Currently, this field is only used for multiple field/property symbols initialized by an AsNew initializer, e.g. "Dim x, y As New Integer" or "WithEvents x, y as New Object"
        ''' </summary>
        Public Overrides ReadOnly Property AdditionalContainingMembers As ImmutableArray(Of Symbol)
            Get
                Return Me._additionalSymbols
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
        Public Overrides Function GetBinder(node As SyntaxNode) As Binder
            Return Nothing
        End Function

        Public Overrides Function GetBinder(stmts As SyntaxList(Of StatementSyntax)) As Binder
            Return Nothing
        End Function
    End Class

End Namespace

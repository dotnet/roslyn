' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a field or property initializer. Holds the symbol and the syntax for the initialization.
    ''' </summary>
    Friend Structure FieldOrPropertyInitializer
        ''' <summary>
        ''' The fields or properties being initialized, or Nothing if this represents an executable statement in script code.
        ''' We can get into a multiple properties case when multiple WithEvents fields are initialized with As New ...
        ''' </summary>
        Public ReadOnly FieldsOrProperties As ImmutableArray(Of Symbol)

        ''' <summary>
        ''' A reference to 
        ''' <see cref="EqualsValueSyntax"/>, 
        ''' <see cref="AsNewClauseSyntax"/>, 
        ''' <see cref="ModifiedIdentifierSyntax"/>,
        ''' <see cref="StatementSyntax"/> (top-level statement).
        ''' </summary>
        Public ReadOnly Syntax As SyntaxReference

        ''' <summary>
        ''' A sum of widths of spans of all preceding initializers
        ''' (instance and static initializers are summed separately, and trivias are not counted).
        ''' </summary>
        Friend ReadOnly PrecedingInitializersLength As Integer

        Friend ReadOnly IsMetadataConstant As Boolean

        ''' <summary>
        ''' Initializer for an executable statement in script code.
        ''' </summary>
        ''' <param name="syntax">The initializer syntax for the statement.</param>
        Public Sub New(syntax As SyntaxReference, precedingInitializersLength As Integer)
            Debug.Assert(TypeOf syntax.GetSyntax() Is StatementSyntax)
            Me.Syntax = syntax
            Me.IsMetadataConstant = False
            Me.PrecedingInitializersLength = precedingInitializersLength
        End Sub

        ''' <summary>
        ''' Initializes a new instance of the <see cref="FieldOrPropertyInitializer" /> structure.
        ''' </summary>
        ''' <param name="field">The field.</param>
        ''' <param name="syntax">The initializer syntax for the field.</param>
        Public Sub New(field As FieldSymbol, syntax As SyntaxReference, precedingInitializersLength As Integer)
            Debug.Assert(field IsNot Nothing)
            Debug.Assert(syntax.GetSyntax().IsKind(SyntaxKind.AsNewClause) OrElse
                         syntax.GetSyntax().IsKind(SyntaxKind.EqualsValue) OrElse
                         syntax.GetSyntax().IsKind(SyntaxKind.ModifiedIdentifier))

            Me.FieldsOrProperties = ImmutableArray.Create(Of Symbol)(field)
            Me.Syntax = syntax
            Me.IsMetadataConstant = field.IsMetadataConstant
            Me.PrecedingInitializersLength = precedingInitializersLength
        End Sub

        ''' <summary>
        ''' Initializes a new instance of the <see cref="FieldOrPropertyInitializer" /> structure.
        ''' </summary>
        Public Sub New(fieldsOrProperties As ImmutableArray(Of Symbol), syntax As SyntaxReference, precedingInitializersLength As Integer)
            Debug.Assert(Not fieldsOrProperties.IsEmpty)
            Debug.Assert(syntax.GetSyntax().IsKind(SyntaxKind.AsNewClause) OrElse syntax.GetSyntax().IsKind(SyntaxKind.EqualsValue))
            Me.FieldsOrProperties = fieldsOrProperties
            Me.Syntax = syntax
            Me.IsMetadataConstant = False
            Me.PrecedingInitializersLength = precedingInitializersLength
        End Sub

        ''' <summary>
        ''' Initializes a new instance of the <see cref="FieldOrPropertyInitializer" /> structure.
        ''' </summary>
        ''' <param name="property">The property.</param>
        ''' <param name="syntax">The initializer syntax for the property.</param>
        Public Sub New([property] As PropertySymbol, syntax As SyntaxReference, precedingInitializersLength As Integer)
            Debug.Assert([property] IsNot Nothing)
            Debug.Assert(syntax IsNot Nothing)
            Debug.Assert(syntax.GetSyntax().IsKind(SyntaxKind.AsNewClause) OrElse syntax.GetSyntax().IsKind(SyntaxKind.EqualsValue))

            Me.FieldsOrProperties = ImmutableArray.Create(Of Symbol)([property])
            Me.Syntax = syntax
            Me.IsMetadataConstant = False
            Me.PrecedingInitializersLength = precedingInitializersLength
        End Sub
    End Structure
End Namespace

' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a field or property initializer. Holds the symbol and the syntax for the initialization.
    ''' </summary>
    Friend Structure FieldOrPropertyInitializer
        ''' <summary>
        ''' The field or property being initialized, or Nothing if this represents an executable statement in script code.
        ''' </summary>
        Public ReadOnly FieldsOrProperty As ImmutableArray(Of Symbol)

        ''' <summary>
        ''' A reference to <see cref="EqualsValueSyntax"/>.
        ''' </summary>
        Public ReadOnly Syntax As SyntaxReference

        ''' <summary>
        ''' Initializer for an executable statement in script code.
        ''' </summary>
        ''' <param name="syntax">The initializer syntax for the statement.</param>
        Public Sub New(syntax As SyntaxReference)
            Debug.Assert(syntax IsNot Nothing)
            Me.Syntax = syntax
        End Sub

        ''' <summary>
        ''' Initializes a new instance of the <see cref="FieldOrPropertyInitializer" /> structure.
        ''' </summary>
        ''' <param name="field">The field.</param>
        ''' <param name="syntax">The initializer syntax for the field.</param>
        Public Sub New(field As FieldSymbol, syntax As SyntaxReference)
            Debug.Assert(field IsNot Nothing)
            Debug.Assert(syntax Is Nothing OrElse
                         syntax.GetVisualBasicSyntax.Kind = SyntaxKind.AsNewClause OrElse
                         syntax.GetVisualBasicSyntax.Kind = SyntaxKind.EqualsValue OrElse
                         syntax.GetVisualBasicSyntax.Kind = SyntaxKind.ModifiedIdentifier)

            Me.FieldsOrProperty = ImmutableArray.Create(Of Symbol)(field)
            Me.Syntax = syntax
        End Sub

        ''' <summary>
        ''' Initializes a new instance of the <see cref="FieldOrPropertyInitializer" /> structure.
        ''' </summary>
        Public Sub New(fieldsOrProperties As ImmutableArray(Of Symbol), syntax As SyntaxReference)
            Debug.Assert(Not fieldsOrProperties.IsEmpty)
            Debug.Assert(syntax.GetSyntax.VisualBasicKind = SyntaxKind.AsNewClause OrElse syntax.GetSyntax.VisualBasicKind = SyntaxKind.EqualsValue)

            Me.FieldsOrProperty = fieldsOrProperties
            Me.Syntax = syntax
        End Sub

        ''' <summary>
        ''' Initializes a new instance of the <see cref="FieldOrPropertyInitializer" /> structure.
        ''' </summary>
        ''' <param name="property">The property.</param>
        ''' <param name="syntax">The initializer syntax for the property.</param>
        Public Sub New([property] As PropertySymbol, syntax As SyntaxReference)
            Debug.Assert([property] IsNot Nothing)
            Debug.Assert(syntax IsNot Nothing)
            Debug.Assert(syntax.GetSyntax.VisualBasicKind = SyntaxKind.AsNewClause OrElse syntax.GetSyntax.VisualBasicKind = SyntaxKind.EqualsValue)

            Me.FieldsOrProperty = ImmutableArray.Create(Of Symbol)([property])
            Me.Syntax = syntax
        End Sub
    End Structure
End Namespace

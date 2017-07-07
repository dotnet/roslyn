﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Public Class TypeStatementSyntax

        Public ReadOnly Property Arity As Integer
            Get
                Return If(Me.TypeParameterList Is Nothing, 0, Me.TypeParameterList.Parameters.Count)
            End Get
        End Property

        ''' <summary>
        ''' Returns the keyword indicating the kind of declaration being made: "Class", "Structure", "Module", "Interface", etc.
        ''' </summary>
        Public MustOverride ReadOnly Property DeclarationKeyword As SyntaxToken

        ''' <summary>
        ''' Returns a copy of this <see cref="TypeStatementSyntax"/> with the <see cref="DeclarationKeyword"/> property changed to the
        ''' specified value. Returns this instance if the specified value is the same as the current value.
        ''' </summary>
        Public MustOverride Function WithDeclarationKeyword(keyword As SyntaxToken) As TypeStatementSyntax

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use DeclarationKeyword or a more specific property (e.g. ClassKeyword) instead.", True)>
        Public ReadOnly Property Keyword As SyntaxToken
            Get
                Return DeclarationKeyword
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use DeclarationKeyword or a more specific property (e.g. WithClassKeyword) instead.", True)>
        Public Function WithKeyword(keyword As SyntaxToken) As TypeStatementSyntax
            Return WithDeclarationKeyword(keyword)
        End Function

    End Class

    Partial Public Class ModuleStatementSyntax

        Public Overrides ReadOnly Property DeclarationKeyword As SyntaxToken
            Get
                Return ModuleKeyword
            End Get
        End Property

        Public Overrides Function WithDeclarationKeyword(keyword As SyntaxToken) As TypeStatementSyntax
            Return WithModuleKeyword(keyword)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows ReadOnly Property Keyword As SyntaxToken
            Get
                Return DeclarationKeyword
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows Function WithKeyword(keyword As SyntaxToken) As ModuleStatementSyntax
            Return WithModuleKeyword(keyword)
        End Function

    End Class

    Partial Public Class StructureStatementSyntax

        Public Overrides ReadOnly Property DeclarationKeyword As SyntaxToken
            Get
                Return StructureKeyword
            End Get
        End Property

        Public Overrides Function WithDeclarationKeyword(keyword As SyntaxToken) As TypeStatementSyntax
            Return WithStructureKeyword(keyword)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows ReadOnly Property Keyword As SyntaxToken
            Get
                Return DeclarationKeyword
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows Function WithKeyword(keyword As SyntaxToken) As StructureStatementSyntax
            Return WithStructureKeyword(keyword)
        End Function

    End Class

    Partial Public Class ClassStatementSyntax

        Public Overrides ReadOnly Property DeclarationKeyword As SyntaxToken
            Get
                Return ClassKeyword
            End Get
        End Property

        Public Overrides Function WithDeclarationKeyword(keyword As SyntaxToken) As TypeStatementSyntax
            Return WithClassKeyword(keyword)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows ReadOnly Property Keyword As SyntaxToken
            Get
                Return DeclarationKeyword
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows Function WithKeyword(keyword As SyntaxToken) As ClassStatementSyntax
            Return WithClassKeyword(keyword)
        End Function

    End Class

    Partial Public Class InterfaceStatementSyntax

        Public Overrides ReadOnly Property DeclarationKeyword As SyntaxToken
            Get
                Return InterfaceKeyword
            End Get
        End Property

        Public Overrides Function WithDeclarationKeyword(keyword As SyntaxToken) As TypeStatementSyntax
            Return WithInterfaceKeyword(keyword)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows ReadOnly Property Keyword As SyntaxToken
            Get
                Return DeclarationKeyword
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows Function WithKeyword(keyword As SyntaxToken) As InterfaceStatementSyntax
            Return WithInterfaceKeyword(keyword)
        End Function

    End Class

End Namespace

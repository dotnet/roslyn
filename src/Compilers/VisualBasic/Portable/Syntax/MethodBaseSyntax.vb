' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Public Class MethodBaseSyntax

        ''' <summary>
        ''' Returns the keyword indicating the kind of declaration being made: "Sub", "Function", "Event", "Property", etc. Does not return either the "Declare" or "Delegate" keywords.
        ''' </summary>
        Public MustOverride ReadOnly Property DeclarationKeyword As SyntaxToken

        ''' <summary>
        ''' Returns a copy of this <see cref="MethodBaseSyntax"/> with the <see cref="DeclarationKeyword"/> property changed to the
        ''' specified value. Returns this instance if the specified value is the same as the current value.
        ''' </summary>
        Public MustOverride Function WithDeclarationKeyword(keyword As SyntaxToken) As MethodBaseSyntax

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use DeclarationKeyword or a more specific property (e.g. SubOrFunctionKeyword) instead.", True)>
        Public ReadOnly Property Keyword As SyntaxToken
            Get
                Return DeclarationKeyword
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use DeclarationKeyword or a more specific property (e.g. WithSubOrFunctionKeyword) instead.", True)>
        Public Function WithKeyword(keyword As SyntaxToken) As MethodBaseSyntax
            Return WithDeclarationKeyword(keyword)
        End Function

    End Class

    Partial Public Class MethodStatementSyntax

        Public Overrides ReadOnly Property DeclarationKeyword As SyntaxToken
            Get
                Return SubOrFunctionKeyword
            End Get
        End Property

        Public Overrides Function WithDeclarationKeyword(keyword As SyntaxToken) As MethodBaseSyntax
            Return WithSubOrFunctionKeyword(keyword)
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
        Public Shadows Function WithKeyword(keyword As SyntaxToken) As MethodStatementSyntax
            Return WithSubOrFunctionKeyword(keyword)
        End Function

    End Class

    Partial Public Class DelegateStatementSyntax

        Public Overrides ReadOnly Property DeclarationKeyword As SyntaxToken
            Get
                Return SubOrFunctionKeyword
            End Get
        End Property

        Public Overrides Function WithDeclarationKeyword(keyword As SyntaxToken) As MethodBaseSyntax
            Return WithSubOrFunctionKeyword(keyword)
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
        Public Shadows Function WithKeyword(keyword As SyntaxToken) As DelegateStatementSyntax
            Return WithSubOrFunctionKeyword(keyword)
        End Function
    End Class

    Partial Public Class DeclareStatementSyntax

        Public Overrides ReadOnly Property DeclarationKeyword As SyntaxToken
            Get
                Return SubOrFunctionKeyword
            End Get
        End Property

        Public Overrides Function WithDeclarationKeyword(keyword As SyntaxToken) As MethodBaseSyntax
            Return WithSubOrFunctionKeyword(keyword)
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
        Public Shadows Function WithKeyword(keyword As SyntaxToken) As DeclareStatementSyntax
            Return WithSubOrFunctionKeyword(keyword)
        End Function

    End Class

    Partial Public Class LambdaHeaderSyntax

        Public Overrides ReadOnly Property DeclarationKeyword As SyntaxToken
            Get
                Return SubOrFunctionKeyword
            End Get
        End Property

        Public Overrides Function WithDeclarationKeyword(keyword As SyntaxToken) As MethodBaseSyntax
            Return WithSubOrFunctionKeyword(keyword)
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
        Public Shadows Function WithKeyword(keyword As SyntaxToken) As LambdaHeaderSyntax
            Return WithSubOrFunctionKeyword(keyword)
        End Function

    End Class

    Partial Public Class SubNewStatementSyntax

        Public Overrides ReadOnly Property DeclarationKeyword As SyntaxToken
            Get
                Return SubKeyword
            End Get
        End Property

        Public Overrides Function WithDeclarationKeyword(keyword As SyntaxToken) As MethodBaseSyntax
            Return WithSubKeyword(keyword)
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
        Public Shadows Function WithKeyword(keyword As SyntaxToken) As SubNewStatementSyntax
            Return WithSubKeyword(keyword)
        End Function

    End Class

    Partial Public Class EventStatementSyntax

        Public Overrides ReadOnly Property DeclarationKeyword As SyntaxToken
            Get
                Return EventKeyword
            End Get
        End Property

        Public Overrides Function WithDeclarationKeyword(keyword As SyntaxToken) As MethodBaseSyntax
            Return WithEventKeyword(keyword)
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
        Public Shadows Function WithKeyword(keyword As SyntaxToken) As EventStatementSyntax
            Return WithEventKeyword(keyword)
        End Function

    End Class

    Partial Public Class PropertyStatementSyntax

        Public Overrides ReadOnly Property DeclarationKeyword As SyntaxToken
            Get
                Return PropertyKeyword
            End Get
        End Property

        Public Overrides Function WithDeclarationKeyword(keyword As SyntaxToken) As MethodBaseSyntax
            Return WithPropertyKeyword(keyword)
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
        Public Shadows Function WithKeyword(keyword As SyntaxToken) As PropertyStatementSyntax
            Return WithPropertyKeyword(keyword)
        End Function

    End Class

    Partial Public Class OperatorStatementSyntax

        Public Overrides ReadOnly Property DeclarationKeyword As SyntaxToken
            Get
                Return OperatorKeyword
            End Get
        End Property

        Public Overrides Function WithDeclarationKeyword(keyword As SyntaxToken) As MethodBaseSyntax
            Return WithOperatorKeyword(keyword)
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
        Public Shadows Function WithKeyword(keyword As SyntaxToken) As OperatorStatementSyntax
            Return WithOperatorKeyword(keyword)
        End Function

    End Class

    Partial Public Class AccessorStatementSyntax

        Public Overrides ReadOnly Property DeclarationKeyword As SyntaxToken
            Get
                Return AccessorKeyword
            End Get
        End Property

        Public Overrides Function WithDeclarationKeyword(keyword As SyntaxToken) As MethodBaseSyntax
            Return WithAccessorKeyword(keyword)
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
        Public Shadows Function WithKeyword(keyword As SyntaxToken) As AccessorStatementSyntax
            Return WithAccessorKeyword(keyword)
        End Function

    End Class

End Namespace

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Public Class SyntaxFacts

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use IsAccessorStatementAccessorKeyword instead.", True)>
        Public Shared Function IsAccessorStatementKeyword(kind As SyntaxKind) As Boolean
            Return IsAccessorStatementAccessorKeyword(kind)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use IsDeclareStatementSubOrFunctionKeyword instead.", True)>
        Public Shared Function IsDeclareStatementKeyword(kind As SyntaxKind) As Boolean
            Return IsDeclareStatementSubOrFunctionKeyword(kind)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use IsDelegateStatementSubOrFunctionKeyword instead.", True)>
        Public Shared Function IsDelegateStatementKeyword(kind As SyntaxKind) As Boolean
            Return IsDelegateStatementSubOrFunctionKeyword(kind)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use IsLambdaHeaderSubOrFunctionKeyword instead.", True)>
        Public Shared Function IsLambdaHeaderKeyword(kind As SyntaxKind) As Boolean
            Return IsLambdaHeaderSubOrFunctionKeyword(kind)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use IsMethodStatementSubOrFunctionKeyword instead.", True)>
        Public Shared Function IsMethodStatementKeyword(kind As SyntaxKind) As Boolean
            Return IsMethodStatementSubOrFunctionKeyword(kind)
        End Function

    End Class

End Namespace

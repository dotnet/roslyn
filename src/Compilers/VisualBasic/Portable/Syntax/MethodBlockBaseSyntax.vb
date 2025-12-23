' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Public Class MethodBlockBaseSyntax

        ''' <summary>
        ''' The statement that begins the block declaration.
        ''' </summary>
        Public MustOverride ReadOnly Property BlockStatement As MethodBaseSyntax

        ''' <summary>
        ''' The statement that ends the block declaration.
        ''' </summary>
        Public MustOverride ReadOnly Property EndBlockStatement As EndBlockStatementSyntax

        ''' <summary>
        ''' Returns a copy of this <see cref="MethodBlockBaseSyntax"/> with the <see cref="BlockStatement"/> property changed to the
        ''' specified value. Returns this instance if the specified value is the same as the current value.
        ''' </summary>
        Public MustOverride Function WithBlockStatement(blockStatement As MethodBaseSyntax) As MethodBlockBaseSyntax

        ''' <summary>
        ''' Returns a copy of this <see cref="MethodBlockBaseSyntax"/> with the <see cref="EndBlockStatement"/> property changed to the
        ''' specified value. Returns this instance if the specified value is the same as the current value.
        ''' </summary>
        Public MustOverride Function WithEndBlockStatement(endBlockStatement As EndBlockStatementSyntax) As MethodBlockBaseSyntax

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use BlockStatement or a more specific property (e.g. SubOrFunctionStatement) instead.", True)>
        Public ReadOnly Property Begin As MethodBaseSyntax
            Get
                Return BlockStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use WithBlockStatement or a more specific property (e.g. WithSubOrFunctionStatement) instead.", True)>
        Public Function WithBegin(begin As MethodBaseSyntax) As MethodBlockBaseSyntax
            Return WithBlockStatement(begin)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use EndBlockStatement or a more specific property (e.g. EndSubOrFunctionStatement) instead.", True)>
        Public ReadOnly Property [End] As EndBlockStatementSyntax
            Get
                Return EndBlockStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use WithEndBlockStatement or a more specific property (e.g. WithEndSubOrFunctionStatement) instead.", True)>
        Public Function WithEnd([end] As EndBlockStatementSyntax) As MethodBlockBaseSyntax
            Return WithEndBlockStatement([end])
        End Function

    End Class

    Partial Public Class AccessorBlockSyntax

        Public Overrides ReadOnly Property BlockStatement As MethodBaseSyntax
            Get
                Return AccessorStatement
            End Get
        End Property

        Public Overrides ReadOnly Property EndBlockStatement As EndBlockStatementSyntax
            Get
                Return EndAccessorStatement
            End Get
        End Property

        Public Overrides Function WithBlockStatement(blockStatement As MethodBaseSyntax) As MethodBlockBaseSyntax
            Return WithAccessorStatement(DirectCast(blockStatement, AccessorStatementSyntax))
        End Function

        Public Overrides Function WithEndBlockStatement(endBlockStatement As EndBlockStatementSyntax) As MethodBlockBaseSyntax
            Return WithEndAccessorStatement(endBlockStatement)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use AccessorStatement instead.", True)>
        Public Shadows ReadOnly Property Begin As AccessorStatementSyntax
            Get
                Return AccessorStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use EndAccessorStatement instead.", True)>
        Public Shadows ReadOnly Property [End] As EndBlockStatementSyntax
            Get
                Return EndAccessorStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use WithAccessorStatement instead.", True)>
        Public Shadows Function WithBegin(begin As AccessorStatementSyntax) As AccessorBlockSyntax
            Return WithAccessorStatement(begin)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use WithEndAccessorStatement instead.", True)>
        Public Shadows Function WithEnd([end] As EndBlockStatementSyntax) As AccessorBlockSyntax
            Return WithEndAccessorStatement([end])
        End Function

    End Class

    Partial Public Class ConstructorBlockSyntax

        Public Overrides ReadOnly Property BlockStatement As MethodBaseSyntax
            Get
                Return SubNewStatement
            End Get
        End Property

        Public Overrides ReadOnly Property EndBlockStatement As EndBlockStatementSyntax
            Get
                Return EndSubStatement
            End Get
        End Property

        Public Overrides Function WithBlockStatement(blockStatement As MethodBaseSyntax) As MethodBlockBaseSyntax
            Return WithSubNewStatement(DirectCast(blockStatement, SubNewStatementSyntax))
        End Function

        Public Overrides Function WithEndBlockStatement(endBlockStatement As EndBlockStatementSyntax) As MethodBlockBaseSyntax
            Return WithEndSubStatement(endBlockStatement)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use SubNewStatement instead.", True)>
        Public Shadows ReadOnly Property Begin As SubNewStatementSyntax
            Get
                Return SubNewStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use EndSubStatement instead.", True)>
        Public Shadows ReadOnly Property [End] As EndBlockStatementSyntax
            Get
                Return EndSubStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use WithSubNewStatement instead.", True)>
        Public Shadows Function WithBegin(begin As SubNewStatementSyntax) As ConstructorBlockSyntax
            Return WithSubNewStatement(begin)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use WithEndSubStatement instead.", True)>
        Public Shadows Function WithEnd([end] As EndBlockStatementSyntax) As ConstructorBlockSyntax
            Return WithEndSubStatement([end])
        End Function

    End Class

    Partial Public Class MethodBlockSyntax

        Public Overrides ReadOnly Property BlockStatement As MethodBaseSyntax
            Get
                Return SubOrFunctionStatement
            End Get
        End Property

        Public Overrides ReadOnly Property EndBlockStatement As EndBlockStatementSyntax
            Get
                Return EndSubOrFunctionStatement
            End Get
        End Property

        Public Overrides Function WithBlockStatement(blockStatement As MethodBaseSyntax) As MethodBlockBaseSyntax
            Return WithSubOrFunctionStatement(DirectCast(blockStatement, MethodStatementSyntax))
        End Function

        Public Overrides Function WithEndBlockStatement(endBlockStatement As EndBlockStatementSyntax) As MethodBlockBaseSyntax
            Return WithEndSubOrFunctionStatement(endBlockStatement)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use SubOrFunctionStatement instead.", True)>
        Public Shadows ReadOnly Property Begin As MethodStatementSyntax
            Get
                Return SubOrFunctionStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use EndSubOrFunctionStatement instead.", True)>
        Public Shadows ReadOnly Property [End] As EndBlockStatementSyntax
            Get
                Return EndSubOrFunctionStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use WithSubOrFunctionStatement instead.", True)>
        Public Shadows Function WithBegin(begin As MethodStatementSyntax) As MethodBlockSyntax
            Return WithSubOrFunctionStatement(begin)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use WithEndSubOrFunctionStatement instead.", True)>
        Public Shadows Function WithEnd([end] As EndBlockStatementSyntax) As MethodBlockSyntax
            Return WithEndSubOrFunctionStatement([end])
        End Function

    End Class

    Partial Public Class OperatorBlockSyntax

        Public Overrides ReadOnly Property BlockStatement As MethodBaseSyntax
            Get
                Return OperatorStatement
            End Get
        End Property

        Public Overrides ReadOnly Property EndBlockStatement As EndBlockStatementSyntax
            Get
                Return EndOperatorStatement
            End Get
        End Property

        Public Overrides Function WithBlockStatement(blockStatement As MethodBaseSyntax) As MethodBlockBaseSyntax
            Return WithOperatorStatement(DirectCast(blockStatement, OperatorStatementSyntax))
        End Function

        Public Overrides Function WithEndBlockStatement(endBlockStatement As EndBlockStatementSyntax) As MethodBlockBaseSyntax
            Return WithEndOperatorStatement(endBlockStatement)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use OperatorStatement instead.", True)>
        Public Shadows ReadOnly Property Begin As OperatorStatementSyntax
            Get
                Return OperatorStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use EndOperatorStatement instead.", True)>
        Public Shadows ReadOnly Property [End] As EndBlockStatementSyntax
            Get
                Return EndOperatorStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use WithOperatorStatement instead.", True)>
        Public Shadows Function WithBegin(begin As OperatorStatementSyntax) As OperatorBlockSyntax
            Return WithOperatorStatement(begin)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use WithEndOperatorStatement instead.", True)>
        Public Shadows Function WithEnd([end] As EndBlockStatementSyntax) As OperatorBlockSyntax
            Return WithEndOperatorStatement([end])
        End Function

    End Class

End Namespace

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend Class MethodBlockBaseSyntax

        ''' <summary>
        ''' The statement that begins the block declaration.
        ''' </summary>
        Public MustOverride ReadOnly Property Begin As MethodBaseSyntax

        ''' <summary>
        ''' The statement that ends the block declaration.
        ''' </summary>
        Public MustOverride ReadOnly Property [End] As EndBlockStatementSyntax

    End Class

    Partial Friend Class AccessorBlockSyntax

        Public Overrides ReadOnly Property Begin As MethodBaseSyntax
            Get
                Return AccessorStatement
            End Get
        End Property

        Public Overrides ReadOnly Property [End] As EndBlockStatementSyntax
            Get
                Return EndAccessorStatement
            End Get
        End Property

    End Class

    Partial Friend Class ConstructorBlockSyntax

        Public Overrides ReadOnly Property Begin As MethodBaseSyntax
            Get
                Return SubNewStatement
            End Get
        End Property

        Public Overrides ReadOnly Property [End] As EndBlockStatementSyntax
            Get
                Return EndSubStatement
            End Get
        End Property

    End Class

    Partial Friend Class MethodBlockSyntax

        Public Overrides ReadOnly Property Begin As MethodBaseSyntax
            Get
                Return SubOrFunctionStatement
            End Get
        End Property

        Public Overrides ReadOnly Property [End] As EndBlockStatementSyntax
            Get
                Return EndSubOrFunctionStatement
            End Get
        End Property

    End Class

    Partial Friend Class OperatorBlockSyntax

        Public Overrides ReadOnly Property Begin As MethodBaseSyntax
            Get
                Return OperatorStatement
            End Get
        End Property

        Public Overrides ReadOnly Property [End] As EndBlockStatementSyntax
            Get
                Return EndOperatorStatement
            End Get
        End Property

    End Class

End Namespace

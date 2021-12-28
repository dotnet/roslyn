' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Public Class TypeBlockSyntax

        ''' <summary>
        ''' The statement that begins the block declaration.
        ''' </summary>
        Public MustOverride ReadOnly Property BlockStatement As TypeStatementSyntax

        ''' <summary>
        ''' The statement that ends the block declaration.
        ''' </summary>
        Public MustOverride ReadOnly Property EndBlockStatement As EndBlockStatementSyntax

        ''' <summary>
        ''' Returns a copy of this <see cref="TypeBlockSyntax"/> with the <see cref="BlockStatement"/> property changed to the
        ''' specified value. Returns this instance if the specified value is the same as the current value.
        ''' </summary>
        Public MustOverride Function WithBlockStatement(blockStatement As TypeStatementSyntax) As TypeBlockSyntax

        ''' <summary>
        ''' Returns a copy of this <see cref="TypeBlockSyntax"/> with the <see cref="EndBlockStatement"/> property changed to the
        ''' specified value. Returns this instance if the specified value is the same as the current value.
        ''' </summary>
        Public MustOverride Function WithEndBlockStatement(endBlockStatement As EndBlockStatementSyntax) As TypeBlockSyntax

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use BlockStatement or a more specific property (e.g. ClassStatement) instead.", True)>
        Public ReadOnly Property Begin As TypeStatementSyntax
            Get
                Return BlockStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use WithBlockStatement or a more specific property (e.g. WithClassStatement) instead.", True)>
        Public Function WithBegin(begin As TypeStatementSyntax) As TypeBlockSyntax
            Return WithBlockStatement(begin)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use EndBlockStatement or a more specific property (e.g. EndClassStatement) instead.", True)>
        Public ReadOnly Property [End] As EndBlockStatementSyntax
            Get
                Return EndBlockStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use WithEndBlockStatement or a more specific property (e.g. WithEndClassStatement) instead.", True)>
        Public Function WithEnd([end] As EndBlockStatementSyntax) As TypeBlockSyntax
            Return WithEndBlockStatement([end])
        End Function

    End Class

    Partial Public Class ClassBlockSyntax

        Public Overrides ReadOnly Property BlockStatement As TypeStatementSyntax
            Get
                Return ClassStatement
            End Get
        End Property

        Public Overrides ReadOnly Property EndBlockStatement As EndBlockStatementSyntax
            Get
                Return EndClassStatement
            End Get
        End Property

        Public Overrides Function WithBlockStatement(blockStatement As TypeStatementSyntax) As TypeBlockSyntax
            Return WithClassStatement(DirectCast(blockStatement, ClassStatementSyntax))
        End Function

        Public Overrides Function WithEndBlockStatement(endBlockStatement As EndBlockStatementSyntax) As TypeBlockSyntax
            Return WithEndClassStatement(endBlockStatement)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows ReadOnly Property Begin As ClassStatementSyntax
            Get
                Return ClassStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows ReadOnly Property [End] As EndBlockStatementSyntax
            Get
                Return EndClassStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows Function WithBegin(begin As ClassStatementSyntax) As ClassBlockSyntax
            Return WithClassStatement(begin)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows Function WithEnd([end] As EndBlockStatementSyntax) As ClassBlockSyntax
            Return WithEndClassStatement([end])
        End Function

    End Class

    Partial Public Class StructureBlockSyntax

        Public Overrides ReadOnly Property BlockStatement As TypeStatementSyntax
            Get
                Return StructureStatement
            End Get
        End Property

        Public Overrides ReadOnly Property EndBlockStatement As EndBlockStatementSyntax
            Get
                Return EndStructureStatement
            End Get
        End Property

        Public Overrides Function WithBlockStatement(blockStatement As TypeStatementSyntax) As TypeBlockSyntax
            Return WithStructureStatement(DirectCast(blockStatement, StructureStatementSyntax))
        End Function

        Public Overrides Function WithEndBlockStatement(endBlockStatement As EndBlockStatementSyntax) As TypeBlockSyntax
            Return WithEndStructureStatement(endBlockStatement)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows ReadOnly Property Begin As StructureStatementSyntax
            Get
                Return StructureStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows ReadOnly Property [End] As EndBlockStatementSyntax
            Get
                Return EndStructureStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows Function WithBegin(begin As StructureStatementSyntax) As StructureBlockSyntax
            Return WithStructureStatement(begin)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows Function WithEnd([end] As EndBlockStatementSyntax) As StructureBlockSyntax
            Return WithEndStructureStatement([end])
        End Function

    End Class

    Partial Public Class InterfaceBlockSyntax

        Public Overrides ReadOnly Property BlockStatement As TypeStatementSyntax
            Get
                Return InterfaceStatement
            End Get
        End Property

        Public Overrides ReadOnly Property EndBlockStatement As EndBlockStatementSyntax
            Get
                Return EndInterfaceStatement
            End Get
        End Property

        Public Overrides Function WithBlockStatement(blockStatement As TypeStatementSyntax) As TypeBlockSyntax
            Return WithInterfaceStatement(DirectCast(blockStatement, InterfaceStatementSyntax))
        End Function

        Public Overrides Function WithEndBlockStatement(endBlockStatement As EndBlockStatementSyntax) As TypeBlockSyntax
            Return WithEndInterfaceStatement(endBlockStatement)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows ReadOnly Property Begin As InterfaceStatementSyntax
            Get
                Return InterfaceStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows ReadOnly Property [End] As EndBlockStatementSyntax
            Get
                Return EndInterfaceStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows Function WithBegin(begin As InterfaceStatementSyntax) As InterfaceBlockSyntax
            Return WithInterfaceStatement(begin)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows Function WithEnd([end] As EndBlockStatementSyntax) As InterfaceBlockSyntax
            Return WithEndInterfaceStatement([end])
        End Function

    End Class

    Partial Public Class ModuleBlockSyntax

        Public Overrides ReadOnly Property BlockStatement As TypeStatementSyntax
            Get
                Return ModuleStatement
            End Get
        End Property

        Public Overrides ReadOnly Property EndBlockStatement As EndBlockStatementSyntax
            Get
                Return EndModuleStatement
            End Get
        End Property

        Public Overrides Function WithBlockStatement(blockStatement As TypeStatementSyntax) As TypeBlockSyntax
            Return WithModuleStatement(DirectCast(blockStatement, ModuleStatementSyntax))
        End Function

        Public Overrides Function WithEndBlockStatement(endBlockStatement As EndBlockStatementSyntax) As TypeBlockSyntax
            Return WithEndModuleStatement(endBlockStatement)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows ReadOnly Property Begin As ModuleStatementSyntax
            Get
                Return ModuleStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows ReadOnly Property [End] As EndBlockStatementSyntax
            Get
                Return EndModuleStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows Function WithBegin(begin As ModuleStatementSyntax) As ModuleBlockSyntax
            Return WithModuleStatement(begin)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete.", True)>
        Public Shadows Function WithEnd([end] As EndBlockStatementSyntax) As ModuleBlockSyntax
            Return WithEndModuleStatement([end])
        End Function

    End Class

End Namespace

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend Class TypeBlockSyntax

        ''' <summary>
        ''' The statement that begins the block declaration.
        ''' </summary>
        Public MustOverride ReadOnly Property BlockStatement As TypeStatementSyntax

        ''' <summary>
        ''' The statement that ends the block declaration.
        ''' </summary>
        Public MustOverride ReadOnly Property EndBlockStatement As EndBlockStatementSyntax

    End Class

    Partial Friend Class ClassBlockSyntax

        Public Overrides ReadOnly Property BlockStatement As TypeStatementSyntax
            Get
                Return ClassStatement
            End Get
        End Property

        Public Overrides ReadOnly Property EndBlockStatement As EndBlockStatementSyntax
            Get
                Return EndClassStatement
            End Get
        End Property

    End Class

    Partial Friend Class StructureBlockSyntax

        Public Overrides ReadOnly Property BlockStatement As TypeStatementSyntax
            Get
                Return StructureStatement
            End Get
        End Property

        Public Overrides ReadOnly Property EndBlockStatement As EndBlockStatementSyntax
            Get
                Return EndStructureStatement
            End Get
        End Property

    End Class

    Partial Friend Class InterfaceBlockSyntax

        Public Overrides ReadOnly Property BlockStatement As TypeStatementSyntax
            Get
                Return InterfaceStatement
            End Get
        End Property

        Public Overrides ReadOnly Property EndBlockStatement As EndBlockStatementSyntax
            Get
                Return EndInterfaceStatement
            End Get
        End Property

    End Class

    Partial Friend Class ModuleBlockSyntax

        Public Overrides ReadOnly Property BlockStatement As TypeStatementSyntax
            Get
                Return ModuleStatement
            End Get
        End Property

        Public Overrides ReadOnly Property EndBlockStatement As EndBlockStatementSyntax
            Get
                Return EndModuleStatement
            End Get
        End Property

    End Class

End Namespace

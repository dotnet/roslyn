' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend MustInherit Class LabelSymbol
        Inherits Symbol
        Implements ILabelSymbol

        Public Sub New(name As String)
            Me._name = name
        End Sub

        Private ReadOnly _name As String

        Public Overrides ReadOnly Property Name As String
            Get
                Return Me._name
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.NotApplicable
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Friend Overloads Overrides Function Accept(Of TArgument, TResult)(visitor As VisualBasicSymbolVisitor(Of TArgument, TResult), arg As TArgument) As TResult
            Return visitor.VisitLabel(Me, arg)
        End Function

        Public Overridable ReadOnly Property ContainingMethod As MethodSymbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return ContainingMethod
            End Get
        End Property

        Public Overrides ReadOnly Property Kind As SymbolKind
            Get
                Return SymbolKind.Label
            End Get
        End Property

        ' Get the token that defined this label symbol. This is useful for robustly checking
        ' if a label symbol actually matches a particular definition, even in the presence of duplicates.
        Friend Overridable ReadOnly Property LabelName As SyntaxToken
            Get
                Return Nothing
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

#Region "ILabelSymbol"
        Friend ReadOnly Property ILabelSymbol_ContainingMethod As IMethodSymbol Implements ILabelSymbol.ContainingMethod
            Get
                Return ContainingMethod
            End Get
        End Property
#End Region

#Region "ISymbol"

        Public Overrides Sub Accept(visitor As SymbolVisitor)
            visitor.VisitLabel(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitLabel(Me)
        End Function

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As SymbolVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitLabel(Me, argument)
        End Function

        Public Overrides Sub Accept(visitor As VisualBasicSymbolVisitor)
            visitor.VisitLabel(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitLabel(Me)
        End Function

#End Region

    End Class

    Friend Class GeneratedLabelSymbol
        Inherits LabelSymbol

        Public Sub New(name As String)
            MyBase.New(name)
        End Sub

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property
    End Class

    Friend Class GeneratedUnstructuredExceptionHandlingResumeLabel
        Inherits GeneratedLabelSymbol

        ''' <summary>
        ''' A [Resume] or [On Error Resume Next] statement.
        ''' </summary>
        Public ReadOnly ResumeStatement As StatementSyntax

        Public Sub New(resumeStmt As StatementSyntax)
            MyBase.New("$VB$UnstructuredExceptionHandling_TargetResumeLabel")

            Debug.Assert(resumeStmt.Kind = SyntaxKind.OnErrorResumeNextStatement OrElse
                         resumeStmt.Kind = SyntaxKind.ResumeNextStatement OrElse
                         resumeStmt.Kind = SyntaxKind.ResumeStatement)
            ResumeStatement = resumeStmt
        End Sub
    End Class

End Namespace


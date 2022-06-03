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

    Friend NotInheritable Class SourceLabelSymbol
        Inherits LabelSymbol

        Private ReadOnly _labelName As SyntaxToken ' the label name token, this can be an identifier or an integer literal. This is used as its location.
        Private ReadOnly _containingMethod As MethodSymbol
        Private ReadOnly _binder As Binder

        Public Sub New(labelNameToken As SyntaxToken, containingMethod As MethodSymbol, binder As Binder)
            ' Note: use the same method here to get the name as in Binder.BindLabelStatement when looking up the label
            ' Explanation: some methods do not add the type character and return e.g. C for C$ (e.g. GetIdentifierText())
            MyBase.New(labelNameToken.ValueText)
            Debug.Assert(labelNameToken.Kind = SyntaxKind.IdentifierToken OrElse labelNameToken.Kind = SyntaxKind.IntegerLiteralToken)

            _labelName = labelNameToken
            _containingMethod = containingMethod
            _binder = binder
        End Sub

        ' Get the token that defined this label symbol. This is useful for robustly checking
        ' if a label symbol actually matches a particular definition, even in the presence of duplicates.
        Friend Overrides ReadOnly Property LabelName As SyntaxToken
            Get
                Return _labelName
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingMethod As MethodSymbol
            Get
                Return _containingMethod
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingMethod
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray.Create(Of Location)(_labelName.GetLocation())
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Dim parentNode = _labelName.Parent
                Debug.Assert(TypeOf parentNode Is LabelStatementSyntax)
                Return ImmutableArray.Create(Of SyntaxReference)(DirectCast(parentNode.GetReference(), SyntaxReference))
            End Get
        End Property

        Public Overrides Function Equals(obj As Object) As Boolean
            If obj Is Me Then
                Return True
            End If
            Dim symbol = TryCast(obj, SourceLabelSymbol)
            Return symbol IsNot Nothing AndAlso symbol._labelName.Equals(Me._labelName) AndAlso
                Equals(symbol._containingMethod, Me._containingMethod)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Me._labelName.GetHashCode()
        End Function
    End Class
End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.ObjectModel
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend NotInheritable Class BadTokenSyntax
        Inherits PunctuationSyntax

        Private ReadOnly _subKind As SyntaxSubKind

        Friend Sub New(kind As SyntaxKind, subKind As SyntaxSubKind, errors As DiagnosticInfo(), annotations As SyntaxAnnotation(), text As String, leadingTrivia As GreenNode, trailingTrivia As GreenNode)
            MyBase.New(kind, errors, annotations, text, leadingTrivia, trailingTrivia)

            _subKind = subKind
        End Sub

        Friend Sub New(reader As ObjectReader)
            MyBase.New(reader)
            _subKind = CType(reader.ReadUInt16(), SyntaxSubKind)
        End Sub

        Friend Shared Shadows CreateInstance As Func(Of ObjectReader, Object) = Function(o) New BadTokenSyntax(o)

        Friend Overrides Sub WriteTo(writer As ObjectWriter)
            MyBase.WriteTo(writer)
            writer.WriteUInt16(CType(_subKind, UShort))
        End Sub

        Shared Sub New()
            ObjectBinder.RegisterTypeReader(GetType(BadTokenSyntax), Function(r) New BadTokenSyntax(r))
        End Sub

        Friend ReadOnly Property SubKind As SyntaxSubKind
            Get
                Return _subKind
            End Get
        End Property

        Public Overrides Function WithLeadingTrivia(trivia As GreenNode) As GreenNode
            Return New BadTokenSyntax(Kind, SubKind, GetDiagnostics, GetAnnotations, Text, trivia, GetTrailingTrivia)
        End Function

        Public Overrides Function WithTrailingTrivia(trivia As GreenNode) As GreenNode
            Return New BadTokenSyntax(Kind, SubKind, GetDiagnostics, GetAnnotations, Text, GetLeadingTrivia, trivia)
        End Function

        Friend Overrides Function SetDiagnostics(newErrors As DiagnosticInfo()) As GreenNode
            Return New BadTokenSyntax(Kind, SubKind, newErrors, GetAnnotations, Text, GetLeadingTrivia, GetTrailingTrivia)
        End Function

        Friend Overrides Function SetAnnotations(annotations As SyntaxAnnotation()) As GreenNode
            Return New BadTokenSyntax(Kind, SubKind, GetDiagnostics, annotations, Text, GetLeadingTrivia, GetTrailingTrivia)
        End Function
    End Class

End Namespace

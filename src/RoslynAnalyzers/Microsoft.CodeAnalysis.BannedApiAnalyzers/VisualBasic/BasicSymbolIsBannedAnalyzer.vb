' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.BannedApiAnalyzers

Namespace Microsoft.CodeAnalysis.VisualBasic.BannedApiAnalyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicSymbolIsBannedAnalyzer
        Inherits SymbolIsBannedAnalyzer(Of SyntaxKind)

        Protected Overrides ReadOnly Property XmlCrefSyntaxKind As SyntaxKind
            Get
                Return SyntaxKind.XmlCrefAttribute
            End Get
        End Property

        Protected Overrides ReadOnly Property BaseTypeSyntaxKinds As ImmutableArray(Of SyntaxKind)
            Get
                Return ImmutableArray.Create(SyntaxKind.InheritsStatement, SyntaxKind.ImplementsStatement)
            End Get
        End Property

        Protected Overrides ReadOnly Property SymbolDisplayFormat As SymbolDisplayFormat
            Get
                Return SymbolDisplayFormat.VisualBasicShortErrorMessageFormat
            End Get
        End Property

        Protected Overrides Function GetReferenceSyntaxNodeFromXmlCref(syntaxNode As SyntaxNode) As SyntaxNode
            Return CType(syntaxNode, XmlCrefAttributeSyntax).Reference
        End Function

        Protected Overrides Function GetTypeSyntaxNodesFromBaseType(syntaxNode As SyntaxNode) As IEnumerable(Of SyntaxNode)
            If syntaxNode.IsKind(SyntaxKind.InheritsStatement) Then
                Return CType(syntaxNode, InheritsStatementSyntax).Types
            ElseIf syntaxNode.IsKind(SyntaxKind.ImplementsStatement) Then
                Return CType(syntaxNode, ImplementsStatementSyntax).Types
            Else
                Return ImmutableArray(Of SyntaxNode).Empty
            End If
        End Function

    End Class
End Namespace

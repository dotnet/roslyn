' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Analyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicSymbolIsBannedInAnalyzersAnalyzer
        Inherits SymbolIsBannedInAnalyzersAnalyzer(Of SyntaxKind)

        Protected Overrides ReadOnly Property XmlCrefSyntaxKind As SyntaxKind
            Get
                Return SyntaxKind.XmlCrefAttribute
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

    End Class
End Namespace
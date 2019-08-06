' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
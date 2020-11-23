' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FileHeaders
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.FileHeaders
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicFileHeaderDiagnosticAnalyzer
        Inherits AbstractFileHeaderDiagnosticAnalyzer

        Public Sub New()
            MyBase.New(LanguageNames.VisualBasic)
        End Sub

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts
            Get
                Return VisualBasicSyntaxFacts.Instance
            End Get
        End Property
    End Class
End Namespace

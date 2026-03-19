' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting

Namespace Microsoft.CodeAnalysis.CodeStyle
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicFormattingAnalyzer
        Inherits AbstractFormattingAnalyzer

        Protected Overrides ReadOnly Property SyntaxFormatting As ISyntaxFormatting
            Get
                Return VisualBasicSyntaxFormatting.Instance
            End Get
        End Property
    End Class
End Namespace

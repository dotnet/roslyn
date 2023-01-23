' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.SimplifyInterpolation
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.VirtualChars
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyInterpolation
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicSimplifyInterpolationDiagnosticAnalyzer
        Inherits AbstractSimplifyInterpolationDiagnosticAnalyzer(Of InterpolationSyntax, ExpressionSyntax)

        Protected Overrides Function GetHelpers() As AbstractSimplifyInterpolationHelpers
            Return VisualBasicSimplifyInterpolationHelpers.Instance
        End Function

        Protected Overrides Function GetVirtualCharService() As IVirtualCharService
            Return VisualBasicVirtualCharService.Instance
        End Function

        Protected Overrides Function GetSyntaxFacts() As ISyntaxFacts
            Return VisualBasicSyntaxFacts.Instance
        End Function
    End Class
End Namespace

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
    Friend NotInheritable Class VisualBasicSimplifyInterpolationDiagnosticAnalyzer
        Inherits AbstractSimplifyInterpolationDiagnosticAnalyzer(Of InterpolationSyntax, ExpressionSyntax)

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance
        Protected Overrides ReadOnly Property VirtualCharService As IVirtualCharService = VisualBasicVirtualCharService.Instance
        Protected Overrides ReadOnly Property Helpers As AbstractSimplifyInterpolationHelpers(Of InterpolationSyntax, ExpressionSyntax) = VisualBasicSimplifyInterpolationHelpers.Instance
    End Class
End Namespace

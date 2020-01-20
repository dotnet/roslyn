' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
Imports Microsoft.CodeAnalysis.SimplifyInterpolation
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.VirtualChars
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyInterpolation
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicSimplifyInterpolationDiagnosticAnalyzer
        Inherits AbstractSimplifyInterpolationDiagnosticAnalyzer(Of InterpolationSyntax, ExpressionSyntax)

        Protected Overrides Function GetVirtualCharService() As IVirtualCharService
            Return VisualBasicVirtualCharService.Instance
        End Function
    End Class
End Namespace

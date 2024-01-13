' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.RemoveRedundantEquality
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveRedundantEquality
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicRemoveRedundantEqualityDiagnosticAnalyzer
        Inherits AbstractRemoveRedundantEqualityDiagnosticAnalyzer
        Public Sub New()
            MyBase.New(VisualBasicSyntaxFacts.Instance)
        End Sub
    End Class
End Namespace

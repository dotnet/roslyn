' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.VisualBasic.ParenthesesForMethodInvocations
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicAddParenthesesAnalyzer
        Inherits VisualBasicParenthesesDiagnosticAnalyzerBase

        Private Shared ReadOnly s_title As New LocalizableResourceString(
            NameOf(VisualBasicAnalyzersResources.Add_parentheses_to_method_invocation),
            VisualBasicAnalyzersResources.ResourceManager,
            GetType(VisualBasicAnalyzersResources))

        Public Sub New()
            MyBase.New(IDEDiagnosticIds.AddParenthesesForMethodInvocationsDiagnosticId, s_title, s_title)
        End Sub
    End Class
End Namespace

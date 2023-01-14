' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVB = Microsoft.CodeAnalysis.VisualBasic.Testing.VisualBasicCodeFixVerifier(
    Of Microsoft.CodeAnalysis.CodeStyle.VisualBasicFormattingAnalyzer,
    Microsoft.CodeAnalysis.CodeStyle.VisualBasicFormattingCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier)

Namespace Microsoft.CodeAnalysis.CodeStyle
    Public Class FormattingAnalyzerTests
        <Fact>
        Public Async Function TestCaseCorrection() As Task
            Dim testCode = "
class TestClass
    public Sub MyMethod()
    End Sub
end class
"

            ' Currently the analyzer and code fix rely on Formatter.FormatAsync, which does not normalize the casing of
            ' keywords in Visual Basic.
            Await VerifyVB.VerifyAnalyzerAsync(testCode)
        End Function
    End Class
End Namespace

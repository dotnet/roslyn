' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    ' this place is dedicated to scan related error tests
    Public Class ScanErrorTests

#Region "Targeted Error Tests - please arrange tests in the order of error code"

        <WorkItem(897923, "DevDiv/Personal")>
        <Fact>
        Public Sub BC31170ERR_IllegalXmlNameChar()
            Dim nameText = "Nc#" & ChrW(7) & "Name"
            Dim fullText = "<" & nameText & "/>"

            Dim t = DirectCast(SyntaxFactory.ParseExpression(fullText), XmlEmptyElementSyntax)

            Assert.Equal(fullText, t.ToFullString())
            Assert.Equal(True, t.ContainsDiagnostics)
            Assert.Equal(3, t.GetSyntaxErrorsNoTree.Count)
            Assert.Equal(31170, t.GetSyntaxErrorsNoTree(2).Code)
        End Sub

        <Fact>
        Public Sub BC36716ERR_LanguageVersion_BinaryLiterals()
            ParseAndVerify("&B1",
                BasicTestBase.Diagnostic(ERRID.ERR_ObsoleteLineNumbersAreLabels, "&B1").WithLocation(1, 1),
                BasicTestBase.Diagnostic(ERRID.ERR_InvOutsideProc, "&B1").WithLocation(1, 1),
                BasicTestBase.Diagnostic(ERRID.ERR_LanguageVersion, "&B1").WithArguments("14.0", "binary literals").WithLocation(1, 1))
        End Sub

        <Fact>
        Public Sub BC36716ERR_LanguageVersion_DigitSeperators()
            ParseAndVerify("1_000",
                BasicTestBase.Diagnostic(ERRID.ERR_ObsoleteLineNumbersAreLabels, "1_000").WithLocation(1, 1),
                BasicTestBase.Diagnostic(ERRID.ERR_InvOutsideProc, "1_000").WithLocation(1, 1),
                BasicTestBase.Diagnostic(ERRID.ERR_LanguageVersion, "1_000").WithArguments("14.0", "digit separators").WithLocation(1, 1))
        End Sub

#End Region

#Region "Mixed Error Tests"

        <WorkItem(881821, "DevDiv/Personal")>
        <Fact>
        Public Sub BC30004ERR_IllegalCharConstant_ScanTwoCharLiteralFollowedByQuote1()
            ParseAndVerify(<![CDATA[
                "  "C
                "
            ]]>,
            <errors>
                <error id="30648"/>
                <error id="30004"/>
            </errors>)
        End Sub
#End Region

    End Class
End Namespace


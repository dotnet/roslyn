' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
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


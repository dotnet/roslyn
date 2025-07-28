' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

<CLSCompliant(False)>
Public Class ParseImports
    Inherits BasicTestBase

    <Fact>
    Public Sub ParseImportsPass()
        ParseAndVerify(<![CDATA[
            Imports System.Text
            Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
            Imports Roslyn.Compilers.VisualBasic
        ]]>)

        ParseAndVerify(<![CDATA[
                Imports t1 = System.Text
                Imports t2 = 
                    Microsoft.Languages.Text
                Imports s1 = Microsoft.VisualBasic.Syntax
        ]]>)
    End Sub

    <Fact>
    Public Sub BC31398ERR_NoTypecharInAlias_ParseImportsFail()
        ParseAndVerify(<![CDATA[
            Imports s$ = System.Text
        ]]>,
        <errors>
            <error id="31398"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub BC30203ERR_ExpectedIdentifier_Bug863004()
        ParseAndVerify(<![CDATA[
            Imports (ModImpErrGen003.Scen25) =
            Default Imports
        ]]>,
        <errors>
            <error id="30193"/>
            <error id="30203"/>
            <error id="30203"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub BC30180ERR_UnrecognizedTypeKeyword_Bug869085()
        ParseAndVerify(<![CDATA[
            imports ns1.GenClass(of of string)
        ]]>,
        <errors>
            <error id="30180"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub ParseXmlNamespace()
        ParseAndVerify(<![CDATA[
            imports <xmlns:p="ns1">
        ]]>)
    End Sub

    <Fact>
    Public Sub ParseXmlNamespaceStart()
        ParseAndVerify(<![CDATA[
            imports <
        ]]>,
            Diagnostic(ERRID.ERR_ExpectedXmlns, ""),
            Diagnostic(ERRID.ERR_ExpectedGreater, "")
        )
    End Sub

    <Fact>
    Public Sub BC31187ERR_ExpectedXmlns()
        ParseAndVerify(<![CDATA[
            imports <xml:p="ns1">
        ]]>,
        Diagnostic(ERRID.ERR_ExpectedXmlns, ""),
        Diagnostic(ERRID.ERR_ExpectedGreater, "xml"))
    End Sub

    <Fact>
    Public Sub BC31187ERR_ExpectedXmlns_2()
        ParseAndVerify(<![CDATA[
            imports <p:xmlns="ns1">
        ]]>,
        Diagnostic(ERRID.ERR_ExpectedXmlns, ""),
        Diagnostic(ERRID.ERR_ExpectedGreater, "p"))
    End Sub

    <WorkItem(879682, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30465ERR_ImportsMustBeFirst()
        ParseAndVerify(<![CDATA[
            Class Class1
            End Class
            Imports System
        ]]>,
        <errors>
            <error id="30465"/>
        </errors>)
    End Sub

    <WorkItem(541486, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541486")>
    <Fact>
    Public Sub ImportsAliasMissingIdentifier()
        Dim tree = ParseAndVerify(<![CDATA[
            Imports = System
        ]]>,
        <errors>
            <error id="30203"/>
        </errors>)

        VerifySyntaxKinds(tree.GetRoot().DescendantNodes.OfType(Of ImportsStatementSyntax).First,
                          SyntaxKind.ImportsStatement,
                                SyntaxKind.ImportsKeyword,
                                SyntaxKind.SimpleImportsClause,
                                      SyntaxKind.ImportAliasClause,
                                            SyntaxKind.IdentifierToken,
                                            SyntaxKind.EqualsToken,
                                      SyntaxKind.IdentifierName,
                                            SyntaxKind.IdentifierToken)
    End Sub

    <WorkItem(541486, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541486")>
    <Fact()>
    Public Sub ImportsMissingIdentifierBeforeComma()
        Dim tree = ParseAndVerify(<![CDATA[
            Imports ,System
        ]]>,
        <errors>
            <error id="30203"/>
        </errors>)

        VerifySyntaxKinds(tree.GetRoot().DescendantNodes.OfType(Of ImportsStatementSyntax).First,
                          SyntaxKind.ImportsStatement,
                                SyntaxKind.ImportsKeyword,
                                SyntaxKind.SimpleImportsClause,
                                        SyntaxKind.IdentifierName,
                                            SyntaxKind.IdentifierToken,
                                SyntaxKind.CommaToken,
                                SyntaxKind.SimpleImportsClause,
                                        SyntaxKind.IdentifierName,
                                            SyntaxKind.IdentifierToken)
    End Sub

    <WorkItem(541803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541803")>
    <Fact>
    Public Sub AnotherImportsAfterComma()
        Dim tree = ParseAndVerify(<![CDATA[
Imports System.Collections.Generic,
                System  ' no errors here

Imports System.Collections,
Imports System
        ]]>,
        <errors>
            <error id="30183"/>
        </errors>)
    End Sub

End Class

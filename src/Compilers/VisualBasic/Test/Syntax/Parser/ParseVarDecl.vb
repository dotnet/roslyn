' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

<CLSCompliant(False)>
Public Class ParseVarDecl
    Inherits BasicTestBase

    <Fact>
    Public Sub ParseBasicVarDecls()
        ParseAndVerify(<![CDATA[
            Module Module1
                dim i as integer
                dim s as string
                dim j() as integer
                dim k(,,) as integer
                dim m(10) as integer
                dim n(10)(,,) as integer

                public public_i as integer
                friend friend_i as integer
                shared shared_i as integer
                protected protected_i as integer

                <clscompliant(true)> dim attribute_i as integer
            End Module

            Class C1
                dim i as integer
                dim s as string
                dim j() as integer
                dim k(,,) as integer
                dim m(10) as integer
                dim n(10)(,,) as integer

                public public_i as integer
                friend friend_i as integer
                shared shared_i as integer
                protected protected_i as integer

                <clscompliant(true)> dim attribute_i as integer
            End Class

            Structure S1
                dim i as integer
                dim s as string
                dim j() as integer
                dim k(,,) as integer
                dim m(10) as integer
                dim n(10)(,,) as integer

                public public_i as integer
                friend friend_i as integer
                shared shared_i as integer
                protected protected_i as integer

                <clscompliant(true)> dim attribute_i as integer
            End Structure
        ]]>)
    End Sub

    <Fact>
    Public Sub ParseArrayDecl()
        ParseAndVerify(<![CDATA[
            Class C1
                Private i1(10) As Integer
                Private i2(,) As Integer
                Private i3(10)(,,) As Integer
                Private i4 As Integer() = {1,2,3} 
                Private i5 As New Integer()
                'Private c1 = New List(Of Integer) From {1, 2, 3} 'ParseTerm does not support New expression yet
                private c2 as new customer with {.a = 1, .b = 2, .c = 3}
            End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug862143()
        ParseAndVerify(<![CDATA[
            Module Module1
                Dim v1 = 1, v2 = 2
            End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug863104()
        ParseAndVerify(<![CDATA[
            Class ClassA
                Dim scen1 As Integer = 1
                Public scen2 = New With {.x = New With {scen2}}
            End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug868003()
        ' Collection initializers for field initialization is not supported, contextual keyword 'from' was not parsed correctly
        ParseAndVerify(<![CDATA[
            Class Class1
                Dim errorList As New System.Collections.Generic.List(Of Integer) From {36534}
            End Class
        ]]>)
    End Sub


    <Fact>
    Public Sub Bug869104()
        ' Custom Keyword should be contextual (only preceding Event keyword)
        ParseAndVerify(<![CDATA[
            Class Class1
                Public Const Custom As String = "custom"
            End Class
        ]]>)
    End Sub

    <WorkItem(898582, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseIncorrectShorArr()
        ParseAndVerify(<![CDATA[
            Sub ArExtNewErr001()
FixedRankArray_19 = New Short() (1,
        ]]>,
        <errors>
            <error id="30987"/>
            <error id="30198"/>
            <error id="32014"/>
            <error id="30026"/>
        </errors>)
    End Sub
    <WorkItem(885792, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30988ERR_UnrecognizedTypeOrWith_MismatchVSUnrecognizedType()
        ParseAndVerify(<![CDATA[
                        Class Class1
                         Sub Main()
                         Dim obj = new {Key .prop1 = "123"}
                         End Sub
                        End Class

            ]]>,
            <errors>
                <error id="30988"/>
            </errors>)
    End Sub

    <WorkItem(527021, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527021")>
    <Fact>
    Public Sub BC30036_ParseErrorMismatchExpectedExpression()
        ParseAndVerify(<![CDATA[
                          Module Module1
                             Dim sb7 As ULong = 18446744073709551616UL
                             Dim sb71 As ULong = 18446744073709551617UL
                          End Module
            ]]>,
            <errors>
                <error id="30036"/>
                <error id="30036"/>
            </errors>)
    End Sub

    <WorkItem(538746, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538746")>
    <Fact>
    Public Sub ParseDecimalLiteralWithExponent()
        ParseAndVerify(<![CDATA[
    Module M
        Sub Main()
            Dim x = 1.0e28d
        End Sub
    End Module
            ]]>)

        Dim d As Decimal = 0
        If (Decimal.TryParse("0E1", Globalization.NumberStyles.AllowExponent, Nothing, d)) Then
            ParseAndVerify(<![CDATA[
    Module M
        Sub Main()
            Dim x = 0.0e28d
        End Sub
    End Module
            ]]>)
        Else
            ParseAndVerify(<![CDATA[
    Module M
        Sub Main()
            Dim x = 0.0e28d
        End Sub
    End Module
            ]]>,
                 <errors>
                     <error id="30036" message="Overflow."/>
                 </errors>)
        End If
    End Sub

    <WorkItem(541293, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541293")>
    <Fact>
    Public Sub ParsePropertyWithFromInitializer()
        ParseAndVerify(<![CDATA[
Module Program

    Property)As New t(Of Integer) From {1, 2, 3}

    Public Property P1 As New List(Of Integer) from {1, 2, 3}

    Public Property P2 as New List(of Integer)
        From {1,2,3}

End Module
            ]]>,
            <errors>
                <error id="30203" message="Identifier expected." start="29" end="29"/>
            </errors>)
    End Sub

    <WorkItem(541293, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541293")>
    <Fact>
    Public Sub ParsePropertyWithFromInitializer_2()
        ParseAndVerify(<![CDATA[
Class C
    Property P As New C
        From {1,2,3}
    Property Q As New C(Nothing)
        From {1,2,3}
End Class
]]>)
    End Sub

    <WorkItem(543755, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543755")>
    <Fact()>
    Public Sub Bug11682()

        ParseAndVerify(<![CDATA[
Module Program
    Sub Main()
        Dim [foo as integer = 23 
        Dim [goo As Char = "d"c

        Dim [ as integer = 23 
        Dim [ As Char = "d"c

        Dim [] as integer = 23 
        Dim [] As Char = "d"c

        Dim [$] as integer = 23 
        Dim [%] As Char = "d"c

        Dim [[]] as integer = 23 
    End Sub
End Module
            ]]>, Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
                 Diagnostic(ERRID.ERR_MissingEndBrack, "[foo"),
                 Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
                 Diagnostic(ERRID.ERR_MissingEndBrack, "[goo"),
                 Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
                 Diagnostic(ERRID.ERR_ExpectedIdentifier, "["),
                 Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
                 Diagnostic(ERRID.ERR_ExpectedIdentifier, "["),
                 Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
                 Diagnostic(ERRID.ERR_ExpectedIdentifier, "[]"),
                 Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
                 Diagnostic(ERRID.ERR_ExpectedIdentifier, "[]"),
                 Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
                 Diagnostic(ERRID.ERR_ExpectedIdentifier, "["),
                 Diagnostic(ERRID.ERR_IllegalChar, "$"),
                 Diagnostic(ERRID.ERR_IllegalChar, "]"),
                 Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
                 Diagnostic(ERRID.ERR_ExpectedIdentifier, "["),
                 Diagnostic(ERRID.ERR_IllegalChar, "%"),
                 Diagnostic(ERRID.ERR_IllegalChar, "]"),
                 Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
                 Diagnostic(ERRID.ERR_ExpectedIdentifier, "["),
                 Diagnostic(ERRID.ERR_ExpectedIdentifier, "[]"),
                 Diagnostic(ERRID.ERR_IllegalChar, "]"))
    End Sub

End Class

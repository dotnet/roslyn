' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine.UnitTests
    Public Class CommandLineArgumentsTests
        Inherits BasicTestBase

        <Fact()>
        <WorkItem(543297, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543297")>
        <WorkItem(546751, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546751")>
        Public Sub TestParseConditionalCompilationSymbols1()
            Dim errors As IEnumerable(Of Diagnostic) = Nothing
            Dim text As String
            Dim dict As IReadOnlyDictionary(Of String, Object)

            text = "Nightly=1, Alpha=2, Beta=3, RC=4, Release=5, Config=Nightly, Config2=""Nightly"", Framework = 4"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            Assert.Equal(8, dict.Count)
            Assert.Equal(1, dict("Config"))
            Assert.Equal(GetType(Integer), (dict("Config")).GetType)
            Assert.Equal("Nightly", dict("Config2"))
            Assert.Equal(GetType(String), (dict("Config2")).GetType)
            Assert.Equal(4, dict("Framework"))
            Assert.Equal(GetType(Integer), (dict("Framework")).GetType)
            Assert.Equal(2, dict("Alpha"))
            Assert.Equal(GetType(Integer), (dict("Alpha")).GetType)

            text = "OnlyEqualsNoValue1, OnlyEqualsNoValue2"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            Assert.Equal(2, dict.Count)
            Assert.Equal(True, dict("OnlyEqualsNoValue1"))
            Assert.Equal(GetType(Boolean), (dict("OnlyEqualsNoValue1")).GetType)
            Assert.Equal(True, dict("OnlyEqualsNoValue2"))
            Assert.Equal(GetType(Boolean), (dict("OnlyEqualsNoValue2")).GetType)

            text = ",,,,,foo=bar,,,,,,,,,,"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            Assert.Equal(1, dict.Count)

            text = ",,,=,,,"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            Assert.Equal(0, dict.Count)
            Assert.Equal(1, errors.Count)
            errors.Verify(Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("Identifier expected.", "= ^^ ^^ "))

            Dim previousSymbols As New Dictionary(Of String, Object)() From {{"Foo", 1}, {"Bar", "Foo"}}
            text = ",,,=,,,"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors, previousSymbols)
            Assert.Equal(2, dict.Count)
            Assert.Equal(1, errors.Count)
            errors.Verify(Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("Identifier expected.", "= ^^ ^^ "))

            text = "OnlyEqualsNoValue1=, Bar=foo"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            Assert.Equal(0, dict.Count)
            Assert.Equal(1, errors.Count)
            errors.Verify(Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("Expression expected.", "OnlyEqualsNoValue1= ^^ ^^ "))

            text = "Bar=foo, OnlyEqualsNoValue1="
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            Assert.Equal(1, dict.Count)
            Assert.Equal(1, errors.Count)
            errors.Verify(Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("Expression expected.", "OnlyEqualsNoValue1= ^^ ^^ "))

            text = """""OnlyEqualsNoValue1"""""
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            Assert.Equal(1, dict.Count)
            Assert.Equal(True, dict("OnlyEqualsNoValue1"))
            Assert.Equal(GetType(Boolean), (dict("OnlyEqualsNoValue1")).GetType)

            text = "key=\""value\"""
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            Assert.Equal(1, dict.Count)
            Assert.Equal("value", dict("key"))
            Assert.Equal(GetType(String), (dict("key")).GetType)

            text = "then=bar" ' keyword :)
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            Assert.Equal(1, errors.Count)
            errors.Verify(Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("Identifier expected.", "then ^^ ^^ =bar"))

            text = "bar=then" ' keyword :)
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            Assert.Equal(1, errors.Count)
            errors.Verify(Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("Syntax error in conditional compilation expression.", "bar= ^^ ^^ then"))

            text = "FOO:BAR"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            Assert.Equal(2, dict.Count)
            Assert.Equal(True, dict("FOO"))
            Assert.Equal(GetType(Boolean), (dict("FOO")).GetType)
            Assert.Equal(True, dict("BAR"))
            Assert.Equal(GetType(Boolean), (dict("BAR")).GetType)

            text = "FOO::::::::BAR"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            Assert.Equal(2, dict.Count)
            Assert.Equal(True, dict("FOO"))
            Assert.Equal(GetType(Boolean), (dict("FOO")).GetType)
            Assert.Equal(True, dict("BAR"))
            Assert.Equal(GetType(Boolean), (dict("BAR")).GetType)

            text = "FOO=23::,,:::BAR"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            Assert.Equal(1, errors.Count)
            errors.Verify(Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("Identifier expected.", "FOO=23:: ^^ , ^^ ,:::"))

            text = "FOO=23,:BAR"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            Assert.Equal(1, errors.Count)
            errors.Verify(Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("Identifier expected.", "FOO=23, ^^ : ^^ BAR"))

            text = "FOO::BAR,,BAZ"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            Assert.Equal(3, dict.Count)
            Assert.Equal(True, dict("FOO"))
            Assert.Equal(GetType(Boolean), (dict("FOO")).GetType)
            Assert.Equal(True, dict("BAR"))
            Assert.Equal(GetType(Boolean), (dict("BAR")).GetType)
            Assert.Equal(True, dict("BAZ"))
            Assert.Equal(GetType(Boolean), (dict("BAZ")).GetType)
        End Sub

        <Fact()>
        Public Sub TestParseConditionalCompilationSymbols_expressions()
            Dim errors As IEnumerable(Of Diagnostic) = Nothing
            Dim text As String
            Dim dict As IReadOnlyDictionary(Of String, Object)

            text = "bar=1+2"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            errors.Verify()
            Assert.Equal(3, dict("bar"))

            text = "bar=1.2+1.0"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            errors.Verify()
            Assert.Equal(2.2, dict("bar"))

            text = "foo=1,bar=foo+2"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            errors.Verify()
            Assert.Equal(3, dict("bar"))

            text = "bar=foo+2,foo=1"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            errors.Verify()
            Assert.Equal(2, dict("bar")) ' foo is known, but not yet initialized

            ' dev 10 does not crash here, not sure what the value is
            text = "bar=1/0"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            errors.Verify()

            text = "A=""A"",B=""B"",T=IF(1>0, A, B)+B+""C"""
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            errors.Verify()
            Assert.Equal(0, errors.Count)
            Assert.Equal("A", dict("A"))
            Assert.Equal("B", dict("B"))
            Assert.Equal("ABC", dict("T"))

            text = "BAR0=nothing, BAR1=2#, BAR2=000.03232323@, BAR3A=True, BAR3b=BAR3A OrElse False"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            errors.Verify()
            Assert.Equal(5, dict.Count)
            Assert.Equal(Nothing, dict("BAR0"))
            'Assert.Equal(GetType(Object), (dict("BAR0")).GetType)
            Assert.Equal(2.0#, dict("BAR1"))
            Assert.Equal(GetType(Double), (dict("BAR1")).GetType)
            Assert.Equal(0.03232323@, dict("BAR2"))
            Assert.Equal(GetType(Decimal), (dict("BAR2")).GetType)
            Assert.Equal(True, dict("BAR3b"))
            Assert.Equal(GetType(Boolean), (dict("BAR3b")).GetType)

            text = "A=""A"",B=""B"",T=IF(1>0, A, B)+B+""C"",RRR=1+""3"""
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            errors.Verify(Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("Conversion from 'String' to 'Double' cannot occur in a constant expression.", "RRR=1+""3"" ^^ ^^ "))

            text = "A=""A"",B=""B"",T=IF(1>0, A, B)+B+""C"",X=IF(1,,,,,RRR=1"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            errors.Verify(
                Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("')' expected.", "X=IF(1,,,,,RRR=1 ^^ ^^ "),
                Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("'If' operator requires either two or three operands.", "X=IF(1,,,,,RRR=1 ^^ ^^ "),
                Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("Expression expected.", "X=IF(1,,,,,RRR=1 ^^ ^^ "))

            text = "A=CHR(128)"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            errors.Verify(Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("End of statement expected.", "A=CHR ^^ ^^ (128)"))

            text = "A=ASCW(""G"")"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            errors.Verify(Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("End of statement expected.", "A=ASCW ^^ ^^ (""G"")"))

            text = "A=1--1,B=1 1"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            errors.Verify(Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("End of statement expected.", "B=1  ^^ ^^ 1"))

            text = "A=1--1,B=1 C=1"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            errors.Verify(Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("End of statement expected.", "B=1  ^^ ^^ C=1"))

            text = "A=111111111111111111111111"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            errors.Verify(Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("Overflow.", "A=111111111111111111111111 ^^ ^^ "))

            text = "A= 2 + " + vbCrLf + "2"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            errors.Verify(Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("Syntax error in conditional compilation expression.", "A= 2 + " + vbCrLf + " ^^ ^^ 2"))

            text = "A= 2 + _" + vbCrLf + "2"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            errors.Verify()
            Assert.Equal(1, dict.Count)
            Assert.Equal(4, dict("A"))
        End Sub

        <Fact(), WorkItem(546034, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546034"), WorkItem(780817, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/780817")>
        Public Sub TestParseConditionalCompilationCaseInsensitiveSymbols()
            Dim errors As IEnumerable(Of Diagnostic) = Nothing
            Dim text = "Blah,blah"
            Dim dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            Assert.Equal(0, errors.Count)
            Assert.Equal(1, dict.Count)
            Assert.Equal(True, dict("Blah"))
            Assert.Equal(True, dict("blah"))

            Dim source =
<compilation name="TestArrayLiteralInferredType">
    <file name="a.vb">
        <![CDATA[
Imports System

Module M1
    Sub Main
        #if Blah
            Console.WriteLine("Blah")
        #end if
        #if blah
            Console.WriteLine("blah")
        #end if
    End Sub
End Module
]]></file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source,
                                                                                               options:=TestOptions.ReleaseExe,
                                                                                               parseOptions:=New VisualBasicParseOptions(preprocessorSymbols:=dict.AsImmutable()))
            CompileAndVerify(comp,
                             expectedOutput:=<![CDATA[
Blah
blah
            ]]>)

            text = "Blah=false,blah=true"
            dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            Assert.Equal(0, errors.Count)
            Assert.Equal(1, dict.Count)
            Assert.Equal(True, dict("Blah"))
            Assert.Equal(True, dict("blah"))

            comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source,
                                                                                               options:=TestOptions.ReleaseExe,
                                                                                               parseOptions:=New VisualBasicParseOptions(preprocessorSymbols:=dict.AsImmutable()))
            CompileAndVerify(comp,
                             expectedOutput:=<![CDATA[
Blah
blah
            ]]>)
        End Sub

        <Fact()>
        <WorkItem(546035, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546035")>
        Public Sub TestParseConditionalCompilationSymbolsInSingleQuote()
            Dim errors As IEnumerable(Of Diagnostic) = Nothing

            Dim text = "'Blah'"
            Dim dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            errors.Verify(Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("Identifier expected.", " ^^ 'Blah' ^^ "))
        End Sub
    End Class
End Namespace

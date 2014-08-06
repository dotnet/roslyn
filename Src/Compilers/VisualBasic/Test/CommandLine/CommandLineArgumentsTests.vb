' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine.UnitTests
    Public Class CommandLineArgumentsTests
        Inherits BasicTestBase

        <Fact()>
        <WorkItem(543297, "DevDiv")>
        <WorkItem(546751, "DevDiv")>
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

        <Fact(), WorkItem(546034, "DevDiv"), WorkItem(780817, "DevDiv")>
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
        <WorkItem(546035, "DevDiv")>
        Public Sub TestParseConditionalCompilationSymbolsInSingleQuote()
            Dim errors As IEnumerable(Of Diagnostic) = Nothing

            Dim text = "'Blah'"
            Dim dict = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(text, errors)
            errors.Verify(Diagnostic(ERRID.ERR_ProjectCCError1).WithArguments("Identifier expected.", " ^^ 'Blah' ^^ "))
        End Sub

        <WorkItem(546536, "DevDiv")>
        <Fact()>
        Sub AllDev11WarningsAreStillWarnings()
            ' the warning numbers have been taken from the Dev11 sources (vb\include\errors.inc)
            Dim allDev11Warnings() As Integer = {40000, 40003, 40004, 40005, 40007, 40008, 40009, 40010, 40011, 40012, 40014, 40018, 40019, 40020, 40021,
                                                 40022, 40023, 40024, 40025, 40026, 40027, 40028, 40029, 40030, 40031, 40032, 40033, 40034, 40035, 40038,
                                                 40039, 40040, 40041, 40042, 40043, 40046, 40047, 40048, 40049, 40050, 40051, 40052, 40053, 40054, 40055,
                                                 40056, 40057, 40059, 41000, 41001, 41002, 41003, 41004, 41005, 41007, 41008, 41998, 41999, 42000, 42001,
                                                 42002, 42004, 42014, 42015, 42016, 42017, 42018, 42019, 42020, 42021, 42022, 42024, 42025, 42026, 42029,
                                                 42030, 42031, 42032, 42033, 42034, 42035, 42036, 42037, 42038, 42099, 42101, 42102, 42104, 42105, 42106,
                                                 42107, 42108, 42109, 42110, 42111, 42203, 42204, 42205, 42206, 42300, 42301, 42302, 42303, 42304, 42305,
                                                 42306, 42307, 42308, 42309, 42310, 42311, 42312, 42313, 42314, 42315, 42316, 42317, 42318, 42319, 42320,
                                                 42321, 42322, 42324, 42326, 42327, 42328, 42332, 42333, 42334, 42335, 42336, 42337, 42338, 42339, 42340,
                                                 42341, 42342, 42343, 42344, 42345, 42346, 42347, 42348, 42349, 42350, 42351, 42352, 42353, 42354, 42355}

            ' the command line warnings (vb\language\commandlineerrors.inc) 
            Dim allDev11CommandLineWarnings() As Integer = {2002, 2007, 2025, 2028, 2034}   'note 2024 was never reachable in the native compiler

            For Each warningNumber In allDev11Warnings
                Assert.Equal(DiagnosticSeverity.Warning, Global.Microsoft.CodeAnalysis.VisualBasic.MessageProvider.Instance.GetSeverity(warningNumber))
            Next

            For Each warningNumber In allDev11CommandLineWarnings
                Assert.Equal(DiagnosticSeverity.Warning, Global.Microsoft.CodeAnalysis.VisualBasic.MessageProvider.Instance.GetSeverity(warningNumber))
            Next
        End Sub
    End Class
End Namespace

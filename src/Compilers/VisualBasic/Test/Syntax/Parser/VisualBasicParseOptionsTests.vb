' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Linq
Imports Roslyn.Test.Utilities

Public Class VisualBasicParseOptionsTests
    Inherits BasicTestBase

    Private Sub TestProperty(Of T)(factory As Func(Of VisualBasicParseOptions, T, VisualBasicParseOptions), getter As Func(Of VisualBasicParseOptions, T), validValue As T)
        Dim oldOpt1 = VisualBasicParseOptions.Default
        Dim newOpt1 = factory(oldOpt1, validValue)
        Dim newOpt2 = factory(newOpt1, validValue)
        Assert.Equal(validValue, getter(newOpt1))
        Assert.Same(newOpt2, newOpt1)
    End Sub

    <Fact>
    Public Sub WithXxx()
        TestProperty(Function(old, value) old.WithKind(value), Function(opt) opt.Kind, SourceCodeKind.Script)
        TestProperty(Function(old, value) old.WithLanguageVersion(value), Function(opt) opt.LanguageVersion, LanguageVersion.VisualBasic9)
        TestProperty(Function(old, value) old.WithDocumentationMode(value), Function(opt) opt.DocumentationMode, DocumentationMode.None)

        Assert.Throws(Of ArgumentOutOfRangeException)(Function() VisualBasicParseOptions.Default.WithKind(DirectCast(Integer.MaxValue, SourceCodeKind)))
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() VisualBasicParseOptions.Default.WithLanguageVersion(DirectCast(1000, LanguageVersion)))
    End Sub

    <Fact>
    Public Sub WithPreprocessorSymbols()
        Dim syms = ImmutableArray.Create(New KeyValuePair(Of String, Object)("A", 1),
                                         New KeyValuePair(Of String, Object)("B", 2),
                                         New KeyValuePair(Of String, Object)("C", 3))

        TestProperty(Function(old, value) old.WithPreprocessorSymbols(value), Function(opt) opt.PreprocessorSymbols, syms)

        Assert.Equal(0, VisualBasicParseOptions.Default.WithPreprocessorSymbols(syms).WithPreprocessorSymbols(CType(Nothing, ImmutableArray(Of KeyValuePair(Of String, Object)))).PreprocessorSymbols.Length)
        Assert.Equal(0, VisualBasicParseOptions.Default.WithPreprocessorSymbols(syms).WithPreprocessorSymbols(DirectCast(Nothing, IEnumerable(Of KeyValuePair(Of String, Object)))).PreprocessorSymbols.Length)
        Assert.Equal(0, VisualBasicParseOptions.Default.WithPreprocessorSymbols(syms).WithPreprocessorSymbols(DirectCast(Nothing, KeyValuePair(Of String, Object)())).PreprocessorSymbols.Length)

        Dim syms2 = {New KeyValuePair(Of String, Object)("A", 1),
                     New KeyValuePair(Of String, Object)("B", New List(Of String)()),
                     New KeyValuePair(Of String, Object)("C", 3)}

        Assert.Throws(Of ArgumentException)(Function() New VisualBasicParseOptions(preprocessorSymbols:=syms2))
        Assert.Throws(Of ArgumentException)(Function() VisualBasicParseOptions.Default.WithPreprocessorSymbols(syms2))
    End Sub

    <Fact>
    Public Sub ConstructorValidation()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() New VisualBasicParseOptions(kind:=DirectCast(Int32.MaxValue, SourceCodeKind)))
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() New VisualBasicParseOptions(languageVersion:=DirectCast(1000, LanguageVersion)))
    End Sub

    <Fact, WorkItem(546206, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546206")>
    Public Sub InvalidDefineSymbols()

        ' Command line: error BC31030: Project-level conditional compilation constant 'xxx' is not valid: Identifier expected

        Dim syms = ImmutableArray.Create(New KeyValuePair(Of String, Object)("", 1))
        Assert.Throws(Of ArgumentException)(Function() New VisualBasicParseOptions(preprocessorSymbols:=syms))

        syms = ImmutableArray.Create(New KeyValuePair(Of String, Object)(" ", 1))
        Assert.Throws(Of ArgumentException)(Function() New VisualBasicParseOptions(preprocessorSymbols:=syms))

        syms = ImmutableArray.Create(New KeyValuePair(Of String, Object)("Good", 1),
                                     New KeyValuePair(Of String, Object)(Nothing, 2))
        Assert.Throws(Of ArgumentException)(Function() New VisualBasicParseOptions(preprocessorSymbols:=syms))

        syms = ImmutableArray.Create(New KeyValuePair(Of String, Object)("Good", 1),
                                     New KeyValuePair(Of String, Object)("Bad.Symbol", 2))
        Assert.Throws(Of ArgumentException)(Function() New VisualBasicParseOptions(preprocessorSymbols:=syms))

        syms = ImmutableArray.Create(New KeyValuePair(Of String, Object)("123", 1),
                                     New KeyValuePair(Of String, Object)("Bad/Symbol", 2),
                                     New KeyValuePair(Of String, Object)("Good", 3))
        Assert.Throws(Of ArgumentException)(Function() New VisualBasicParseOptions(preprocessorSymbols:=syms))
    End Sub

    <Fact>
    Public Sub PredefinedPreprocessorSymbolsTests()
        Dim options = VisualBasicParseOptions.Default
        Dim empty = ImmutableArray.Create(Of KeyValuePair(Of String, Object))()

        Dim symbols = AddPredefinedPreprocessorSymbols(OutputKind.NetModule)
        AssertEx.SetEqual({New KeyValuePair(Of String, Object)("VBC_VER", PredefinedPreprocessorSymbols.CurrentVersionNumber), New KeyValuePair(Of String, Object)("TARGET", "module")}, symbols.AsEnumerable)

        ' if the symbols are already there, don't change their values
        symbols = AddPredefinedPreprocessorSymbols(OutputKind.DynamicallyLinkedLibrary, symbols)
        AssertEx.SetEqual({New KeyValuePair(Of String, Object)("VBC_VER", PredefinedPreprocessorSymbols.CurrentVersionNumber), New KeyValuePair(Of String, Object)("TARGET", "module")}, symbols.AsEnumerable)

        symbols = AddPredefinedPreprocessorSymbols(OutputKind.WindowsApplication,
                                                   {New KeyValuePair(Of String, Object)("VBC_VER", "Foo"), New KeyValuePair(Of String, Object)("TARGET", 123)})
        AssertEx.SetEqual({New KeyValuePair(Of String, Object)("VBC_VER", "Foo"), New KeyValuePair(Of String, Object)("TARGET", 123)}, symbols.AsEnumerable)

        symbols = AddPredefinedPreprocessorSymbols(OutputKind.WindowsApplication,
                                                   New KeyValuePair(Of String, Object)("VBC_VER", "Foo"), New KeyValuePair(Of String, Object)("TARGET", 123))
        AssertEx.SetEqual({New KeyValuePair(Of String, Object)("VBC_VER", "Foo"), New KeyValuePair(Of String, Object)("TARGET", 123)}, symbols.AsEnumerable)

        symbols = AddPredefinedPreprocessorSymbols(OutputKind.ConsoleApplication, empty)
        AssertEx.SetEqual({New KeyValuePair(Of String, Object)("VBC_VER", PredefinedPreprocessorSymbols.CurrentVersionNumber), New KeyValuePair(Of String, Object)("TARGET", "exe")}, symbols.AsEnumerable)

        symbols = AddPredefinedPreprocessorSymbols(OutputKind.WindowsApplication, empty)
        AssertEx.SetEqual({New KeyValuePair(Of String, Object)("VBC_VER", PredefinedPreprocessorSymbols.CurrentVersionNumber), New KeyValuePair(Of String, Object)("TARGET", "winexe")}, symbols.AsEnumerable)
    End Sub

    <Fact>
    Public Sub CurrentVersionNumber()
        Dim highest = System.Enum.
            GetValues(GetType(LanguageVersion)).
            Cast(Of LanguageVersion).
            Select(Function(x) CInt(x)).
            Max()

        Assert.Equal(highest, CInt(PredefinedPreprocessorSymbols.CurrentVersionNumber))
    End Sub

    <Fact>
    Public Sub PredefinedPreprocessorSymbols_Win8()
        Dim options = VisualBasicParseOptions.Default

        Dim symbols = AddPredefinedPreprocessorSymbols(OutputKind.WindowsRuntimeApplication)
        AssertEx.SetEqual({New KeyValuePair(Of String, Object)("VBC_VER", PredefinedPreprocessorSymbols.CurrentVersionNumber), New KeyValuePair(Of String, Object)("TARGET", "appcontainerexe")}, symbols.AsEnumerable)

        symbols = AddPredefinedPreprocessorSymbols(OutputKind.WindowsRuntimeMetadata)
        AssertEx.SetEqual({New KeyValuePair(Of String, Object)("VBC_VER", PredefinedPreprocessorSymbols.CurrentVersionNumber), New KeyValuePair(Of String, Object)("TARGET", "winmdobj")}, symbols.AsEnumerable)
    End Sub

    <Fact>
    Public Sub ParseOptionsPass()
        ParseAndVerify(<![CDATA[
                Option strict
                Option strict on
                Option strict off
            ]]>)

        ParseAndVerify(<![CDATA[
                        Option infer
                        Option infer on
                        option infer off
                    ]]>)

        ParseAndVerify(<![CDATA[
                option explicit
                Option explicit On
                Option explicit off
            ]]>)

        ParseAndVerify(<![CDATA[
                Option compare text
                Option compare binary
            ]]>)
    End Sub

    <Fact()>
    Public Sub BC30208ERR_ExpectedOptionCompare()

        ParseAndVerify(<![CDATA[
                Option text
            ]]>,
            <errors>
                <error id="30208"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30979ERR_InvalidOptionInfer()
        ParseAndVerify(<![CDATA[
                Option infer xyz
            ]]>,
            <errors>
                <error id="30979"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC31141ERR_InvalidOptionStrictCustom()
        ParseAndVerify(<![CDATA[
                Option strict custom
            ]]>,
            <errors>
                <error id="31141"/>
            </errors>)
    End Sub

    <Fact, WorkItem(536060, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536060")>
    Public Sub BC30620ERR_InvalidOptionStrict_FollowedByAssemblyAttribute()
        ParseAndVerify(<![CDATA[
            Option Strict False
            <Assembly: CLSCompliant(True)> 
        ]]>,
        <errors>
            <error id="30620"/>
        </errors>)
    End Sub

    <Fact, WorkItem(536067, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536067")>
    Public Sub BC30627ERR_OptionStmtWrongOrder()
        ParseAndVerify(<![CDATA[
            Imports System
            Option Infer On
        ]]>,
        <errors>
            <error id="30627"/>
        </errors>)
    End Sub

    <Fact, WorkItem(536362, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536362")>
    Public Sub BC30206ERR_ExpectedForOptionStmt_NullReferenceException()
        ParseAndVerify(<![CDATA[
            Option
        ]]>,
        <errors>
            <error id="30206"/>
        </errors>)

        ParseAndVerify(<![CDATA[
                Option on
            ]]>,
    <errors>
        <error id="30206"/>
    </errors>)
    End Sub

    <Fact, WorkItem(536432, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536432")>
    Public Sub BC30205ERR_ExpectedEOS_ParseOption_ExtraSyntaxAtEOL()
        ParseAndVerify(<![CDATA[
            Option Infer On O
        ]]>,
        <errors>
            <error id="30205"/>
        </errors>)
    End Sub

    ''' <summary>
    ''' If this test fails, please update the <see cref="ParseOptions.GetHashCode" />
    ''' And <see cref="ParseOptions.Equals" /> methods to
    ''' make sure they are doing the right thing with your New field And then update the baseline
    ''' here.
    ''' </summary>
    <Fact>
    Public Sub TestFieldsForEqualsAndGetHashCode()
        ReflectionAssert.AssertPublicAndInternalFieldsAndProperties(
                (GetType(VisualBasicParseOptions)),
                "Features",
                "LanguageVersion",
                "PreprocessorSymbolNames",
                "PreprocessorSymbols")
    End Sub
End Class

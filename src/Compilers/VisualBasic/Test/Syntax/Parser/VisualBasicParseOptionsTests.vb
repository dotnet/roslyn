' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization
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
    End Sub

    <Fact>
    Public Sub WithLatestLanguageVersion()
        Dim oldOpt1 = VisualBasicParseOptions.Default
        Dim newOpt1 = oldOpt1.WithLanguageVersion(LanguageVersion.Latest)
        Dim newOpt2 = newOpt1.WithLanguageVersion(LanguageVersion.Latest)
        Assert.Equal(LanguageVersion.Latest.MapSpecifiedToEffectiveVersion, newOpt1.LanguageVersion)
        Assert.Equal(LanguageVersion.Latest.MapSpecifiedToEffectiveVersion, newOpt2.LanguageVersion)
        newOpt1 = oldOpt1.WithLanguageVersion(LanguageVersion.Default)
        newOpt2 = newOpt1.WithLanguageVersion(LanguageVersion.Default)
        Assert.Equal(LanguageVersion.Default.MapSpecifiedToEffectiveVersion, newOpt1.LanguageVersion)
        Assert.Equal(LanguageVersion.Default.MapSpecifiedToEffectiveVersion, newOpt2.LanguageVersion)
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
                                                   {New KeyValuePair(Of String, Object)("VBC_VER", "Goo"), New KeyValuePair(Of String, Object)("TARGET", 123)})
        AssertEx.SetEqual({New KeyValuePair(Of String, Object)("VBC_VER", "Goo"), New KeyValuePair(Of String, Object)("TARGET", 123)}, symbols.AsEnumerable)

        symbols = AddPredefinedPreprocessorSymbols(OutputKind.WindowsApplication,
                                                   New KeyValuePair(Of String, Object)("VBC_VER", "Goo"), New KeyValuePair(Of String, Object)("TARGET", 123))
        AssertEx.SetEqual({New KeyValuePair(Of String, Object)("VBC_VER", "Goo"), New KeyValuePair(Of String, Object)("TARGET", 123)}, symbols.AsEnumerable)

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
            Where(Function(x) x <> LanguageVersion.Latest).
            Max().
            ToDisplayString()

        Assert.Equal(highest, PredefinedPreprocessorSymbols.CurrentVersionNumber.ToString(CultureInfo.InvariantCulture))
    End Sub

    <Fact, WorkItem(21094, "https://github.com/dotnet/roslyn/issues/21094")>
    Public Sub CurrentVersionNumberIsCultureIndependent()
        Dim currentCulture = CultureInfo.CurrentCulture
        Try
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture
            Dim invariantCultureVersion = PredefinedPreprocessorSymbols.CurrentVersionNumber
            ' cs-CZ uses decimal comma, which can cause issues
            CultureInfo.CurrentCulture = New CultureInfo("cs-CZ", useUserOverride:=False)
            Dim czechCultureVersion = PredefinedPreprocessorSymbols.CurrentVersionNumber
            Assert.Equal(invariantCultureVersion, czechCultureVersion)
        Finally
            CultureInfo.CurrentCulture = currentCulture
        End Try
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
                "Language",
                "LanguageVersion",
                "PreprocessorSymbolNames",
                "PreprocessorSymbols",
                "SpecifiedLanguageVersion")
    End Sub

    <Fact>
    Public Sub SpecifiedKindIsMappedCorrectly()
        Dim options = New VisualBasicParseOptions()
        Assert.Equal(SourceCodeKind.Regular, options.Kind)
        Assert.Equal(SourceCodeKind.Regular, options.SpecifiedKind)

        options.Errors.Verify()

        options = New VisualBasicParseOptions(kind:=SourceCodeKind.Regular)
        Assert.Equal(SourceCodeKind.Regular, options.Kind)
        Assert.Equal(SourceCodeKind.Regular, options.SpecifiedKind)

        options.Errors.Verify()

        options = New VisualBasicParseOptions(kind:=SourceCodeKind.Script)
        Assert.Equal(SourceCodeKind.Script, options.Kind)
        Assert.Equal(SourceCodeKind.Script, options.SpecifiedKind)

        options.Errors.Verify()

#Disable Warning BC40000 ' SourceCodeKind.Interactive is obsolete
        options = New VisualBasicParseOptions(kind:=SourceCodeKind.Interactive)
        Assert.Equal(SourceCodeKind.Script, options.Kind)
        Assert.Equal(SourceCodeKind.Interactive, options.SpecifiedKind)
#Enable Warning BC40000 ' SourceCodeKind.Interactive is obsolete

        options.Errors.Verify(Diagnostic(ERRID.ERR_BadSourceCodeKind).WithArguments("Interactive").WithLocation(1, 1))

        options = New VisualBasicParseOptions(kind:=CType(Int32.MinValue, SourceCodeKind))
        Assert.Equal(SourceCodeKind.Regular, options.Kind)
        Assert.Equal(CType(Int32.MinValue, SourceCodeKind), options.SpecifiedKind)

        options.Errors.Verify(Diagnostic(ERRID.ERR_BadSourceCodeKind).WithArguments("-2147483648").WithLocation(1, 1))
    End Sub

    <Fact>
    Public Sub TwoOptionsWithDifferentSpecifiedKindShouldNotHaveTheSameHashCodes()
        Dim options1 = New VisualBasicParseOptions(kind:=SourceCodeKind.Script)
        Dim options2 = New VisualBasicParseOptions(kind:=SourceCodeKind.Script)

        Assert.Equal(options1.GetHashCode(), options2.GetHashCode())

        ' They both map internally to SourceCodeKind.Script
#Disable Warning BC40000 ' SourceCodeKind.Interactive is obsolete
        options1 = New VisualBasicParseOptions(kind:=SourceCodeKind.Script)
        options2 = New VisualBasicParseOptions(kind:=SourceCodeKind.Interactive)
#Enable Warning BC40000 ' SourceCodeKind.Interactive Is obsolete

        Assert.NotEqual(options1.GetHashCode(), options2.GetHashCode())
    End Sub

    <Fact>
    Public Sub TwoOptionsWithDifferentSpecifiedKindShouldNotBeEqual()
        Dim options1 = New VisualBasicParseOptions(kind:=SourceCodeKind.Script)
        Dim options2 = New VisualBasicParseOptions(kind:=SourceCodeKind.Script)

        Assert.True(options1.Equals(options2))

        ' They both map internally to SourceCodeKind.Script
#Disable Warning BC40000 ' SourceCodeKind.Interactive is obsolete
        options1 = New VisualBasicParseOptions(kind:=SourceCodeKind.Script)
        options2 = New VisualBasicParseOptions(kind:=SourceCodeKind.Interactive)
#Enable Warning BC40000 ' SourceCodeKind.Interactive Is obsolete

        Assert.False(options1.Equals(options2))
    End Sub

    <Fact>
    Public Sub BadSourceCodeKindShouldProduceDiagnostics()
#Disable Warning BC40000 ' Type Or member Is obsolete
        Dim options = New VisualBasicParseOptions(kind:=SourceCodeKind.Interactive)
#Enable Warning BC40000 ' Type Or member Is obsolete

        options.Errors.Verify(Diagnostic(ERRID.ERR_BadSourceCodeKind).WithArguments("Interactive").WithLocation(1, 1))
    End Sub

    <Fact>
    Public Sub BadDocumentationModeShouldProduceDiagnostics()
        Dim options = New VisualBasicParseOptions(documentationMode:=CType(100, DocumentationMode))

        options.Errors.Verify(Diagnostic(ERRID.ERR_BadDocumentationMode).WithArguments("100").WithLocation(1, 1))
    End Sub

    <Fact>
    Public Sub BadLanguageVersionShouldProduceDiagnostics()
        Dim options = New VisualBasicParseOptions(languageVersion:=DirectCast(10000, LanguageVersion))

        options.Errors.Verify(Diagnostic(ERRID.ERR_BadLanguageVersion).WithArguments("10000").WithLocation(1, 1))
    End Sub

    <Fact>
    Public Sub BadPreProcessorSymbolsShouldProduceDiagnostics()
        Dim symbols = New Dictionary(Of String, Object)
        symbols.Add("test", Nothing)
        symbols.Add("1", Nothing)
        Dim options = New VisualBasicParseOptions(preprocessorSymbols:=symbols)

        options.Errors.Verify(Diagnostic(ERRID.ERR_ConditionalCompilationConstantNotValid).WithArguments("Identifier expected.", "1").WithLocation(1, 1))
    End Sub

    <Fact>
    Public Sub BadSourceCodeKindShouldProduceDiagnostics_WithVariation()
#Disable Warning BC40000 ' Type Or member Is obsolete
        Dim options = New VisualBasicParseOptions().WithKind(SourceCodeKind.Interactive)
#Enable Warning BC40000 ' Type Or member Is obsolete

        options.Errors.Verify(Diagnostic(ERRID.ERR_BadSourceCodeKind).WithArguments("Interactive").WithLocation(1, 1))
    End Sub

    <Fact>
    Public Sub BadDocumentationModeShouldProduceDiagnostics_WithVariation()
        Dim options = New VisualBasicParseOptions().WithDocumentationMode(CType(100, DocumentationMode))

        options.Errors.Verify(Diagnostic(ERRID.ERR_BadDocumentationMode).WithArguments("100").WithLocation(1, 1))
    End Sub

    <Fact>
    Public Sub BadLanguageVersionShouldProduceDiagnostics_WithVariation()
        Dim options = New VisualBasicParseOptions().WithLanguageVersion(DirectCast(10000, LanguageVersion))

        options.Errors.Verify(Diagnostic(ERRID.ERR_BadLanguageVersion).WithArguments("10000").WithLocation(1, 1))
    End Sub

    <Fact>
    Public Sub BadPreProcessorSymbolsShouldProduceDiagnostics_EmptyString()
        Dim symbols = New Dictionary(Of String, Object)
        symbols.Add("", Nothing)
        Dim options = New VisualBasicParseOptions().WithPreprocessorSymbols(symbols)

        options.Errors.Verify(Diagnostic(ERRID.ERR_ConditionalCompilationConstantNotValid).WithArguments("Identifier expected.", "").WithLocation(1, 1))
    End Sub

    <Fact>
    Public Sub BadPreProcessorSymbolsShouldProduceDiagnostics_WhiteSpaceString()
        Dim symbols = New Dictionary(Of String, Object)
        symbols.Add(" ", Nothing)
        Dim options = New VisualBasicParseOptions().WithPreprocessorSymbols(symbols)

        options.Errors.Verify(Diagnostic(ERRID.ERR_ConditionalCompilationConstantNotValid).WithArguments("Identifier expected.", " ").WithLocation(1, 1))
    End Sub

    <Fact>
    Public Sub BadPreProcessorSymbolsShouldProduceDiagnostics_SymbolWithDots()
        Dim symbols = New Dictionary(Of String, Object)
        symbols.Add("Good", Nothing)
        symbols.Add("Bad.Symbol", Nothing)
        Dim options = New VisualBasicParseOptions().WithPreprocessorSymbols(symbols)

        options.Errors.Verify(Diagnostic(ERRID.ERR_ConditionalCompilationConstantNotValid).WithArguments("Identifier expected.", "Bad.Symbol").WithLocation(1, 1))
    End Sub

    <Fact>
    Public Sub BadPreProcessorSymbolsShouldProduceDiagnostics_SymbolWithSlashes()
        Dim symbols = New Dictionary(Of String, Object)
        symbols.Add("Good", Nothing)
        symbols.Add("Bad\\Symbol", Nothing)
        Dim options = New VisualBasicParseOptions().WithPreprocessorSymbols(symbols)

        options.Errors.Verify(Diagnostic(ERRID.ERR_ConditionalCompilationConstantNotValid).WithArguments("Identifier expected.", "Bad\\Symbol").WithLocation(1, 1))
    End Sub

End Class

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports System.Reflection.Metadata
Imports System.Reflection.PortableExecutable
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.DiaSymReader
Imports Roslyn.Test.PdbUtilities
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.SharedResourceHelpers
Imports Roslyn.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine.UnitTests

    Partial Public Class CommandLineTests
        Inherits BasicTestBase

        Private ReadOnly _baseDirectory As String = TempRoot.Root
        Private Shared ReadOnly s_basicCompilerExecutable As String = Path.Combine(Path.GetDirectoryName(GetType(CommandLineTests).Assembly.Location), Path.Combine("dependency", "vbc.exe"))
        Private Shared ReadOnly s_defaultSdkDirectory As String = RuntimeEnvironment.GetRuntimeDirectory()
        Private Shared ReadOnly s_compilerVersion As String = FileVersionInfo.GetVersionInfo(GetType(CommandLineTests).Assembly.Location).FileVersion
        Private Shared ReadOnly s_compilerShortCommitHash As String = CommonCompiler.ExtractShortCommitHash(GetType(CommandLineTests).Assembly.GetCustomAttribute(Of CommitHashAttribute).Hash)

        Private Shared Function DefaultParse(args As IEnumerable(Of String), baseDirectory As String, Optional sdkDirectory As String = Nothing, Optional additionalReferenceDirectories As String = Nothing) As VisualBasicCommandLineArguments
            sdkDirectory = If(sdkDirectory, s_defaultSdkDirectory)
            Return VisualBasicCommandLineParser.Default.Parse(args, baseDirectory, sdkDirectory, additionalReferenceDirectories)
        End Function

        Private Shared Function FullParse(commandLine As String, baseDirectory As String, Optional sdkDirectory As String = Nothing, Optional additionalReferenceDirectories As String = Nothing) As VisualBasicCommandLineArguments
            sdkDirectory = If(sdkDirectory, s_defaultSdkDirectory)
            Dim args = CommandLineParser.SplitCommandLineIntoArguments(commandLine, removeHashComments:=True)
            Return VisualBasicCommandLineParser.Default.Parse(args, baseDirectory, sdkDirectory, additionalReferenceDirectories)
        End Function

        Private Shared Function InteractiveParse(args As IEnumerable(Of String), baseDirectory As String, Optional sdkDirectory As String = Nothing, Optional additionalReferenceDirectories As String = Nothing) As VisualBasicCommandLineArguments
            sdkDirectory = If(sdkDirectory, s_defaultSdkDirectory)
            Return VisualBasicCommandLineParser.Script.Parse(args, baseDirectory, sdkDirectory, additionalReferenceDirectories)
        End Function

        <Fact, WorkItem(946954, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/946954")>
        Public Sub CompilerBinariesAreAnyCPU()
            Assert.Equal(ProcessorArchitecture.MSIL, AssemblyName.GetAssemblyName(s_basicCompilerExecutable).ProcessorArchitecture)
        End Sub

        <Theory(),
    InlineData({"/t:library", "/nowarn", "/warnaserror-"}, ReportDiagnostic.Suppress),
    InlineData({"/t:library", "/nowarn", "/warnaserror"}, ReportDiagnostic.Error),
    InlineData({"/t:library", "/nowarn", "/warnaserror+"}, ReportDiagnostic.Error),
    InlineData({"/t:library", "/warnaserror-", "/nowarn"}, ReportDiagnostic.Suppress),
    InlineData({"/t:library", "/warnaserror", "/nowarn"}, ReportDiagnostic.Suppress),
    InlineData({"/t:library", "/warnaserror+", "/nowarn"}, ReportDiagnostic.Suppress)>
        <WorkItem(546322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546322")>
        Public Sub NowarnWarnaserrorTest(Args As String(), Result As ReportDiagnostic)
            Dim src As String = Temp.CreateFile().WriteAllText("
Class C
End Class
").Path
            Dim args0 = Args.Concat(src).ToArray
            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, args0)
            Assert.Equal(cmd.Arguments.CompilationOptions.GeneralDiagnosticOption, Result)
            CleanupAllGeneratedFiles(src)
        End Sub

        <Fact, WorkItem(21508, "https://github.com/dotnet/roslyn/issues/21508")>
        Public Sub ArgumentStartWithDashAndContainingSlash()
            Dim args As VisualBasicCommandLineArguments
            Dim folder = Temp.CreateDirectory()

            args = DefaultParse({"-debug+/debug:portable"}, folder.Path)
            args.Errors.AssertTheseDiagnostics(<errors>
BC2007: unrecognized option '-debug+/debug:portable'; ignored
BC2008: no input sources specified
                                               </errors>)
        End Sub

        <Fact, WorkItem(545247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545247")>
        Public Sub CommandLineCompilationWithQuotedMainArgument()
            ' Arguments with quoted rootnamespace and main type are unquoted when
            ' the arguments are read in by the command line compiler.
            Dim src As String = Temp.CreateFile().WriteAllText("
Module Module1
    Sub Main()
    
    End Sub
End Module
").Path

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/target:exe", "/rootnamespace:""test""", "/main:""test.Module1""", src})

                Dim exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Equal("", output.ToString().Trim())
            End Using
        End Sub

        <Fact>
        Public Sub CreateCompilationWithKeyFile()
            Dim source = "
Public Class C
    Public Shared Sub Main()
    End Sub
End Class"

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName).WriteAllText(source)
            Dim cmd = New MockVisualBasicCompiler(dir.Path, {"/nologo", "a.vb", "/keyfile:key.snk"})
            Dim comp = cmd.CreateCompilation(TextWriter.Null, New TouchedFileLogger(), NullErrorLogger.Instance)

            Assert.IsType(Of DesktopStrongNameProvider)(comp.Options.StrongNameProvider)
        End Sub

        <Fact>
        Public Sub CreateCompilationWithCryptoContainer()
            Dim source = "
Public Class C
    Public Shared Sub Main()
    End Sub
End Class"

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName).WriteAllText(source)
            Dim cmd = New MockVisualBasicCompiler(dir.Path, {"/nologo", "a.vb", "/keycontainer:aaa"})
            Dim comp = cmd.CreateCompilation(TextWriter.Null, New TouchedFileLogger(), NullErrorLogger.Instance)

            Assert.True(TypeOf comp.Options.StrongNameProvider Is DesktopStrongNameProvider)
        End Sub

        <Fact>
        Public Sub CreateCompilationWithStrongNameFallbackCommand()
            Dim source = "
Public Class C
    Public Shared Sub Main()
    End Sub
End Class"

            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.vb").WriteAllText(source)
            Dim cmd = New MockVisualBasicCompiler(dir.Path, {"/nologo", "a.vb", "/features:UseLegacyStrongNameProvider"})
            Dim comp = cmd.CreateCompilation(TextWriter.Null, New TouchedFileLogger(), NullErrorLogger.Instance)

            Assert.True(TypeOf comp.Options.StrongNameProvider Is DesktopStrongNameProvider)
        End Sub

#Region "ParseQuoted_MainType_Rootnamespace"
        <Category("ParseQuoted_MainType_Rootnamespace"), Theory, InlineData({"/main:Test", "a.vb"}), InlineData({"/main:""Test""", "a.vb"})>
        Public Sub ParseQuoted_MainType(ParamArray args() As String)
            'These options are always unquoted when parsed in VisualBasicCommandLineParser.Parse.
            Dim ParsedArgs = DefaultParse(args, _baseDirectory)
            ParsedArgs.Errors.Verify()
            Assert.Equal("Test", ParsedArgs.CompilationOptions.MainTypeName)
        End Sub

        <Category("ParseQuoted_MainType_Rootnamespace"), Theory, InlineData({"/rootnamespace:Test", "a.vb"}), InlineData({"/rootnamespace:""Test""", "a.vb"})>
        Public Sub ParseQuoted_Rootnamespace(ParamArray Args() As String)
            ' These options are always unquoted when parsed in VisualBasicCommandLineParser.Parse.
            Dim ParsedArgs = DefaultParse(Args, _baseDirectory)
            ParsedArgs.Errors.Verify()
            Assert.Equal("Test", ParsedArgs.CompilationOptions.RootNamespace)
        End Sub

        <Category("ParseQuoted_MainType_Rootnamespace"), Fact>
        Public Sub ParseQuoted_MainType_Rootnamespace()
            Dim ParsedArgs = DefaultParse({"/rootnamespace:""test""", "/main:""test.Module1""", "a.vb"}, _baseDirectory)
            ParsedArgs.Errors.Verify()
            Assert.Equal("test.Module1", ParsedArgs.CompilationOptions.MainTypeName)
            Assert.Equal("test", ParsedArgs.CompilationOptions.RootNamespace)
        End Sub

        <Category("ParseQuoted_MainType_Rootnamespace"), Fact>
        Public Sub ParseQuoted_MainType_Rootnamespace_Cyrillic()
            ' Use of Cyrillic namespace
            Dim ParsedArgs = DefaultParse({"/rootnamespace:""решения""", "/main:""решения.Module1""", "a.vb"}, _baseDirectory)
            ParsedArgs.Errors.Verify()
            Assert.Equal("решения.Module1", ParsedArgs.CompilationOptions.MainTypeName)
            Assert.Equal("решения", ParsedArgs.CompilationOptions.RootNamespace)
        End Sub
#End Region

        <WorkItem(722561, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/722561"), Theory,
     InlineData("/nologo", "/t:library", "/nowarn:-1"),
     InlineData("/nologo", "/t:library", "/nowarn:-12345678901234567890"),
     InlineData("/nologo", "/t:library", "/nowarn:-1234567890123456789")>
        Public Sub Bug_722561(ParamArray args As String())
            Dim src As String = Temp.CreateFile().WriteAllText("
Public Class C
End Class
").Path

            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, args.Concat(src).ToArray)
            Dim result As Integer
            Using writer As New StringWriter()
                result = cmd.Run(writer, Nothing)

                Assert.Equal(String.Empty, writer.ToString.Trim)
            End Using
            '  {"/nologo", "/t:library", "/nowarn:-1", src}
            ' Previous versions of the compiler used to report warnings (BC2026, BC2014)
            ' whenever an unrecognized warning code was supplied via /nowarn or /warnaserror.
            ' We no longer generate a warning in such cases.

            CleanupAllGeneratedFiles(src)
        End Sub

        <Fact>
        Public Sub VbcTest()
            Using output As New StringWriter()

                Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/preferreduilang:en"})
                cmd.Run(output, Nothing)

                Assert.True(output.ToString().StartsWith(s_logoLine1, StringComparison.Ordinal), "vbc should print logo and help if no args specified")
            End Using
        End Sub

        <Theory, InlineData("/nologo", "/t:library"), InlineData("/nologo+", "/t:library")>
        Public Sub VbcNologo_1(ParamArray args As String())
            Dim src = Temp.CreateFile().WriteAllText("
Class C
End Class
").Path

            Using output As New StringWriter()

                Dim cmd As New MockVisualBasicCompiler(Nothing, _baseDirectory, args.Concat(src).ToArray)
                Dim exitCode = cmd.Run(output, Nothing)

                Assert.Equal(0, exitCode)
                Assert.Equal("", output.ToString().Trim())
            End Using

            CleanupAllGeneratedFiles(src)
        End Sub

        <Fact>
        Public Sub VbcNologo_2()
            Dim src As String = Temp.CreateFile().WriteAllText("
Class C
End Class
").Path

            Using output As New StringWriter()

                Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/t:library", "/preferreduilang:en", src})
                Dim exitCode = cmd.Run(output, Nothing)

                Assert.Equal(0, exitCode)
                Dim patched As String = Regex.Replace(output.ToString().Trim(), "version \d+\.\d+\.\d+(\.\d+)?", "version A.B.C.D")
                patched = ReplaceCommitHash(patched)
                Assert.Equal(
"Microsoft (R) Visual Basic Compiler version A.B.C.D (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.",
                patched)
                ' Privately queued builds have 3-part version numbers instead of 4.  Since we're throwing away the version number,
                ' making the last part optional will fix this.
            End Using
            CleanupAllGeneratedFiles(src)
        End Sub

        <Theory,
     InlineData("Microsoft (R) Visual Basic Compiler version A.B.C.D (<developer build>)", "Microsoft (R) Visual Basic Compiler version A.B.C.D (HASH)"),
     InlineData("Microsoft (R) Visual Basic Compiler version A.B.C.D (ABCDEF01)", "Microsoft (R) Visual Basic Compiler version A.B.C.D (HASH)"),
     InlineData("Microsoft (R) Visual Basic Compiler version A.B.C.D (abcdef90)", "Microsoft (R) Visual Basic Compiler version A.B.C.D (HASH)"),
     InlineData("Microsoft (R) Visual Basic Compiler version A.B.C.D (12345678)", "Microsoft (R) Visual Basic Compiler version A.B.C.D (HASH)")>
        Public Sub TestReplaceCommitHash(orig As String, expected As String)
            Assert.Equal(expected, ReplaceCommitHash(orig))
        End Sub

        Private Shared Function ReplaceCommitHash(s As String) As String
            Return Regex.Replace(s, "(\((<developer build>|[a-fA-F0-9]{8})\))", "(HASH)")
        End Function

        <Fact>
        Public Sub VbcNologo_2a()
            Dim src As String = Temp.CreateFile().WriteAllText("
Class C
End Class
").Path

            Using output As New StringWriter()

                Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo-", "/preferreduilang:en", "/t:library", src})
                Dim exitCode = cmd.Run(output, Nothing)

                Assert.Equal(0, exitCode)
                Dim patched As String = Regex.Replace(output.ToString().Trim(), "version \d+\.\d+\.\d+(\.\d+)?", "version A.B.C.D")
                patched = ReplaceCommitHash(patched)
                Assert.Equal(
"Microsoft (R) Visual Basic Compiler version A.B.C.D (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.",
                patched)
                ' Privately queued builds have 3-part version numbers instead of 4.  Since we're throwing away the version number,
                ' making the last part optional will fix this.
            End Using
            CleanupAllGeneratedFiles(src)
        End Sub

        <Theory>
        <InlineData("/C ""{0}"" /nologo /preferreduilang:en /t:library {1} > {2}", "?"c)> ' Redirection Off
        <InlineData("/C ""{0}"" /utf8output /nologo /preferreduilang:en /t:library {1} > {2}", "♚"c)> ' Redirection On
        Public Sub VbcUtf8Output_WithRedirecting(cmdline As String, ch As Char)
            Dim src As String = Temp.CreateFile().WriteAllText("♚", New System.Text.UTF8Encoding(False)).Path

            Dim tempOut = Temp.CreateFile()

            Dim output = ProcessUtilities.RunAndGetOutput("cmd", String.Format(cmdline, s_basicCompilerExecutable, src, tempOut.Path), expectedRetCode:=1)
            Assert.Equal("", output.Trim())

            Assert.Equal(
$"SRC.VB(1) : error BC30037: Character is not valid.

{ch}
~", tempOut.ReadAllText().Trim().Replace(src, "SRC.VB"))

            CleanupAllGeneratedFiles(src)
        End Sub

        <Fact()>
        Public Sub ResponseFiles1()
            Dim rsp As String = Temp.CreateFile().WriteAllText("
/r:System.dll
/nostdlib
/vbruntime-
# this is ignored
System.Console.WriteLine(&quot;*?&quot;);  # this is error
a.vb
").Path
            Dim cmd = New MockVisualBasicCompiler(rsp, _baseDirectory, {"b.vb"})

            AssertEx.Equal({"System.dll"}, cmd.Arguments.MetadataReferences.Select(Function(r) r.Reference))
            AssertEx.Equal(
        {
            Path.Combine(_baseDirectory, "a.vb"),
            Path.Combine(_baseDirectory, "b.vb")
        },
        cmd.Arguments.SourceFiles.Select(Function(file) file.Path))
            Assert.NotEmpty(cmd.Arguments.Errors)


            CleanupAllGeneratedFiles(rsp)
        End Sub

        <WorkItem(685392, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/685392")>
        <Fact()>
        Public Sub ResponseFiles_RootNamespace()
            Dim rsp As String = Temp.CreateFile().WriteAllText("
/r:System.dll
/rootnamespace:""Hello""
a.vb
").Path
            Dim cmd = New MockVisualBasicCompiler(rsp, _baseDirectory, {"b.vb"})

            Assert.Equal("Hello", cmd.Arguments.CompilationOptions.RootNamespace)

            CleanupAllGeneratedFiles(rsp)
        End Sub

        Private Sub AssertGlobalImports(expectedImportStrings As String(), actualImports As GlobalImport())
            Assert.Equal(expectedImportStrings.Length, actualImports.Count)
            For i = 0 To expectedImportStrings.Length - 1
                Assert.Equal(expectedImportStrings(i), actualImports(i).Clause.ToString)
            Next
        End Sub

        <Fact>
        Public Sub ParseGlobalImports()
            Dim args = DefaultParse({"/imports: System ,System.Xml ,System.Linq", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            AssertEx.Equal({"System", "System.Xml", "System.Linq"}, args.CompilationOptions.GlobalImports.Select(Function(import) import.Clause.ToString()))

            args = DefaultParse({"/impORt: System,,,,,", "/IMPORTs:,,,Microsoft.VisualBasic,,System.IO", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            AssertEx.Equal({"System", "Microsoft.VisualBasic", "System.IO"}, args.CompilationOptions.GlobalImports.Select(Function(import) import.Clause.ToString()))

            args = DefaultParse({"/impORt: System, ,, ,,", "a.vb"}, _baseDirectory)
            args.Errors.Verify(Diagnostic(ERRID.ERR_ExpectedIdentifier),
                           Diagnostic(ERRID.ERR_ExpectedIdentifier))

            args = DefaultParse({"/impORt:", "a.vb"}, _baseDirectory)
            args.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("import", ":<str>"))

            args = DefaultParse({"/impORts:", "a.vb"}, _baseDirectory)
            args.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("imports", ":<import_list>"))

            args = DefaultParse({"/imports", "a.vb"}, _baseDirectory)
            args.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("imports", ":<import_list>"))

            args = DefaultParse({"/imports+", "a.vb"}, _baseDirectory)
            args.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/imports+")) ' TODO: Dev11 reports ERR_ArgumentRequired
        End Sub

        <Fact>
        Public Sub ParseInteractive()
            Dim args As VisualBasicCommandLineArguments

            args = DefaultParse({}, _baseDirectory)
            args.Errors.Verify()
            Assert.False(args.InteractiveMode)

            args = DefaultParse({"/i"}, _baseDirectory)
            args.Errors.Verify({Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/i").WithLocation(1, 1),
                           Diagnostic(ERRID.ERR_NoSources).WithLocation(1, 1)})
            Assert.False(args.InteractiveMode)

            args = InteractiveParse({}, _baseDirectory)
            args.Errors.Verify()
            Assert.True(args.InteractiveMode)

            args = InteractiveParse({"a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.False(args.InteractiveMode)

            args = InteractiveParse({"/i", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.True(args.InteractiveMode)

            args = InteractiveParse({"/i+", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.True(args.InteractiveMode)

            args = InteractiveParse({"/i+ /i-", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.False(args.InteractiveMode)

            For Each flag In {"i", "i+", "i-"}
                args = InteractiveParse({"/" + flag + ":arg"}, _baseDirectory)
                args.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("i").WithLocation(1, 1))
            Next
        End Sub

#Region "Parse InstrumentTestNames"
        <Category("Parse InstrumentTestNames"), Fact>
        Public Sub ParseInstrumentTestNames()
            Dim ParsedArgs As VisualBasicCommandLineArguments
            ParsedArgs = DefaultParse({}, _baseDirectory)
            Assert.True(ParsedArgs.EmitOptions.InstrumentationKinds.SequenceEqual({}))
        End Sub

        <Category("Parse InstrumentTestNames"), Theory, InlineData({"/instrument", "a.vb"}), InlineData({"/instrument:""""", "a.vb"}), InlineData({"/instrument:", "a.vb"}), InlineData({"/instrument:", "Test.Flag.Name", "a.vb"})>
        Public Sub ParseInstrumentTestNames_Errosr(ParamArray Args() As String)
            Dim ParsedArgs = DefaultParse(Args, _baseDirectory)
            ParsedArgs.Errors.Verify({Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("instrument", ":<string>").WithLocation(1, 1)})
            Assert.True(ParsedArgs.EmitOptions.InstrumentationKinds.SequenceEqual({}))
        End Sub

        <Category("Parse InstrumentTestNames"), Theory,
         InlineData({"/instrument:InvalidOption", "a.vb"}, "InvalidOption", False),
         InlineData({"/instrument:None", "a.vb"}, "None", False),
         InlineData({"/instrument:""TestCoverage,InvalidOption""", "a.vb"}, "InvalidOption", True)>
        Public Sub ParseInstrumentTestNames_InvalidInstrumentationKind(Args() As String, argName As String, TestCoverage As Boolean)
            Dim ParsedArgs = DefaultParse(Args, _baseDirectory)
            ParsedArgs.Errors.Verify({Diagnostic(ERRID.ERR_InvalidInstrumentationKind).WithArguments(argName).WithLocation(1, 1)})
            Assert.True(ParsedArgs.EmitOptions.InstrumentationKinds.SequenceEqual(If(TestCoverage, {InstrumentationKind.TestCoverage}, {})))
        End Sub

        <Category("Parse InstrumentTestNames"), Theory,
         InlineData({"/instrument:TestCoverage", "a.vb"}),
         InlineData({"/instrument:""TestCoverage""", "a.vb"}),
         InlineData({"/instrument:""TESTCOVERAGE""", "a.vb"}),
         InlineData({"/instrument:TestCoverage,TestCoverage", "a.vb"}),
         InlineData({"/instrument:TestCoverage", "/instrument:TestCoverage", "a.vb"})>
        Public Sub ParseInstrumentTestNames_TestCoverage(ParamArray Args() As String)
            Dim ParsedArgs = DefaultParse(Args, _baseDirectory)
            ParsedArgs.Errors.Verify()
            Assert.True(ParsedArgs.EmitOptions.InstrumentationKinds.SequenceEqual({InstrumentationKind.TestCoverage}))
        End Sub
#End Region

        <Fact>
        Public Sub ResponseFiles2()
            Dim rsp As String = Temp.CreateFile().WriteAllText("
    /r:System
    /r:System.Core
    /r:System.Data
    /r:System.Data.DataSetExtensions
    /r:System.Xml
    /r:System.Xml.Linq
    /imports:System
    /imports:System.Collections.Generic
    /imports:System.Linq
    /imports:System.Text").Path
            Dim cmd = New MockVbi(rsp, _baseDirectory, {"b.vbx"})

            ' TODO (tomat): mscorlib, vbruntime order
            'AssertEx.Equal({GetType(Object).Assembly.Location,
            '                GetType(Microsoft.VisualBasic.Globals).Assembly.Location,
            '                "System", "System.Core", "System.Data", "System.Data.DataSetExtensions", "System.Xml", "System.Xml.Linq"},
            '               cmd.Arguments.AssemblyReferences.Select(Function(r)
            '                                                           Return If(r.Kind = ReferenceKind.AssemblyName,
            '                                                               (DirectCast(r, AssemblyNameReference)).Name,
            '                                                               (DirectCast(r, AssemblyFileReference)).Path)
            '                                                       End Function))

            AssertEx.Equal({"System", "System.Collections.Generic", "System.Linq", "System.Text"},
                       cmd.Arguments.CompilationOptions.GlobalImports.Select(Function(import) import.Clause.ToString()))
        End Sub

#Region "Win32ResourceOptions"

        Private Sub Assert_Win32ResourceArguments(args As String(), ByRef compilation As VisualBasicCompilation,
                           expected_ErrorCount As Int32, expected_ERRID As Int32, expected_first_count As Int32)
            Dim parsedArgs = DefaultParse(args, _baseDirectory)
            Dim errors As IEnumerable(Of DiagnosticInfo) = Nothing
            CommonCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, errors)
            Assert.Equal(expected_ErrorCount, errors.Count)
            Assert.Equal(expected_ERRID, errors.First.Code)
            Assert.Equal(expected_first_count, errors.First.Arguments.Count)
        End Sub

        <Fact, WorkItem(546028, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546028")>
        Public Sub Win32ResourceArguments()
            Dim compilation = CreateCompilationWithMscorlib40(New VisualBasicSyntaxTree() {})

            Assert_Win32ResourceArguments({"/win32manifest:..\here\there\everywhere\nonexistent"}, compilation, 1, ERRID.ERR_UnableToReadUacManifest2, 2)
            Assert_Win32ResourceArguments({"/Win32icon:\bogus"}, compilation, 1, ERRID.ERR_UnableToOpenResourceFile1, 2)
            Assert_Win32ResourceArguments({"/Win32Resource:\bogus"}, compilation, 1, ERRID.ERR_UnableToOpenResourceFile1, 2)
            Assert_Win32ResourceArguments({"/win32manifest:goo.win32data:bar.win32data2"}, compilation, 1, ERRID.ERR_UnableToReadUacManifest2, 2)
            Assert_Win32ResourceArguments({"/Win32icon:goo.win32data:bar.win32data2"}, compilation, 1, ERRID.ERR_UnableToOpenResourceFile1, 2)
            Assert_Win32ResourceArguments({"/Win32Resource:goo.win32data:bar.win32data2"}, compilation, 1, ERRID.ERR_UnableToOpenResourceFile1, 2)

        End Sub

        <Fact>
        Public Sub Win32IconContainsGarbage()
            Dim tmpFileName As String = Temp.CreateFile().WriteAllBytes(New Byte() {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}).Path
            Dim parsedArgs = DefaultParse({"/win32icon:" + tmpFileName}, _baseDirectory)
            Dim compilation = CreateCompilationWithMscorlib40(New VisualBasicSyntaxTree() {})
            Dim errors As IEnumerable(Of DiagnosticInfo) = Nothing
            CommonCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, errors)
            Assert.Equal(1, errors.Count())
            Assert.Equal(DirectCast(ERRID.ERR_ErrorCreatingWin32ResourceFile, Integer), errors.First().Code)
            Assert.Equal(1, errors.First().Arguments.Count())


            CleanupAllGeneratedFiles(tmpFileName)
        End Sub

        <WorkItem(217718, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=217718")>
        <Fact>
        Public Sub BadWin32Resource()
            Dim source = Temp.CreateFile(prefix:="", extension:=".vb").WriteAllText("
Module Test 
    Sub Main() 
    End Sub 
End Module").Path
            Dim badres = Temp.CreateFile().WriteAllBytes(New Byte() {0, 0}).Path

            Dim baseDir = Path.GetDirectoryName(source)
            Dim fileName = Path.GetFileName(source)

            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                Dim exitCode = New MockVisualBasicCompiler(Nothing, baseDir,
        {
            "/nologo",
            "/preferreduilang:en",
            "/win32resource:" + badres,
            source
        }).Run(outWriter)

                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC30136: Error creating Win32 resources: Unrecognized resource file format.", outWriter.ToString().Trim())
            End Using
            CleanupAllGeneratedFiles(source)
            CleanupAllGeneratedFiles(badres)
        End Sub

        <Fact>
        Public Sub Win32ResourceOptions_Valid()
            CheckWin32ResourceOptions({"/win32resource:a"}, "a", Nothing, Nothing, False)

            CheckWin32ResourceOptions({"/win32icon:b"}, Nothing, "b", Nothing, False)

            CheckWin32ResourceOptions({"/win32manifest:c"}, Nothing, Nothing, "c", False)

            CheckWin32ResourceOptions({"/nowin32manifest"}, Nothing, Nothing, Nothing, True)
        End Sub

        <Fact>
        Public Sub Win32ResourceOptions_Empty()
            CheckWin32ResourceOptions({"/win32resource"}, Nothing, Nothing, Nothing, False, Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32resource", ":<file>"))
            CheckWin32ResourceOptions({"/win32resource:"}, Nothing, Nothing, Nothing, False, Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32resource", ":<file>"))
            CheckWin32ResourceOptions({"/win32resource: "}, Nothing, Nothing, Nothing, False, Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32resource", ":<file>"))
            CheckWin32ResourceOptions({"/win32icon"}, Nothing, Nothing, Nothing, False, Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32icon", ":<file>"))
            CheckWin32ResourceOptions({"/win32icon:"}, Nothing, Nothing, Nothing, False, Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32icon", ":<file>"))
            CheckWin32ResourceOptions({"/win32icon: "}, Nothing, Nothing, Nothing, False, Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32icon", ":<file>"))

            CheckWin32ResourceOptions({"/win32manifest"}, Nothing, Nothing, Nothing, False, Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32manifest", ":<file>"))
            CheckWin32ResourceOptions({"/win32manifest:"}, Nothing, Nothing, Nothing, False, Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32manifest", ":<file>"))
            CheckWin32ResourceOptions({"/win32manifest: "}, Nothing, Nothing, Nothing, False, Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32manifest", ":<file>"))

            CheckWin32ResourceOptions({"/nowin32manifest"}, Nothing, Nothing, Nothing, True)
            CheckWin32ResourceOptions({"/nowin32manifest:"}, Nothing, Nothing, Nothing, False, Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/nowin32manifest:"))
            CheckWin32ResourceOptions({"/nowin32manifest: "}, Nothing, Nothing, Nothing, False, Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/nowin32manifest:"))
        End Sub

        <Fact>
        Public Sub Win32ResourceOptions_Combinations()
            ' last occurrence wins
            CheckWin32ResourceOptions({"/win32resource:r", "/win32resource:s"}, "s", Nothing, Nothing, False)
            ' illegal
            CheckWin32ResourceOptions({"/win32resource:r", "/win32icon:i"}, "r", "i", Nothing, False, Diagnostic(ERRID.ERR_IconFileAndWin32ResFile))
            ' documented as illegal, but works in dev10
            CheckWin32ResourceOptions({"/win32resource:r", "/win32manifest:m"}, "r", Nothing, "m", False, Diagnostic(ERRID.ERR_CantHaveWin32ResAndManifest))
            ' fine
            CheckWin32ResourceOptions({"/win32resource:r", "/nowin32manifest"}, "r", Nothing, Nothing, True)

            ' illegal
            CheckWin32ResourceOptions({"/win32icon:i", "/win32resource:r"}, "r", "i", Nothing, False, Diagnostic(ERRID.ERR_IconFileAndWin32ResFile))
            ' last occurrence wins
            CheckWin32ResourceOptions({"/win32icon:i", "/win32icon:j"}, Nothing, "j", Nothing, False)
            ' fine
            CheckWin32ResourceOptions({"/win32icon:i", "/win32manifest:m"}, Nothing, "i", "m", False)
            ' fine
            CheckWin32ResourceOptions({"/win32icon:i", "/nowin32manifest"}, Nothing, "i", Nothing, True)

            ' documented as illegal, but works in dev10
            CheckWin32ResourceOptions({"/win32manifest:m", "/win32resource:r"}, "r", Nothing, "m", False, Diagnostic(ERRID.ERR_CantHaveWin32ResAndManifest))
            ' fine
            CheckWin32ResourceOptions({"/win32manifest:m", "/win32icon:i"}, Nothing, "i", "m", False)
            ' last occurrence wins
            CheckWin32ResourceOptions({"/win32manifest:m", "/win32manifest:n"}, Nothing, Nothing, "n", False)
            ' illegal
            CheckWin32ResourceOptions({"/win32manifest:m", "/nowin32manifest"}, Nothing, Nothing, "m", True, Diagnostic(ERRID.ERR_ConflictingManifestSwitches))

            ' fine
            CheckWin32ResourceOptions({"/nowin32manifest", "/win32resource:r"}, "r", Nothing, Nothing, True)
            ' fine
            CheckWin32ResourceOptions({"/nowin32manifest", "/win32icon:i"}, Nothing, "i", Nothing, True)
            ' illegal
            CheckWin32ResourceOptions({"/nowin32manifest", "/win32manifest:m"}, Nothing, Nothing, "m", True, Diagnostic(ERRID.ERR_ConflictingManifestSwitches))
            ' fine
            CheckWin32ResourceOptions({"/nowin32manifest", "/nowin32manifest"}, Nothing, Nothing, Nothing, True)
        End Sub

        <Fact>
        Public Sub Win32ResourceOptions_SimplyInvalid()

            Dim parsedArgs = DefaultParse({"/win32resource", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32resource", ":<file>"))

            parsedArgs = DefaultParse({"/win32resource+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/win32resource+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/win32resource-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/win32resource-")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/win32icon", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32icon", ":<file>"))

            parsedArgs = DefaultParse({"/win32icon+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/win32icon+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/win32icon-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/win32icon-")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/win32manifest", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32manifest", ":<file>"))

            parsedArgs = DefaultParse({"/win32manifest+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/win32manifest+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/win32manifest-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/win32manifest-")) ' TODO: Dev11 reports ERR_ArgumentRequired

        End Sub

        Private Sub CheckWin32ResourceOptions(args As String(), expectedResourceFile As String, expectedIcon As String, expectedManifest As String, expectedNoManifest As Boolean, ParamArray diags As DiagnosticDescription())
            Dim parsedArgs = DefaultParse(args.Concat({"Test.vb"}), _baseDirectory)
            parsedArgs.Errors.Verify(diags)
            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(expectedResourceFile, parsedArgs.Win32ResourceFile)
            Assert.Equal(expectedIcon, parsedArgs.Win32Icon)
            Assert.Equal(expectedManifest, parsedArgs.Win32Manifest)
            Assert.Equal(expectedNoManifest, parsedArgs.NoWin32Manifest)
        End Sub

        <Fact>
        Public Sub ParseResourceDescription()
            Dim diags = New List(Of Diagnostic)()
            Dim desc As ResourceDescription

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "\somepath\someFile.goo.bar", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("someFile.goo.bar", desc.FileName)
            Assert.Equal("someFile.goo.bar", desc.ResourceName)
            Assert.True(desc.IsPublic)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "\somepath\someFile.goo.bar,someName", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("someFile.goo.bar", desc.FileName)
            Assert.Equal("someName", desc.ResourceName)
            Assert.True(desc.IsPublic)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "\somepath\someFile.goo.bar,someName,public", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("someFile.goo.bar", desc.FileName)
            Assert.Equal("someName", desc.ResourceName)
            Assert.True(desc.IsPublic)

            ' use file name in place of missing resource name
            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "\somepath\someFile.goo.bar,,private", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("someFile.goo.bar", desc.FileName)
            Assert.Equal("someFile.goo.bar", desc.ResourceName)
            Assert.False(desc.IsPublic)

            ' quoted accessibility is fine
            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "\somepath\someFile.goo.bar,,""private""", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("someFile.goo.bar", desc.FileName)
            Assert.Equal("someFile.goo.bar", desc.ResourceName)
            Assert.False(desc.IsPublic)

            ' leading commas are ignored...
            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", ",,\somepath\someFile.goo.bar,,private", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("someFile.goo.bar", desc.FileName)
            Assert.Equal("someFile.goo.bar", desc.ResourceName)
            Assert.False(desc.IsPublic)

            ' ...as long as there's no whitespace between them
            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", ", ,\somepath\someFile.goo.bar,,private", _baseDirectory, diags, embedded:=False)
            diags.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("resource", " "))
            diags.Clear()
            Assert.Null(desc)

            ' trailing commas are ignored...
            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "\somepath\someFile.goo.bar,,private", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("someFile.goo.bar", desc.FileName)
            Assert.Equal("someFile.goo.bar", desc.ResourceName)
            Assert.False(desc.IsPublic)

            ' ...even if there's whitespace between them
            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "\somepath\someFile.goo.bar,,private, ,", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("someFile.goo.bar", desc.FileName)
            Assert.Equal("someFile.goo.bar", desc.ResourceName)
            Assert.False(desc.IsPublic)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "\somepath\someFile.goo.bar,someName,publi", _baseDirectory, diags, embedded:=False)
            diags.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("resource", "publi"))
            diags.Clear()
            Assert.Null(desc)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "D:rive\relative\path,someName,public", _baseDirectory, diags, embedded:=False)
            diags.Verify(Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments("D:rive\relative\path"))
            diags.Clear()
            Assert.Null(desc)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "inva\l*d?path,someName,public", _baseDirectory, diags, embedded:=False)
            diags.Verify(Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments("inva\l*d?path"))
            diags.Clear()
            Assert.Null(desc)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", Nothing, _baseDirectory, diags, embedded:=False)
            diags.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("resource", ":<resinfo>"))
            diags.Clear()
            Assert.Null(desc)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "", _baseDirectory, diags, embedded:=False)
            diags.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("resource", ":<resinfo>"))
            diags.Clear()
            Assert.Null(desc)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", " ", _baseDirectory, diags, embedded:=False)
            diags.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("resource", " "))
            diags.Clear()
            Assert.Null(desc)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", " , ", _baseDirectory, diags, embedded:=False)
            diags.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("resource", " "))
            diags.Clear()
            Assert.Null(desc)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "path, ", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("path", desc.FileName)
            Assert.Equal("path", desc.ResourceName)
            Assert.True(desc.IsPublic)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", " ,name", _baseDirectory, diags, embedded:=False)
            diags.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("resource", " "))
            diags.Clear()
            Assert.Null(desc)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", " , , ", _baseDirectory, diags, embedded:=False)
            diags.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("resource", " "))
            diags.Clear()
            Assert.Null(desc)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "path, , ", _baseDirectory, diags, embedded:=False)
            diags.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("resource", " "))
            diags.Clear()
            Assert.Null(desc)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", " ,name, ", _baseDirectory, diags, embedded:=False)
            diags.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("resource", " "))
            diags.Clear()
            Assert.Null(desc)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", " , ,private", _baseDirectory, diags, embedded:=False)
            diags.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("resource", " "))
            diags.Clear()
            Assert.Null(desc)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "path,name,", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("path", desc.FileName)
            Assert.Equal("name", desc.ResourceName)
            Assert.True(desc.IsPublic)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "path,name,,", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("path", desc.FileName)
            Assert.Equal("name", desc.ResourceName)
            Assert.True(desc.IsPublic)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "path,name, ", _baseDirectory, diags, embedded:=False)
            diags.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("resource", " "))
            diags.Clear()
            Assert.Null(desc)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "path, ,private", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("path", desc.FileName)
            Assert.Equal("path", desc.ResourceName)
            Assert.False(desc.IsPublic)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", " ,name,private", _baseDirectory, diags, embedded:=False)
            diags.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("resource", " "))
            diags.Clear()
            Assert.Null(desc)
        End Sub

        <Fact>
        Public Sub ManagedResourceOptions()
            Dim parsedArgs As VisualBasicCommandLineArguments
            Dim resourceDescription As ResourceDescription

            parsedArgs = DefaultParse({"/resource:a", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.DisplayHelp)
            resourceDescription = parsedArgs.ManifestResources.Single()
            Assert.Null(resourceDescription.FileName) ' since embedded
            Assert.Equal("a", resourceDescription.ResourceName)

            parsedArgs = DefaultParse({"/res:b", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.DisplayHelp)
            resourceDescription = parsedArgs.ManifestResources.Single()
            Assert.Null(resourceDescription.FileName) ' since embedded
            Assert.Equal("b", resourceDescription.ResourceName)

            parsedArgs = DefaultParse({"/linkresource:c", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.DisplayHelp)
            resourceDescription = parsedArgs.ManifestResources.Single()
            Assert.Equal("c", resourceDescription.FileName)
            Assert.Equal("c", resourceDescription.ResourceName)

            parsedArgs = DefaultParse({"/linkres:d", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.DisplayHelp)
            resourceDescription = parsedArgs.ManifestResources.Single()
            Assert.Equal("d", resourceDescription.FileName)
            Assert.Equal("d", resourceDescription.ResourceName)
        End Sub

        Shared Iterator Function ManagedResourceOptions_SimpleErrors_Data() As IEnumerable(Of Object())
            Yield {"/resource:", "a.vb", Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("resource", ":<resinfo>")}
            Yield {"/resource: ", "a.vb", Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("resource", ":<resinfo>")}
            Yield {"/resource", "a.vb", Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("resource", ":<resinfo>")}
            Yield {"/RES+", "a.vb", Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/RES+")} ' TODO: Dev11 reports ERR_ArgumentRequired
            Yield {"/res-:", "a.vb", Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/res-:")} ' TODO: Dev11 reports ERR_ArgumentRequired
            Yield {"/linkresource:", "a.vb", Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("linkresource", ":<resinfo>")}
            Yield {"/linkresource: ", "a.vb", Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("linkresource", ":<resinfo>")}
            Yield {"/linkresource", "a.vb", Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("linkresource", ":<resinfo>")}
            Yield {"/linkRES+", "a.vb", Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/linkRES+")} ' TODO: Dev11 reports ERR_ArgumentRequired
            Yield {"/linkres-:", "a.vb", Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/linkres-:")} ' TODO: Dev11 reports ERR_ArgumentRequired
        End Function

        <Theory, MemberData("ManagedResourceOptions_SimpleErrors_Data")>
        Public Sub ManagedResourceOptions_SimpleErrors(arg0 As String, arg1 As String, diag As DiagnosticDescription)
            Dim parsedArgs = DefaultParse({arg0, arg1}, _baseDirectory)
            parsedArgs.Errors.Verify(diag)
        End Sub

        <Fact>
        Public Sub ModuleManifest()
            Dim parsedArgs = DefaultParse({"/win32manifest:blah", "/target:module", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.WRN_IgnoreModuleManifest))

            ' Illegal, but not clobbered.
            Assert.Equal("blah", parsedArgs.Win32Manifest)
        End Sub
#End Region

#Region "Argument Parsing"
        <Fact>
        Public Sub ArgumentParsing()
            Dim parsedArgs = InteractiveParse({"\\"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(".exe"))
            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(True, parsedArgs.SourceFiles.Any())
        End Sub

        <Theory,
            InlineData({"a + b"}, False, False, True),
            InlineData({"a + b; c"}, False, False, True),
            InlineData({"/help"}, False, True, False),
            InlineData({"/?"}, False, True, False),
            InlineData({"@dd"}, True, False, False),
            InlineData({"c /define:DEBUG"}, False, False, True),
            InlineData({"""/r d.dll"""}, False, False, True)>
        Public Sub ArgumentParsing_DisplayHelp(args() As String, HasErrors As Boolean, DisplayHelp As Boolean, HasSourceFiles As Boolean)
            Dim parsedArgs = InteractiveParse(args, _baseDirectory)
            Assert.Equal(HasErrors, parsedArgs.Errors.Any())
            Assert.Equal(DisplayHelp, parsedArgs.DisplayHelp)
            Assert.Equal(HasSourceFiles, parsedArgs.SourceFiles.Any())
        End Sub

        <Theory,
            InlineData({"/version"}, False, True, False),
            InlineData({"/version", "c"}, False, True, True),
            InlineData({"/version:something"}, True, False, False)>
        Public Sub ArgumentParsing_DispalyVersion(args() As String, HasErrors As Boolean, DisplayVersion As Boolean, HasSourceFiles As Boolean)
            Dim parsedArgs = InteractiveParse(args, _baseDirectory)
            Assert.Equal(HasErrors, parsedArgs.Errors.Any())
            Assert.Equal(DisplayVersion, parsedArgs.DisplayVersion)
            Assert.Equal(HasSourceFiles, parsedArgs.SourceFiles.Any())
        End Sub
#End Region

#Region "LangVersion"

        <Theory,
        InlineData({"/langversion:9", "a.VB"}, LanguageVersion.VisualBasic9), InlineData({"/langVERSION:9.0", "a.vb"}, LanguageVersion.VisualBasic9),
        InlineData({"/langVERSION:10", "a.vb"}, LanguageVersion.VisualBasic10), InlineData({"/langVERSION:10.0", "a.vb"}, LanguageVersion.VisualBasic10),
        InlineData({"/langVERSION:11", "a.vb"}, LanguageVersion.VisualBasic11), InlineData({"/langVERSION:11.0", "a.vb"}, LanguageVersion.VisualBasic11),
        InlineData({"/langVERSION:12", "a.vb"}, LanguageVersion.VisualBasic12), InlineData({"/langVERSION:12.0", "a.vb"}, LanguageVersion.VisualBasic12),
        InlineData({"/langVERSION:14", "a.vb"}, LanguageVersion.VisualBasic14), InlineData({"/langVERSION:14.0", "a.vb"}, LanguageVersion.VisualBasic14),
        InlineData({"/langVERSION:15", "a.vb"}, LanguageVersion.VisualBasic15), InlineData({"/langVERSION:15.0", "a.vb"}, LanguageVersion.VisualBasic15),
        InlineData({"/langVERSION:15.3", "a.vb"}, LanguageVersion.VisualBasic15_3), InlineData({"/langVERSION:15.5", "a.vb"}, LanguageVersion.VisualBasic15_5)>
        Public Sub LangVersion(Args() As String, expectedLanguageVersion As LanguageVersion)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(expectedLanguageVersion, parsedArgs.ParseOptions.LanguageVersion)
            ' The canary check is a reminder that this test needs to be updated when a language version is added
            LanguageVersionAdded_Canary()
        End Sub

        <Fact>
        Public Sub LangVersion_Default()
            Dim parsedArgs = DefaultParse({"/langVERSION:default", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.Default, parsedArgs.ParseOptions.SpecifiedLanguageVersion)
            Assert.Equal(LanguageVersion.VisualBasic15, parsedArgs.ParseOptions.LanguageVersion)
        End Sub

        <Fact>
        Public Sub LangVersion_Latest()
            Dim parsedArgs = DefaultParse({"/langVERSION:latest", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.Latest, parsedArgs.ParseOptions.SpecifiedLanguageVersion)
            Assert.Equal(LanguageVersion.VisualBasic15_5, parsedArgs.ParseOptions.LanguageVersion)
        End Sub

        <Fact>
        Public Sub LangVersion_Default_CurrentVersion()
            ' default: "current version"
            Dim parsedArgs = DefaultParse({"a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic15, parsedArgs.ParseOptions.LanguageVersion)
        End Sub

        <Fact> Public Sub LangVersion_Overriding()
            Dim parsedArgs = DefaultParse({"/langVERSION:10", "/langVERSION:9.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic9, parsedArgs.ParseOptions.LanguageVersion)
        End Sub

        <Theory, InlineData({"/langVERSION", "a.vb"}), InlineData({"/langVERSION:", "a.vb"})>
        Public Sub LangVersion_Error_Argument_Required(ParamArray Args() As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("langversion", ":<number>"))
            Assert.Equal(LanguageVersion.VisualBasic15, parsedArgs.ParseOptions.LanguageVersion)
        End Sub

        <Fact> Public Sub LangVersion_Error_BadSwitch()
            Dim parsedArgs = DefaultParse({"/langVERSION+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/langVERSION+")) ' TODO: Dev11 reports ERR_ArgumentRequired
            Assert.Equal(LanguageVersion.VisualBasic15, parsedArgs.ParseOptions.LanguageVersion)
        End Sub

        <Fact> Public Sub LAngVersion_Error_InvalidSwitchValue()
            Dim parsedArgs = DefaultParse({"/langVERSION:8", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("langversion", "8"))
            Assert.Equal(LanguageVersion.VisualBasic15, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:" & (LanguageVersion.VisualBasic12 + 1), "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("langversion", CStr(LanguageVersion.VisualBasic12 + 1)))
            Assert.Equal(LanguageVersion.VisualBasic15, parsedArgs.ParseOptions.LanguageVersion)
        End Sub

#End Region

#Region "DelaySign"
        <Theory, InlineData({"/delaysign", "a.cs"}, True), InlineData({"/delaysign+", "a.cs"}, True), InlineData({"/DELAYsign-", "a.cs"}, False)>
        Public Sub DelaySign(Args() As String, CheckDelaySign As Boolean)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.NotNull(parsedArgs.CompilationOptions.DelaySign)
            Assert.Equal(CheckDelaySign, parsedArgs.CompilationOptions.DelaySign)
        End Sub

        <Fact>
        Sub DelaySign()
            Dim parsedArgs = DefaultParse({"/delaysign:-", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("delaysign"))

            parsedArgs = InteractiveParse({"/d:a=1"}, _baseDirectory) ' test default value
            parsedArgs.Errors.Verify()
            Assert.Null(parsedArgs.CompilationOptions.DelaySign)
        End Sub
#End Region

#Region "OutputVerbose"
        <Fact, WorkItem(546113, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546113")>
        Public Sub OutputVerbose()
            Dim parsedArgs = InteractiveParse({"/d:a=1"}, _baseDirectory) ' test default value
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Normal, parsedArgs.OutputLevel)
        End Sub

        <Theory,
            InlineData({"/verbose", "a.vb"}, OutputLevel.Verbose),
            InlineData({"/verbose+", "a.vb"}, OutputLevel.Verbose),
            InlineData({"/verbose-", "a.vb"}, OutputLevel.Normal),
            InlineData({"/quiet", "/verbose", "a.vb"}, OutputLevel.Verbose),
            InlineData({"/quiet", "/verbose-", "a.vb"}, OutputLevel.Normal)>
        Friend Sub OutputVerbose_DefaultParse(Args() As String, ExpectedOutputLevel As OutputLevel)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(ExpectedOutputLevel, parsedArgs.OutputLevel)
        End Sub

        <Theory, InlineData({"/VERBOSE:-", "a.vb"}, "/VERBOSE:-"), InlineData({"/verbOSE:", "a.vb"}, "/verbOSE:")>
        Public Sub OutputVerbose_Errors_BadSwitch(Args() As String, WithArg As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments(WithArg))
        End Sub

        <Theory, InlineData({"/verbose-:", "a.vb"}, "verbose"), InlineData({"/verbose+:", "a.vb"}, "verbose")>
        Public Sub OutputVerbose_Errors_SwitchNeedsBool(Args() As String, WithArg As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments(WithArg))
        End Sub
#End Region

#Region "OutputQuiet"
        <Fact, WorkItem(546113, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546113")>
        Friend Sub OutputQuiet()
            Dim parsedArgs = InteractiveParse({"/d:a=1"}, _baseDirectory) ' test default value
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Normal, parsedArgs.OutputLevel)
        End Sub

        <Theory, WorkItem(546113, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546113"),
        InlineData({"/quiet", "a.vb"}, OutputLevel.Quiet), InlineData({"/quiet+", "a.vb"}, OutputLevel.Quiet), InlineData({"/quiet-", "a.vb"}, OutputLevel.Normal),
        InlineData({"/verbose", "/quiet", "a.vb"}, OutputLevel.Quiet), InlineData({"/verbose", "/quiet-", "a.vb"}, OutputLevel.Normal)>
        Friend Sub OutputQuiet_DefaultParse(Args() As String, ExpectedOutputLevel As OutputLevel)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(ExpectedOutputLevel, parsedArgs.OutputLevel)
        End Sub

        <Theory, InlineData({"/QUIET:-", "a.vb"}, "/QUIET:-"), InlineData({"/quiET:", "a.vb"}, "/quiET:")>
        Public Sub OutputQuiet_WRN_BadSwitchl(Args() As String, WithArg As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments(WithArg))
        End Sub

        <Theory, InlineData({"/quiet-:", "a.vb"}, "quiet"), InlineData({"/quiet+:", "a.vb"}, "quiet")>
        Public Sub OutputQuiet_ERR_SwitchNeedsBool(Args() As String, WithArg As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments(WithArg))
        End Sub
#End Region

#Region "Optimize"
        <Theory, InlineData({"/optimize", "a.vb"}, OptimizationLevel.Release), InlineData({"a.vb"}, OptimizationLevel.Debug),
         InlineData({"/OPTIMIZE+", "a.vb"}, OptimizationLevel.Release), InlineData({"/optimize-", "a.vb"}, OptimizationLevel.Debug),
         InlineData({"/optimize-", "/optimize+", "a.vb"}, OptimizationLevel.Release)>
        Friend Sub Optimize(Args() As String, ExpectedOptimizationLevel As OptimizationLevel)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(ExpectedOptimizationLevel, parsedArgs.CompilationOptions.OptimizationLevel)
        End Sub
        <Theory, InlineData({"/OPTIMIZE:", "a.cs"}, "optimize"), InlineData({"/OPTIMIZE+:", "a.cs"}, "optimize"), InlineData({"/optimize-:", "a.cs"}, "optimize")>
        Public Sub Optimize_Err_SwitchNeedsBool(Args() As String, WithArg As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments(WithArg).WithLocation(1, 1))
        End Sub
#End Region

#Region "Deterministic"
        <WorkItem(5417, "DevDiv"), Theory,
         InlineData({"a.vb"}, False), InlineData({"/deterministic+", "a.vb"}, True), InlineData({"/deterministic", "a.vb"}, True),
         InlineData({"/DETERMINISTIC+", "a.vb"}, True), InlineData({"/deterministic-", "a.vb"}, False)>
        Public Sub Deterministic(args() As String, IsDeterministic As Boolean)
            Dim ParsedArgs = DefaultParse(args, _baseDirectory)
            ParsedArgs.Errors.Verify()
            Assert.Equal(IsDeterministic, ParsedArgs.CompilationOptions.Deterministic)
        End Sub
#End Region

#Region "Parallel"
        <WorkItem(546301, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546301"), Theory,
         InlineData({"/parallel", "a.vb"}, True), InlineData({"/p", "a.vb"}, True), InlineData({"a.vb"}, True), InlineData({"/PARALLEL+", "a.vb"}, True),
         InlineData({"/PARALLEL-", "a.vb"}, False), InlineData({"/PArallel-", "/PArallel+", "a.vb"}, True), InlineData({"/P+", "a.vb"}, True), InlineData({"/P-", "a.vb"}, False),
         InlineData({"/P-", "/P+", "a.vb"}, True)>
        Public Sub Parallel(args() As String, IsConcurrentBuild As Boolean)
            Dim parsedArgs = DefaultParse(args, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(IsConcurrentBuild, parsedArgs.CompilationOptions.ConcurrentBuild)
        End Sub

        <Theory,
         InlineData({"/parallel:", "a.vb"}, "parallel"), InlineData({"/parallel+:", "a.vb"}, "parallel"), InlineData({"/parallel-:", "a.vb"}, "parallel"),
         InlineData({"/p:", "a.vb"}, "p"), InlineData({"/p+:", "a.vb"}, "p"), InlineData({"/p-:", "a.vb"}, "p")>
        Sub Parallel_Errors(args() As String, arg As String)
            Dim parsedArgs = DefaultParse(args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments(arg))
        End Sub
#End Region

#Region "SubsystemVersionTests"
        <Theory>
        <InlineData({"/subsystemversion:4.0", "a.vb"}, 4, 0)>
        <InlineData({"/subsystemversion:0.0", "a.vb"}, 0, 0)> ' wrongly supported subsystem version. CompilationOptions data will be faithful to the user input. It is normalized at the time of emit.
        <InlineData({"/subsystemversion:0", "a.vb"}, 0, 0)>' no error in Dev11
        <InlineData({"/subsystemversion:3.99", "a.vb"}, 3, 99)>' no error in Dev11
        <InlineData({"/subsystemversion:4.0", "/subsystemversion:5.333", "a.vb"}, 5, 333)>' no error in Dev11
        Public Sub SubsystemVersionTests(Args() As String, Major As Int32, Minor As Int32)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(SubsystemVersion.Create(Major, Minor), parsedArgs.EmitOptions.SubsystemVersion)
        End Sub

        <Fact>
        Public Sub SubsystemVersionTests()
            Dim parsedArgs = DefaultParse({"/subsystemversion:4.0", "/subsystemversion:5.333", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(SubsystemVersion.Create(5, 333), parsedArgs.EmitOptions.SubsystemVersion)

            parsedArgs = DefaultParse({"/subsystemversion:4.2 ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            ' WRN_BadSwitch
            parsedArgs = DefaultParse({"/subsystemversion-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/subsystemversion-")) ' TODO: Dev11 reports ERRID.ERR_ArgumentRequired
        End Sub

        <Theory,
            InlineData({"/subsystemversion:", "a.vb"}, {"subsystemversion", ":<version>"}),
            InlineData({"/subsystemversion", "a.vb"}, {"subsystemversion", ":<version>"}),
            InlineData({"/subsystemversion: ", "a.vb"}, {"subsystemversion", ":<version>"})>
        Public Sub SubsystemVersionTests_ERR_ArgumentRequired(Args() As String, WithArgs() As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments(WithArgs))
        End Sub

        <Theory,
            InlineData({"/subsystemversion: 4.1", "a.vb"}, " 4.1"),
            InlineData({"/subsystemversion:4 .0", "a.vb"}, "4 .0"),
            InlineData({"/subsystemversion:4. 0", "a.vb"}, "4. 0"),
            InlineData({"/subsystemversion:.", "a.vb"}, "."),
            InlineData({"/subsystemversion:4.", "a.vb"}, "4."),
            InlineData({"/subsystemversion:.0", "a.vb"}, ".0"),
            InlineData({"/subsystemversion:4.65536", "a.vb"}, "4.65536"),
            InlineData({"/subsystemversion:65536.0", "a.vb"}, "65536.0"),
            InlineData({"/subsystemversion:-4.0", "a.vb"}, "-4.0")>
        Public Sub SubsystemVersionTests_ERR_InvalidSubSystemVersion(Args() As String, WithArg As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments(WithArg))
            '' TODO: incompatibilities: versions lower than '6.2' and 'arm', 'winmdobj', 'appcontainer'
        End Sub
#End Region

#Region "Codepage"
        <Theory, InlineData({"/CodePage:1200", "a.vb"}, "Unicode"), InlineData({"/CodePage:1200", "/CodePage:65001", "a.vb"}, "Unicode (UTF-8)")>
        Public Sub Codepage(Args() As String, ExpectedEncodingName As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(ExpectedEncodingName, parsedArgs.Encoding.EncodingName)
        End Sub

        <Theory, InlineData({"/codepage:0", "a.vb"}, {"0"}), InlineData({"/codepage:abc", "a.vb"}, {"abc"}), InlineData({"/codepage:-5", "a.vb"}, {"-5"})>
        Public Sub Codepage_ERR_BadCodepage(Args() As String, WithArgs As String())
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadCodepage).WithArguments(WithArgs))
        End Sub

        <Theory, InlineData({"/codepage: ", "a.vb"}, {"codepage", ":<number>"}), InlineData({"/codepage:", "a.vb"}, {"codepage", ":<number>"}), InlineData({"/codepage", "a.vb"}, {"codepage", ":<number>"})>
        Public Sub Codepage_ERR_ArgumentRequired(Args() As String, WithArgs() As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments(WithArgs))
        End Sub

        <Theory, InlineData({"/codepage+", "a.vb"}, {"/codepage+"})>' Dev11 reports ERR_ArgumentRequired
        Public Sub Codepage_WRN_BadSwitch(Args() As String, WithArgs() As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments(WithArgs))
        End Sub
#End Region

#Region "ChecksumAlgorithm"
        Public Shared Iterator Function ChecksumAlgorithm_Data() As IEnumerable(Of Object())
            Yield {SourceHashAlgorithm.Sha1, HashAlgorithmName.SHA256, "/checksumAlgorithm:sHa1", "a.cs"}
            Yield {SourceHashAlgorithm.Sha256, HashAlgorithmName.SHA256, "/checksumAlgorithm:sha256", "a.cs"}
            Yield {SourceHashAlgorithm.Sha1, HashAlgorithmName.SHA256, "a.cs"}
        End Function

        <Theory, MemberData(NameOf(ChecksumAlgorithm) & "_Data"), WorkItem(24735, "https://github.com/dotnet/roslyn/issues/24735")>
        Public Sub ChecksumAlgorithm(ExpectedSourceHashAlgorithm As SourceHashAlgorithm, ExpectedHashAlgorithmName As HashAlgorithmName, ParamArray Args As String())
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(ExpectedSourceHashAlgorithm, parsedArgs.ChecksumAlgorithm)
            Assert.Equal(ExpectedHashAlgorithmName, parsedArgs.EmitOptions.PdbChecksumAlgorithm)
        End Sub

        <Theory, InlineData({"/checksumAlgorithm:256", "a.cs"}, {"256"}), InlineData({"/checksumAlgorithm:sha-1", "a.cs"}, {"sha-1"}), InlineData({"/checksumAlgorithm:sha", "a.cs"}, {"sha"})>
        Public Sub ChecksumAlgorithm_ERR_BadChecksumAlgorithm(Args() As String, WithArgs() As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadChecksumAlgorithm).WithArguments(WithArgs))
        End Sub

        <Theory,
            InlineData({"/checksumAlgorithm: ", "a.cs"}, {"checksumalgorithm", ":<algorithm>"}), InlineData({"/checksumAlgorithm:", "a.cs"}, {"checksumalgorithm", ":<algorithm>"}), InlineData({"/checksumAlgorithm", "a.cs"}, {"checksumalgorithm", ":<algorithm>"})>
        Public Sub ChecksumAlgorithm_ERR_ArgumentRequired(Args() As String, WithArgs() As String)
            Dim parsedArgs = DefaultParse({"/checksumAlgorithm: ", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("checksumalgorithm", ":<algorithm>"))
        End Sub

        <Theory, InlineData({"/checksumAlgorithm: ", "a.cs"}, {"checksumalgorithm", ":<algorithm>"})>
        Public Sub ChecksumAlgorithm_WRN_BadSwitch(Args() As String, WithArgs() As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments(WithArgs))
        End Sub
#End Region

#Region "MainTypeName"
        <Theory, InlineData({"/main:A.B.C", "a.vb"}, "A.B.C"), InlineData({"/Main:A.B.C", "/M:X.Y.Z", "a.vb"}, "X.Y.Z")>
        Public Sub MainTypeName(Args() As String, ExpectedMainTypeName As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(ExpectedMainTypeName, parsedArgs.CompilationOptions.MainTypeName)
        End Sub

        <Theory, InlineData({"/MAIN: ", "a.vb"}, {"main", ":<class>"}, True), InlineData({"/maiN:", "a.vb"}, {"main", ":<class>"}), InlineData({"/m", "a.vb"}, {"m", ":<class>"})>
        Public Sub MainTypeName_ERR_ArgumentRequired(Args() As String, WithArgs() As String, Optional CheckMainTypeName As Boolean = False)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments(WithArgs))
            If CheckMainTypeName Then Assert.Equal(Nothing, parsedArgs.CompilationOptions.MainTypeName) ' EDMAURER Dev11 accepts and MainTypeName is " "
        End Sub

        <Theory, InlineData({"/MAIN:XYZ", "/t:library", "a.vb"}, OutputKind.DynamicallyLinkedLibrary), InlineData({"/MAIN:XYZ", "/t:module", "a.vb"}, OutputKind.NetModule)>
        Public Sub MainTypeName_OutputKind(Args() As String, ExpectedOutputKind As OutputKind)
            ' incompatibilities ignored by Dev11
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("XYZ", parsedArgs.CompilationOptions.MainTypeName)
            Assert.Equal(ExpectedOutputKind, parsedArgs.CompilationOptions.OutputKind)
        End Sub

        <Theory, InlineData({"/m+", "a.vb"}, {"/m+"})> ' Dev11 reports ERR_ArgumentRequired
        Public Sub MainTypeName_WRN_BadSwitch(Args() As String, WithArgs() As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments(WithArgs))
        End Sub
#End Region

#Region "OptionCompare"
        <Theory,
            InlineData({"/optioncompare"}, {"optioncompare", ":binary|text"}, True, 1, False),
            InlineData({"/optioncompare:text", "/optioncompare"}, {"optioncompare", ":binary|text"}, True, 1, True),
            InlineData({"/opTioncompare:Text", "/optioncomparE:bINARY"}, Nothing, False, 0, False),
            InlineData({"/d:a=1"}, Nothing, False, 0, False)>
        Public Sub OptionCompare(Args() As String, WithArgs() As String, Verify As Boolean, ExpectedErrorCount As Integer, ExpectedOptionCompareText As Boolean)
            Dim parsedArgs = InteractiveParse(Args, _baseDirectory)
            If Verify Then parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments(WithArgs))
            Assert.Equal(ExpectedErrorCount, parsedArgs.Errors.Length)
            Assert.Equal(ExpectedOptionCompareText, parsedArgs.CompilationOptions.OptionCompareText)
        End Sub
#End Region

#Region "OptionExplicit"
        <Theory, InlineData({"/optiONexplicit"}), InlineData({"/optionexplicit+", "/optiONexplicit-", "/optiONexpliCIT+"}), InlineData({"/d:a=1"})>
        Public Sub OptionExplicit(ParamArray Args() As String)
            Dim parsedArgs = InteractiveParse(Args, _baseDirectory)
            Assert.Equal(0, parsedArgs.Errors.Length)
            Assert.Equal(True, parsedArgs.CompilationOptions.OptionExplicit)
        End Sub

        <Theory, InlineData({"/optiONexplicit:+"}), InlineData({"/optiONexplicit-:"}), InlineData({"/optionexplicit+", "/optiONexplicit-:"})>
        Public Sub OptionExplicit_ERR_SwitchNeedsBool(ParamArray Args() As String)
            Dim parsedArgs = InteractiveParse(Args, _baseDirectory)
            Assert.Equal(1, parsedArgs.Errors.Length)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optionexplicit"))
            Assert.Equal(True, parsedArgs.CompilationOptions.OptionExplicit)
        End Sub
#End Region

#Region "OptionInfer"
        <Theory, InlineData({"/optiONinfer"}, True), InlineData({"/optioninfer+", "/optioninfeR-", "/OptionInfer+"}, True), InlineData({"/d:a=1"}, False)>
        Public Sub OptionInfer(Args() As String, ExpectedOptionInfer As Boolean)
            Dim parsedArgs = InteractiveParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(ExpectedOptionInfer, parsedArgs.CompilationOptions.OptionInfer)
        End Sub

        <Theory, InlineData({"/OptionInfer:+"}), InlineData({"/OPTIONinfer-:"}), InlineData({"/optioninfer+", "/optioninFER-:"})>
        Public Sub OptionInfer_ERR_SwitchNeedsBool(ParamArray Args() As String)
            Dim parsedArgs = InteractiveParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optioninfer"))
        End Sub
#End Region

        Private ReadOnly s_VBC_VER As Double = PredefinedPreprocessorSymbols.CurrentVersionNumber

#Region "LanguageVersion"
        <Fact>
        Public Sub LanguageVersionAdded_Canary()
            ' When a new version is added, this test will break. This list must be checked:
            ' - update the command-line error for bad /langver flag (<see cref="ERRID.IDS_VBCHelp"/>)
            ' - update the "UpgradeProject" codefixer (not yet supported in VB)
            ' - update the IDE drop-down for selecting Language Version (not yet supported in VB)
            ' - update all the tests that call this canary
            ' - update the command-line documentation (CommandLine.md)
            AssertEx.SetEqual({"default", "9", "10", "11", "12", "14", "15", "15.3", "15.5", "latest"}, GetLanguageVersions)
            ' For minor versions, the format should be "x.y", such as "15.3"
        End Sub

        <Fact>
        Public Sub LanguageVersion_GetErrorCode()
            Dim versions = [Enum].GetValues(GetType(LanguageVersion)).
                              Cast(Of LanguageVersion).
                              Except({LanguageVersion.Default, LanguageVersion.Latest}).
                              Select(Function(v) v.GetErrorName())

            Dim errorCodes = {"9.0", "10.0", "11.0", "12.0", "14.0", "15.0", "15.3", "15.5"}

            AssertEx.SetEqual(versions, errorCodes)

            ' The canary check is a reminder that this test needs to be updated when a language version is added
            LanguageVersionAdded_Canary()
        End Sub

        <Fact>
        Public Sub LanguageVersion_MapSpecifiedToEffectiveVersion_Default()
            Assert.Equal(LanguageVersion.VisualBasic15, LanguageVersion.Default.MapSpecifiedToEffectiveVersion())
        End Sub

        <Fact>
        Public Sub LanguageVersion_MapSpecifiedToEffectiveVersion_Latest()
            Assert.Equal(LanguageVersion.VisualBasic15_5, LanguageVersion.Latest.MapSpecifiedToEffectiveVersion())
        End Sub

        <Fact>
        Public Sub LanguageVersion_MapSpecifiedToEffectiveVersion()
            Assert.Equal(LanguageVersion.VisualBasic9, LanguageVersion.VisualBasic9.MapSpecifiedToEffectiveVersion())
            Assert.Equal(LanguageVersion.VisualBasic10, LanguageVersion.VisualBasic10.MapSpecifiedToEffectiveVersion())
            Assert.Equal(LanguageVersion.VisualBasic11, LanguageVersion.VisualBasic11.MapSpecifiedToEffectiveVersion())
            Assert.Equal(LanguageVersion.VisualBasic12, LanguageVersion.VisualBasic12.MapSpecifiedToEffectiveVersion())
            Assert.Equal(LanguageVersion.VisualBasic14, LanguageVersion.VisualBasic14.MapSpecifiedToEffectiveVersion())
            Assert.Equal(LanguageVersion.VisualBasic15, LanguageVersion.VisualBasic15.MapSpecifiedToEffectiveVersion())
            Assert.Equal(LanguageVersion.VisualBasic15_3, LanguageVersion.VisualBasic15_3.MapSpecifiedToEffectiveVersion())
            Assert.Equal(LanguageVersion.VisualBasic15_5, LanguageVersion.VisualBasic15_5.MapSpecifiedToEffectiveVersion())

            ' The canary check is a reminder that this test needs to be updated when a language version is added
            LanguageVersionAdded_Canary()
        End Sub

        <Theory,
        InlineData("9", True, LanguageVersion.VisualBasic9),
        InlineData("9.0", True, LanguageVersion.VisualBasic9),
        InlineData("10", True, LanguageVersion.VisualBasic10),
        InlineData("10.0", True, LanguageVersion.VisualBasic10),
        InlineData("11", True, LanguageVersion.VisualBasic11),
        InlineData("11.0", True, LanguageVersion.VisualBasic11),
        InlineData("12", True, LanguageVersion.VisualBasic12),
        InlineData("12.0", True, LanguageVersion.VisualBasic12),
        InlineData("14", True, LanguageVersion.VisualBasic14),
        InlineData("14.0", True, LanguageVersion.VisualBasic14),
        InlineData("15", True, LanguageVersion.VisualBasic15),
        InlineData("15.0", True, LanguageVersion.VisualBasic15),
        InlineData("15.3", True, LanguageVersion.VisualBasic15_3),
        InlineData("15.5", True, LanguageVersion.VisualBasic15_5),
        InlineData("DEFAULT", True, LanguageVersion.Default),
        InlineData("default", True, LanguageVersion.Default),
        InlineData("LATEST", True, LanguageVersion.Latest),
        InlineData("latest", True, LanguageVersion.Latest),
        InlineData(Nothing, False, LanguageVersion.Default),
        InlineData("bad", False, LanguageVersion.Default)>
        Public Sub LanguageVersion_TryParseDisplayString(input As String, success As Boolean, expected As LanguageVersion)
            Dim version As LanguageVersion
            Assert.Equal(success, input.TryParse(version))
            Assert.Equal(expected, version)

            ' The canary check is a reminder that this test needs to be updated when a language version is added
            LanguageVersionAdded_Canary()
        End Sub

        Private Shared Function GetLanguageVersions() As IEnumerable(Of String)
            Return [Enum].GetValues(GetType(LanguageVersion)).Cast(Of LanguageVersion)().Select(Function(v) v.ToDisplayString())
        End Function

        <Fact>
        Public Sub LanguageVersion_ListLangVersions()
            Dim dir = Temp.CreateDirectory()
            Using outWriter As New StringWriter()
                Dim exitCode As Integer = New MockVisualBasicCompiler(Nothing, dir.ToString(), {"/langversion:?"}).Run(outWriter, Nothing)
                Assert.Equal(0, exitCode)

                Dim actual = outWriter.ToString()
                Dim expected = GetLanguageVersions()
                Dim acceptableSurroundingChar = {CChar(vbCr), CChar(vbLf), "("c, ")"c, " "c}

                For Each v In expected
                    Dim foundIndex = actual.IndexOf(v)
                    Assert.True(foundIndex > 0, $"Missing version '{v}'")
                    Assert.True(Array.IndexOf(acceptableSurroundingChar, actual(foundIndex - 1)) >= 0)
                    Assert.True(Array.IndexOf(acceptableSurroundingChar, actual(foundIndex + v.Length)) >= 0)
                Next
            End Using
        End Sub
#End Region

#Region "TestDefines"
        <Theory,
     InlineData({"/D:a=True,b=1", "a.vb"}, {"a", CObj(True)}, {"b", CObj(1)}, {"TARGET", CObj("exe")}, {"VBC_VER", CObj(Double.NaN)}),
     InlineData({"/D:a=True,b=1", "/define:a=""123"",b=False", "a.vb"}, {"a", CObj("123")}, {"b", CObj(False)}, {"TARGET", CObj("exe")}, {"VBC_VER", CObj(Double.NaN)}),
     InlineData({"/D:a=""\\\\a"",b=""\\\\\b""", "a.vb"}, {"a", CObj("\\\\a")}, {"b", CObj("\\\\\b")}, {"TARGET", CObj("exe")}, {"VBC_VER", CObj(Double.NaN)}),
     InlineData({"/define:DEBUG", "a.vb"}, {"DEBUG", CObj(True)}, {"TARGET", CObj("exe")}, {"VBC_VER", CObj(Double.NaN)}),
     InlineData({"/D:TARGET=True,VBC_VER=1", "a.vb"}, {"TARGET", CObj(True)}, {"VBC_VER", CObj(1)})>
        Public Sub TestDefines(args() As String, ParamArray symbols()() As Object)
            Dim parsedArgs = DefaultParse(args, _baseDirectory)
            Assert.False(parsedArgs.Errors.Any)
            Assert.Equal(symbols.Length, parsedArgs.ParseOptions.PreprocessorSymbols.Length)
            Dim sortedDefines = parsedArgs.ParseOptions.PreprocessorSymbols.Select(Function(d) New With {d.Key, d.Value}).OrderBy(Function(o) o.Key)
            For i = 0 To symbols.Length - 1
                Assert.Equal(symbols(i)(0), sortedDefines(i).Key)
                Dim value = symbols(i)(1)
                If TypeOf value Is Double Then
                    Dim dbl As Double = CDbl(value)
                    If Double.IsNaN(dbl) Then dbl = s_VBC_VER
                    Assert.Equal(dbl, sortedDefines(i).Value)
                Else
                    Assert.Equal(value, sortedDefines(i).Value)
                End If
            Next
        End Sub
#End Region

#Region "Option Strict"
        <Theory,
        InlineData(VisualBasic.OptionStrict.On, {"/optionStrict", "a.vb"}, False),
        InlineData(VisualBasic.OptionStrict.On, {"/optionStrict+", "a.vb"}, False),
        InlineData(VisualBasic.OptionStrict.Off, {"/optionStrict-", "a.vb"}, False),
        InlineData(VisualBasic.OptionStrict.Custom, {"/OptionStrict:cusTom", "a.vb"}, False),
        InlineData(VisualBasic.OptionStrict.Off, {"/OptionStrict:cusTom", "/optionstrict-", "a.vb"}, False),
        InlineData(VisualBasic.OptionStrict.Custom, {"/optionstrict-", "/OptionStrict:cusTom", "a.vb"}, False),
        InlineData(Nothing, {"/optionstrict:", "/OptionStrict:cusTom", "a.vb"}, True),
        InlineData(Nothing, {"/optionstrict:xxx", "a.vb"}, True)>
        Public Sub OptionStrict(strictness As OptionStrict?, args As String(), CheckDiagnostic As Boolean)
            Dim parsedArgs = DefaultParse(args, _baseDirectory)
            If CheckDiagnostic Then
                parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("optionstrict", ":custom"))
            Else
                parsedArgs.Errors.Verify
            End If
            If strictness.HasValue Then
                Assert.Equal(strictness.Value, parsedArgs.CompilationOptions.OptionStrict)
            End If
        End Sub
#End Region

#Region "Rootnamespace"
        <WorkItem(546319, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546319")>
        <WorkItem(546318, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546318")>
        <WorkItem(685392, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/685392")>
        <Theory>
        <InlineData({"/rootnamespace:One.Two.Three", "a.vb"}, "One.Two.Three")>
        <InlineData({"/rootnamespace:One Two Three", "/rootnamespace:One.Two.Three", "a.vb"}, "One.Two.Three")>
        <InlineData({"/rootnamespace:""One.Two.Three""", "a.vb"}, "One.Two.Three")>
        <InlineData({"/rootnamespace:[global]", "a.vb"}, "[global]")>
        <InlineData({"/rootnamespace:goo.[global].bar", "a.vb"}, "goo.[global].bar")>
        <InlineData({"/rootnamespace:goo.[bar]", "a.vb"}, "goo.[bar]")>
        <InlineData({"/rootnamespace:__.___", "a.vb"}, "__.___")>
        Public Sub RootNamespace(Args() As String, ExpectedRootnamespace As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(ExpectedRootnamespace, parsedArgs.CompilationOptions.RootNamespace)
        End Sub

        <Theory, InlineData({"/rootnamespace", "a.vb"}), InlineData({"/rootnamespace:", "a.vb"}), InlineData({"/rootnamespace: ", "a.vb"})>
        Public Sub RootNamespace_ERR_ArgumentRequired(ParamArray Args() As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("rootnamespace", ":<string>"))
        End Sub
        <Theory, InlineData({"/rootnamespace+", "a.vb"}, "/rootnamespace+"), InlineData({"/rootnamespace-:", "a.vb"}, "/rootnamespace-:")>
        Public Sub Rootnamespace_WRN_BadSwitch(Args() As String, WithArg As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments(WithArg)) ' TODO: Dev11 reports ERR_ArgumentRequired
        End Sub

        <Theory,
         InlineData({"/rootnamespace:+", "a.vb"}, "+"), InlineData({"/rootnamespace: A.B.C", "a.vb"}, " A.B.C"),
         InlineData({"/rootnamespace:[abcdef", "a.vb"}, "[abcdef"), InlineData({"/rootnamespace:abcdef]", "a.vb"}, "abcdef]"),
         InlineData({"/rootnamespace:[[abcdef]]", "a.vb"}, "[[abcdef]]"), InlineData({"/rootnamespace:goo$", "a.vb"}, "goo$"),
         InlineData({"/rootnamespace:I(", "a.vb"}, "I("), InlineData({"/rootnamespace:_", "a.vb"}, "_"),
         InlineData({"/rootnamespace:[_]", "a.vb"}, "[_]"), InlineData({"/rootnamespace:[", "a.vb"}, "["),
         InlineData({"/rootnamespace:]", "a.vb"}, "]"), InlineData({"/rootnamespace:[]", "a.vb"}, "[]")>
        Public Sub Rootnamespace_WRN_BadNamespaceName1(Args() As String, WithArg As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments(WithArg))
        End Sub
#End Region

#Region "Link_SimpleTests"
        <Theory, InlineData({"/link:a", "/link:b,,,,c", "a.vb"}, {"a", "b", "c"}), InlineData({"/Link: ,,, b ,,", "a.vb"}, {" ", " b "})>
        Public Sub Link_SimpleTests_MetadataReferences(Args() As String, ExpectedMetadataReferences() As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertEx.Equal(ExpectedMetadataReferences, parsedArgs.MetadataReferences.Where(Function(res) res.Properties.EmbedInteropTypes).Select(Function(res) res.Reference))
        End Sub

        <Theory, InlineData({"/l:", "a.vb"}), InlineData({"/L", "a.vb"})>
        Public Sub Link_SimpleTests_ERR_ArgumentRequired(ParamArray Args() As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("l", ":<file_list>"))
        End Sub

        <Theory, InlineData({"/l+", "a.vb"}, "/l+"), InlineData({"/link-:", "a.vb"}, "/link-:")>
        Public Sub Link_SimpleTests(Args() As String, WithArg As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments(WithArg)) ' TODO: Dev11 reports ERR_ArgumentRequired
        End Sub
#End Region

        <Fact>
        Public Sub Recurse_SimpleTests()
            Dim dir = Temp.CreateDirectory()
            Dim file1 = dir.CreateFile("a.vb").WriteAllText("")
            Dim file2 = dir.CreateFile("b.vb").WriteAllText("")
            Dim file3 = dir.CreateFile("c.txt").WriteAllText("")
            Dim file4 = dir.CreateDirectory("d1").CreateFile("d.txt").WriteAllText("")
            Dim file5 = dir.CreateDirectory("d2").CreateFile("e.vb").WriteAllText("")

            Dim parsedArgs = DefaultParse({"/recurse:" & dir.ToString() & "\*.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertEx.Equal({"{DIR}\a.vb", "{DIR}\b.vb", "{DIR}\d2\e.vb"}, parsedArgs.SourceFiles.Select(Function(file) file.Path.Replace(dir.ToString(), "{DIR}")))

            parsedArgs = DefaultParse({"*.vb"}, dir.ToString())
            parsedArgs.Errors.Verify()
            AssertEx.Equal({"{DIR}\a.vb", "{DIR}\b.vb"}, parsedArgs.SourceFiles.Select(Function(file) file.Path.Replace(dir.ToString(), "{DIR}")))

            parsedArgs = DefaultParse({"/reCURSE:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("recurse", ":<wildcard>"))

            parsedArgs = DefaultParse({"/RECURSE: ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("recurse", ":<wildcard>"))

            parsedArgs = DefaultParse({"/recurse", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("recurse", ":<wildcard>"))

            parsedArgs = DefaultParse({"/recurse+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/recurse+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/recurse-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/recurse-:")) ' TODO: Dev11 reports ERR_ArgumentRequired

            CleanupAllGeneratedFiles(file1.Path)
            CleanupAllGeneratedFiles(file2.Path)
            CleanupAllGeneratedFiles(file3.Path)
            CleanupAllGeneratedFiles(file4.Path)
            CleanupAllGeneratedFiles(file5.Path)
        End Sub

        <WorkItem(545991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545991")>
        <WorkItem(546009, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546009")>
        <Fact>
        Public Sub Recurse_SimpleTests2()
            Dim folder = Temp.CreateDirectory()
            Dim file1 = folder.CreateFile("a.cs").WriteAllText("")
            Dim file2 = folder.CreateFile("b.vb").WriteAllText("")
            Dim file3 = folder.CreateFile("c.cpp").WriteAllText("")
            Dim file4 = folder.CreateDirectory("A").CreateFile("A_d.txt").WriteAllText("")
            Dim file5 = folder.CreateDirectory("B").CreateFile("B_e.vb").WriteAllText("")
            Dim file6 = folder.CreateDirectory("C").CreateFile("B_f.cs").WriteAllText("")

            Dim exitCode As Integer
            Using outWriter As New StringWriter()
                exitCode = New MockVisualBasicCompiler(Nothing, folder.Path, {"/nologo", "/preferreduilang:en", "/t:library", "/recurse:.", "b.vb", "/out:abc.dll"}).Run(outWriter, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC2014: the value '.' is invalid for option 'recurse'", outWriter.ToString().Trim())
            End Using
            Using outWriter = New StringWriter()
                exitCode = New MockVisualBasicCompiler(Nothing, folder.Path, {"/nologo", "/preferreduilang:en", "/t:library", "/recurse:. ", "b.vb", "/out:abc.dll"}).Run(outWriter, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC2014: the value '.' is invalid for option 'recurse'", outWriter.ToString().Trim())
            End Using
            Using outWriter = New StringWriter()
                exitCode = New MockVisualBasicCompiler(Nothing, folder.Path, {"/nologo", "/preferreduilang:en", "/t:library", "/recurse:   . ", "/out:abc.dll"}).Run(outWriter, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC2014: the value '   .' is invalid for option 'recurse'|vbc : error BC2008: no input sources specified", outWriter.ToString().Trim().Replace(vbCrLf, "|"))
            End Using
            Using outWriter = New StringWriter()
                exitCode = New MockVisualBasicCompiler(Nothing, folder.Path, {"/nologo", "/preferreduilang:en", "/t:library", "/recurse:./.", "/out:abc.dll"}).Run(outWriter, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC2014: the value './.' is invalid for option 'recurse'|vbc : error BC2008: no input sources specified", outWriter.ToString().Trim().Replace(vbCrLf, "|"))
            End Using

            Dim args As VisualBasicCommandLineArguments
            Dim resolvedSourceFiles As String()

            args = DefaultParse({"/recurse:*.cp*", "/recurse:b\*.v*", "/out:a.dll"}, folder.Path)
            args.Errors.Verify()
            resolvedSourceFiles = args.SourceFiles.Select(Function(f) f.Path).ToArray()
            AssertEx.Equal({folder.Path + "\c.cpp", folder.Path + "\b\B_e.vb"}, resolvedSourceFiles)

            args = DefaultParse({"/recurse:.\\\\\\*.vb", "/out:a.dll"}, folder.Path)
            args.Errors.Verify()
            resolvedSourceFiles = args.SourceFiles.Select(Function(f) f.Path).ToArray()
            Assert.Equal(2, resolvedSourceFiles.Length)

            args = DefaultParse({"/recurse:.////*.vb", "/out:a.dll"}, folder.Path)
            args.Errors.Verify()
            resolvedSourceFiles = args.SourceFiles.Select(Function(f) f.Path).ToArray()
            Assert.Equal(2, resolvedSourceFiles.Length)

            CleanupAllGeneratedFiles(file1.Path)
            CleanupAllGeneratedFiles(file2.Path)
            CleanupAllGeneratedFiles(file3.Path)
            CleanupAllGeneratedFiles(file4.Path)
            CleanupAllGeneratedFiles(file5.Path)
            CleanupAllGeneratedFiles(file6.Path)
        End Sub

        <WorkItem(948285, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/948285")>
        <Fact>
        Public Sub Recurse_SimpleTests3()
            Dim folder = Temp.CreateDirectory()
            Using outWriter = New StringWriter()
                Dim exitCode = New MockVisualBasicCompiler(Nothing, folder.Path, {"/nologo", "/preferreduilang:en", "/t:exe", "/out:abc.exe"}).Run(outWriter, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC2008: no input sources specified", outWriter.ToString().Trim().Replace(vbCrLf, "|"))
            End Using
        End Sub

        <Fact>
        Public Sub Reference_SimpleTests()
            Dim parsedArgs = DefaultParse({"/nostdlib", "/vbruntime-", "/r:a", "/REFERENCE:b,,,,c", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertEx.Equal({"a", "b", "c"},
                       parsedArgs.MetadataReferences.
                                  Where(Function(res) Not res.Properties.EmbedInteropTypes AndAlso Not res.Reference.EndsWith("mscorlib.dll", StringComparison.Ordinal)).
                                  Select(Function(res) res.Reference))

            parsedArgs = DefaultParse({"/Reference: ,,, b ,,", "/nostdlib", "/vbruntime-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertEx.Equal({" ", " b "},
                       parsedArgs.MetadataReferences.
                                  Where(Function(res) Not res.Properties.EmbedInteropTypes AndAlso Not res.Reference.EndsWith("mscorlib.dll", StringComparison.Ordinal)).
                                  Select(Function(res) res.Reference))

            parsedArgs = DefaultParse({"/r:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("r", ":<file_list>"))

            parsedArgs = DefaultParse({"/R", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("r", ":<file_list>"))

            parsedArgs = DefaultParse({"/reference+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/reference+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/reference-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/reference-:")) ' TODO: Dev11 reports ERR_ArgumentRequired
        End Sub

        Private Class SimpleMetadataResolver
            Inherits MetadataReferenceResolver

            Private ReadOnly _pathResolver As RelativePathResolver

            Public Sub New(baseDirectory As String)
                _pathResolver = New RelativePathResolver(ImmutableArray(Of String).Empty, baseDirectory)
            End Sub

            Public Overrides Function ResolveReference(reference As String, baseFilePath As String, properties As MetadataReferenceProperties) As ImmutableArray(Of PortableExecutableReference)
                Dim resolvedPath = _pathResolver.ResolvePath(reference, baseFilePath)

                If resolvedPath Is Nothing OrElse Not File.Exists(reference) Then
                    Return Nothing
                End If

                Return ImmutableArray.Create(MetadataReference.CreateFromFile(resolvedPath, properties))
            End Function

            Public Overrides Function Equals(other As Object) As Boolean
                Return True
            End Function

            Public Overrides Function GetHashCode() As Integer
                Return 1
            End Function
        End Class

        <Fact>
        Public Sub Reference_CorLibraryAddedWhenThereAreUnresolvedReferences()
            Dim parsedArgs = DefaultParse({"/r:unresolved", "a.vb"}, _baseDirectory)

            Dim metadataResolver = New SimpleMetadataResolver(_baseDirectory)
            Dim references = parsedArgs.ResolveMetadataReferences(metadataResolver).ToImmutableArray()

            Assert.Equal(4, references.Length)
            Assert.Contains(references, Function(r) r.IsUnresolved)
            Assert.Contains(references, Function(r)
                                            Dim peRef = TryCast(r, PortableExecutableReference)
                                            Return peRef IsNot Nothing AndAlso
                                               peRef.FilePath.EndsWith("mscorlib.dll", StringComparison.Ordinal)
                                        End Function)
        End Sub

        <Fact>
        Public Sub Reference_CorLibraryAddedWhenThereAreNoUnresolvedReferences()
            Dim parsedArgs = DefaultParse({"a.vb"}, _baseDirectory)

            Dim metadataResolver = New SimpleMetadataResolver(_baseDirectory)
            Dim references = parsedArgs.ResolveMetadataReferences(metadataResolver).ToImmutableArray()

            Assert.Equal(3, references.Length)
            Assert.DoesNotContain(references, Function(r) r.IsUnresolved)
            Assert.Contains(references, Function(r)
                                            Dim peRef = TryCast(r, PortableExecutableReference)
                                            Return peRef IsNot Nothing AndAlso
                                               peRef.FilePath.EndsWith("mscorlib.dll", StringComparison.Ordinal)
                                        End Function)
        End Sub

        Private Function CreateRuleSetFile(source As XDocument) As TempFile
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.ruleset")
            file.WriteAllText(source.ToString())
            Return file
        End Function

        <Fact>
        Public Sub RulesetSwitchPositive()

            Dim source = <?xml version="1.0" encoding="utf-8"?>
                         <RuleSet Name="Ruleset1" Description="Test" ToolsVersion="12.0">
                             <IncludeAll Action="Warning"/>
                             <Rules AnalyzerId="Microsoft.Analyzers.ManagedCodeAnalysis" RuleNamespace="Microsoft.Rules.Managed">
                                 <Rule Id="CA1012" Action="Error"/>
                                 <Rule Id="CA1013" Action="Warning"/>
                                 <Rule Id="CA1014" Action="None"/>
                             </Rules>
                         </RuleSet>

            Dim file = CreateRuleSetFile(source)
            Dim parsedArgs = DefaultParse(New String() {"/ruleset:" + file.Path, "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(expected:=file.Path, actual:=parsedArgs.RuleSetPath)
            Assert.True(parsedArgs.CompilationOptions.SpecificDiagnosticOptions.ContainsKey("CA1012"))
            Assert.True(parsedArgs.CompilationOptions.SpecificDiagnosticOptions("CA1012") = ReportDiagnostic.Error)
            Assert.True(parsedArgs.CompilationOptions.SpecificDiagnosticOptions.ContainsKey("CA1013"))
            Assert.True(parsedArgs.CompilationOptions.SpecificDiagnosticOptions("CA1013") = ReportDiagnostic.Warn)
            Assert.True(parsedArgs.CompilationOptions.SpecificDiagnosticOptions.ContainsKey("CA1014"))
            Assert.True(parsedArgs.CompilationOptions.SpecificDiagnosticOptions("CA1014") = ReportDiagnostic.Suppress)
            Assert.True(parsedArgs.CompilationOptions.GeneralDiagnosticOption = ReportDiagnostic.Warn)
        End Sub

        <Fact>
        Public Sub RuleSetSwitchQuoted()
            Dim source = <?xml version="1.0" encoding="utf-8"?>
                         <RuleSet Name="Ruleset1" Description="Test" ToolsVersion="12.0">
                             <IncludeAll Action="Warning"/>
                             <Rules AnalyzerId="Microsoft.Analyzers.ManagedCodeAnalysis" RuleNamespace="Microsoft.Rules.Managed">
                                 <Rule Id="CA1012" Action="Error"/>
                                 <Rule Id="CA1013" Action="Warning"/>
                                 <Rule Id="CA1014" Action="None"/>
                             </Rules>
                         </RuleSet>

            Dim file = CreateRuleSetFile(source)
            Dim parsedArgs = DefaultParse(New String() {"/ruleset:" + """" + file.Path + """", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(expected:=file.Path, actual:=parsedArgs.RuleSetPath)
        End Sub

        <Fact>
        Public Sub RulesetSwitchParseErrors()
            Dim parsedArgs = DefaultParse(New String() {"/ruleset", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(
        Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("ruleset", ":<file>"))
            Assert.Null(parsedArgs.RuleSetPath)

            parsedArgs = DefaultParse(New String() {"/ruleset", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(
        Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("ruleset", ":<file>"))
            Assert.Null(parsedArgs.RuleSetPath)

            parsedArgs = DefaultParse(New String() {"/ruleset:blah", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(
        Diagnostic(ERRID.ERR_CantReadRulesetFile).WithArguments(Path.Combine(TempRoot.Root, "blah"), "File not found."))
            Assert.Equal(expected:=Path.Combine(TempRoot.Root, "blah"), actual:=parsedArgs.RuleSetPath)

            parsedArgs = DefaultParse(New String() {"/ruleset:blah;blah.ruleset", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(
        Diagnostic(ERRID.ERR_CantReadRulesetFile).WithArguments(Path.Combine(TempRoot.Root, "blah;blah.ruleset"), "File not found."))
            Assert.Equal(expected:=Path.Combine(TempRoot.Root, "blah;blah.ruleset"), actual:=parsedArgs.RuleSetPath)

            Dim file = CreateRuleSetFile(New XDocument())
            parsedArgs = DefaultParse(New String() {"/ruleset:" + file.Path, "a.cs"}, _baseDirectory)
            'parsedArgs.Errors.Verify(
            '   Diagnostic(ERRID.ERR_CantReadRulesetFile).WithArguments(file.Path, "Root element is missing."))
            Assert.Equal(expected:=file.Path, actual:=parsedArgs.RuleSetPath)
            Dim err = parsedArgs.Errors.Single()

            Assert.Equal(ERRID.ERR_CantReadRulesetFile, err.Code)
            Assert.Equal(2, err.Arguments.Count)
            Assert.Equal(file.Path, DirectCast(err.Arguments(0), String))
            Dim currentUICultureName = Thread.CurrentThread.CurrentUICulture.Name
            If currentUICultureName.Length = 0 OrElse currentUICultureName.StartsWith("en", StringComparison.OrdinalIgnoreCase) Then
                Assert.Equal(err.Arguments(1), "Root element is missing.")
            End If
        End Sub

        <Fact>
        Public Sub Target_SimpleTests()
            Dim parsedArgs = DefaultParse({"/target:exe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputKind.ConsoleApplication, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = DefaultParse({"/t:module", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputKind.NetModule, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = DefaultParse({"/target:library", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputKind.DynamicallyLinkedLibrary, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = DefaultParse({"/TARGET:winexe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputKind.WindowsApplication, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = DefaultParse({"/target:winmdobj", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputKind.WindowsRuntimeMetadata, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = DefaultParse({"/target:appcontainerexe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputKind.WindowsRuntimeApplication, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = DefaultParse({"/target:winexe", "/T:exe", "/target:module", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputKind.NetModule, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = DefaultParse({"/t", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("t", ":exe|winexe|library|module|appcontainerexe|winmdobj"))

            parsedArgs = DefaultParse({"/target:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("target", ":exe|winexe|library|module|appcontainerexe|winmdobj"))

            parsedArgs = DefaultParse({"/target:xyz", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("target", "xyz"))

            parsedArgs = DefaultParse({"/T+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/T+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/TARGET-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/TARGET-:")) ' TODO: Dev11 reports ERR_ArgumentRequired
        End Sub

        <Fact>
        Public Sub Target_SimpleTestsNoSourceFile()
            Dim parsedArgs = DefaultParse({"/target:exe"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_NoSources).WithLocation(1, 1))
            Assert.Equal(OutputKind.ConsoleApplication, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = DefaultParse({"/t:module"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_NoSources).WithLocation(1, 1))
            Assert.Equal(OutputKind.NetModule, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = DefaultParse({"/target:library"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_NoSources).WithLocation(1, 1))
            Assert.Equal(OutputKind.DynamicallyLinkedLibrary, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = DefaultParse({"/TARGET:winexe"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_NoSources).WithLocation(1, 1))
            Assert.Equal(OutputKind.WindowsApplication, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = DefaultParse({"/target:winmdobj"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_NoSources).WithLocation(1, 1))
            Assert.Equal(OutputKind.WindowsRuntimeMetadata, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = DefaultParse({"/target:appcontainerexe"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_NoSources).WithLocation(1, 1))
            Assert.Equal(OutputKind.WindowsRuntimeApplication, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = DefaultParse({"/target:winexe", "/T:exe", "/target:module"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_NoSources).WithLocation(1, 1))
            Assert.Equal(OutputKind.NetModule, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = DefaultParse({"/t"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("t", ":exe|winexe|library|module|appcontainerexe|winmdobj"),
            Diagnostic(ERRID.ERR_NoSources).WithLocation(1, 1))

            parsedArgs = DefaultParse({"/target:"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("target", ":exe|winexe|library|module|appcontainerexe|winmdobj"),
            Diagnostic(ERRID.ERR_NoSources).WithLocation(1, 1))

            parsedArgs = DefaultParse({"/target:xyz"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("target", "xyz"),
            Diagnostic(ERRID.ERR_NoSources).WithLocation(1, 1))

            parsedArgs = DefaultParse({"/T+"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/T+"),
            Diagnostic(ERRID.ERR_NoSources).WithLocation(1, 1)) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/TARGET-:"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/TARGET-:"),
            Diagnostic(ERRID.ERR_NoSources).WithLocation(1, 1)) ' TODO: Dev11 reports ERR_ArgumentRequired
        End Sub

        <Fact>
        Public Sub Utf8Output()
            Dim parsedArgs = DefaultParse({"/utf8output", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.Utf8Output)

            parsedArgs = DefaultParse({"/utf8output+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.Utf8Output)

            parsedArgs = DefaultParse({"/utf8output-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.Utf8Output)

            ' default
            parsedArgs = DefaultParse({"/nologo", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.Utf8Output)

            ' overriding
            parsedArgs = DefaultParse({"/utf8output+", "/utf8output-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.Utf8Output)

            ' errors
            parsedArgs = DefaultParse({"/utf8output:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("utf8output"))

        End Sub

        <Fact>
        Public Sub Debug()
            Dim platformPdbKind = If(PathUtilities.IsUnixLikePlatform, DebugInformationFormat.PortablePdb, DebugInformationFormat.Pdb)

            Dim parsedArgs = DefaultParse({"a.vb"}, _baseDirectory)
            Assert.False(parsedArgs.EmitPdb)
            parsedArgs.Errors.Verify()

            parsedArgs = DefaultParse({"/debug-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.False(parsedArgs.EmitPdb)
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind)

            parsedArgs = DefaultParse({"/debug", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.True(parsedArgs.EmitPdb)
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind)

            parsedArgs = DefaultParse({"/debug+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.True(parsedArgs.EmitPdb)
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind)

            parsedArgs = DefaultParse({"/debug+", "/debug-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.False(parsedArgs.EmitPdb)
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind)

            parsedArgs = DefaultParse({"/debug:full", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.True(parsedArgs.EmitPdb)
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind)

            parsedArgs = DefaultParse({"/debug:FULL", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.True(parsedArgs.EmitPdb)
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind)

            parsedArgs = DefaultParse({"/debug:pdbonly", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.True(parsedArgs.EmitPdb)
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind)

            parsedArgs = DefaultParse({"/debug:portable", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.True(parsedArgs.EmitPdb)
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, DebugInformationFormat.PortablePdb)

            parsedArgs = DefaultParse({"/debug:embedded", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.True(parsedArgs.EmitPdb)
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, DebugInformationFormat.Embedded)

            parsedArgs = DefaultParse({"/debug:PDBONLY", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.True(parsedArgs.EmitPdb)
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind)

            parsedArgs = DefaultParse({"/debug:full", "/debug:pdbonly", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.True(parsedArgs.EmitPdb)
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind)

            parsedArgs = DefaultParse({"/debug:pdbonly", "/debug:full", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.True(parsedArgs.EmitPdb)
            Assert.Equal(platformPdbKind, parsedArgs.EmitOptions.DebugInformationFormat)

            parsedArgs = DefaultParse({"/debug:pdbonly", "/debug-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.False(parsedArgs.EmitPdb)
            Assert.Equal(platformPdbKind, parsedArgs.EmitOptions.DebugInformationFormat)

            parsedArgs = DefaultParse({"/debug:pdbonly", "/debug-", "/debug", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.True(parsedArgs.EmitPdb)
            Assert.Equal(platformPdbKind, parsedArgs.EmitOptions.DebugInformationFormat)

            parsedArgs = DefaultParse({"/debug:pdbonly", "/debug-", "/debug+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.True(parsedArgs.EmitPdb)
            Assert.Equal(platformPdbKind, parsedArgs.EmitOptions.DebugInformationFormat)

            parsedArgs = DefaultParse({"/debug:embedded", "/debug-", "/debug+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.True(parsedArgs.EmitPdb)
            Assert.Equal(DebugInformationFormat.Embedded, parsedArgs.EmitOptions.DebugInformationFormat)

            parsedArgs = DefaultParse({"/debug:embedded", "/debug-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.False(parsedArgs.EmitPdb)
            Assert.Equal(DebugInformationFormat.Embedded, parsedArgs.EmitOptions.DebugInformationFormat)

            parsedArgs = DefaultParse({"/debug:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("debug", ""))

            parsedArgs = DefaultParse({"/debug:+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("debug", "+"))

            parsedArgs = DefaultParse({"/debug:invalid", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("debug", "invalid"))

            parsedArgs = DefaultParse({"/debug-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("debug"))

            parsedArgs = DefaultParse({"/pdb:something", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/pdb:something"))
        End Sub

        <Fact>
        Public Sub SourceLink()
            Dim parsedArgs = DefaultParse({"/sourcelink:sl.json", "/debug:portable", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(Path.Combine(_baseDirectory, "sl.json"), parsedArgs.SourceLink)

            parsedArgs = DefaultParse({"/sourcelink:sl.json", "/debug:embedded", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(Path.Combine(_baseDirectory, "sl.json"), parsedArgs.SourceLink)

            parsedArgs = DefaultParse({"/sourcelink:""s l.json""", "/debug:embedded", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(Path.Combine(_baseDirectory, "s l.json"), parsedArgs.SourceLink)

            parsedArgs = DefaultParse({"/sourcelink:sl.json", "/debug:full", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = DefaultParse({"/sourcelink:sl.json", "/debug:pdbonly", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = DefaultParse({"/sourcelink:sl.json", "/debug-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SourceLinkRequiresPdb))

            parsedArgs = DefaultParse({"/sourcelink:sl.json", "/debug+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = DefaultParse({"/sourcelink:sl.json", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SourceLinkRequiresPdb))
        End Sub

        <Fact>
        Public Sub SourceLink_EndToEnd_EmbeddedPortable()
            Dim dir = Temp.CreateDirectory()

            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText("
Class C 
  Public Shared Sub Main()
  End Sub
End Class")

            Dim sl = dir.CreateFile("sl.json")
            sl.WriteAllText("{ ""documents"" : {} }")

            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/debug:embedded", "/sourcelink:sl.json", "a.vb"})
                Dim exitCode As Integer = vbc.Run(outWriter)
                Assert.Equal(0, exitCode)

                Dim peStream = File.OpenRead(Path.Combine(dir.Path, "a.exe"))

                Using peReader = New PEReader(peStream)
                    Dim entry = peReader.ReadDebugDirectory().Single(Function(e) e.Type = DebugDirectoryEntryType.EmbeddedPortablePdb)

                    Using mdProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry)
                        Dim blob = mdProvider.GetMetadataReader().GetSourceLinkBlob()
                        AssertEx.Equal(File.ReadAllBytes(sl.Path), blob)
                    End Using
                End Using
            End Using
            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact>
        Public Sub SourceLink_EndToEnd_Portable()
            Dim dir = Temp.CreateDirectory()

            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText("
Class C 
  Public Shared Sub Main()
  End Sub
End Class")

            Dim sl = dir.CreateFile("sl.json")
            sl.WriteAllText("{ ""documents"" : {} }")

            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/debug:portable", "/sourcelink:sl.json", "a.vb"})
                Dim exitCode As Integer = vbc.Run(outWriter)
                Assert.Equal(0, exitCode)

                Dim pdbStream = File.OpenRead(Path.Combine(dir.Path, "a.pdb"))
                Using mdProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream)
                    Dim blob = mdProvider.GetMetadataReader().GetSourceLinkBlob()
                    AssertEx.Equal(File.ReadAllBytes(sl.Path), blob)
                End Using
            End Using
            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact>
        Public Sub Embed()
            Dim parsedArgs = DefaultParse({"a.vb "}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Empty(parsedArgs.EmbeddedFiles)

            parsedArgs = DefaultParse({"/embed", "/debug:portable", "a.vb", "b.vb", "c.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertEx.Equal(parsedArgs.SourceFiles, parsedArgs.EmbeddedFiles)
            AssertEx.Equal(
            {"a.vb", "b.vb", "c.vb"}.Select(Function(f) Path.Combine(_baseDirectory, f)),
            parsedArgs.EmbeddedFiles.Select(Function(f) f.Path))

            parsedArgs = DefaultParse({"/embed:a.vb", "/embed:b.vb", "/debug:embedded", "a.vb", "b.vb", "c.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertEx.Equal(
            {"a.vb", "b.vb"}.Select(Function(f) Path.Combine(_baseDirectory, f)),
            parsedArgs.EmbeddedFiles.Select(Function(f) f.Path))

            parsedArgs = DefaultParse({"/embed:a.vb;b.vb", "/debug:portable", "a.vb", "b.vb", "c.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertEx.Equal(
            {"a.vb", "b.vb"}.Select(Function(f) Path.Combine(_baseDirectory, f)),
            parsedArgs.EmbeddedFiles.Select(Function(f) f.Path))

            parsedArgs = DefaultParse({"/embed:a.txt", "/embed", "/debug:portable", "a.vb", "b.vb", "c.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertEx.Equal(
            {"a.txt", "a.vb", "b.vb", "c.vb"}.Select(Function(f) Path.Combine(_baseDirectory, f)),
            parsedArgs.EmbeddedFiles.Select(Function(f) f.Path))

            parsedArgs = DefaultParse({"/embed", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_CannotEmbedWithoutPdb))

            parsedArgs = DefaultParse({"/embed:a.txt", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_CannotEmbedWithoutPdb))

            parsedArgs = DefaultParse({"/embed", "/debug-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_CannotEmbedWithoutPdb))

            parsedArgs = DefaultParse({"/embed:a.txt", "/debug-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_CannotEmbedWithoutPdb))

            parsedArgs = DefaultParse({"/embed", "/debug:full", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = DefaultParse({"/embed", "/debug:pdbonly", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = DefaultParse({"/embed", "/debug+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
        End Sub

        <Theory>
        <InlineData("/debug:portable", "/embed", {"embed.vb", "embed2.vb", "embed.xyz"})>
        <InlineData("/debug:portable", "/embed:embed.vb", {"embed.vb", "embed.xyz"})>
        <InlineData("/debug:portable", "/embed:embed2.vb", {"embed2.vb"})>
        <InlineData("/debug:portable", "/embed:embed.xyz", {"embed.xyz"})>
        <InlineData("/debug:embedded", "/embed", {"embed.vb", "embed2.vb", "embed.xyz"})>
        <InlineData("/debug:embedded", "/embed:embed.vb", {"embed.vb", "embed.xyz"})>
        <InlineData("/debug:embedded", "/embed:embed2.vb", {"embed2.vb"})>
        <InlineData("/debug:embedded", "/embed:embed.xyz", {"embed.xyz"})>
        <InlineData("/debug:full", "/embed", {"embed.vb", "embed2.vb", "embed.xyz"})>
        <InlineData("/debug:full", "/embed:embed.vb", {"embed.vb", "embed.xyz"})>
        <InlineData("/debug:full", "/embed:embed2.vb", {"embed2.vb"})>
        <InlineData("/debug:full", "/embed:embed.xyz", {"embed.xyz"})>
        Public Sub Embed_EndToEnd(debugSwitch As String, embedSwitch As String, expectedEmbedded As String())
            ' embed.vb: large enough To compress, has #line directives
            Const embed_vb =
"'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Class Program
    Shared Sub Main()
#ExternalSource(""embed.xyz"", 1)
        System.Console.WriteLine(""Hello, World"")

        System.Console.WriteLine(""Goodbye, World"")
#End ExternalSource
    End Sub
End Class
'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''"

            ' embed2.vb: small enough to not compress, no sequence points
            Const embed2_vb =
"Class C
End Class"

            ' target of #ExternalSource
            Const embed_xyz =
"print Hello, World

print Goodbye, World"

            Assert.True(embed_vb.Length >= EmbeddedText.CompressionThreshold)
            Assert.True(embed2_vb.Length < EmbeddedText.CompressionThreshold)

            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("embed.vb").WriteAllText(embed_vb)
            Dim src2 = dir.CreateFile("embed2.vb").WriteAllText(embed2_vb)
            Dim txt = dir.CreateFile("embed.xyz").WriteAllText(embed_xyz)

            Dim expectedEmbeddedMap = PooledObjects.PooledDictionary(Of String, String).GetInstance
            If expectedEmbedded.Contains("embed.vb") Then expectedEmbeddedMap.Add(src.Path, embed_vb)
            If expectedEmbedded.Contains("embed2.vb") Then expectedEmbeddedMap.Add(src2.Path, embed2_vb)
            If expectedEmbedded.Contains("embed.xyz") Then expectedEmbeddedMap.Add(txt.Path, embed_xyz)

            Using output = New StringWriter(CultureInfo.InvariantCulture)
                Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", debugSwitch, embedSwitch, "embed.vb", "embed2.vb"})
                Dim exitCode = vbc.Run(output)
                Assert.Equal("", output.ToString().Trim())
                Assert.Equal(0, exitCode)

                Select Case debugSwitch
                    Case "/debug:embedded"
                        ValidateEmbeddedSources_Portable(expectedEmbeddedMap, dir, isEmbeddedPdb:=True)
                    Case "/debug:portable"
                        ValidateEmbeddedSources_Portable(expectedEmbeddedMap, dir, isEmbeddedPdb:=False)
                    Case "/debug:full"
                        ValidateEmbeddedSources_Windows(expectedEmbeddedMap, dir)
                End Select
            End Using
            Assert.Empty(expectedEmbeddedMap)
            expectedEmbeddedMap.Free()
            CleanupAllGeneratedFiles(src.Path)
        End Sub

        Private Shared Sub ValidateEmbeddedSources_Portable(expectedEmbeddedMap As Dictionary(Of String, String), dir As TempDirectory, isEmbeddedPdb As Boolean)
            Using peReader As New PEReader(File.OpenRead(Path.Combine(dir.Path, "embed.exe")))
                Dim entry = peReader.ReadDebugDirectory().SingleOrDefault(Function(e) e.Type = DebugDirectoryEntryType.EmbeddedPortablePdb)
                Assert.Equal(isEmbeddedPdb, entry.DataSize > 0)

                Using mdProvider As MetadataReaderProvider = If(
                isEmbeddedPdb,
                peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry),
                MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(Path.Combine(dir.Path, "embed.pdb"))))

                    Dim mdReader = mdProvider.GetMetadataReader()
                    For Each handle In mdReader.Documents
                        Dim doc = mdReader.GetDocument(handle)
                        Dim docPath = mdReader.GetString(doc.Name)

                        Dim embeddedSource = mdReader.GetEmbeddedSource(handle)
                        If embeddedSource Is Nothing Then
                            Continue For
                        End If

                        Assert.True(TypeOf embeddedSource.Encoding Is UTF8Encoding AndAlso embeddedSource.Encoding.GetPreamble().Length = 0)
                        Assert.Equal(expectedEmbeddedMap(docPath), embeddedSource.ToString())
                        Assert.True(expectedEmbeddedMap.Remove(docPath))
                    Next
                End Using
            End Using
        End Sub

        Private Shared Sub ValidateEmbeddedSources_Windows(expectedEmbeddedMap As Dictionary(Of String, String), dir As TempDirectory)
            Dim symReader As ISymUnmanagedReader5 = Nothing

            Try
                symReader = SymReaderFactory.CreateReader(File.OpenRead(Path.Combine(dir.Path, "embed.pdb")))

                For Each doc In symReader.GetDocuments()
                    Dim docPath = doc.GetName()

                    Dim sourceBlob = doc.GetEmbeddedSource()
                    If sourceBlob.Array Is Nothing Then Continue For

                    Dim sourceStr = Encoding.UTF8.GetString(sourceBlob.Array, sourceBlob.Offset, sourceBlob.Count)

                    Assert.Equal(expectedEmbeddedMap(docPath), sourceStr)
                    Assert.True(expectedEmbeddedMap.Remove(docPath))
                Next
            Finally
                symReader?.Dispose()
            End Try
        End Sub

        <CompilerTrait(CompilerFeature.Determinism)>
        <Fact>
        Public Sub PathMapParser()
            Dim parsedArgs = DefaultParse({"/pathmap:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/pathmap:").WithLocation(1, 1))
            Assert.Equal(ImmutableArray.Create(Of KeyValuePair(Of String, String))(), parsedArgs.PathMap)

            parsedArgs = DefaultParse({"/pathmap:K1=V1", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(KeyValuePair.Create("K1\", "V1\"), parsedArgs.PathMap(0))

            parsedArgs = DefaultParse({"/pathmap:C:\goo\=/", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(KeyValuePair.Create("C:\goo\", "/"), parsedArgs.PathMap(0))

            parsedArgs = DefaultParse({"/pathmap:K1=V1,K2=V2", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(KeyValuePair.Create("K1\", "V1\"), parsedArgs.PathMap(0))
            Assert.Equal(KeyValuePair.Create("K2\", "V2\"), parsedArgs.PathMap(1))

            parsedArgs = DefaultParse({"/pathmap:,,,", "a.vb"}, _baseDirectory)
            Assert.Equal(4, parsedArgs.Errors.Count())
            Assert.Equal(ERRID.ERR_InvalidPathMap, parsedArgs.Errors(0).Code)
            Assert.Equal(ERRID.ERR_InvalidPathMap, parsedArgs.Errors(1).Code)
            Assert.Equal(ERRID.ERR_InvalidPathMap, parsedArgs.Errors(2).Code)
            Assert.Equal(ERRID.ERR_InvalidPathMap, parsedArgs.Errors(3).Code)

            parsedArgs = DefaultParse({"/pathmap:k=,=v", "a.vb"}, _baseDirectory)
            Assert.Equal(2, parsedArgs.Errors.Count())
            Assert.Equal(ERRID.ERR_InvalidPathMap, parsedArgs.Errors(0).Code)
            Assert.Equal(ERRID.ERR_InvalidPathMap, parsedArgs.Errors(1).Code)

            parsedArgs = DefaultParse({"/pathmap:k=v=bad", "a.vb"}, _baseDirectory)
            Assert.Equal(1, parsedArgs.Errors.Count())
            Assert.Equal(ERRID.ERR_InvalidPathMap, parsedArgs.Errors(0).Code)

            parsedArgs = DefaultParse({"/pathmap:""supporting spaces=is hard""", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(KeyValuePair.Create("supporting spaces\", "is hard\"), parsedArgs.PathMap(0))

            parsedArgs = DefaultParse({"/pathmap:""K 1=V 1"",""K 2=V 2""", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(KeyValuePair.Create("K 1\", "V 1\"), parsedArgs.PathMap(0))
            Assert.Equal(KeyValuePair.Create("K 2\", "V 2\"), parsedArgs.PathMap(1))

            parsedArgs = DefaultParse({"/pathmap:""K 1""=""V 1"",""K 2""=""V 2""", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(KeyValuePair.Create("K 1\", "V 1\"), parsedArgs.PathMap(0))
            Assert.Equal(KeyValuePair.Create("K 2\", "V 2\"), parsedArgs.PathMap(1))
        End Sub

        ' PathMapKeepsCrossPlatformRoot and PathMapInconsistentSlashes should be in an
        ' assembly that is ran cross-platform, but as no visual basic test assemblies are
        ' run cross-platform, put this here in the hopes that this will eventually be ported.
        <Theory>
        <InlineData("C:\", "/", "C:\", "/")>
        <InlineData("C:\temp\", "/temp/", "C:\temp", "/temp")>
        <InlineData("C:\temp\", "/temp/", "C:\temp\", "/temp/")>
        <InlineData("/", "C:\", "/", "C:\")>
        <InlineData("/temp/", "C:\temp\", "/temp", "C:\temp")>
        <InlineData("/temp/", "C:\temp\", "/temp/", "C:\temp\")>
        Public Sub PathMapKeepsCrossPlatformRoot(expectedFrom As String, expectedTo As String, sourceFrom As String, sourceTo As String)
            Dim pathmapArg = $"/pathmap:{sourceFrom}={sourceTo}"
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({pathmapArg, "a.cs"}, TempRoot.Root, RuntimeEnvironment.GetRuntimeDirectory(), Nothing)
            parsedArgs.Errors.Verify()
            Dim expected = New KeyValuePair(Of String, String)(expectedFrom, expectedTo)
            Assert.Equal(expected, parsedArgs.PathMap(0))
        End Sub

        <Fact>
        Public Sub PathMapInconsistentSlashes()
            Dim Parse = Function(args() As String) As VisualBasicCommandLineArguments
                            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse(args, TempRoot.Root, RuntimeEnvironment.GetRuntimeDirectory(), Nothing)
                            parsedArgs.Errors.Verify()
                            Return parsedArgs
                        End Function
            Dim sep = PathUtilities.DirectorySeparatorChar
            Assert.Equal(New KeyValuePair(Of String, String)("C:\temp/goo" + sep, "/temp\goo" + sep), Parse({"/pathmap:C:\temp/goo=/temp\goo", "a.cs"}).PathMap(0))
            Assert.Equal(New KeyValuePair(Of String, String)("noslash" + sep, "withoutslash" + sep), Parse({"/pathmap:noslash=withoutslash", "a.cs"}).PathMap(0))
            Dim doublemap = Parse({"/pathmap:/temp=/goo,/temp/=/bar", "a.cs"}).PathMap
            Assert.Equal(New KeyValuePair(Of String, String)("/temp/", "/goo/"), doublemap(0))
            Assert.Equal(New KeyValuePair(Of String, String)("/temp/", "/bar/"), doublemap(1))
        End Sub

        <CompilerTrait(CompilerFeature.Determinism)>
        <Fact>
        Public Sub PathMapPdbDeterminism()
            Dim assertPdbEmit =
            Sub(dir As TempDirectory, pePdbPath As String, extraArgs As String())

                Dim source =
"
Imports System
Module Program
    Sub Main()
    End Sub
End Module
"

                Dim src = dir.CreateFile("a.vb").WriteAllText(source)
                Dim pdbPath = Path.Combine(dir.Path, "a.pdb")
                Dim defaultArgs = {"/nologo", "/debug", "a.vb"}
                Dim isDeterministic = extraArgs.Contains("/deterministic")
                Dim args = defaultArgs.Concat(extraArgs).ToArray()
                Using outWriter = New StringWriter(CultureInfo.InvariantCulture)

                    Dim vbc = New MockVisualBasicCompiler(dir.Path, args)
                    Dim exitCode = vbc.Run(outWriter)
                    Assert.Equal(0, exitCode)

                    Dim exePath = Path.Combine(dir.Path, "a.exe")
                    Assert.True(File.Exists(exePath))
                    Assert.True(File.Exists(pdbPath))

                    Using peStream = File.OpenRead(exePath)
                        PdbValidation.ValidateDebugDirectory(peStream, Nothing, pePdbPath, hashAlgorithm:=Nothing, hasEmbeddedPdb:=False, isDeterministic)
                    End Using
                End Using
            End Sub

            ' No mappings
            Using dir As New DisposableDirectory(Temp)
                Dim pePdbPath = Path.Combine(dir.Path, "a.pdb")
                assertPdbEmit(dir, pePdbPath, {})
            End Using

            ' Simple mapping
            Using dir As New DisposableDirectory(Temp)
                Dim pePdbPath = "q:\a.pdb"
                assertPdbEmit(dir, pePdbPath, {$"/pathmap:{dir.Path}=q:\"})
            End Using

            ' Simple mapping deterministic
            Using dir As New DisposableDirectory(Temp)
                Dim pePdbPath = "q:\a.pdb"
                assertPdbEmit(dir, pePdbPath, {$"/pathmap:{dir.Path}=q:\", "/deterministic"})
            End Using

            ' Partial mapping
            Using dir As New DisposableDirectory(Temp)
                Dim subDir = dir.CreateDirectory("example")
                Dim pePdbPath = "q:\example\a.pdb"
                assertPdbEmit(subDir, pePdbPath, {$"/pathmap:{dir.Path}=q:\"})
            End Using

            ' Legacy feature flag
            Using dir As New DisposableDirectory(Temp)
                Dim pePdbPath = Path.Combine(dir.Path, "a.pdb")
                assertPdbEmit(dir, "a.pdb", {"/features:pdb-path-determinism"})
            End Using

            ' Unix path map
            Using dir As New DisposableDirectory(Temp)
                Dim pdbPath = Path.Combine(dir.Path, "a.pdb")
                assertPdbEmit(dir, "/a.pdb", {$"/pathmap:{dir.Path}=/"})
            End Using

            ' Multi-specified path map with mixed slashes
            Using dir As New DisposableDirectory(Temp)
                Dim pdbPath = Path.Combine(dir.Path, "a.pdb")
                assertPdbEmit(dir, "/goo/a.pdb", {$"/pathmap:{dir.Path}=/goo,{dir.Path}{PathUtilities.DirectorySeparatorChar}=/bar"})
            End Using
        End Sub


        <WorkItem(540891, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540891")>
        <Fact>
        Public Sub ParseOut()
            Const baseDirectory As String = "C:\abc\def\baz"

            ' Should preserve fully qualified paths
            Dim parsedArgs = DefaultParse({"/out:C:\MyFolder\MyBinary.dll", "/t:library", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("MyBinary", parsedArgs.CompilationName)
            Assert.Equal("MyBinary.dll", parsedArgs.OutputFileName)
            Assert.Equal("MyBinary.dll", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal("C:\MyFolder", parsedArgs.OutputDirectory)

            parsedArgs = DefaultParse({"/out:""C:\My Folder\MyBinary.dll""", "/t:library", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("MyBinary", parsedArgs.CompilationName)
            Assert.Equal("MyBinary.dll", parsedArgs.OutputFileName)
            Assert.Equal("MyBinary.dll", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal("C:\My Folder", parsedArgs.OutputDirectory)

            parsedArgs = DefaultParse({"/refout:", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("refout", ":<file>").WithLocation(1, 1))

            parsedArgs = DefaultParse({"/refout:ref.dll", "/refonly", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.ERR_NoRefOutWhenRefOnly).WithLocation(1, 1))

            parsedArgs = DefaultParse({"/refonly:incorrect", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("refonly").WithLocation(1, 1))

            parsedArgs = DefaultParse({"/refout:ref.dll", "/target:module", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.ERR_NoNetModuleOutputWhenRefOutOrRefOnly).WithLocation(1, 1))

            parsedArgs = DefaultParse({"/refout:ref.dll", "/link:b", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = DefaultParse({"/refonly", "/link:b", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = DefaultParse({"/refonly", "/target:module", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.ERR_NoNetModuleOutputWhenRefOutOrRefOnly).WithLocation(1, 1))

            parsedArgs = DefaultParse({"/out:C:\""My Folder""\MyBinary.dll", "/t:library", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments("C:""My Folder\MyBinary.dll").WithLocation(1, 1))

            parsedArgs = DefaultParse({"/out:MyBinary.dll", "/t:library", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("MyBinary", parsedArgs.CompilationName)
            Assert.Equal("MyBinary.dll", parsedArgs.OutputFileName)
            Assert.Equal("MyBinary.dll", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory)

            parsedArgs = DefaultParse({"/out:Ignored.dll", "/out:MyBinary.dll", "/t:library", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("MyBinary", parsedArgs.CompilationName)
            Assert.Equal("MyBinary.dll", parsedArgs.OutputFileName)
            Assert.Equal("MyBinary.dll", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory)

            parsedArgs = DefaultParse({"/out:..\MyBinary.dll", "/t:library", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("MyBinary", parsedArgs.CompilationName)
            Assert.Equal("MyBinary.dll", parsedArgs.OutputFileName)
            Assert.Equal("MyBinary.dll", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal("C:\abc\def", parsedArgs.OutputDirectory)

            ' not specified: exe
            parsedArgs = DefaultParse({"a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("a", parsedArgs.CompilationName)
            Assert.Equal("a.exe", parsedArgs.OutputFileName)
            Assert.Equal("a.exe", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory)

            ' not specified: dll
            parsedArgs = DefaultParse({"/target:library", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("a", parsedArgs.CompilationName)
            Assert.Equal("a.dll", parsedArgs.OutputFileName)
            Assert.Equal("a.dll", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory)

            ' not specified: module
            parsedArgs = DefaultParse({"/target:module", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Null(parsedArgs.CompilationName)
            Assert.Equal("a.netmodule", parsedArgs.OutputFileName)
            Assert.Equal("a.netmodule", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory)

            ' not specified: appcontainerexe
            parsedArgs = DefaultParse({"/target:appcontainerexe", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("a", parsedArgs.CompilationName)
            Assert.Equal("a.exe", parsedArgs.OutputFileName)
            Assert.Equal("a.exe", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory)

            ' not specified: winmdobj
            parsedArgs = DefaultParse({"/target:winmdobj", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("a", parsedArgs.CompilationName)
            Assert.Equal("a.winmdobj", parsedArgs.OutputFileName)
            Assert.Equal("a.winmdobj", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory)

            ' drive-relative path:
            Dim currentDrive As Char = Directory.GetCurrentDirectory()(0)
            parsedArgs = DefaultParse({currentDrive + ":a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(currentDrive + ":a.vb"))

            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory)

            ' UNC
            parsedArgs = DefaultParse({"/out:\\b", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments("\\b"))

            Assert.Equal("a.exe", parsedArgs.OutputFileName)
            Assert.Equal("a", parsedArgs.CompilationName)
            Assert.Equal("a.exe", parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/out:\\server\share\file.exe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal("\\server\share", parsedArgs.OutputDirectory)
            Assert.Equal("file.exe", parsedArgs.OutputFileName)
            Assert.Equal("file", parsedArgs.CompilationName)
            Assert.Equal("file.exe", parsedArgs.CompilationOptions.ModuleName)

            ' invalid name
            parsedArgs = DefaultParse({"/out:a.b" & vbNullChar & "b", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments("a.b" & vbNullChar & "b"))

            Assert.Equal("a.exe", parsedArgs.OutputFileName)
            Assert.Equal("a", parsedArgs.CompilationName)
            Assert.Equal("a.exe", parsedArgs.CompilationOptions.ModuleName)

            ' Temp Skip: Unicode?
            ' parsedArgs = DefaultParse({"/out:a" & ChrW(&HD800) & "b.dll", "a.vb"}, _baseDirectory)
            ' parsedArgs.Errors.Verify(
            '    Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments("a" & ChrW(&HD800) & "b.dll"))

            ' Assert.Equal("a.exe", parsedArgs.OutputFileName)
            ' Assert.Equal("a", parsedArgs.CompilationName)
            ' Assert.Equal("a.exe", parsedArgs.CompilationOptions.ModuleName)

            ' Temp Skip: error message changed (path)
            'parsedArgs = DefaultParse({"/out:"" a.dll""", "a.vb"}, _baseDirectory)
            'parsedArgs.Errors.Verify(
            '    Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(" a.dll"))

            'Assert.Equal("a.exe", parsedArgs.OutputFileName)
            'Assert.Equal("a", parsedArgs.CompilationName)
            'Assert.Equal("a.exe", parsedArgs.CompilationOptions.ModuleName)

            ' Dev11 reports BC2012: can't open 'a<>.z' for writing
            parsedArgs = DefaultParse({"/out:""a<>.dll""", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments("a<>.dll"))

            Assert.Equal("a.exe", parsedArgs.OutputFileName)
            Assert.Equal("a", parsedArgs.CompilationName)
            Assert.Equal("a.exe", parsedArgs.CompilationOptions.ModuleName)

            ' bad value
            parsedArgs = DefaultParse({"/out", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("out", ":<file>"))

            parsedArgs = DefaultParse({"/OUT:", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("out", ":<file>"))

            parsedArgs = DefaultParse({"/REFOUT:", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("refout", ":<file>"))

            parsedArgs = DefaultParse({"/refout:ref.dll", "/refonly", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_NoRefOutWhenRefOnly).WithLocation(1, 1))

            parsedArgs = DefaultParse({"/out+", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/out+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/out-:", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/out-:")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/out:.exe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(".exe"))

            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/t:exe", "/out:.exe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(".exe"))

            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/t:library", "/out:.dll", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(".dll"))

            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/t:module", "/out:.netmodule", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal(".netmodule", parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Equal(".netmodule", parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({".vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(".exe"))

            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/t:exe", ".vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(".exe"))

            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/t:library", ".vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(".dll"))

            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/t:module", ".vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal(".netmodule", parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Equal(".netmodule", parsedArgs.CompilationOptions.ModuleName)
        End Sub

        <Theory,
     InlineData({"/out:.x", "a.vb"}, Nothing, ".x", ".x.exe", ".x.exe"),
     InlineData({"/target:winexe", "/out:.x.eXe", "a.vb"}, Nothing, ".x", ".x.eXe", ".x.eXe"),
     InlineData({"/target:library", "/out:.x", "a.vb"}, Nothing, ".x", ".x.dll", ".x.dll"),
     InlineData({"/target:library", "/out:.X.Dll", "a.vb"}, Nothing, ".X", ".X.Dll", ".X.Dll"),
     InlineData({"/target:module", "/out:.x", "a.vb"}, Nothing, Nothing, ".x", ".x"),
     InlineData({"/target:module", "/out:x.dll", "a.vb"}, Nothing, Nothing, "x.dll", "x.dll"),
     InlineData({"/target:module", "/out:.x.netmodule", "a.vb"}, Nothing, Nothing, ".x.netmodule", ".x.netmodule"),
     InlineData({"/target:module", "/out:x", "a.vb"}, Nothing, Nothing, "x.netmodule", "x.netmodule"),
     InlineData({"/target:library", "/out:.dll", "a.vb"}, ".dll", Nothing, Nothing, Nothing),
     InlineData({"/target:winexe", "/out:.exe", "a.vb"}, ".exe", Nothing, Nothing, Nothing)
    >
        Public Sub ParseOut2(args() As String, VerifyFTL_InputFileNameTooLong As String, compilationName As String, OutputFileName As String, ModuleName As String)
            ' exe
            Dim parsedArgs = DefaultParse(args, _baseDirectory)
            If VerifyFTL_InputFileNameTooLong Is Nothing Then
                parsedArgs.Errors.Verify()
            Else
                parsedArgs.Errors.Verify(Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(VerifyFTL_InputFileNameTooLong))
            End If
            Assert.Equal(compilationName, parsedArgs.CompilationName)
            Assert.Equal(OutputFileName, parsedArgs.OutputFileName)
            Assert.Equal(ModuleName, parsedArgs.CompilationOptions.ModuleName)
        End Sub

        <WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497"), Theory>
        <InlineData({"/keyfile:", "/target:library", "/nologo", "/preferreduilang:en", "a.vb"})> ' EmptyKeyFile
        <InlineData({"/keyfile:""""", "/target:library", "/nologo", "/preferreduilang:en", "a.vb"})> ' PublicSign
        <InlineData({"/keyfile:", "/publicsign", "/target:library", "/nologo", "/preferreduilang:en", "a.vb"})> ' EmptyKeyFile PublicSign
        <InlineData({"/keyfile:""""", "/publicsign", "/target:library", "/nologo", "/preferreduilang:en", "a.vb"})>
        Public Sub ConsistentErrorMessageWhenProviding_KeyFile(ParamArray Args() As String)
            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                Dim vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, Args)
                Dim exitCode = vbc.Run(outWriter)

                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC2006: option 'keyfile' requires ':<file>'", outWriter.ToString().Trim())
            End Using

        End Sub


        <Fact, WorkItem(531020, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531020")>
        Public Sub ParseDocBreak1()
            Const baseDirectory As String = "C:\abc\def\baz"

            ' In dev11, this appears to be equivalent to /doc- (i.e. don't parse and don't output).
            Dim parsedArgs = DefaultParse({"/doc:""""", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("doc", ":<file>"))
            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)
        End Sub

        <Fact, WorkItem(705173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/705173")>
        Public Sub Ensure_UTF8_Explicit_Prefix_In_Documentation_Comment_File()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("src.vb")
            src.WriteAllText(
"
''' <summary>ABC...XYZ</summary>
Class C
End Class
")

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable,
                                     $"/nologo /doc:{dir.ToString}\src.xml /t:library {src.ToString}",
                                     startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Dim fileContents = File.ReadAllBytes(dir.ToString() & "\src.xml")
            Assert.InRange(fileContents.Length, 4, Integer.MaxValue)
            Assert.Equal(&HEF, fileContents(0))
            Assert.Equal(&HBB, fileContents(1))
            Assert.Equal(&HBF, fileContents(2))

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(733242, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/733242")>
        Public Sub Bug733242()
            Dim dir = Temp.CreateDirectory()

            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
"
''' <summary>ABC...XYZ</summary>
Class C
End Class
")

            Dim xml = dir.CreateFile("a.xml")
            xml.WriteAllText("EMPTY")

            Using xmlFileHandle As FileStream = File.Open(xml.ToString(), FileMode.Open, FileAccess.Read, FileShare.Delete Or FileShare.ReadWrite)

                Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, $"/nologo /t:library /doc+ {src.ToString}", startFolder:=dir.ToString(), expectedRetCode:=0)
                AssertOutput(<text></text>, output)

                Assert.True(File.Exists(Path.Combine(dir.ToString(), "a.xml")))

                Using reader As New StreamReader(xmlFileHandle)
                    Dim content = reader.ReadToEnd()
                    AssertOutput(
<text>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
a
</name>
</assembly>
<members>
<member name="T:C">
 <summary>ABC...XYZ</summary>
</member>
</members>
</doc>
]]></text>,
content)
                End Using

            End Using

            CleanupAllGeneratedFiles(src.Path)
            CleanupAllGeneratedFiles(xml.Path)
        End Sub

        <Fact, WorkItem(768605, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768605")>
        Public Sub Bug768605()
            Dim dir = Temp.CreateDirectory()

            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
"''' <summary>ABC</summary>
Class C: End Class
''' <summary>XYZ</summary>
Class E: End Class")

            Dim xml = dir.CreateFile("a.xml")
            xml.WriteAllText("EMPTY")

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, String.Format("/nologo /t:library /doc+ {0}", src.ToString()), startFolder:=dir.ToString(), expectedRetCode:=0)
            AssertOutput(<text></text>, output)

            Using reader As New StreamReader(xml.ToString())
                Dim content = reader.ReadToEnd()
                AssertOutput(
<text>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
a
</name>
</assembly>
<members>
<member name="T:C">
 <summary>ABC</summary>
</member>
<member name="T:E">
 <summary>XYZ</summary>
</member>
</members>
</doc>
]]>
</text>,
content)
            End Using

            src.WriteAllText(
"
''' <summary>ABC</summary>
Class C: End Class
".Replace(vbLf, vbCrLf))

            output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, String.Format("/nologo /t:library /doc+ {0}", src.ToString()), startFolder:=dir.ToString(), expectedRetCode:=0)
            AssertOutput(<text></text>, output)

            Using reader As New StreamReader(xml.ToString())
                Dim content = reader.ReadToEnd()
                AssertOutput(
<text>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
a
</name>
</assembly>
<members>
<member name="T:C">
 <summary>ABC</summary>
</member>
</members>
</doc>
]]>
</text>,
content)
            End Using

            CleanupAllGeneratedFiles(src.Path)
            CleanupAllGeneratedFiles(xml.Path)
        End Sub

        <Fact, WorkItem(705148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/705148")>
        Public Sub Bug705148a()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
"''' <summary>ABC...XYZ</summary>
Class C
End Class")

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, $"/nologo /t:library /doc:abcdfg.xyz /doc+ {src.ToString()}", startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "a.xml")))

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(705148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/705148")>
        Public Sub Bug705148b()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
"''' <summary>ABC...XYZ</summary>
Class C
End Class")

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, $"/nologo /t:library /doc /out:MyXml.dll {src.ToString}", startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "MyXml.xml")))

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(705148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/705148")>
        Public Sub Bug705148c()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
"''' <summary>ABC...XYZ</summary>
Class C
End Class")

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, $"/nologo /t:library /doc:doc.xml /doc+ {src.ToString}", startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "a.xml")))

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(705202, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/705202")>
        Public Sub Bug705202a()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
"''' <summary>ABC...XYZ</summary>
Class C
End Class")

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, $"/nologo /t:library /doc:doc.xml /out:out.dll {src.ToString}", startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "doc.xml")))

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(705202, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/705202")>
        Public Sub Bug705202b()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
"''' <summary>ABC...XYZ</summary>
Class C
End Class")

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, $"/nologo /t:library /doc:doc.xml /doc /out:out.dll {src.ToString}", startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "out.xml")))

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(705202, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/705202")>
        Public Sub Bug705202c()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
"''' <summary>ABC...XYZ</summary>
Class C
End Class")

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, $"/nologo /t:library /doc:doc.xml /out:out.dll /doc+ {src.ToString}", startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "out.xml")))

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(531021, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531021")>
        Public Sub ParseDocBreak2()

            ' In dev11, if you give an invalid file name, the documentation comments
            ' are parsed but writing the XML file fails with (warning!) BC42311.
            Const baseDirectory As String = "C:\abc\def\baz"

            Dim parsedArgs = DefaultParse({"/doc:"" """, "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.WRN_XMLCannotWriteToXMLDocFile2).WithArguments(" ", "The system cannot find the path specified"))
            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            parsedArgs = DefaultParse({"/doc:"" \ """, "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.WRN_XMLCannotWriteToXMLDocFile2).WithArguments(" \ ", "The system cannot find the path specified"))
            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            ' UNC
            parsedArgs = DefaultParse({"/doc:\\b", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.WRN_XMLCannotWriteToXMLDocFile2).WithArguments("\\b", "The system cannot find the path specified"))

            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode) ' Even though the format was incorrect

            ' invalid name:
            parsedArgs = DefaultParse({"/doc:a.b" + ChrW(0) + "b", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.WRN_XMLCannotWriteToXMLDocFile2).WithArguments("a.b" + ChrW(0) + "b", "The system cannot find the path specified"))

            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode) ' Even though the format was incorrect

            parsedArgs = DefaultParse({"/doc:a" + ChrW(55296) + "b.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.WRN_XMLCannotWriteToXMLDocFile2).WithArguments("a" + ChrW(55296) + "b.xml", "The system cannot find the path specified"))

            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode) ' Even though the format was incorrect

            parsedArgs = DefaultParse({"/doc:""a<>.xml""", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.WRN_XMLCannotWriteToXMLDocFile2).WithArguments("a<>.xml", "The system cannot find the path specified"))

            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode) ' Even though the format was incorrect
        End Sub

        <Fact>
        Public Sub ParseDoc()
            Const baseDirectory As String = "C:\abc\def\baz"

            Dim parsedArgs = DefaultParse({"/doc:", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("doc", ":<file>"))
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            parsedArgs = DefaultParse({"/doc", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(Path.Combine(baseDirectory, "a.xml"), parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            parsedArgs = DefaultParse({"/doc+", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(Path.Combine(baseDirectory, "a.xml"), parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            parsedArgs = DefaultParse({"/doc-", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.None, parsedArgs.ParseOptions.DocumentationMode)

            parsedArgs = DefaultParse({"/doc+:abc.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("doc"))
            Assert.Equal(Path.Combine(baseDirectory, "a.xml"), parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            parsedArgs = DefaultParse({"/doc-:a.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("doc"))
            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.None, parsedArgs.ParseOptions.DocumentationMode)

            ' Should preserve fully qualified paths
            parsedArgs = DefaultParse({"/doc:C:\MyFolder\MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("C:\MyFolder\MyBinary.xml", parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            ' Should handle quotes
            parsedArgs = DefaultParse({"/doc:""C:\My Folder\MyBinary.xml""", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("C:\My Folder\MyBinary.xml", parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            ' Should expand partially qualified paths
            parsedArgs = DefaultParse({"/doc:MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(Path.Combine(baseDirectory, "MyBinary.xml"), parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            ' Should expand partially qualified paths
            parsedArgs = DefaultParse({"/doc:..\MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("C:\abc\def\MyBinary.xml", parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            ' drive-relative path:
            Dim currentDrive As Char = Directory.GetCurrentDirectory()(0)
            parsedArgs = DefaultParse({"/doc:" + currentDrive + ":a.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.WRN_XMLCannotWriteToXMLDocFile2).WithArguments(currentDrive + ":a.xml", "The system cannot find the path specified"))

            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode) ' Even though the format was incorrect

            ' UNC
            parsedArgs = DefaultParse({"/doc:\\server\share\file.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal("\\server\share\file.xml", parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)
        End Sub

        <Fact>
        Public Sub ParseDocAndOut()
            Const baseDirectory As String = "C:\abc\def\baz"

            ' Can specify separate directories for binary and XML output.
            Dim parsedArgs = DefaultParse({"/doc:a\b.xml", "/out:c\d.exe", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal("C:\abc\def\baz\a\b.xml", parsedArgs.DocumentationPath)

            Assert.Equal("C:\abc\def\baz\c", parsedArgs.OutputDirectory)
            Assert.Equal("d.exe", parsedArgs.OutputFileName)

            ' XML does not fall back on output directory.
            parsedArgs = DefaultParse({"/doc:b.xml", "/out:c\d.exe", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal("C:\abc\def\baz\b.xml", parsedArgs.DocumentationPath)

            Assert.Equal("C:\abc\def\baz\c", parsedArgs.OutputDirectory)
            Assert.Equal("d.exe", parsedArgs.OutputFileName)
        End Sub

        <Fact>
        Public Sub ParseDocMultiple()
            Const baseDirectory As String = "C:\abc\def\baz"

            Dim parsedArgs = DefaultParse({"/doc+", "/doc-", "/doc+", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)
            Assert.Equal(Path.Combine(baseDirectory, "a.xml"), parsedArgs.DocumentationPath)

            parsedArgs = DefaultParse({"/doc-", "/doc+", "/doc-", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(DocumentationMode.None, parsedArgs.ParseOptions.DocumentationMode)
            Assert.Null(parsedArgs.DocumentationPath)

            parsedArgs = DefaultParse({"/doc:a.xml", "/doc-", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(DocumentationMode.None, parsedArgs.ParseOptions.DocumentationMode)
            Assert.Null(parsedArgs.DocumentationPath)

            parsedArgs = DefaultParse({"/doc:abc.xml", "/doc+", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)
            Assert.Equal(Path.Combine(baseDirectory, "a.xml"), parsedArgs.DocumentationPath)

            parsedArgs = DefaultParse({"/doc-", "/doc:a.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)
            Assert.Equal(Path.Combine(baseDirectory, "a.xml"), parsedArgs.DocumentationPath)

            parsedArgs = DefaultParse({"/doc+", "/doc:a.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)
            Assert.Equal(Path.Combine(baseDirectory, "a.xml"), parsedArgs.DocumentationPath)
        End Sub

        <Fact>
        Public Sub ParseErrorLog()
            Const baseDirectory As String = "C:\abc\def\baz"

            Dim parsedArgs = DefaultParse({"/errorlog:", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("errorlog", ":<file>"))
            Assert.Null(parsedArgs.ErrorLogPath)
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics)

            parsedArgs = DefaultParse({"/errorlog", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("errorlog", ":<file>"))
            Assert.Null(parsedArgs.ErrorLogPath)
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics)

            ' Should preserve fully qualified paths
            parsedArgs = DefaultParse({"/errorlog:C:\MyFolder\MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("C:\MyFolder\MyBinary.xml", parsedArgs.ErrorLogPath)
            Assert.True(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics)

            ' Should handle quotes
            parsedArgs = DefaultParse({"/errorlog:""C:\My Folder\MyBinary.xml""", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("C:\My Folder\MyBinary.xml", parsedArgs.ErrorLogPath)
            Assert.True(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics)

            ' Quote after a \ is treated as an escape
            parsedArgs = DefaultParse({"/errorlog:C:\""My Folder""\MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments("C:""My Folder\MyBinary.xml").WithLocation(1, 1))

            ' Should expand partially qualified paths
            parsedArgs = DefaultParse({"/errorlog:MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(Path.Combine(baseDirectory, "MyBinary.xml"), parsedArgs.ErrorLogPath)

            ' Should expand partially qualified paths
            parsedArgs = DefaultParse({"/errorlog:..\MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("C:\abc\def\MyBinary.xml", parsedArgs.ErrorLogPath)
            Assert.True(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics)

            ' drive-relative path:
            Dim currentDrive As Char = Directory.GetCurrentDirectory()(0)
            Dim filePath = currentDrive + ":a.xml"
            parsedArgs = DefaultParse({"/errorlog:" + filePath, "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(filePath))

            Assert.Null(parsedArgs.ErrorLogPath)
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics)

            ' UNC
            parsedArgs = DefaultParse({"/errorlog:\\server\share\file.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal("\\server\share\file.xml", parsedArgs.ErrorLogPath)
            Assert.True(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics)
        End Sub

        <Fact>
        Public Sub ParseErrorLogAndOut()
            Const baseDirectory As String = "C:\abc\def\baz"

            ' Can specify separate directories for binary and error log output.
            Dim parsedArgs = DefaultParse({"/errorlog:a\b.xml", "/out:c\d.exe", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal("C:\abc\def\baz\a\b.xml", parsedArgs.ErrorLogPath)

            Assert.Equal("C:\abc\def\baz\c", parsedArgs.OutputDirectory)
            Assert.Equal("d.exe", parsedArgs.OutputFileName)

            ' error log does not fall back on output directory.
            parsedArgs = DefaultParse({"/errorlog:b.xml", "/out:c\d.exe", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal("C:\abc\def\baz\b.xml", parsedArgs.ErrorLogPath)

            Assert.Equal("C:\abc\def\baz\c", parsedArgs.OutputDirectory)
            Assert.Equal("d.exe", parsedArgs.OutputFileName)
        End Sub

        <Fact>
        Public Sub KeyContainerAndKeyFile()
            ' KEYCONTAINER
            Dim parsedArgs = DefaultParse({"/KeyContainer:key-cont-name", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("key-cont-name", parsedArgs.CompilationOptions.CryptoKeyContainer)

            parsedArgs = DefaultParse({"/KEYcontainer", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("keycontainer", ":<string>"))
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyContainer)

            parsedArgs = DefaultParse({"/keycontainer-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/keycontainer-"))
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyContainer)

            parsedArgs = DefaultParse({"/keycontainer:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("keycontainer", ":<string>"))
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyContainer)

            parsedArgs = DefaultParse({"/keycontainer: ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("keycontainer", ":<string>"))
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyContainer)

            ' KEYFILE
            parsedArgs = DefaultParse({"/keyfile:\somepath\s""ome Fil""e.goo.bar", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("\somepath\some File.goo.bar", parsedArgs.CompilationOptions.CryptoKeyFile)

            parsedArgs = DefaultParse({"/keyFile", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("keyfile", ":<file>"))
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyFile)

            parsedArgs = DefaultParse({"/keyfile-", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/keyfile-"))
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyFile)

            parsedArgs = DefaultParse({"/keyfile: ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("keyfile", ":<file>"))
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyFile)

            ' default value
            parsedArgs = DefaultParse({"a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(Nothing, parsedArgs.CompilationOptions.CryptoKeyContainer)
            Assert.Equal(Nothing, parsedArgs.CompilationOptions.CryptoKeyFile)

            ' keyfile/keycontainer conflicts 
            parsedArgs = DefaultParse({"/keycontainer:a", "/keyfile:b", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(Nothing, parsedArgs.CompilationOptions.CryptoKeyContainer)
            Assert.Equal("b", parsedArgs.CompilationOptions.CryptoKeyFile)

            ' keyfile/keycontainer conflicts 
            parsedArgs = DefaultParse({"/keyfile:b", "/keycontainer:a", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("a", parsedArgs.CompilationOptions.CryptoKeyContainer)
            Assert.Equal(Nothing, parsedArgs.CompilationOptions.CryptoKeyFile)

        End Sub

        <Fact, WorkItem(530088, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530088")>
        Public Sub Platform()
            ' test recognizing all options
            Dim parsedArgs = DefaultParse({"/platform:X86", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CodeAnalysis.Platform.X86, parsedArgs.CompilationOptions.Platform)

            parsedArgs = DefaultParse({"/platform:x64", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CodeAnalysis.Platform.X64, parsedArgs.CompilationOptions.Platform)

            parsedArgs = DefaultParse({"/platform:itanium", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CodeAnalysis.Platform.Itanium, parsedArgs.CompilationOptions.Platform)

            parsedArgs = DefaultParse({"/platform:anycpu", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CodeAnalysis.Platform.AnyCpu, parsedArgs.CompilationOptions.Platform)

            parsedArgs = DefaultParse({"/platform:anycpu32bitpreferred", "/t:exe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CodeAnalysis.Platform.AnyCpu32BitPreferred, parsedArgs.CompilationOptions.Platform)

            parsedArgs = DefaultParse({"/platform:anycpu32bitpreferred", "/t:appcontainerexe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CodeAnalysis.Platform.AnyCpu32BitPreferred, parsedArgs.CompilationOptions.Platform)

            parsedArgs = DefaultParse({"/platform:arm", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CodeAnalysis.Platform.Arm, parsedArgs.CompilationOptions.Platform)

            ' test default (AnyCPU)
            parsedArgs = DefaultParse({"/debug-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CodeAnalysis.Platform.AnyCpu, parsedArgs.CompilationOptions.Platform)

            ' test missing 
            parsedArgs = DefaultParse({"/platform:", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("platform", ":<string>"))
            parsedArgs = DefaultParse({"/platform", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("platform", ":<string>"))
            parsedArgs = DefaultParse({"/platform+", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/platform+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            ' test illegal input
            parsedArgs = DefaultParse({"/platform:abcdef", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("platform", "abcdef"))

            ' test overriding
            parsedArgs = DefaultParse({"/platform:anycpu32bitpreferred", "/platform:anycpu", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CodeAnalysis.Platform.AnyCpu, parsedArgs.CompilationOptions.Platform)

            ' test illegal
            parsedArgs = DefaultParse({"/platform:anycpu32bitpreferred", "/t:library", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_LibAnycpu32bitPreferredConflict).WithArguments("Platform", "AnyCpu32BitPreferred").WithLocation(1, 1))

            parsedArgs = DefaultParse({"/platform:anycpu", "/platform:anycpu32bitpreferred", "/target:winmdobj", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_LibAnycpu32bitPreferredConflict).WithArguments("Platform", "AnyCpu32BitPreferred").WithLocation(1, 1))
        End Sub

        <Theory>
        <InlineData({"/filealign:512", "a.vb"}, 512), InlineData({"/filealign:1024", "a.vb"}, 1024), InlineData({"/filealign:2048", "a.vb"}, 2048), InlineData({"/filealign:4096", "a.vb"}, 4096), InlineData({"/filealign:8192", "a.vb"}, 8192)>
        <InlineData({"/filealign:01000", "a.vb"}, 512), InlineData({"/filealign:02000", "a.vb"}, 1024), InlineData({"/filealign:04000", "a.vb"}, 2048), InlineData({"/filealign:010000", "a.vb"}, 4096), InlineData({"/filealign:020000", "a.vb"}, 8192)> ' Oct Values
        <InlineData({"/filealign:0x200", "a.vb"}, 512), InlineData({"/filealign:0x400", "a.vb"}, 1024), InlineData({"/filealign:0x800", "a.vb"}, 2048), InlineData({"/filealign:0x1000", "a.vb"}, 4096), InlineData({"/filealign:0x2000", "a.vb"}, 8192)> ' Hex Values
        <InlineData({"/platform:x86", "a.vb"}, 0)> ' test default (no value)
        Public Sub FileAlignment(args() As String, expectedFileAlignment As Int32)
            Dim parsedArgs = DefaultParse(args, _baseDirectory)
            Assert.Equal(expectedFileAlignment, parsedArgs.EmitOptions.FileAlignment)

        End Sub

        <Fact> Public Sub FileAlignment_Missing()
            Dim parsedArgs = DefaultParse({"/filealign:", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("filealign", ":<number>"))
        End Sub

        <Theory,
        InlineData({"/filealign:0", "a.vb"}, "0"), InlineData({"/filealign:0x", "a.vb"}, "0x"), InlineData({"/filealign:0x0", "a.vb"}, "0x0"), InlineData({"/filealign:-1", "a.vb"}, "-1"), InlineData({"/filealign:-0x100", "a.vb"}, "-0x100")>
        Public Sub FileAlignment_Illegal(args() As String, expectedFileAlignmentArg As String)
            Dim parsedArgs = DefaultParse(args, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("filealign", expectedFileAlignmentArg))
        End Sub

        <Theory, InlineData({"/removeintcheckS", "a.vb"}, False), InlineData({"/removeintcheckS+", "a.vb"}, False), InlineData({"/removeintcheckS-", "a.vb"}, True), InlineData({"/removeintchecks+", "/removeintchecks-", "a.vb"}, True)>
        Public Sub RemoveIntChecks(args() As String, CheckOverflow As Boolean)
            Dim parsedArgs = DefaultParse(args, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CheckOverflow, parsedArgs.CompilationOptions.CheckOverflow)
        End Sub

        <Theory, InlineData({"/removeintchecks:", "a.vb"}), InlineData({"/removeintchecks:+", "a.vb"}), InlineData({"/removeintchecks+:", "a.vb"})>
        Public Sub RemoveIntChecks_Errors(ParamArray args() As String)
            Dim parsedArgs = DefaultParse(args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("removeintchecks"))
        End Sub

        <Theory,
    InlineData({"/baseaddress:0", "a.vb"}, CULng(&H_0000)),
    InlineData({"/baseaddress:1024", "a.vb"}, CULng(&H_1024)), InlineData({"/baseaddress:2048", "a.vb"}, CULng(&H_2048)),
    InlineData({"/baseaddress:4096", "a.vb"}, CULng(&H_4096)), InlineData({"/baseaddress:8192", "a.vb"}, CULng(&H_8192)),
    InlineData({"/baseaddress:0x200", "a.vb"}, CULng(&H_0200)), InlineData({"/baseaddress:0x400", "a.vb"}, CULng(&H_0400)),
    InlineData({"/baseaddress:0x800", "a.vb"}, CULng(&H_0800)), InlineData({"/baseaddress:0x1000", "a.vb"}, CULng(&H_1000)),
    InlineData({"/baseaddress:0xFFFFFFFFFFFFFFFF", "a.vb"}, ULong.MaxValue), InlineData({"/baseaddress:FFFFFFFFFFFFFFFF", "a.vb"}, ULong.MaxValue),
    InlineData({"/baseaddress:00", "a.vb"}, CULng(&H_0000)),
    InlineData({"/baseaddress:01024", "a.vb"}, CULng(&H_1024)), InlineData({"/baseaddress:02048", "a.vb"}, CULng(&H_2048)),
    InlineData({"/baseaddress:04096", "a.vb"}, CULng(&H_4096)), InlineData({"/baseaddress:08192", "a.vb"}, CULng(&H_8192)),
    InlineData({"/platform:x86", "a.vb"}, CULng(&H_0000))>
        Public Sub BaseAddress(args As String(), addr As ULong)
            ' This test is about what passes the parser. Even if a value was accepted by the parser it might not be considered
            ' as a valid base address later on (e.g. values >0x8000).

            ' test decimal values being treated as hex
            Dim parsedArgs = DefaultParse(args, _baseDirectory)
            Assert.Equal(addr, parsedArgs.EmitOptions.BaseAddress)
        End Sub

        <Fact()>
        Public Sub BaseAddress()
            ' test missing 
            Dim parsedArgs = DefaultParse({"/baseaddress:", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("baseaddress", ":<number>"))

            ' test illegal
            parsedArgs = DefaultParse({"/baseaddress:0x10000000000000000", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("baseaddress", "0x10000000000000000"))
            parsedArgs = DefaultParse({"/BASEADDRESS:-1", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("baseaddress", "-1"))
            parsedArgs = DefaultParse({"/BASEADDRESS:" + ULong.MaxValue.ToString, "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("baseaddress", ULong.MaxValue.ToString))
        End Sub

        <Fact()>
        Public Sub BinaryFile()
            Dim binaryPath = Temp.CreateFile().WriteAllBytes(TestResources.NetFX.v4_0_30319.mscorlib).Path
            Dim outWriter As New StringWriter()
            Dim exitCode As Integer = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/preferreduilang:en", binaryPath}).Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC2015: the file '" + binaryPath + "' is not a text file", outWriter.ToString.Trim())

            CleanupAllGeneratedFiles(binaryPath)
        End Sub

        <Fact()>
        Public Sub AddModule()
            Dim parsedArgs = DefaultParse({"/nostdlib", "/vbruntime-", "/addMODULE:c:\,d:\x\y\z,abc,,", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(3, parsedArgs.MetadataReferences.Length)
            Assert.Equal("c:\", parsedArgs.MetadataReferences(0).Reference)
            Assert.Equal(MetadataImageKind.Module, parsedArgs.MetadataReferences(0).Properties.Kind)
            Assert.Equal("d:\x\y\z", parsedArgs.MetadataReferences(1).Reference)
            Assert.Equal(MetadataImageKind.Module, parsedArgs.MetadataReferences(1).Properties.Kind)
            Assert.Equal("abc", parsedArgs.MetadataReferences(2).Reference)
            Assert.Equal(MetadataImageKind.Module, parsedArgs.MetadataReferences(2).Properties.Kind)
            Assert.False(parsedArgs.MetadataReferences(0).Reference.EndsWith("mscorlib.dll", StringComparison.Ordinal))
            Assert.False(parsedArgs.MetadataReferences(1).Reference.EndsWith("mscorlib.dll", StringComparison.Ordinal))
            Assert.False(parsedArgs.MetadataReferences(2).Reference.EndsWith("mscorlib.dll", StringComparison.Ordinal))
            Assert.True(parsedArgs.DefaultCoreLibraryReference.Value.Reference.EndsWith("mscorlib.dll", StringComparison.Ordinal))
            Assert.Equal(MetadataImageKind.Assembly, parsedArgs.DefaultCoreLibraryReference.Value.Properties.Kind)

            parsedArgs = DefaultParse({"/ADDMODULE", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("addmodule", ":<file_list>"))

            parsedArgs = DefaultParse({"/addmodule:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("addmodule", ":<file_list>"))

            parsedArgs = DefaultParse({"/addmodule+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/addmodule+")) ' TODO: Dev11 reports ERR_ArgumentRequired
        End Sub

        <Fact()>
        Public Sub LibPathsAndLibEnvVariable()
            Dim parsedArgs = DefaultParse({"/libpath:c:\,d:\x\y\z,abc,,", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertReferencePathsEqual(parsedArgs.ReferencePaths, Nothing, "c:\", "d:\x\y\z", Path.Combine(_baseDirectory, "abc"))

            parsedArgs = DefaultParse({"/lib:c:\Windows", "/libpaths:abc\def, , , ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertReferencePathsEqual(parsedArgs.ReferencePaths, Nothing, "c:\Windows", Path.Combine(_baseDirectory, "abc\def"))

            parsedArgs = DefaultParse({"/libpath", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("libpath", ":<path_list>"))

            parsedArgs = DefaultParse({"/libpath:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("libpath", ":<path_list>"))

            parsedArgs = DefaultParse({"/libpath+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/libpath+")) ' TODO: Dev11 reports ERR_ArgumentRequired
        End Sub

        <Fact(), WorkItem(546005, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546005")>
        Public Sub LibPathsAndLibEnvVariable_Relative_vbc()
            Dim tempFolder = Temp.CreateDirectory()
            Dim baseDirectory = tempFolder.ToString()

            Dim subFolder = tempFolder.CreateDirectory("temp")
            Dim subDirectory = subFolder.ToString()

            Dim src = Temp.CreateFile("a.vb")
            src.WriteAllText("Imports System")

            Dim exitCode As Integer
            Using outWriter = New StringWriter()
                exitCode = New MockVisualBasicCompiler(Nothing, subDirectory, {"/nologo", "/t:library", "/out:abc.xyz", src.ToString()}).Run(outWriter, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Equal("", outWriter.ToString().Trim())
            End Using
            Using outWriter = New StringWriter()
                exitCode = New MockVisualBasicCompiler(Nothing, baseDirectory, {"/nologo", "/libpath:temp", "/r:abc.xyz.dll", "/t:library", src.ToString()}).Run(outWriter, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Equal("", outWriter.ToString().Trim())
            End Using
            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact()>
        Public Sub UnableWriteOutput()
            Dim tempFolder = Temp.CreateDirectory()
            Dim baseDirectory = tempFolder.ToString()
            Dim subFolder = tempFolder.CreateDirectory("temp.dll")

            Dim src = Temp.CreateFile("a.vb")
            src.WriteAllText("Imports System")

            Using outWriter As New StringWriter()
                Dim exitCode As Integer = New MockVisualBasicCompiler(Nothing, baseDirectory, {"/nologo", "/preferreduilang:en", "/t:library", "/out:" & subFolder.ToString(), src.ToString()}).Run(outWriter, Nothing)
                Assert.Equal(1, exitCode)
                Assert.True(outWriter.ToString().Contains("error BC2012: can't open '" & subFolder.ToString() & "' for writing: ")) ' Cannot create a file when that file already exists.
            End Using
            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact()>
        Public Sub SdkPathAndLibEnvVariable()
            Dim parsedArgs = DefaultParse({"/libpath:c:lib2", "/sdkpath:<>,d:\sdk1", "/vbruntime*", "/nostdlib", "a.vb"}, _baseDirectory)

            ' invalid paths are ignored
            parsedArgs.Errors.Verify()
            AssertReferencePathsEqual(parsedArgs.ReferencePaths, "d:\sdk1")

            parsedArgs = DefaultParse({"/sdkpath:c:\Windows", "/sdkpath:d:\Windows", "/vbruntime*", "/nostdlib", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertReferencePathsEqual(parsedArgs.ReferencePaths, "d:\Windows")

            parsedArgs = DefaultParse({"/sdkpath:c:\Windows,d:\blah", "a.vb"}, _baseDirectory)
            AssertReferencePathsEqual(parsedArgs.ReferencePaths, "c:\Windows", "d:\blah")

            parsedArgs = DefaultParse({"/libpath:c:\Windows,d:\blah", "/sdkpath:c:\lib2", "a.vb"}, _baseDirectory)
            AssertReferencePathsEqual(parsedArgs.ReferencePaths, "c:\lib2", "c:\Windows", "d:\blah")

            parsedArgs = DefaultParse({"/sdkpath", "/vbruntime*", "/nostdlib", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("sdkpath", ":<path>"))

            parsedArgs = DefaultParse({"/sdkpath:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("sdkpath", ":<path>"))

            parsedArgs = DefaultParse({"/sdkpath+", "/vbruntime*", "/nostdlib", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/sdkpath+")) ' TODO: Dev11 reports ERR_ArgumentRequired
        End Sub

        <Fact()>
        Public Sub VbRuntime()

            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("src.vb")
            src.WriteAllText(
"
Imports Microsoft.VisualBasic
Class C
Dim a = vbLf
Dim b = Loc
End Class")

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /preferreduilang:en /vbruntime /t:library " & src.ToString(), expectedRetCode:=1)
            AssertOutput(
"src.vb(5) : error BC30455: Argument not specified for parameter 'FileNumber' of 'Public Function Loc(FileNumber As Integer) As Long'.
Dim b = Loc
        ~~~
", output)

            output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /preferreduilang:en /vbruntime+ /t:library " & src.ToString(), expectedRetCode:=1)
            AssertOutput(
<text>
src.vb(5) : error BC30455: Argument not specified for parameter 'FileNumber' of 'Public Function Loc(FileNumber As Integer) As Long'.
Dim b = Loc
        ~~~
</text>, output)

            output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /preferreduilang:en /vbruntime* /t:library /r:System.dll " & src.ToString(), expectedRetCode:=1)
            AssertOutput(
<text>
src.vb(5) : error BC30451: 'Loc' is not declared. It may be inaccessible due to its protection level.
Dim b = Loc
        ~~~
</text>, output)

            output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /preferreduilang:en /vbruntime+ /vbruntime:abc /vbruntime* /t:library /r:System.dll " & src.ToString(), expectedRetCode:=1)
            AssertOutput(
<text>
src.vb(5) : error BC30451: 'Loc' is not declared. It may be inaccessible due to its protection level.
Dim b = Loc
        ~~~
</text>, output)

            output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /preferreduilang:en /vbruntime+ /vbruntime:abc /t:library " & src.ToString(), expectedRetCode:=1)
            AssertOutput(
<text>
vbc : error BC2017: could not find library 'abc'
</text>, output)

            Dim newVbCore = dir.CreateFile("Microsoft.VisualBasic.dll")
            newVbCore.WriteAllBytes(File.ReadAllBytes(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "Microsoft.VisualBasic.dll")))

            output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /preferreduilang:en /vbruntime:" & newVbCore.ToString() & " /t:library " & src.ToString(), expectedRetCode:=1)
            AssertOutput(
<text>
src.vb(5) : error BC30455: Argument not specified for parameter 'FileNumber' of 'Public Function Loc(FileNumber As Integer) As Long'.
Dim b = Loc
        ~~~
</text>, output)

            CleanupAllGeneratedFiles(src.Path)
        End Sub


        <WorkItem(997208, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/997208")>
        <Fact>
        Public Sub VbRuntime02()

            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("src.vb")
            src.WriteAllText(
<text>
Imports Microsoft.VisualBasic
Class C
Dim a = vbLf
Dim b = Loc
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /preferreduilang:en /nostdlib /r:mscorlib.dll /vbruntime- /t:library /d:_MyType=\""Empty\"" " & src.ToString(), expectedRetCode:=1)
            AssertOutput(
<text>
src.vb(2) : warning BC40056: Namespace or type specified in the Imports 'Microsoft.VisualBasic' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports Microsoft.VisualBasic
        ~~~~~~~~~~~~~~~~~~~~~
src.vb(4) : error BC30451: 'vbLf' is not declared. It may be inaccessible due to its protection level.
Dim a = vbLf
        ~~~~
src.vb(5) : error BC30451: 'Loc' is not declared. It may be inaccessible due to its protection level.
Dim b = Loc
        ~~~
</text>, output)

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact()>
        Public Sub VbRuntimeEmbeddedIsIncompatibleWithNetModule()
            Dim opt = TestOptions.ReleaseModule

            opt = opt.WithEmbedVbCoreRuntime(True)
            opt.Errors.Verify(Diagnostic(ERRID.ERR_VBCoreNetModuleConflict))

            CreateCompilationWithMscorlib40AndVBRuntime(<compilation><file/></compilation>, opt).GetDiagnostics().Verify(Diagnostic(ERRID.ERR_VBCoreNetModuleConflict))

            opt = opt.WithOutputKind(OutputKind.DynamicallyLinkedLibrary)
            opt.Errors.Verify()

            CreateCompilationWithMscorlib40AndVBRuntime(<compilation><file/></compilation>, opt).GetDiagnostics().Verify()
        End Sub

        <Fact()>
        Public Sub SdkPathInAction()

            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("src.vb")
            src.WriteAllText(
<text>
Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /preferreduilang:en /nostdlib /sdkpath:l:\x /t:library " & src.ToString(), expectedRetCode:=1, startFolder:=dir.Path)
            AssertOutput(
<text>
        vbc : error BC2017: could not find library 'Microsoft.VisualBasic.dll'
        </text>, output)

            output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /preferreduilang:en /nostdlib /r:mscorlib.dll /vbruntime- /sdkpath:c:folder /t:library " & src.ToString(), expectedRetCode:=1, startFolder:=dir.Path)
            AssertOutput(
<text> 
vbc : error BC2017: could not find library 'mscorlib.dll'
</text>, output)

            output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /preferreduilang:en /nostdlib /sdkpath:" & dir.Path & " /t:library " & src.ToString(), expectedRetCode:=1, startFolder:=dir.Path)
            AssertOutput(
<text>
vbc : error BC2017: could not find library 'Microsoft.VisualBasic.dll'
</text>, output.Replace(dir.Path, "{SDKPATH}"))

            ' Create 'System.Runtime.dll'
            Dim sysRuntime = dir.CreateFile("System.Runtime.dll")
            sysRuntime.WriteAllBytes(File.ReadAllBytes(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "System.Runtime.dll")))

            output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /preferreduilang:en /nostdlib /sdkpath:" & dir.Path & " /t:library " & src.ToString(), expectedRetCode:=1, startFolder:=dir.Path)
            AssertOutput(
<text>
vbc : error BC2017: could not find library 'Microsoft.VisualBasic.dll'
</text>, output.Replace(dir.Path, "{SDKPATH}"))

            ' trash in 'System.Runtime.dll'
            sysRuntime.WriteAllBytes({0, 1, 2, 3, 4, 5})

            output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /preferreduilang:en /nostdlib /sdkpath:" & dir.Path & " /t:library " & src.ToString(), expectedRetCode:=1, startFolder:=dir.Path)
            AssertOutput(
<text>
vbc : error BC2017: could not find library 'Microsoft.VisualBasic.dll'
</text>, output.Replace(dir.Path, "{SDKPATH}"))

            ' Create 'mscorlib.dll'
            Dim msCorLib = dir.CreateFile("mscorlib.dll")
            msCorLib.WriteAllBytes(File.ReadAllBytes(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "mscorlib.dll")))

            ' NOT: both libraries exist, but 'System.Runtime.dll' is invalid, so we need to pick up 'mscorlib.dll'
            output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /nostdlib /sdkpath:" & dir.Path & " /t:library /vbruntime* /r:" & Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "System.dll") & " " & src.ToString(), startFolder:=dir.Path)
            AssertOutput(<text></text>, output.Replace(dir.Path, "{SDKPATH}")) ' SUCCESSFUL BUILD with 'mscorlib.dll' and embedded VbCore

            File.Delete(sysRuntime.Path)

            ' NOTE: only 'mscorlib.dll' exists
            output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /nostdlib /sdkpath:" & dir.Path & " /t:library /vbruntime* /r:" & Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "System.dll") & " " & src.ToString(), startFolder:=dir.Path)
            AssertOutput(<text></text>, output.Replace(dir.Path, "{SDKPATH}"))

            File.Delete(msCorLib.Path)

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <WorkItem(598158, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598158")>
        <Fact()>
        Public Sub MultiplePathsInSdkPath()

            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("src.vb")
            src.WriteAllText(
<text>
Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output As String = ""

            Dim subFolder1 = dir.CreateDirectory("fldr1")
            Dim subFolder2 = dir.CreateDirectory("fldr2")

            Dim sdkMultiPath = subFolder1.Path & "," & subFolder2.Path
            Dim cmd As String = " /nologo /preferreduilang:en /sdkpath:" & sdkMultiPath &
                  " /t:library /r:" & Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "System.dll") &
                  " " & src.ToString()
            Dim cmdNoStdLibNoRuntime As String = "/nostdlib /vbruntime* /r:mscorlib.dll /preferreduilang:en" & cmd

            ' NOTE: no 'mscorlib.dll' exists
            output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, cmdNoStdLibNoRuntime, startFolder:=dir.Path, expectedRetCode:=1)
            AssertOutput(<text>vbc : error BC2017: could not find library 'mscorlib.dll'</text>, output.Replace(dir.Path, "{SDKPATH}"))

            ' Create '<dir>\fldr2\mscorlib.dll'
            Dim msCorLib = subFolder2.CreateFile("mscorlib.dll")
            msCorLib.WriteAllBytes(File.ReadAllBytes(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "mscorlib.dll")))

            ' NOTE: only 'mscorlib.dll' exists
            output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, cmdNoStdLibNoRuntime, startFolder:=dir.Path)
            AssertOutput(<text></text>, output.Replace(dir.Path, "{SDKPATH}"))

            output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, cmd, startFolder:=dir.Path, expectedRetCode:=1)
            AssertOutput(
<text>
vbc : warning BC40049: Could not find standard library 'System.dll'.
vbc : error BC2017: could not find library 'Microsoft.VisualBasic.dll'
</text>, output.Replace(dir.Path, "{SDKPATH}"))

            File.Delete(msCorLib.Path)
            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact()>
        Public Sub NostdlibInAction()

            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("src.vb")
            src.WriteAllText(
<text>
Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /preferreduilang:en /nostdlib /t:library " & src.ToString(), startFolder:=dir.Path, expectedRetCode:=1)
            Assert.Contains("error BC30002: Type 'Global.System.ComponentModel.EditorBrowsable' is not defined.", output, StringComparison.Ordinal)

            output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /nostdlib /define:_MYTYPE=\""Empty\"" /t:library " & src.ToString(), startFolder:=dir.Path)
            AssertOutput(<text></text>, output)

            output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /preferreduilang:en /nostdlib /sdkpath:x:\ /vbruntime- /define:_MYTYPE=\""Empty\"" /t:library " & src.ToString(), expectedRetCode:=1, startFolder:=dir.Path)
            AssertOutput(
<text>
src.vb(2) : error BC30002: Type 'System.Void' is not defined.
Class C
~~~~~~~
End Class
~~~~~~~~~
src.vb(2) : error BC31091: Import of type 'Object' from assembly or module 'src.dll' failed.
Class C
      ~
</text>, output)

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        Private Sub AssertOutput(expected As XElement, output As String, Optional fileName As String = "src.vb")
            AssertOutput(expected.Value.Replace(vbLf, vbCrLf).Trim, output, fileName)
        End Sub

        Private Sub AssertOutput(expected As String, output As String, Optional fileName As String = "src.vb")
            output = Regex.Replace(output, "^.*" & fileName, fileName, RegexOptions.Multiline)
            output = Regex.Replace(output, "\r\n\s*\r\n", vbCrLf) ' empty strings
            output = output.Trim()
            expected = expected.Trim '
            Assert.Equal(expected, output)
        End Sub

        <Fact()>
        Public Sub ResponsePathInSearchPath()
            Dim file = Temp.CreateDirectory().CreateFile("vb.rsp")
            file.WriteAllText("")

            Dim parsedArgs = DefaultParse({"/libpath:c:\lib2,", "@" & file.ToString(), "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertReferencePathsEqual(parsedArgs.ReferencePaths, Nothing, Path.GetDirectoryName(file.ToString()), "c:\lib2")

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        Private Sub AssertReferencePathsEqual(refPaths As ImmutableArray(Of String), sdkPathOrNothing As String, ParamArray paths() As String)
            Assert.Equal(1 + paths.Length, refPaths.Length)
            Assert.Equal(If(sdkPathOrNothing, RuntimeEnvironment.GetRuntimeDirectory()), refPaths(0))
            For i = 0 To paths.Count - 1
                Assert.Equal(paths(i), refPaths(i + 1))
            Next
        End Sub

        <Fact()>
        Public Sub HighEntropyVirtualAddressSpace()
            Dim parsedArgs = DefaultParse({"/highentropyva", "a.vb"}, _baseDirectory)
            Assert.True(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace)
            parsedArgs = DefaultParse({"/highentropyva+", "a.vb"}, _baseDirectory)
            Assert.True(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace)
            parsedArgs = DefaultParse({"/highentropyva-", "a.vb"}, _baseDirectory)
            Assert.False(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace)
            parsedArgs = DefaultParse({"/highentropyva:+", "a.vb"}, _baseDirectory)
            Assert.False(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/highentropyva:+"))
            parsedArgs = DefaultParse({"/highentropyva:", "a.vb"}, _baseDirectory)
            Assert.False(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/highentropyva:"))
            parsedArgs = DefaultParse({"/highentropyva+ /highentropyva-", "a.vb"}, _baseDirectory)
            Assert.False(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace)
        End Sub

        <Fact>
        Public Sub Win32ResQuotes()
            Dim responseFile As String() = {
            " /win32resource:d:\\""abc def""\a""b c""d\a.res"
        }

            Dim args = DefaultParse(VisualBasicCommandLineParser.ParseResponseLines(responseFile), "c:\")
            Assert.Equal("d:\abc def\ab cd\a.res", args.Win32ResourceFile)

            responseFile = {
            " /win32icon:d:\\""abc def""\a""b c""d\a.ico"
        }

            args = DefaultParse(VisualBasicCommandLineParser.ParseResponseLines(responseFile), "c:\")
            Assert.Equal("d:\abc def\ab cd\a.ico", args.Win32Icon)

            responseFile = {
            " /win32manifest:d:\\""abc def""\a""b c""d\a.manifest"
        }

            args = DefaultParse(VisualBasicCommandLineParser.ParseResponseLines(responseFile), "c:\")
            Assert.Equal("d:\abc def\ab cd\a.manifest", args.Win32Manifest)
        End Sub

        <Fact>
        Public Sub ResourceOnlyCompile()
            Dim parsedArgs = DefaultParse({"/resource:goo.vb,ed", "/out:e.dll"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            parsedArgs = DefaultParse({"/resource:goo.vb,ed"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_NoSourcesOut))
        End Sub

        <Fact>
        Public Sub OutputFileName1()
            Dim source1 = <![CDATA[
Class A
End Class
]]>

            Dim source2 = <![CDATA[
Class B
    Shared Sub Main()
    End Sub
End Class
]]>

            ' Name comes from name of first file.
            CheckOutputFileName(
            source1, source2,
            inputName1:="p.cs", inputName2:="q.cs",
            commandLineArguments:={"/target:library"},
            expectedOutputName:="p.dll")
        End Sub

        <Fact>
        Public Sub OutputFileName2()
            Dim source1 = <![CDATA[
Class A
End Class
]]>

            Dim source2 = <![CDATA[
Class B
    Shared Sub Main()
    End Sub
End Class
]]>

            ' Name comes from command-line option.
            CheckOutputFileName(
            source1, source2,
            inputName1:="p.cs", inputName2:="q.cs",
            commandLineArguments:={"/target:library", "/out:r.dll"},
            expectedOutputName:="r.dll")
        End Sub

        <Fact>
        Public Sub OutputFileName3()
            Dim source1 = <![CDATA[
Class A
End Class
]]>

            Dim source2 = <![CDATA[
Class B
    Shared Sub Main()
    End Sub
End Class
]]>

            ' Name comes from name of first file.
            CheckOutputFileName(
            source1, source2,
            inputName1:="p.cs", inputName2:="q.cs",
            commandLineArguments:={"/target:exe"},
            expectedOutputName:="p.exe")
        End Sub

        <Fact>
        Public Sub OutputFileName4()
            Dim source1 = <![CDATA[
Class A
End Class
]]>

            Dim source2 = <![CDATA[
Class B
    Shared Sub Main()
    End Sub
End Class
]]>

            ' Name comes from command-line option.
            CheckOutputFileName(
            source1, source2,
            inputName1:="p.cs", inputName2:="q.cs",
            commandLineArguments:={"/target:exe", "/out:r.exe"},
            expectedOutputName:="r.exe")
        End Sub

        <Fact>
        Public Sub OutputFileName5()
            Dim source1 = <![CDATA[
Class A
    Shared Sub Main()
    End Sub
End Class
]]>

            Dim source2 = <![CDATA[
Class B
    Shared Sub Main()
    End Sub
End Class
]]>

            ' Name comes from name of first file.
            CheckOutputFileName(
            source1, source2,
            inputName1:="p.cs", inputName2:="q.cs",
            commandLineArguments:={"/target:exe", "/main:A"},
            expectedOutputName:="p.exe")
        End Sub

        <Fact>
        Public Sub OutputFileName6()
            Dim source1 = <![CDATA[
Class A
    Shared Sub Main()
    End Sub
End Class
]]>

            Dim source2 = <![CDATA[
Class B
    Shared Sub Main()
    End Sub
End Class
]]>

            ' Name comes from name of first file.
            CheckOutputFileName(
            source1, source2,
            inputName1:="p.cs", inputName2:="q.cs",
            commandLineArguments:={"/target:exe", "/main:B"},
            expectedOutputName:="p.exe")
        End Sub

        <WorkItem(545773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545773")>
        <Fact>
        Public Sub OutputFileName7()
            Dim source1 = <![CDATA[
Class A
End Class
]]>

            Dim source2 = <![CDATA[
Class B
    Shared Sub Main()
    End Sub
End Class
]]>

            ' Name comes from command-line option.
            CheckOutputFileName(
            source1, source2,
            inputName1:="p.cs", inputName2:="q.cs",
            commandLineArguments:={"/target:library", "/out:goo"},
            expectedOutputName:="goo.dll")
        End Sub

        <WorkItem(545773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545773")>
        <Fact>
        Public Sub OutputFileName8()
            Dim source1 = <![CDATA[
Class A
End Class
]]>

            Dim source2 = <![CDATA[
Class B
    Shared Sub Main()
    End Sub
End Class
]]>

            ' Name comes from command-line option.
            CheckOutputFileName(
            source1, source2,
            inputName1:="p.cs", inputName2:="q.cs",
            commandLineArguments:={"/target:library", "/out:goo. "},
            expectedOutputName:="goo.dll")
        End Sub

        <WorkItem(545773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545773")>
        <Fact>
        Public Sub OutputFileName9()
            Dim source1 = <![CDATA[
Class A
End Class
]]>

            Dim source2 = <![CDATA[
Class B
    Shared Sub Main()
    End Sub
End Class
]]>

            ' Name comes from command-line option.
            CheckOutputFileName(
            source1, source2,
            inputName1:="p.cs", inputName2:="q.cs",
            commandLineArguments:={"/target:library", "/out:goo.a"},
            expectedOutputName:="goo.a.dll")
        End Sub

        <WorkItem(545773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545773")>
        <Fact>
        Public Sub OutputFileName10()
            Dim source1 = <![CDATA[
Class A
End Class
]]>

            Dim source2 = <![CDATA[
Class B
    Shared Sub Main()
    End Sub
End Class
]]>

            ' Name comes from command-line option.
            CheckOutputFileName(
            source1, source2,
            inputName1:="p.cs", inputName2:="q.cs",
            commandLineArguments:={"/target:module", "/out:goo.a"},
            expectedOutputName:="goo.a")
        End Sub

        <WorkItem(545773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545773")>
        <Fact>
        Public Sub OutputFileName11()
            Dim source1 = <![CDATA[
Class A
End Class
]]>

            Dim source2 = <![CDATA[
Class B
    Shared Sub Main()
    End Sub
End Class
]]>

            ' Name comes from command-line option.
            CheckOutputFileName(
            source1, source2,
            inputName1:="p.cs", inputName2:="q.cs",
            commandLineArguments:={"/target:module", "/out:goo.a . . . . "},
            expectedOutputName:="goo.a")
        End Sub

        <WorkItem(545773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545773")>
        <Fact>
        Public Sub OutputFileName12()
            Dim source1 = <![CDATA[
Class A
End Class
]]>

            Dim source2 = <![CDATA[
Class B
    Shared Sub Main()
    End Sub
End Class
]]>

            ' Name comes from command-line option.
            CheckOutputFileName(
            source1, source2,
            inputName1:="p.cs", inputName2:="q.cs",
            commandLineArguments:={"/target:module", "/out:goo. . . . . "},
            expectedOutputName:="goo.netmodule")
        End Sub

        <Fact>
        Public Sub OutputFileName13()
            Dim source1 = <![CDATA[
Class A
End Class
]]>

            Dim source2 = <![CDATA[
Class B
    Shared Sub Main()
    End Sub
End Class
]]>

            ' Name comes from name of first file.
            CheckOutputFileName(
            source1, source2,
            inputName1:="p.cs", inputName2:="q.cs",
            commandLineArguments:={"/target:winmdobj"},
            expectedOutputName:="p.winmdobj")
        End Sub

        <Fact>
        Public Sub OutputFileName14()
            Dim source1 = <![CDATA[
Class A
End Class
]]>

            Dim source2 = <![CDATA[
Class B
    Shared Sub Main()
    End Sub
End Class
]]>

            ' Name comes from name of first file.
            CheckOutputFileName(
            source1, source2,
            inputName1:="p.cs", inputName2:="q.cs",
            commandLineArguments:={"/target:appcontainerexe"},
            expectedOutputName:="p.exe")
        End Sub

        Private Sub CheckOutputFileName(source1 As XCData, source2 As XCData, inputName1 As String, inputName2 As String, commandLineArguments As String(), expectedOutputName As String)
            Dim dir = Temp.CreateDirectory()

            Dim file1 = dir.CreateFile(inputName1)
            file1.WriteAllText(source1.Value)

            Dim file2 = dir.CreateFile(inputName2)
            file2.WriteAllText(source2.Value)

            Using outWriter As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, commandLineArguments.Concat({inputName1, inputName2}).ToArray())
                Dim exitCode As Integer = vbc.Run(outWriter, Nothing)
                If exitCode <> 0 Then
                    Console.WriteLine(outWriter.ToString())
                    Assert.Equal(0, exitCode)
                End If
            End Using

            Assert.Equal(1, Directory.EnumerateFiles(dir.Path, "*" & PathUtilities.GetExtension(expectedOutputName)).Count())
            Assert.Equal(1, Directory.EnumerateFiles(dir.Path, expectedOutputName).Count())


            If System.IO.File.Exists(expectedOutputName) Then
                System.IO.File.Delete(expectedOutputName)
            End If

            CleanupAllGeneratedFiles(file1.Path)
            CleanupAllGeneratedFiles(file2.Path)
        End Sub

        Private Shared Sub AssertSpecificDiagnostics(expectedCodes As Integer(), expectedOptions As ReportDiagnostic(), args As VisualBasicCommandLineArguments)
            Dim actualOrdered = args.CompilationOptions.SpecificDiagnosticOptions.OrderBy(Function(entry) entry.Key)

            AssertEx.Equal(
            expectedCodes.Select(Function(i) MessageProvider.Instance.GetIdForErrorCode(i)),
            actualOrdered.Select(Function(entry) entry.Key))

            AssertEx.Equal(expectedOptions, actualOrdered.Select(Function(entry) entry.Value))
        End Sub

        <Fact>
        Public Sub WarningsOptions()
            ' Baseline
            Dim parsedArgs = DefaultParse({"a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)

            ' Test for /warnaserrors
            parsedArgs = DefaultParse({"/warnaserror", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Error, parsedArgs.CompilationOptions.GeneralDiagnosticOption)

            ' Test for /warnaserrors+
            parsedArgs = DefaultParse({"/warnaserror+", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Error, parsedArgs.CompilationOptions.GeneralDiagnosticOption)

            ' Test for /warnaserrors:
            parsedArgs = DefaultParse({"/warnaserror:", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)

            ' Test for /warnaserrors:42024,42025
            parsedArgs = DefaultParse({"/warnaserror:42024,42025", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)
            AssertSpecificDiagnostics({42024, 42025}, {ReportDiagnostic.Error, ReportDiagnostic.Error}, parsedArgs)

            ' Test for /warnaserrors+:
            parsedArgs = DefaultParse({"/warnaserror+:", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)

            ' Test for /warnaserrors+:42024,42025
            parsedArgs = DefaultParse({"/warnaserror+:42024,42025", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)
            AssertSpecificDiagnostics({42024, 42025}, {ReportDiagnostic.Error, ReportDiagnostic.Error}, parsedArgs)

            ' Test for /warnaserrors-
            parsedArgs = DefaultParse({"/warnaserror-", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)

            ' Test for /warnaserrors-:
            parsedArgs = DefaultParse({"/warnaserror-:", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)

            ' Test for /warnaserrors-:42024,42025
            parsedArgs = DefaultParse({"/warnaserror-:42024,42025", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)
            AssertSpecificDiagnostics({42024, 42025}, {ReportDiagnostic.Default, ReportDiagnostic.Default}, parsedArgs)

            ' Test for /nowarn
            parsedArgs = DefaultParse({"/nowarn", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Suppress, parsedArgs.CompilationOptions.GeneralDiagnosticOption)

            ' Test for /nowarn:
            parsedArgs = DefaultParse({"/nowarn:", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)

            ' Test for /nowarn:42024,42025
            parsedArgs = DefaultParse({"/nowarn:42024,42025", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)
            AssertSpecificDiagnostics({42024, 42025}, {ReportDiagnostic.Suppress, ReportDiagnostic.Suppress}, parsedArgs)
        End Sub

        <Fact()>
        Public Sub WarningsErrors()
            ' Previous versions of the compiler used to report warnings (BC2026, BC2014)
            ' whenever an unrecognized warning code was supplied via /nowarn or /warnaserror.
            ' We no longer generate a warning in such cases.

            ' Test for /warnaserrors:1
            Dim parsedArgs = DefaultParse({"/warnaserror:1", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            ' Test for /warnaserrors:abc
            parsedArgs = DefaultParse({"/warnaserror:abc", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            ' Test for /nowarn:1
            parsedArgs = DefaultParse({"/nowarn:1", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            ' Test for /nowarn:abc
            parsedArgs = DefaultParse({"/nowarn:abc", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
        End Sub

        <WorkItem(545025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545025")>
        <Fact()>
        Public Sub CompilationWithWarnAsError()
            Dim source = <![CDATA[
Class A
    Shared Sub Main()
    End Sub
End Class
]]>
            ' Baseline without warning options (expect success)
            Dim exitCode As Integer = GetExitCode(source.Value, "a.vb", {})
            Assert.Equal(0, exitCode)

            ' The case with /warnaserror (expect to be success, since there will be no warning)
            exitCode = GetExitCode(source.Value, "b.vb", {"/warnaserror"})
            Assert.Equal(0, exitCode)

            ' The case with /warnaserror and /nowarn:1 (expect success)
            ' Note that even though the command line option has a warning, it is not going to become an error
            ' in order to avoid the halt of compilation. 
            exitCode = GetExitCode(source.Value, "c.vb", {"/warnaserror", "/nowarn:1"})
            Assert.Equal(0, exitCode)
        End Sub

        Public Function GetExitCode(source As String, fileName As String, commandLineArguments As String()) As Integer
            Dim dir = Temp.CreateDirectory()
            Dim file1 = dir.CreateFile(fileName)
            file1.WriteAllText(source)

            Using outWriter As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, commandLineArguments.Concat({fileName}).ToArray())
                Return vbc.Run(outWriter, Nothing)
            End Using
        End Function

        <WorkItem(545214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545214")>
        <Fact()>
        Public Sub ErrorMessageWithSquiggles_01()
            Dim source =
            <compilation>
                <file name="a.vb">Imports System

Module Program
    Sub Main(args As String())
        Dim x As Integer
        Dim yy As Integer
        Const zzz As Long = 0
    End Sub

    Function goo()
    End Function
End Module
                    </file>
            </compilation>

            Dim result =
                <file name="output">Microsoft (R) Visual Basic Compiler version VERSION (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.

PATH(5) : warning BC42024: Unused local variable: 'x'.

        Dim x As Integer
            ~           
PATH(6) : warning BC42024: Unused local variable: 'yy'.

        Dim yy As Integer
            ~~           
PATH(7) : warning BC42099: Unused local constant: 'zzz'.

        Const zzz As Long = 0
              ~~~            
PATH(11) : warning BC42105: Function 'goo' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.

    End Function
    ~~~~~~~~~~~~
</file>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
                vbc.Run(output, Nothing)

                Dim expected = ReplacePathAndVersionAndHash(result, file).Trim()
                Dim actual = output.ToString().Trim()
                Assert.Equal(expected, actual)
            End Using

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545214")>
        <Fact()>
        Public Sub ErrorMessageWithSquiggles_02()
            ' It verifies the case where diagnostic does not have the associated location in it.
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System.Runtime.CompilerServices

Module Module1
    Delegate Sub delegateType()

    Sub main()
        Dim a As ArgIterator = Nothing
        Dim d As delegateType = AddressOf a.Goo
    End Sub

    <Extension()> _
    Public Function Goo(ByVal x As ArgIterator) as Integer
	Return 1
    End Function
End Module
]]>
                </file>
            </compilation>

            Dim result =
                <file name="output">Microsoft (R) Visual Basic Compiler version VERSION (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.

PATH(9) : error BC36640: Instance of restricted type 'ArgIterator' cannot be used in a lambda expression.

        Dim d As delegateType = AddressOf a.Goo
                                          ~    
</file>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en", "-imports:System"})
                vbc.Run(output, Nothing)

                Assert.Equal(ReplacePathAndVersionAndHash(result, file), output.ToString())
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545214")>
        <Fact()>
        Public Sub ErrorMessageWithSquiggles_03()
            ' It verifies the case where the squiggles covers the error span with tabs in it.
            Dim source = "Module Module1" + vbCrLf +
                     "  Sub Main()" + vbCrLf +
                     "      Dim x As Integer = ""a" + vbTab + vbTab + vbTab + "b""c ' There is a tab in the string." + vbCrLf +
                     "  End Sub" + vbCrLf +
                     "End Module" + vbCrLf

            Dim result = <file name="output">Microsoft (R) Visual Basic Compiler version VERSION (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.

PATH(3) : error BC30201: Expression expected.

      Dim x As Integer = "a            b"c ' There is a tab in the string.
                         ~                                                 
PATH(3) : error BC30004: Character constant must contain exactly one character.

      Dim x As Integer = "a            b"c ' There is a tab in the string.
                         ~~~~~~~~~~~~~~~~~                       
</file>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
                vbc.Run(output, Nothing)

                Dim expected = ReplacePathAndVersionAndHash(result, file).Trim()
                Dim actual = output.ToString().Trim()
                Assert.Equal(expected, actual)
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545214")>
        <Fact()>
        Public Sub ErrorMessageWithSquiggles_04()
            ' It verifies the case where the squiggles covers multiple lines.
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System.Collections.Generic
Module Module1
    Sub Main()
        Dim i3 = From el In {
                3, 33, 333
                } Select el
    End Sub
End Module
]]>
                </file>
            </compilation>

            Dim result =
                <file name="output">Microsoft (R) Visual Basic Compiler version VERSION (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.

PATH(5) : error BC36593: Expression of type 'Integer()' is not queryable. Make sure you are not missing an assembly reference and/or namespace import for the LINQ provider.

        Dim i3 = From el In {
                            ~
                3, 33, 333
~~~~~~~~~~~~~~~~~~~~~~~~~~
                } Select el
~~~~~~~~~~~~~~~~~          
</file>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
                vbc.Run(output, Nothing)

                Assert.Equal(ReplacePathAndVersionAndHash(result, file), output.ToString())
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545214")>
        <Fact()>
        Public Sub ErrorMessageWithSquiggles_05()
            ' It verifies the case where the squiggles covers multiple lines.
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System.Collections.Generic
Module _
    Module1
    Sub Main()
    End Sub
'End Module
]]>
                </file>
            </compilation>

            Dim result =
                <file name="output">Microsoft (R) Visual Basic Compiler version VERSION (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.

PATH(3) : error BC30625: 'Module' statement must end with a matching 'End Module'.

Module _
~~~~~~~~
    Module1
~~~~~~~~~~~
</file>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
                vbc.Run(output, Nothing)

                Assert.Equal(ReplacePathAndVersionAndHash(result, file), output.ToString())
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545214")>
        <Fact()>
        Public Sub ErrorMessageWithSquiggles_06()
            ' It verifies the case where the squiggles covers the very long error span.
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System
Imports System.Collections.Generic

Module Program

    Event eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee()

    Event eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee()

    Sub Main(args As String())
    End Sub
End Module
]]>
                </file>
            </compilation>

            Dim result =
                <file name="output">Microsoft (R) Visual Basic Compiler version VERSION (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.

PATH(7) : error BC37220: Name 'eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeEventHandler' exceeds the maximum length allowed in metadata.

    Event eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee()
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</file>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
                vbc.Run(output, Nothing)
                Assert.Equal(ReplacePathAndVersionAndHash(result, file), output.ToString())
            End Using

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545214")>
        <Fact()>
        Public Sub ErrorMessageWithSquiggles_07()
            ' It verifies the case where the error is on the last line.
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System

Module Module1
    Sub Main()
        Console.WriteLine("Hello from VB")
    End Sub
End Class]]>
                </file>
            </compilation>

            Dim result =
                <file name="output">Microsoft (R) Visual Basic Compiler version VERSION (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.

PATH(4) : error BC30625: 'Module' statement must end with a matching 'End Module'.

Module Module1
~~~~~~~~~~~~~~
PATH(8) : error BC30460: 'End Class' must be preceded by a matching 'Class'.

End Class
~~~~~~~~~
</file>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
                vbc.Run(output, Nothing)

                Assert.Equal(ReplacePathAndVersionAndHash(result, file), output.ToString())
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(531606, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531606")>
        <Fact()>
        Public Sub ErrorMessageWithSquiggles_08()
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System

Module Module1
    Sub Main()
        Dim i As system.Boolean,
    End Sub
End Module
]]>
                </file>
            </compilation>

            Dim result =
<file name="output">Microsoft (R) Visual Basic Compiler version VERSION (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.

PATH(6) : error BC30203: Identifier expected.

        Dim i As system.Boolean,
                                ~
</file>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
                vbc.Run(output, Nothing)

                Assert.Equal(ReplacePathAndVersionAndHash(result, file), output.ToString())
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        Private Shared Function ReplacePathAndVersionAndHash(result As XElement, file As TempFile) As String
            Return result.Value.Replace("PATH", file.Path).Replace("VERSION", s_compilerVersion).Replace("HASH", s_compilerShortCommitHash).Replace(vbLf, vbCrLf)
        End Function

        <WorkItem(545247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545247")>
        <Fact()>
        Public Sub CompilationWithNonExistingOutPath()
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System

Module Module1
    Sub Main()
    End Sub
End Module
]]>
                </file>
            </compilation>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/target:exe", "/preferreduilang:en", "/out:sub\a.exe"})
                Dim exitCode = vbc.Run(output, Nothing)

                Assert.Equal(1, exitCode)
                Assert.Contains("error BC2012: can't open '" + dir.Path + "\sub\a.exe' for writing", output.ToString(), StringComparison.Ordinal)
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545247")>
        <Fact()>
        Public Sub CompilationWithWrongOutPath_01()
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System

Module Module1
    Sub Main()
    End Sub
End Module
]]>
                </file>
            </compilation>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en", "/target:exe", "/out:sub\"})
                Dim exitCode = vbc.Run(output, Nothing)

                Assert.Equal(1, exitCode)
                Dim message = output.ToString()
                Assert.Contains("error BC2032: File name", message, StringComparison.Ordinal)
                Assert.Contains("sub", message, StringComparison.Ordinal)
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545247")>
        <Fact()>
        Public Sub CompilationWithWrongOutPath_02()
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System

Module Module1
    Sub Main()
    End Sub
End Module
]]>
                </file>
            </compilation>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en", "/target:exe", "/out:sub\ "})
                Dim exitCode = vbc.Run(output, Nothing)

                Assert.Equal(1, exitCode)
                Dim message = output.ToString()
                Assert.Contains("error BC2032: File name", message, StringComparison.Ordinal)
                Assert.Contains("sub", message, StringComparison.Ordinal)
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545247")>
        <Fact()>
        Public Sub CompilationWithWrongOutPath_03()
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System

Module Module1
    Sub Main()
    End Sub
End Module
]]>
                </file>
            </compilation>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en", "/target:exe", "/out:aaa:\a.exe"})
                Dim exitCode = vbc.Run(output, Nothing)

                Assert.Equal(1, exitCode)
                Assert.Contains("error BC2032: File name 'aaa:\a.exe' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long", output.ToString(), StringComparison.Ordinal)
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545247")>
        <Fact()>
        Public Sub CompilationWithWrongOutPath_04()
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System

Module Module1
    Sub Main()
    End Sub
End Module
]]>
                </file>
            </compilation>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en", "/target:exe", "/out: "})
                Dim exitCode = vbc.Run(output, Nothing)

                Assert.Equal(1, exitCode)
                Assert.Contains("error BC2006: option 'out' requires ':<file>'", output.ToString(), StringComparison.Ordinal)
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <Fact()>
        Public Sub SpecifyProperCodePage()
            ' Class <UTF8 Cyrillic Character>
            ' End Class
            Dim source() As Byte = {
                                &H43, &H6C, &H61, &H73, &H73, &H20, &HD0, &H96, &HD, &HA,
                                &H45, &H6E, &H64, &H20, &H43, &H6C, &H61, &H73, &H73
                               }

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllBytes(source)

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /t:library " & file.ToString(), startFolder:=dir.Path)
            Assert.Equal("", output) ' Autodetected UTF8, NO ERROR

            output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /preferreduilang:en /t:library /codepage:20127 " & file.ToString(), expectedRetCode:=1, startFolder:=dir.Path) ' 20127: US-ASCII
            ' 0xd0, 0x96 ==> 'Ð–' ==> ERROR
            Dim expected = <result>
a.vb(1) : error BC30203: Identifier expected.

Class ??
      ~                           
    </result>.Value.Replace(vbLf, vbCrLf).Trim()
            Dim actual = Regex.Replace(output, "^.*a.vb", "a.vb", RegexOptions.Multiline).Trim()

            Assert.Equal(expected, actual)
        End Sub

        <Fact()>
        Public Sub EmittedSubsystemVersion()
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(
<text>
    Class C
    End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim comp = VisualBasicCompilation.Create("a.dll", options:=TestOptions.ReleaseDll)
            Dim peHeaders = New PEHeaders(comp.EmitToStream(New EmitOptions(subsystemVersion:=SubsystemVersion.Create(5, 1))))
            Assert.Equal(5, peHeaders.PEHeader.MajorSubsystemVersion)
            Assert.Equal(1, peHeaders.PEHeader.MinorSubsystemVersion)

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <Fact>
        Public Sub DefaultManifestForExe()
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System

Module Module1
    Sub Main()
    End Sub
End Module
]]>
                </file>
            </compilation>

            Dim expectedManifest =
<?xml version="1.0" encoding="utf-16"?>
<ManifestResource Size="490">
    <Contents><![CDATA[<?xml version="1.0" encoding="UTF-8" standalone="yes"?>

<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
  <assemblyIdentity version="1.0.0.0" name="MyApplication.app"/>
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security>
      <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
        <requestedExecutionLevel level="asInvoker" uiAccess="false"/>
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>]]></Contents>
</ManifestResource>

            CheckManifestXml(source, OutputKind.ConsoleApplication, explicitManifest:=Nothing, expectedManifest:=expectedManifest)
        End Sub

        <Fact>
        Public Sub DefaultManifestForDll()
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System

Module Module1
    Sub Main()
    End Sub
End Module
]]>
                </file>
            </compilation>

            CheckManifestXml(source, OutputKind.DynamicallyLinkedLibrary, explicitManifest:=Nothing, expectedManifest:=Nothing)
        End Sub

        <Fact>
        Public Sub DefaultManifestForModule()
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System

Module Module1
    Sub Main()
    End Sub
End Module
]]>
                </file>
            </compilation>

            CheckManifestXml(source, OutputKind.NetModule, explicitManifest:=Nothing, expectedManifest:=Nothing)
        End Sub

        <Fact>
        Public Sub DefaultManifestForWinExe()
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System

Module Module1
    Sub Main()
    End Sub
End Module
]]>
                </file>
            </compilation>

            Dim expectedManifest =
<?xml version="1.0" encoding="utf-16"?>
<ManifestResource Size="490">
    <Contents><![CDATA[<?xml version="1.0" encoding="UTF-8" standalone="yes"?>

<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
  <assemblyIdentity version="1.0.0.0" name="MyApplication.app"/>
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security>
      <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
        <requestedExecutionLevel level="asInvoker" uiAccess="false"/>
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>]]></Contents>
</ManifestResource>

            CheckManifestXml(source, OutputKind.WindowsApplication, explicitManifest:=Nothing, expectedManifest:=expectedManifest)
        End Sub

        <Fact>
        Public Sub DefaultManifestForAppContainerExe()
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System

Module Module1
    Sub Main()
    End Sub
End Module
]]>
                </file>
            </compilation>

            Dim expectedManifest =
<?xml version="1.0" encoding="utf-16"?>
<ManifestResource Size="490">
    <Contents><![CDATA[<?xml version="1.0" encoding="UTF-8" standalone="yes"?>

<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
  <assemblyIdentity version="1.0.0.0" name="MyApplication.app"/>
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security>
      <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
        <requestedExecutionLevel level="asInvoker" uiAccess="false"/>
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>]]></Contents>
</ManifestResource>

            CheckManifestXml(source, OutputKind.WindowsRuntimeApplication, explicitManifest:=Nothing, expectedManifest:=expectedManifest)
        End Sub

        <Fact>
        Public Sub DefaultManifestForWinMDObj()
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System

Module Module1
    Sub Main()
    End Sub
End Module
]]>
                </file>
            </compilation>

            CheckManifestXml(source, OutputKind.WindowsRuntimeMetadata, explicitManifest:=Nothing, expectedManifest:=Nothing)
        End Sub

        <Fact>
        Public Sub ExplicitManifestForExe()
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System

Module Module1
    Sub Main()
    End Sub
End Module
]]>
                </file>
            </compilation>

            Dim explicitManifest =
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
    <assemblyIdentity version="1.0.0.0" name="Test.app"/>
    <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
        <security>
            <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
                <requestedExecutionLevel level="asInvoker" uiAccess="false"/>
            </requestedPrivileges>
        </security>
    </trustInfo>
</assembly>

            Dim expectedManifest =
<?xml version="1.0" encoding="utf-16"?>
<ManifestResource Size="421">
    <Contents><![CDATA[<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
  <assemblyIdentity version="1.0.0.0" name="Test.app" />
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security>
      <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
        <requestedExecutionLevel level="asInvoker" uiAccess="false" />
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>]]></Contents>
</ManifestResource>

            CheckManifestXml(source, OutputKind.ConsoleApplication, explicitManifest, expectedManifest)
        End Sub

        <Fact>
        Public Sub ExplicitManifestResForDll()
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System

Module Module1
    Sub Main()
    End Sub
End Module
]]>
                </file>
            </compilation>

            Dim explicitManifest =
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
    <assemblyIdentity version="1.0.0.0" name="Test.app"/>
    <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
        <security>
            <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
                <requestedExecutionLevel level="asInvoker" uiAccess="false"/>
            </requestedPrivileges>
        </security>
    </trustInfo>
</assembly>

            Dim expectedManifest =
<?xml version="1.0" encoding="utf-16"?>
<ManifestResource Size="421">
    <Contents><![CDATA[<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
  <assemblyIdentity version="1.0.0.0" name="Test.app" />
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security>
      <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
        <requestedExecutionLevel level="asInvoker" uiAccess="false" />
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>]]></Contents>
</ManifestResource>

            CheckManifestXml(source, OutputKind.DynamicallyLinkedLibrary, explicitManifest, expectedManifest)
        End Sub

        <Fact>
        Public Sub ExplicitManifestForModule()
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System

Module Module1
    Sub Main()
    End Sub
End Module
]]>
                </file>
            </compilation>

            Dim explicitManifest =
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
    <assemblyIdentity version="1.0.0.0" name="Test.app"/>
    <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
        <security>
            <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
                <requestedExecutionLevel level="asInvoker" uiAccess="false"/>
            </requestedPrivileges>
        </security>
    </trustInfo>
</assembly>

            CheckManifestXml(source, OutputKind.NetModule, explicitManifest, expectedManifest:=Nothing)
        End Sub

        <DllImport("kernel32.dll", SetLastError:=True)> Public Shared Function _
        LoadLibraryEx(lpFileName As String, hFile As IntPtr, dwFlags As UInteger) As IntPtr
        End Function
        <DllImport("kernel32.dll", SetLastError:=True)> Public Shared Function _
        FreeLibrary(hFile As IntPtr) As Boolean
        End Function

        Private Sub CheckManifestXml(source As XElement, outputKind As OutputKind, explicitManifest As XDocument, expectedManifest As XDocument)
            Dim dir = Temp.CreateDirectory()
            Dim sourceFile = dir.CreateFile("Test.cs").WriteAllText(source.Value)

            Dim outputFileName As String
            Dim target As String
            Select Case outputKind
                Case OutputKind.ConsoleApplication
                    outputFileName = "Test.exe"
                    target = "exe"
                Case OutputKind.WindowsApplication
                    outputFileName = "Test.exe"
                    target = "winexe"
                Case OutputKind.DynamicallyLinkedLibrary
                    outputFileName = "Test.dll"
                    target = "library"
                Case OutputKind.NetModule
                    outputFileName = "Test.netmodule"
                    target = "module"
                Case OutputKind.WindowsRuntimeMetadata
                    outputFileName = "Test.winmdobj"
                    target = "winmdobj"
                Case OutputKind.WindowsRuntimeApplication
                    outputFileName = "Test.exe"
                    target = "appcontainerexe"
                Case Else
                    Throw TestExceptionUtilities.UnexpectedValue(outputKind)
            End Select

            Dim vbc As VisualBasicCompiler
            Dim manifestFile As TempFile
            If explicitManifest Is Nothing Then
                vbc = New MockVisualBasicCompiler(Nothing, dir.Path,
            {
                String.Format("/target:{0}", target),
                String.Format("/out:{0}", outputFileName),
                Path.GetFileName(sourceFile.Path)
            })
            Else
                manifestFile = dir.CreateFile("Test.config").WriteAllText(explicitManifest.ToString())
                vbc = New MockVisualBasicCompiler(Nothing, dir.Path,
            {
                String.Format("/target:{0}", target),
                String.Format("/out:{0}", outputFileName),
                String.Format("/win32manifest:{0}", Path.GetFileName(manifestFile.Path)),
                Path.GetFileName(sourceFile.Path)
            })
            End If
            Assert.Equal(0, vbc.Run(New StringWriter(), Nothing))

            Dim library As IntPtr = LoadLibraryEx(Path.Combine(dir.Path, outputFileName), IntPtr.Zero, 2)
            If library = IntPtr.Zero Then
                Throw New Win32Exception(Marshal.GetLastWin32Error())
            End If

            Const resourceType As String = "#24"
            Dim resourceId As String = If(outputKind = OutputKind.DynamicallyLinkedLibrary, "#2", "#1")

            Dim manifestSize As UInteger = Nothing
            If expectedManifest Is Nothing Then
                Assert.Throws(Of Win32Exception)(Function() Win32Res.GetResource(library, resourceId, resourceType, manifestSize))
            Else
                Dim manifestResourcePointer As IntPtr = Win32Res.GetResource(library, resourceId, resourceType, manifestSize)
                Dim actualManifest As String = Win32Res.ManifestResourceToXml(manifestResourcePointer, manifestSize)
                Assert.Equal(expectedManifest.ToString(), XDocument.Parse(actualManifest).ToString())
            End If

            FreeLibrary(library)

            CleanupAllGeneratedFiles(sourceFile.Path)
        End Sub

        <WorkItem(530221, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530221")>
        <WorkItem(5664, "https://github.com/dotnet/roslyn/issues/5664")>
        <ConditionalFact(GetType(IsEnglishLocal))>
        Public Sub Bug15538()
            Dim folder = Temp.CreateDirectory()
            Dim source As String = folder.CreateFile("src.vb").WriteAllText("").Path
            Dim ref As String = folder.CreateFile("ref.dll").WriteAllText("").Path

            Try
                Dim output = ProcessUtilities.RunAndGetOutput("cmd", "/C icacls " & ref & " /inheritance:r /Q")
                Assert.Equal("Successfully processed 1 files; Failed processing 0 files", output.Trim())

                output = ProcessUtilities.RunAndGetOutput("cmd", "/C icacls " & ref & " /deny %USERDOMAIN%\%USERNAME%:(r,WDAC) /Q")
                Assert.Equal("Successfully processed 1 files; Failed processing 0 files", output.Trim())

                output = ProcessUtilities.RunAndGetOutput("cmd", "/C """ & s_basicCompilerExecutable & """ /nologo /preferreduilang:en /r:" & ref & " /t:library " & source, expectedRetCode:=1)
                Assert.True(output.StartsWith("vbc : error BC31011: Unable to load referenced library '" & ref & "': Access to the path '" & ref & "' is denied.", StringComparison.Ordinal))

            Finally
                Dim output = ProcessUtilities.RunAndGetOutput("cmd", "/C icacls " & ref & " /reset /Q")
                Assert.Equal("Successfully processed 1 files; Failed processing 0 files", output.Trim())
                File.Delete(ref)
            End Try

            CleanupAllGeneratedFiles(source)
        End Sub

        <WorkItem(544926, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544926")>
        <Fact()>
        Public Sub ResponseFilesWithNoconfig_01()
            Dim source As String = Temp.CreateFile().WriteAllText(<text>
Imports System

Module Module1
    Sub Main()
        Dim x As Integer    
    End Sub
End Module
</text>.Value).Path

            Dim rsp As String = Temp.CreateFile().WriteAllText(<text>
/warnaserror                                                               
</text>.Value).Path

            ' Checks the base case without /noconfig (expect to see error)
            Dim vbc = New MockVisualBasicCompiler(rsp, _baseDirectory, {source, "/preferreduilang:en"})
            Dim output As New StringWriter()
            Dim exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Contains("error BC42024: Unused local variable: 'x'.", output.ToString(), StringComparison.Ordinal)

            ' Checks the base case with /noconfig (expect to see warning, instead of error)
            vbc = New MockVisualBasicCompiler(rsp, _baseDirectory, {source, "/preferreduilang:en", "/noconfig"})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Contains("warning BC42024: Unused local variable: 'x'.", output.ToString(), StringComparison.Ordinal)

            ' Checks the base case with /NOCONFIG (expect to see warning, instead of error)
            vbc = New MockVisualBasicCompiler(rsp, _baseDirectory, {source, "/preferreduilang:en", "/NOCONFIG"})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Contains("warning BC42024: Unused local variable: 'x'.", output.ToString(), StringComparison.Ordinal)

            ' Checks the base case with -noconfig (expect to see warning, instead of error)
            vbc = New MockVisualBasicCompiler(rsp, _baseDirectory, {source, "/preferreduilang:en", "-noconfig"})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Contains("warning BC42024: Unused local variable: 'x'.", output.ToString(), StringComparison.Ordinal)

            CleanupAllGeneratedFiles(source)
            CleanupAllGeneratedFiles(rsp)
        End Sub

        <WorkItem(544926, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544926")>
        <Fact()>
        Public Sub ResponseFilesWithNoconfig_02()
            Dim source As String = Temp.CreateFile().WriteAllText(<text>
Imports System

Module Module1
    Sub Main()
    End Sub
End Module
</text>.Value).Path

            Dim rsp As String = Temp.CreateFile().WriteAllText(<text>
/noconfig                                        
</text>.Value).Path

            ' Checks the case with /noconfig inside the response file (expect to see warning)
            Dim vbc = New MockVisualBasicCompiler(rsp, _baseDirectory, {source, "/preferreduilang:en"})
            Dim exitCode As Integer
            Using output As New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Contains("warning BC2025: ignoring /noconfig option because it was specified in a response file", output.ToString(), StringComparison.Ordinal)
            End Using
            ' Checks the case with /noconfig inside the response file as along with /nowarn (expect to see warning)
            vbc = New MockVisualBasicCompiler(rsp, _baseDirectory, {source, "/preferreduilang:en", "/nowarn"})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Contains("warning BC2025: ignoring /noconfig option because it was specified in a response file", output.ToString(), StringComparison.Ordinal)
            End Using
            CleanupAllGeneratedFiles(source)
            CleanupAllGeneratedFiles(rsp)
        End Sub

        <WorkItem(544926, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544926")>
        <Fact()>
        Public Sub ResponseFilesWithNoconfig_03()
            Dim source As String = Temp.CreateFile().WriteAllText(<text>
Imports System

Module Module1
    Sub Main()
    End Sub
End Module
</text>.Value).Path

            Dim rsp As String = Temp.CreateFile().WriteAllText(<text>
/NOCONFIG       
</text>.Value).Path

            ' Checks the case with /noconfig inside the response file (expect to see warning)
            Dim vbc = New MockVisualBasicCompiler(rsp, _baseDirectory, {source, "/preferreduilang:en"})
            Dim exitCode As Integer
            Using output As New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Contains("warning BC2025: ignoring /noconfig option because it was specified in a response file", output.ToString(), StringComparison.Ordinal)
            End Using
            ' Checks the case with /NOCONFIG inside the response file as along with /nowarn (expect to see warning)
            vbc = New MockVisualBasicCompiler(rsp, _baseDirectory, {source, "/preferreduilang:en", "/nowarn"})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Contains("warning BC2025: ignoring /noconfig option because it was specified in a response file", output.ToString(), StringComparison.Ordinal)
            End Using
            CleanupAllGeneratedFiles(source)
            CleanupAllGeneratedFiles(rsp)
        End Sub

        <WorkItem(544926, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544926")>
        <Fact()>
        Public Sub ResponseFilesWithNoconfig_04()
            Dim source As String = Temp.CreateFile().WriteAllText(<text>
Imports System

Module Module1
    Sub Main()
    End Sub
End Module
</text>.Value).Path

            Dim rsp As String = Temp.CreateFile().WriteAllText(<text>
-noconfig
</text>.Value).Path

            ' Checks the case with /noconfig inside the response file (expect to see warning)
            Dim vbc = New MockVisualBasicCompiler(rsp, _baseDirectory, {source, "/preferreduilang:en"})
            Dim exitCode As Integer
            Using output As New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Contains("warning BC2025: ignoring /noconfig option because it was specified in a response file", output.ToString(), StringComparison.Ordinal)
            End Using
            ' Checks the case with -noconfig inside the response file as along with /nowarn (expect to see warning)
            vbc = New MockVisualBasicCompiler(rsp, _baseDirectory, {source, "/preferreduilang:en", "/nowarn"})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Contains("warning BC2025: ignoring /noconfig option because it was specified in a response file", output.ToString(), StringComparison.Ordinal)
            End Using
            CleanupAllGeneratedFiles(source)
            CleanupAllGeneratedFiles(rsp)
        End Sub

        <WorkItem(545832, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545832")>
        <Fact()>
        Public Sub ResponseFilesWithEmptyAliasReference()
            Dim source As String = Temp.CreateFile().WriteAllText(<text>
Imports System
</text>.Value).Path

            Dim rsp As String = Temp.CreateFile().WriteAllText(<text>
-nologo
/r:a=""""
</text>.Value).Path

            Dim vbc = New MockVisualBasicCompiler(rsp, _baseDirectory, {source, "/preferreduilang:en"})
            Using output As New StringWriter()
                Dim exitCode = vbc.Run(output, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC2017: could not find library 'a='", output.ToString().Trim())
            End Using
            CleanupAllGeneratedFiles(source)
            CleanupAllGeneratedFiles(rsp)
        End Sub

        <WorkItem(546031, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546031")>
        <WorkItem(546032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546032")>
        <WorkItem(546033, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546033")>
        <Fact()>
        Public Sub InvalidDefineSwitch()
            Dim source As String = Temp.CreateFile().WriteAllText(<text>
Imports System
</text>.Value).Path

            Dim vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define", source})
            Dim exitCode As Integer
            Using output As New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC2006: option 'define' requires ':<symbol_list>'", output.ToString().Trim())
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC2006: option 'define' requires ':<symbol_list>'", output.ToString().Trim())
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define: ", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC2006: option 'define' requires ':<symbol_list>'", output.ToString().Trim())
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:_,", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC31030: Conditional compilation constant '_ ^^ ^^ ' is not valid: Identifier expected.", output.ToString().Trim())
            End Using

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:_a,", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Equal("", output.ToString().Trim())
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:_ a,", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC31030: Conditional compilation constant '_  ^^ ^^ a' is not valid: Identifier expected.", output.ToString().Trim())
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:a,_,b", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC31030: Conditional compilation constant '_ ^^ ^^ ' is not valid: Identifier expected.", output.ToString().Trim())
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:_", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC31030: Conditional compilation constant '_ ^^ ^^ ' is not valid: Identifier expected.", output.ToString().Trim())
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:_ ", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC31030: Conditional compilation constant '_ ^^ ^^ ' is not valid: Identifier expected.", output.ToString().Trim())
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:a,_", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC31030: Conditional compilation constant '_ ^^ ^^ ' is not valid: Identifier expected.", output.ToString().Trim())
            End Using
            CleanupAllGeneratedFiles(source)
        End Sub

        Private Function GetDefaultResponseFilePath() As String
            Return Temp.CreateFile().WriteAllBytes(CommandLineTestResources.vbc_rsp).Path
        End Function

        <Fact>
        Public Sub DefaultResponseFile()
            Dim defaultResponseFile = GetDefaultResponseFilePath()
            Assert.True(File.Exists(defaultResponseFile))
            Dim vbc As New MockVisualBasicCompiler(defaultResponseFile, _baseDirectory, {})

            ' VB includes these by default, with or without the default response file.
            Dim corlibLocation = GetType(Object).Assembly.Location
            Dim corlibDir = Path.GetDirectoryName(corlibLocation)
            Dim systemLocation = Path.Combine(corlibDir, "System.dll")
            Dim msvbLocation = Path.Combine(corlibDir, "Microsoft.VisualBasic.dll")

            Assert.Equal(vbc.Arguments.MetadataReferences.Select(Function(r) r.Reference),
        {
            "Accessibility.dll",
            "System.Configuration.dll",
            "System.Configuration.Install.dll",
            "System.Data.dll",
            "System.Data.OracleClient.dll",
            "System.Deployment.dll",
            "System.Design.dll",
            "System.DirectoryServices.dll",
            "System.dll",
            "System.Drawing.Design.dll",
            "System.Drawing.dll",
            "System.EnterpriseServices.dll",
            "System.Management.dll",
            "System.Messaging.dll",
            "System.Runtime.Remoting.dll",
            "System.Runtime.Serialization.Formatters.Soap.dll",
            "System.Security.dll",
            "System.ServiceProcess.dll",
            "System.Transactions.dll",
            "System.Web.dll",
            "System.Web.Mobile.dll",
            "System.Web.RegularExpressions.dll",
            "System.Web.Services.dll",
            "System.Windows.Forms.dll",
            "System.XML.dll",
            "System.Workflow.Activities.dll",
            "System.Workflow.ComponentModel.dll",
            "System.Workflow.Runtime.dll",
            "System.Runtime.Serialization.dll",
            "System.ServiceModel.dll",
            "System.Core.dll",
            "System.Xml.Linq.dll",
            "System.Data.Linq.dll",
            "System.Data.DataSetExtensions.dll",
            "System.Web.Extensions.dll",
            "System.Web.Extensions.Design.dll",
            "System.ServiceModel.Web.dll",
            systemLocation,
            msvbLocation
        }, StringComparer.OrdinalIgnoreCase)

            Assert.Equal(vbc.Arguments.CompilationOptions.GlobalImports.Select(Function(i) i.Name),
        {
            "System",
            "Microsoft.VisualBasic",
            "System.Linq",
            "System.Xml.Linq"
        })

            Assert.True(vbc.Arguments.CompilationOptions.OptionInfer)
        End Sub

        <Fact>
        Public Sub DefaultResponseFileNoConfig()
            Dim defaultResponseFile = GetDefaultResponseFilePath()
            Assert.True(File.Exists(defaultResponseFile))
            Dim vbc As New MockVisualBasicCompiler(defaultResponseFile, _baseDirectory, {"/noconfig"})

            ' VB includes these by default, with or without the default response file.
            Dim corlibLocation = GetType(Object).Assembly.Location
            Dim corlibDir = Path.GetDirectoryName(corlibLocation)
            Dim systemLocation = Path.Combine(corlibDir, "System.dll")
            Dim msvbLocation = Path.Combine(corlibDir, "Microsoft.VisualBasic.dll")

            Assert.Equal(vbc.Arguments.MetadataReferences.Select(Function(r) r.Reference),
        {
            systemLocation,
            msvbLocation
        }, StringComparer.OrdinalIgnoreCase)

            Assert.Equal(0, vbc.Arguments.CompilationOptions.GlobalImports.Count)

            Assert.False(vbc.Arguments.CompilationOptions.OptionInfer)
        End Sub

        <Fact(), WorkItem(546114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546114")>
        Public Sub TestFilterCommandLineDiagnostics()
            Dim source As String = Temp.CreateFile().WriteAllText(<text>
Module Module1
    Function blah() As Integer
    End Function

    Sub Main()
    End Sub
End Module
</text>.Value).Path

            ' Previous versions of the compiler used to report warnings (BC2026)
            ' whenever an unrecognized warning code was supplied via /nowarn or /warnaserror.
            ' We no longer generate a warning in such cases.
            Dim vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/preferreduilang:en", "/blah", "/nowarn:2007,42353,1234,2026", source})
            Using output = New StringWriter()
                Dim exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Equal("vbc : warning BC2007: unrecognized option '/blah'; ignored", output.ToString().Trim())
            End Using
            CleanupAllGeneratedFiles(source)
        End Sub

        <WorkItem(546305, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546305")>
        <Fact()>
        Public Sub Bug15539()
            Dim source As String = Temp.CreateFile().WriteAllText(<text>
Module Module1
    Sub Main()
    End Sub
End Module
</text>.Value).Path

            Dim vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/preferreduilang:en", "/define:I(", source})
            Dim exitCode As Integer
            Using output As New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC31030: Conditional compilation constant 'I ^^ ^^ ' is not valid: End of statement expected.", output.ToString().Trim())
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/preferreduilang:en", "/define:I*", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC31030: Conditional compilation constant 'I ^^ ^^ ' is not valid: End of statement expected.", output.ToString().Trim())
            End Using
        End Sub

        <Fact()>
        Public Sub TestImportsWithQuotes()
            Dim errors As IEnumerable(Of DiagnosticInfo) = Nothing

            Dim [imports] = "System,""COLL = System.Collections"",System.Diagnostics,""COLLGEN =   System.Collections.Generic"""
            Dim vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/imports:" + [imports]})
            Assert.Equal(4, vbc.Arguments.CompilationOptions.GlobalImports.Count)
            Assert.Equal("System", vbc.Arguments.CompilationOptions.GlobalImports(0).Name)
            Assert.Equal("COLL = System.Collections", vbc.Arguments.CompilationOptions.GlobalImports(1).Name)
            Assert.Equal("System.Diagnostics", vbc.Arguments.CompilationOptions.GlobalImports(2).Name)
            Assert.Equal("COLLGEN =   System.Collections.Generic", vbc.Arguments.CompilationOptions.GlobalImports(3).Name)
        End Sub

        <Fact()>
        Public Sub TestCommandLineSwitchThatNoLongerAreImplemented()
            ' These switches are no longer implemented and should fail silently
            ' the switches have various arguments that can be used
            Dim source As String = Temp.CreateFile().WriteAllText(<text>
Module Module1
    Sub Main()
    End Sub
End Module
</text>.Value).Path
            Dim exitCode As Integer
            Dim vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/netcf", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Equal("", output.ToString().Trim())
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/bugreport", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Equal("", output.ToString().Trim())
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/bugreport:test.dmp", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Equal("", output.ToString().Trim())
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/errorreport", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Equal("", output.ToString().Trim())
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/errorreport:prompt", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Equal("", output.ToString().Trim())
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/errorreport:queue", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Equal("", output.ToString().Trim())
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/errorreport:send", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Equal("", output.ToString().Trim())
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/errorreport:", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Equal("", output.ToString().Trim())
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/bugreport:", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Equal("", output.ToString().Trim())
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/novbruntimeref", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Equal("", output.ToString().Trim())
            End Using
            ' Just to confirm case insensitive
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/errorreport:PROMPT", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
                Assert.Equal("", output.ToString().Trim())
            End Using
            CleanupAllGeneratedFiles(source)
        End Sub

        <WorkItem(531263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531263")>
        <Fact>
        Public Sub EmptyFileName()
            Using outWriter As New StringWriter()
                Dim exitCode = New MockVisualBasicCompiler(Nothing, _baseDirectory, {""}).Run(outWriter, Nothing)
                Assert.NotEqual(0, exitCode)

                ' error BC2032: File name '' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Assert.Contains("BC2032", outWriter.ToString(), StringComparison.Ordinal)
            End Using

        End Sub

        <WorkItem(1119609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1119609"), Theory,
    InlineData("/preferreduilang", True, Nothing), InlineData("/preferreduilang:", True, Nothing),
    InlineData("/preferreduilang:zz", Nothing, True), InlineData("/preferreduilang:en-zz", Nothing, True),
    InlineData("/preferreduilang:en-US", Nothing, False), InlineData("/preferreduilang:de", Nothing, False),
    InlineData("/preferreduilang:de-AT", Nothing, False)>
        Public Sub PreferredUILang(arg As String, Contains_BC2006 As Boolean?, Contains_BC2038 As Boolean?)
            Using outWriter As New StringWriter(CultureInfo.InvariantCulture)
                Dim exitCode = New MockVisualBasicCompiler(Nothing, _baseDirectory, {arg}).Run(outWriter, Nothing)
                Assert.Equal(1, exitCode)
                If Contains_BC2006.HasValue Then
                    If Contains_BC2006.Value Then Assert.Contains("BC2006", outWriter.ToString(), StringComparison.Ordinal) Else Assert.DoesNotContain("BC2006", outWriter.ToString(), StringComparison.Ordinal)
                ElseIf Contains_BC2038.HasValue Then
                    If Contains_BC2038.Value Then Assert.Contains("BC2038", outWriter.ToString(), StringComparison.Ordinal) Else Assert.DoesNotContain("BC2038", outWriter.ToString(), StringComparison.Ordinal)
                End If
            End Using
        End Sub


        <WorkItem(650083, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/650083"), Fact>
        Public Sub ReservedDeviceNameAsFileName()
            ' Source file name
            Dim parsedArgs = DefaultParse({"/t:library", "con.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = DefaultParse({"/out:com1.exe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments("\\.\com1").WithLocation(1, 1))

            parsedArgs = DefaultParse({"/doc:..\lpt2.xml", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_XMLCannotWriteToXMLDocFile2).WithArguments("..\lpt2.xml", "The system cannot find the path specified").WithLocation(1, 1))

            parsedArgs = DefaultParse({"/SdkPath:..\aux", "com.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_CannotFindStandardLibrary1).WithArguments("System.dll").WithLocation(1, 1),
                                 Diagnostic(ERRID.ERR_LibNotFound).WithArguments("Microsoft.VisualBasic.dll").WithLocation(1, 1))

        End Sub

        <Fact()>
        Public Sub ReservedDeviceNameAsFileName2()
            Dim source As String = Temp.CreateFile().WriteAllText(<text>
Module Module1
    Sub Main()
    End Sub
End Module
</text>.Value).Path
            ' Make sure these reserved device names don't affect compiler
            Dim exitCode As Integer
            Dim vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/r:.\com3.dll", "/preferreduilang:en", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Contains("error BC2017: could not find library '.\com3.dll'", output.ToString(), StringComparison.Ordinal)
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/preferreduilang:en", "/link:prn.dll", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Contains("error BC2017: could not find library 'prn.dll'", output.ToString(), StringComparison.Ordinal)
            End Using

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"@aux.rsp", "/preferreduilang:en", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(1, exitCode)

                Dim errMessage = output.ToString().Trim()
                Assert.Contains("error BC2011: unable to open response file", errMessage, StringComparison.Ordinal)
                Assert.Contains("aux.rsp", errMessage, StringComparison.Ordinal)
            End Using
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/preferreduilang:en", "/vbruntime:..\con.dll", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Contains("error BC2017: could not find library '..\con.dll'", output.ToString(), StringComparison.Ordinal)
            End Using
            ' Native VB compiler also ignore invalid lib paths
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/LibPath:lpt1,Lpt2,LPT9", source})
            Using output = New StringWriter()
                exitCode = vbc.Run(output, Nothing)
                Assert.Equal(0, exitCode)
            End Using
            CleanupAllGeneratedFiles(source)
        End Sub

        <Fact, WorkItem(574361, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/574361")>
        Public Sub LangVersionForOldBC36716()

            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("src.vb")
            src.WriteAllText(
<text><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Collections

Friend Module AutoPropAttributesmod
        Class AttrInThisAsmAttribute
            Inherits Attribute
            Public Property Prop() As Integer
        End Class

    Class HasProps
        <CompilerGenerated()>
        Public Property Scen1() As <CompilerGenerated()> Func(Of String)
        <CLSCompliant(False), Obsolete("obsolete message!")>
                <AttrInThisAsmAttribute()>
        Public Property Scen2() As String
    End Class

End Module
]]>
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /t:library /langversion:9 /preferreduilang:en " & src.ToString(), expectedRetCode:=1, startFolder:=dir.Path)
            AssertOutput(
<text><![CDATA[
src.vb(8) : error BC36716: Visual Basic 9.0 does not support auto-implemented properties.
            Public Property Prop() As Integer
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
src.vb(12) : error BC36716: Visual Basic 9.0 does not support auto-implemented properties.
        <CompilerGenerated()>
        ~~~~~~~~~~~~~~~~~~~~~
        Public Property Scen1() As <CompilerGenerated()> Func(Of String)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
src.vb(12) : error BC36716: Visual Basic 9.0 does not support implicit line continuation.
        <CompilerGenerated()>
        ~~~~~~~~~~~~~~~~~~~~~
        Public Property Scen1() As <CompilerGenerated()> Func(Of String)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
src.vb(14) : error BC36716: Visual Basic 9.0 does not support auto-implemented properties.
        <CLSCompliant(False), Obsolete("obsolete message!")>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                <AttrInThisAsmAttribute()>
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        Public Property Scen2() As String
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
src.vb(14) : error BC36716: Visual Basic 9.0 does not support implicit line continuation.
        <CLSCompliant(False), Obsolete("obsolete message!")>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                <AttrInThisAsmAttribute()>
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        Public Property Scen2() As String
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</text>, output)


            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact>
        Public Sub DiagnosticFormatting()
            Dim source = "
Class C
    Sub Main()
        Goo(0)
#ExternalSource(""c:\temp\a\1.vb"", 10)
        Goo(1)
#End ExternalSource
#ExternalSource(""C:\a\..\b.vb"", 20)
        Goo(2)
#End ExternalSource
#ExternalSource(""C:\a\../B.vb"", 30)
        Goo(3)
#End ExternalSource
#ExternalSource(""../b.vb"", 40)
        Goo(4)
#End ExternalSource
#ExternalSource(""..\b.vb"", 50)
        Goo(5)
#End ExternalSource
#ExternalSource(""C:\X.vb"", 60)
        Goo(6)
#End ExternalSource
#ExternalSource(""C:\x.vb"", 70)
        Goo(7)
#End ExternalSource
#ExternalSource(""      "", 90)
		Goo(9)
#End ExternalSource
#ExternalSource(""C:\*.vb"", 100)
		Goo(10)
#End ExternalSource
#ExternalSource("""", 110)
		Goo(11)
#End ExternalSource
        Goo(12)
#ExternalSource(""***"", 140)
        Goo(14)
#End ExternalSource
    End Sub
End Class
"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.vb").WriteAllText(source)

            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/preferreduilang:en", "/t:library", "a.vb"})
                Dim exitCode = vbc.Run(outWriter, Nothing)
                Assert.Equal(1, exitCode)

                ' with /fullpaths off
                Dim expected =
file.Path & "(4) : error BC30451: 'Goo' is not declared. It may be inaccessible due to its protection level.
        Goo(0)
        ~~~   
c:\temp\a\1.vb(10) : error BC30451: 'Goo' is not declared. It may be inaccessible due to its protection level.
        Goo(1)
        ~~~   
C:\b.vb(20) : error BC30451: 'Goo' is not declared. It may be inaccessible due to its protection level.
        Goo(2)
        ~~~   
C:\B.vb(30) : error BC30451: 'Goo' is not declared. It may be inaccessible due to its protection level.
        Goo(3)
        ~~~   
" & Path.GetFullPath(Path.Combine(dir.Path, "..\b.vb")) & "(40) : error BC30451: 'Goo' is not declared. It may be inaccessible due to its protection level.
        Goo(4)
        ~~~   
" & Path.GetFullPath(Path.Combine(dir.Path, "..\b.vb")) & "(50) : error BC30451: 'Goo' is not declared. It may be inaccessible due to its protection level.
        Goo(5)
        ~~~   
C:\X.vb(60) : error BC30451: 'Goo' is not declared. It may be inaccessible due to its protection level.
        Goo(6)
        ~~~   
C:\x.vb(70) : error BC30451: 'Goo' is not declared. It may be inaccessible due to its protection level.
        Goo(7)
        ~~~   
      (90) : error BC30451: 'Goo' is not declared. It may be inaccessible due to its protection level.
        Goo(9)
        ~~~   
C:\*.vb(100) : error BC30451: 'Goo' is not declared. It may be inaccessible due to its protection level.
        Goo(10)
        ~~~    
(110) : error BC30451: 'Goo' is not declared. It may be inaccessible due to its protection level.
        Goo(11)
        ~~~    
" & file.Path & "(35) : error BC30451: 'Goo' is not declared. It may be inaccessible due to its protection level.
        Goo(12)
        ~~~    
***(140) : error BC30451: 'Goo' is not declared. It may be inaccessible due to its protection level.
        Goo(14)
        ~~~    
"
                AssertOutput(expected, outWriter.ToString())
                CleanupAllGeneratedFiles(file.Path)
            End Using
        End Sub

        <Fact>
        Public Sub ParseFeatures()
            Dim args = DefaultParse({"/features:Test", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal("Test", args.ParseOptions.Features.Single().Key)

            args = DefaultParse({"/features:Test", "a.vb", "/Features:Experiment"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(2, args.ParseOptions.Features.Count)
            Assert.True(args.ParseOptions.Features.ContainsKey("Test"))
            Assert.True(args.ParseOptions.Features.ContainsKey("Experiment"))

            args = DefaultParse({"/features:Test=false,Key=value", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.True(args.ParseOptions.Features.SetEquals(New Dictionary(Of String, String) From {{"Test", "false"}, {"Key", "value"}}))

            ' We don't do any rigorous validation of /features arguments...

            args = DefaultParse({"/features", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Empty(args.ParseOptions.Features)

            args = DefaultParse({"/features:Test,", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.True(args.ParseOptions.Features.SetEquals(New Dictionary(Of String, String) From {{"Test", "true"}}))
        End Sub

        <Fact>
        Public Sub ParseAdditionalFile()
            Dim args = DefaultParse({"/additionalfile:web.config", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(Path.Combine(_baseDirectory, "web.config"), args.AdditionalFiles.Single().Path)

            args = DefaultParse({"/additionalfile:web.config", "a.vb", "/additionalfile:app.manifest"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(2, args.AdditionalFiles.Length)
            Assert.Equal(Path.Combine(_baseDirectory, "web.config"), args.AdditionalFiles(0).Path)
            Assert.Equal(Path.Combine(_baseDirectory, "app.manifest"), args.AdditionalFiles(1).Path)

            args = DefaultParse({"/additionalfile:web.config", "a.vb", "/additionalfile:web.config"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(2, args.AdditionalFiles.Length)
            Assert.Equal(Path.Combine(_baseDirectory, "web.config"), args.AdditionalFiles(0).Path)
            Assert.Equal(Path.Combine(_baseDirectory, "web.config"), args.AdditionalFiles(1).Path)

            args = DefaultParse({"/additionalfile:..\web.config", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(Path.Combine(_baseDirectory, "..\web.config"), args.AdditionalFiles.Single().Path)

            Dim baseDir = Temp.CreateDirectory()
            baseDir.CreateFile("web1.config")
            baseDir.CreateFile("web2.config")
            baseDir.CreateFile("web3.config")

            args = DefaultParse({"/additionalfile:web*.config", "a.vb"}, baseDir.Path)
            args.Errors.Verify()
            Assert.Equal(3, args.AdditionalFiles.Length)
            Assert.Equal(Path.Combine(baseDir.Path, "web1.config"), args.AdditionalFiles(0).Path)
            Assert.Equal(Path.Combine(baseDir.Path, "web2.config"), args.AdditionalFiles(1).Path)
            Assert.Equal(Path.Combine(baseDir.Path, "web3.config"), args.AdditionalFiles(2).Path)

            args = DefaultParse({"/additionalfile:web.config;app.manifest", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(2, args.AdditionalFiles.Length)
            Assert.Equal(Path.Combine(_baseDirectory, "web.config"), args.AdditionalFiles(0).Path)
            Assert.Equal(Path.Combine(_baseDirectory, "app.manifest"), args.AdditionalFiles(1).Path)

            args = DefaultParse({"/additionalfile:web.config,app.manifest", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(2, args.AdditionalFiles.Length)
            Assert.Equal(Path.Combine(_baseDirectory, "web.config"), args.AdditionalFiles(0).Path)
            Assert.Equal(Path.Combine(_baseDirectory, "app.manifest"), args.AdditionalFiles(1).Path)

            args = DefaultParse({"/additionalfile:web.config:app.manifest", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(1, args.AdditionalFiles.Length)
            Assert.Equal(Path.Combine(_baseDirectory, "web.config:app.manifest"), args.AdditionalFiles(0).Path)

            args = DefaultParse({"/additionalfile", "a.vb"}, _baseDirectory)
            args.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("additionalfile", ":<file_list>"))
            Assert.Equal(0, args.AdditionalFiles.Length)

            args = DefaultParse({"/additionalfile:", "a.vb"}, _baseDirectory)
            args.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("additionalfile", ":<file_list>"))
            Assert.Equal(0, args.AdditionalFiles.Length)
        End Sub

        Private Shared Sub Verify(actual As IEnumerable(Of Diagnostic), ParamArray expected As DiagnosticDescription())
            actual.Verify(expected)
        End Sub

        Private Const s_logoLine1 As String = "Microsoft (R) Visual Basic Compiler version"
        Private Const s_logoLine2 As String = "Copyright (C) Microsoft Corporation. All rights reserved."

        Private Shared Function OccurrenceCount(source As String, word As String) As Integer
            Dim n = 0
            Dim index = source.IndexOf(word, StringComparison.Ordinal)
            While (index >= 0)
                n += 1
                index = source.IndexOf(word, index + word.Length, StringComparison.Ordinal)
            End While
            Return n
        End Function

        Private Shared Function VerifyOutput(sourceDir As TempDirectory, sourceFile As TempFile,
                                         Optional includeCurrentAssemblyAsAnalyzerReference As Boolean = True,
                                         Optional additionalFlags As String() = Nothing,
                                         Optional expectedInfoCount As Integer = 0,
                                         Optional expectedWarningCount As Integer = 0,
                                         Optional expectedErrorCount As Integer = 0) As String
            Dim args = {
                        "/nologo", "/preferreduilang:en", "/t:library",
                        sourceFile.Path
                   }
            If includeCurrentAssemblyAsAnalyzerReference Then
                args = args.Append("/a:" + Assembly.GetExecutingAssembly().Location)
            End If
            If additionalFlags IsNot Nothing Then
                args = args.Append(additionalFlags)
            End If

            Dim vbc = New MockVisualBasicCompiler(Nothing, sourceDir.Path, args)
            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                Dim exitCode = vbc.Run(outWriter, Nothing)
                Dim output = outWriter.ToString()

                Dim expectedExitCode = If(expectedErrorCount > 0, 1, 0)
                Assert.True(expectedExitCode = exitCode,
                        String.Format("Expected exit code to be '{0}' was '{1}'.{2}Output:{3}{4}", expectedExitCode, exitCode, Environment.NewLine, Environment.NewLine, output))

                Assert.DoesNotContain(" : hidden", output, StringComparison.Ordinal)

                If expectedInfoCount = 0 Then
                    Assert.DoesNotContain(" : info", output, StringComparison.Ordinal)
                Else
                    Assert.Equal(expectedInfoCount, OccurrenceCount(output, " : info"))
                End If

                If expectedWarningCount = 0 Then
                    Assert.DoesNotContain(" : warning", output, StringComparison.Ordinal)
                Else
                    Assert.Equal(expectedWarningCount, OccurrenceCount(output, " : warning"))
                End If

                If expectedErrorCount = 0 Then
                    Assert.DoesNotContain(" : error", output, StringComparison.Ordinal)
                Else
                    Assert.Equal(expectedErrorCount, OccurrenceCount(output, " : error"))
                End If

                Return output
            End Using
        End Function

        <WorkItem(899050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899050")>
        <Fact>
        Public Sub NoWarnAndWarnAsError_AnalyzerDriverWarnings()
            ' This assembly has an abstract MockAbstractDiagnosticAnalyzer type which should cause
            ' compiler warning BC42376 to be produced when compilations created in this test try to load it.
            Dim source = "Imports System"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)

            Dim output = VerifyOutput(dir, file, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that compiler warning BC42376 can be suppressed via /nowarn.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn"})

            ' TEST: Verify that compiler warning BC42376 can be individually suppressed via /nowarn:.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:BC42376"})

            ' TEST: Verify that compiler warning BC42376 can be promoted to an error via /warnaserror+.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+"}, expectedErrorCount:=1)
            Assert.Contains("error BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that compiler warning BC42376 can be individually promoted to an error via /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror:42376"}, expectedErrorCount:=1)
            Assert.Contains("error BC42376", output, StringComparison.Ordinal)

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(899050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899050")>
        <WorkItem(981677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/981677")>
        <Fact>
        Public Sub NoWarnAndWarnAsError_HiddenDiagnostic()
            ' This assembly has a HiddenDiagnosticAnalyzer type which should produce custom hidden
            ' diagnostics for #ExternalSource directives present in the compilations created in this test.
            Dim source = "Imports System
#ExternalSource (""file"", 123)
#End ExternalSource"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)

            Dim output = VerifyOutput(dir, file, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that /nowarn has no impact on custom hidden diagnostic Hidden01.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn"})

            ' TEST: Verify that /nowarn: has no impact on custom hidden diagnostic Hidden01.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:Hidden01"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that /warnaserror+ has no impact on custom hidden diagnostic Hidden01.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+", "/nowarn:42376"})

            ' TEST: Verify that /warnaserror- has no impact on custom hidden diagnostic Hidden01.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that /warnaserror: promotes custom hidden diagnostic Hidden01 to an error.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror:hidden01"}, expectedWarningCount:=1, expectedErrorCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Hidden01: Throwing a diagnostic for #ExternalSource", output, StringComparison.Ordinal)

            ' TEST: Verify that /warnaserror-: has no impact on custom hidden diagnostic Hidden01.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-:Hidden01"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify /nowarn: overrides /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror:Hidden01", "/nowarn:Hidden01"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify /nowarn: overrides /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:hidden01", "/warnaserror:Hidden01"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify /nowarn: overrides /warnaserror-:.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-:Hidden01", "/nowarn:Hidden01"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify /nowarn: overrides /warnaserror-:.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:hidden01", "/warnaserror-:Hidden01"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify /nowarn doesn't override /warnaserror: in the case of custom hidden diagnostics.
            ' Although the compiler normally suppresses printing of hidden diagnostics in the compiler output, they are never really suppressed
            ' because in the IDE features that rely on hidden diagnostics to display light bulb need to continue to work even when users have global
            ' suppression (/nowarn) specified in their project. In other words, /nowarn flag is a no-op for hidden diagnostics.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn", "/warnaserror:Hidden01"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(2) : error Hidden01: Throwing a diagnostic for #ExternalSource", output, StringComparison.Ordinal)

            ' TEST: Verify /nowarn doesn't override /warnaserror: in the case of custom hidden diagnostics.
            ' Although the compiler normally suppresses printing of hidden diagnostics in the compiler output, they are never really suppressed
            ' because in the IDE features that rely on hidden diagnostics to display light bulb need to continue to work even when users have global
            ' suppression (/nowarn) specified in their project. In other words, /nowarn flag is a no-op for hidden diagnostics.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror:HIDDen01", "/nowarn"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(2) : error Hidden01: Throwing a diagnostic for #ExternalSource", output, StringComparison.Ordinal)

            ' TEST: Verify /nowarn and /warnaserror-: have no impact  on custom hidden diagnostic Hidden01.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-:Hidden01", "/nowarn"})

            ' TEST: Verify /nowarn and /warnaserror-: have no impact  on custom hidden diagnostic Hidden01.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn", "/warnaserror-:Hidden01"})

            ' TEST: Sanity test for /nowarn and /nowarn:.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn", "/nowarn:Hidden01"})

            ' TEST: Sanity test for /nowarn and /nowarn:.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:Hidden01", "/nowarn"})

            ' TEST: Verify that last /warnaserror[+/-]: flag on command line wins.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+:Hidden01", "/warnaserror-:hidden01"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that last /warnaserror[+/-]: flag on command line wins.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-:Hidden01", "/warnaserror+:hidden01"}, expectedWarningCount:=1, expectedErrorCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Hidden01: Throwing a diagnostic for #ExternalSource", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-", "/warnaserror+:hidden01"}, expectedWarningCount:=1, expectedErrorCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Hidden01: Throwing a diagnostic for #ExternalSource", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+:hiddEn01", "/warnaserror+", "/nowarn:42376"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(2) : error Hidden01: Throwing a diagnostic for #ExternalSource", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+:HiDden01", "/warnaserror-"}, expectedWarningCount:=1, expectedErrorCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Hidden01: Throwing a diagnostic for #ExternalSource", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+", "/warnaserror-:Hidden01", "/nowarn:42376"})

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-", "/warnaserror-:Hidden01"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-:Hidden01", "/warnaserror-"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+:HiDden01", "/warnaserror+", "/nowarn:42376"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(2) : error Hidden01: Throwing a diagnostic for #ExternalSource", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+", "/warnaserror+:HiDden01", "/nowarn:42376"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(2) : error Hidden01: Throwing a diagnostic for #ExternalSource", output, StringComparison.Ordinal)

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(899050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899050")>
        <WorkItem(981677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/981677")>
        <Fact>
        Public Sub NoWarnAndWarnAsError_InfoDiagnostic()
            ' This assembly has an InfoDiagnosticAnalyzer type which should produce custom info
            ' diagnostics for the #Enable directives present in the compilations created in this test.
            Dim source = "Imports System
#Enable Warning"
            Dim name = "a.vb"

            Dim output = GetOutput(name, source, expectedWarningCount:=1, expectedInfoCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : info Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that custom info diagnostic Info01 can be suppressed via /nowarn.
            output = GetOutput(name, source, additionalFlags:={"/nowarn"})

            ' TEST: Verify that custom info diagnostic Info01 can be individually suppressed via /nowarn:.
            output = GetOutput(name, source, additionalFlags:={"/nowarn:Info01"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that custom info diagnostic Info01 can never be promoted to an error via /warnaserror+.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror+", "/nowarn:42376"}, expectedInfoCount:=1)
            Assert.Contains("a.vb(2) : info Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that custom info diagnostic Info01 is still reported as an info when /warnaserror- is used.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror-"}, expectedWarningCount:=1, expectedInfoCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : info Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that custom info diagnostic Info01 can be individually promoted to an error via /warnaserror:.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror:info01"}, expectedWarningCount:=1, expectedErrorCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that custom info diagnostic Info01 is still reported as an info when passed to /warnaserror-:.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror-:info01"}, expectedWarningCount:=1, expectedInfoCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : info Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify /nowarn: overrides /warnaserror:.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror:Info01", "/nowarn:info01"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify /nowarn: overrides /warnaserror:.
            output = GetOutput(name, source, additionalFlags:={"/nowarn:INFO01", "/warnaserror:Info01"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify /nowarn: overrides /warnaserror-:.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror-:Info01", "/nowarn:info01"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify /nowarn: overrides /warnaserror-:.
            output = GetOutput(name, source, additionalFlags:={"/nowarn:INFO01", "/warnaserror-:Info01"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify /nowarn overrides /warnaserror:.
            output = GetOutput(name, source, additionalFlags:={"/nowarn", "/warnaserror:Info01"})

            ' TEST: Verify /nowarn overrides /warnaserror:.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror:Info01", "/nowarn"})

            ' TEST: Verify /nowarn overrides /warnaserror-:.
            output = GetOutput(name, source, additionalFlags:={"/nowarn", "/warnaserror-:Info01"})

            ' TEST: Verify /nowarn overrides /warnaserror-:.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror-:Info01", "/nowarn"})

            ' TEST: Sanity test for /nowarn and /nowarn:.
            output = GetOutput(name, source, additionalFlags:={"/nowarn", "/nowarn:Info01"})

            ' TEST: Sanity test for /nowarn and /nowarn:.
            output = GetOutput(name, source, additionalFlags:={"/nowarn:Info01", "/nowarn"})

            ' TEST: Verify that last /warnaserror[+/-]: flag on command line wins.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror+:Info01", "/warnaserror-:info01"}, expectedWarningCount:=1, expectedInfoCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : info Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that last /warnaserror[+/-]: flag on command line wins.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror-:Info01", "/warnaserror+:INfo01"}, expectedWarningCount:=1, expectedErrorCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = GetOutput(name, source, additionalFlags:={"/warnaserror-", "/warnaserror+:info01"}, expectedWarningCount:=1, expectedErrorCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = GetOutput(name, source, additionalFlags:={"/warnaserror+:InFo01", "/warnaserror+", "/nowarn:42376"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(2) : error Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = GetOutput(name, source, additionalFlags:={"/warnaserror+:InfO01", "/warnaserror-"}, expectedWarningCount:=1, expectedErrorCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = GetOutput(name, source, additionalFlags:={"/warnaserror+", "/warnaserror-:INfo01", "/nowarn:42376"}, expectedInfoCount:=1)
            Assert.Contains("a.vb(2) : info Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = GetOutput(name, source, additionalFlags:={"/warnaserror-", "/warnaserror-:INfo01"}, expectedWarningCount:=1, expectedInfoCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : info Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = GetOutput(name, source, additionalFlags:={"/warnaserror-:Info01", "/warnaserror-"}, expectedWarningCount:=1, expectedInfoCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : info Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = GetOutput(name, source, additionalFlags:={"/warnaserror+", "/warnaserror+:Info01", "/nowarn:42376"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(2) : error Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = GetOutput(name, source, additionalFlags:={"/warnaserror+:InFO01", "/warnaserror+", "/nowarn:42376"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(2) : error Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)
        End Sub

        Private Function GetOutput(name As String,
                               source As String,
                      Optional includeCurrentAssemblyAsAnalyzerReference As Boolean = True,
                      Optional additionalFlags As String() = Nothing,
                      Optional expectedInfoCount As Integer = 0,
                      Optional expectedWarningCount As Integer = 0,
                      Optional expectedErrorCount As Integer = 0) As String
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(name)
            file.WriteAllText(source)
            Dim output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference, additionalFlags, expectedInfoCount, expectedWarningCount, expectedErrorCount)
            CleanupAllGeneratedFiles(file.Path)
            Return output
        End Function

        <WorkItem(899050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899050")>
        <WorkItem(981677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/981677")>
        <WorkItem(998069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/998069")>
        <WorkItem(998724, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/998724")>
        <Fact>
        Public Sub NoWarnAndWarnAsError_WarningDiagnostic()
            ' This assembly has a WarningDiagnosticAnalyzer type which should produce custom warning
            ' diagnostics for source types present in the compilations created in this test.
            Dim source = "Imports System
Module Module1
    Sub Main
        Dim x as Integer
    End Sub
End Module"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)

            Dim output = VerifyOutput(dir, file, expectedWarningCount:=4)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : warning Warning03: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(4) : warning BC42024: Unused local variable: 'x'.", output, StringComparison.Ordinal)

            ' TEST: Verify that compiler warning BC42024 as well as custom warning diagnostics Warning01 and Warning03 can be suppressed via /nowarn.
            ' This doesn't work for BC42376 currently (Bug 899050).
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn"})

            ' TEST: Verify that compiler warning BC42024 as well as custom warning diagnostics Warning01 and Warning03 can be individually suppressed via /nowarn:.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:warning01,Warning03,bc42024,58000"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that compiler warning BC42024 as well as custom warning diagnostics Warning01 and Warning03 can be promoted to errors via /warnaserror.
            ' Promoting compiler warning BC42024 to an error causes us to no longer report any custom warning diagnostics as errors (Bug 998069).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror"}, expectedWarningCount:=0, expectedErrorCount:=1)
            Assert.Contains("error BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that compiler warning BC42024 as well as custom warning diagnostics Warning01 and Warning03 can be promoted to errors via /warnaserror+.
            ' This doesn't work correctly currently - promoting compiler warning BC42024 to an error causes us to no longer report any custom warning diagnostics as errors (Bug 998069).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+"}, expectedWarningCount:=0, expectedErrorCount:=1)
            Assert.Contains("error BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that /warnaserror- keeps compiler warning BC42024 as well as custom warning diagnostics Warning01 and Warning03 as warnings.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-"}, expectedWarningCount:=4)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : warning Warning03: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(4) : warning BC42024: Unused local variable: 'x'.", output, StringComparison.Ordinal)

            ' TEST: Verify that custom warning diagnostics Warning01 and Warning03 can be individually promoted to errors via /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror:warning01,Something,warning03"}, expectedWarningCount:=2, expectedErrorCount:=2)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Warning03: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(4) : warning BC42024: Unused local variable: 'x'.", output, StringComparison.Ordinal)

            ' TEST: Verify that compiler warning BC42024 can be individually promoted to an error via /warnaserror+:.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+:bc42024"}, expectedWarningCount:=3, expectedErrorCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : warning Warning03: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(4) : error BC42024: Unused local variable: 'x'.", output, StringComparison.Ordinal)

            ' TEST: Verify that custom warning diagnostics Warning01 and Warning03 as well as compiler warning BC42024 can be individually promoted to errors via /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror:warning01,Warning03,bc42024,58000"}, expectedWarningCount:=1, expectedErrorCount:=3)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Warning03: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(4) : error BC42024: Unused local variable: 'x'.", output, StringComparison.Ordinal)

            ' TEST: Verify that last flag on command line wins between /nowarn and /warnaserror.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror", "/nowarn"})

            ' TEST: Verify that last flag on command line wins between /nowarn and /warnaserror+.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn", "/warnaserror+"}, expectedErrorCount:=1)
            Assert.Contains("error BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that /nowarn overrides /warnaserror-.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn", "/warnaserror-"})

            ' TEST: Verify that /nowarn overrides /warnaserror-.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-", "/nowarn"})

            ' TEST: Verify that /nowarn: overrides /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror:Something,042024,Warning01,Warning03", "/nowarn:warning01,Warning03,bc42024,58000"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that /nowarn: overrides /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:warning01,Warning03,bc42024,58000", "/warnaserror:Something,042024,Warning01,Warning03"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that /nowarn: overrides /warnaserror-:.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-:Something,042024,Warning01,Warning03", "/nowarn:warning01,Warning03,bc42024,58000"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that /nowarn: overrides /warnaserror-:.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:warning01,Warning03,bc42024,58000", "/warnaserror-:Something,042024,Warning01,Warning03"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that /nowarn: overrides /warnaserror+.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+", "/nowarn:warning01,Warning03,bc42024,58000,42376"})

            ' TEST: Verify that /nowarn: overrides /warnaserror.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:warning01,Warning03,bc42024,58000,42376", "/warnaserror"})

            ' TEST: Verify that /nowarn: overrides /warnaserror-.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-", "/nowarn:warning01,Warning03,bc42024,58000,42376"})

            ' TEST: Verify that /nowarn: overrides /warnaserror-.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:warning01,Warning03,bc42024,58000,42376", "/warnaserror-"})

            ' TEST: Verify that /nowarn: overrides /warnaserror-:.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-:warning01,Warning03,bc42024,58000", "/nowarn:warning01,Warning03,bc42024,58000"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that /nowarn: overrides /warnaserror-:.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:warning01,Warning03,bc42024,58000", "/warnaserror-:warning01,Warning03,bc42024,58000"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that /nowarn overrides /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn", "/warnaserror:Something,042024,Warning01,Warning03,42376"})

            ' TEST: Verify that /nowarn: overrides /warnaserror.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:warning01,Warning03,bc42024,58000,42376", "/warnaserror"})

            ' TEST: Verify that /nowarn overrides /warnaserror-:.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-:Something,042024,Warning01,Warning03,42376", "/nowarn"})

            ' TEST: Verify that /nowarn overrides /warnaserror-:.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn", "/warnaserror-:Something,042024,Warning01,Warning03,42376"})

            ' TEST: Sanity test for /nowarn and /nowarn:.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn", "/nowarn:Something,042024,Warning01,Warning03,42376"})

            ' TEST: Sanity test for /nowarn: and /nowarn.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:Something,042024,Warning01,Warning03,42376", "/nowarn"})

            ' TEST: Verify that last /warnaserror[+/-] flag on command line wins.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-", "/warnaserror+"}, expectedErrorCount:=1)
            Assert.Contains("error BC42376", output, StringComparison.Ordinal)

            ' Note: Old native compiler behaved strangely for the below case.
            ' When /warnaserror+ and /warnaserror- appeared on the same command line, native compiler would allow /warnaserror+ to win always
            ' regardless of order. However when /warnaserror+:xyz and /warnaserror-:xyz appeared on the same command line, native compiler
            ' would allow the flag that appeared last on the command line to win. Roslyn compiler allows the last flag that appears on the
            ' command line to win in both cases. This is not a breaking change since at worst this only makes a case that used to be an error
            ' in the native compiler to be a warning in Roslyn.

            ' TEST: Verify that last /warnaserror[+/-] flag on command line wins.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+", "/warnaserror-"}, expectedWarningCount:=4)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : warning Warning03: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(4) : warning BC42024: Unused local variable: 'x'.", output, StringComparison.Ordinal)

            ' TEST: Verify that last /warnaserror[+/-]: flag on command line wins.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-:warning01,Warning03", "/warnaserror+:Warning01,Warning03"}, expectedWarningCount:=2, expectedErrorCount:=2)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Warning03: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(4) : warning BC42024: Unused local variable: 'x'.", output, StringComparison.Ordinal)

            ' TEST: Verify that last /warnaserror[+/-]: flag on command line wins.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+:Warning01,Warning03", "/warnaserror-:warning01,Warning03"}, expectedWarningCount:=4)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : warning Warning03: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(4) : warning BC42024: Unused local variable: 'x'.", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-:warning01,Warning03,bc42024,58000,42376", "/warnaserror+"}, expectedWarningCount:=4)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : warning Warning03: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(4) : warning BC42024: Unused local variable: 'x'.", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror:warning01,Warning03,58000", "/warnaserror-"}, expectedWarningCount:=2, expectedErrorCount:=2)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Warning03: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(4) : warning BC42024: Unused local variable: 'x'.", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-", "/warnaserror+:warning01,Warning03,bc42024,58000"}, expectedWarningCount:=1, expectedErrorCount:=3)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Warning03: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(4) : error BC42024: Unused local variable: 'x'.", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+", "/warnaserror-:warning01,Warning03,bc42024,58000,42376"}, expectedWarningCount:=4)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : warning Warning03: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(4) : warning BC42024: Unused local variable: 'x'.", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+", "/warnaserror+:warning01,Warning03,bc42024,58000,42376"}, expectedErrorCount:=1)
            Assert.Contains("error BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror:warning01,Warning03,bc42024,58000,42376", "/warnaserror"}, expectedErrorCount:=1)
            Assert.Contains("error BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-", "/warnaserror-:warning01,Warning03,bc42024,58000,42376"}, expectedWarningCount:=4)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : warning Warning03: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(4) : warning BC42024: Unused local variable: 'x'.", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-:warning01,Warning03,bc42024,58000,42376", "/warnaserror-"}, expectedWarningCount:=4)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : warning Warning03: Throwing a diagnostic for types declared", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(4) : warning BC42024: Unused local variable: 'x'.", output, StringComparison.Ordinal)

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(899050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899050")>
        <WorkItem(981677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/981677")>
        <Fact>
        Public Sub NoWarnAndWarnAsError_ErrorDiagnostic()
            ' This assembly has an ErrorDiagnosticAnalyzer type which should produce custom error
            ' diagnostics for #Disable directives present in the compilations created in this test.
            Dim source = "Imports System
#Disable Warning"

            Dim dir = Temp.CreateDirectory()

            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)

            ' TEST: Verify that custom error diagnostic Error01 can't be suppressed via /nowarn.
            Dim output = VerifyOutput(dir, file, additionalFlags:={"/nowarn"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(2) : error Error01: Throwing a diagnostic for #Disable", output, StringComparison.Ordinal)

            ' TEST: Verify that custom error diagnostic Error01 can be suppressed via /nowarn:.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:Error01"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that custom error diagnostic Error01 can be suppressed via /nowarn:.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:ERROR01"}, expectedWarningCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that /nowarn: overrides /warnaserror+.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+", "/nowarn:ERROR01,42376"})

            ' TEST: Verify that /nowarn: overrides /warnaserror.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:ERROR01,42376", "/warnaserror"})

            ' TEST: Verify that /nowarn: overrides /warnaserror+:.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+:Error01,42376", "/nowarn:ERROR01,42376"})

            ' TEST: Verify that /nowarn: overrides /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:ERROR01,42376", "/warnaserror:Error01,42376"})

            ' TEST: Verify that /nowarn: overrides /warnaserror-.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-", "/nowarn:ERROR01,42376"})

            ' TEST: Verify that /nowarn: overrides /warnaserror-.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:ERROR01,42376", "/warnaserror-"})

            ' TEST: Verify that /nowarn: overrides /warnaserror-:.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-:Error01,42376", "/nowarn:ERROR01,42376"})

            ' TEST: Verify that /nowarn: overrides /warnaserror-:.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:ERROR01,42376", "/warnaserror-:Error01,42376"})

            ' TEST: Verify that nothing bad happens when using /warnaserror[+/-] when custom error diagnostic Error01 is present.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror", "/nowarn:42376"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(2) : error Error01: Throwing a diagnostic for #Disable", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+", "/nowarn:42376"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(2) : error Error01: Throwing a diagnostic for #Disable", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-"}, expectedWarningCount:=1, expectedErrorCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Error01: Throwing a diagnostic for #Disable", output, StringComparison.Ordinal)

            ' TEST: Verify that nothing bad happens if someone passes custom error diagnostic Error01 to /warnaserror[+/-]:.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror:Error01"}, expectedWarningCount:=1, expectedErrorCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Error01: Throwing a diagnostic for #Disable", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+:ERROR01"}, expectedWarningCount:=1, expectedErrorCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Error01: Throwing a diagnostic for #Disable", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-:Error01"}, expectedWarningCount:=1, expectedErrorCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Error01: Throwing a diagnostic for #Disable", output, StringComparison.Ordinal)

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(981677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/981677")>
        <Fact>
        Public Sub NoWarnAndWarnAsError_CompilerErrorDiagnostic()
            Dim source = "Imports System
Module Module1
    Sub Main
        Dim x as Integer = New Exception()
    End Sub
End Module"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)

            Dim output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference:=False, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            ' TEST: Verify that compiler error BC30311 can't be suppressed via /nowarn.
            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference:=False, additionalFlags:={"/nowarn"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            ' TEST: Verify that compiler error BC30311 can't be suppressed via /nowarn:.
            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference:=False, additionalFlags:={"/nowarn:30311"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference:=False, additionalFlags:={"/nowarn:BC30311"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference:=False, additionalFlags:={"/nowarn:bc30311"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            ' TEST: Verify that nothing bad happens when using /warnaserror[+/-] when compiler error BC30311 is present.
            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference:=False, additionalFlags:={"/warnaserror"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference:=False, additionalFlags:={"/warnaserror+"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference:=False, additionalFlags:={"/warnaserror-"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            ' TEST: Verify that nothing bad happens if someone passes BC30311 to /warnaserror[+/-]:.
            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference:=False, additionalFlags:={"/warnaserror:30311"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference:=False, additionalFlags:={"/warnaserror+:BC30311"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference:=False, additionalFlags:={"/warnaserror+:bc30311"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference:=False, additionalFlags:={"/warnaserror-:30311"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference:=False, additionalFlags:={"/warnaserror-:BC30311"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference:=False, additionalFlags:={"/warnaserror-:bc30311"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <Fact, WorkItem(1091972, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1091972"), WorkItem(444, "CodePlex")>
        Public Sub Bug1091972()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
<text>
''' &lt;summary&gt;ABC...XYZ&lt;/summary&gt;
Class C
    Shared Sub Main()
        Dim textStreamReader = New System.IO.StreamReader(GetType(C).Assembly.GetManifestResourceStream("doc.xml"))
        System.Console.WriteLine(textStreamReader.ReadToEnd())
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, String.Format("/nologo /doc:doc.xml /out:out.exe /resource:doc.xml {0}", src.ToString()), startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "doc.xml")))

            Dim expected = <text>
                               <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
out
</name>
</assembly>
<members>
<member name="T:C">
 <summary>ABC...XYZ</summary>
</member>
</members>
</doc>
]]>
                           </text>

            Using reader As New StreamReader(Path.Combine(dir.ToString(), "doc.xml"))
                Dim content = reader.ReadToEnd()
                AssertOutput(expected, content)
            End Using

            output = ProcessUtilities.RunAndGetOutput(Path.Combine(dir.ToString(), "out.exe"), startFolder:=dir.ToString())
            AssertOutput(expected, output)

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_GeneralCommandLineOptionOverridesGeneralRuleSetOption()
            Dim dir = Temp.CreateDirectory()

            Dim ruleSetSource = "<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
</RuleSet>
"
            Dim ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource)

            Dim arguments = DefaultParse({"/ruleset:Rules.RuleSet", "/WarnAsError+", "A.vb"}, dir.Path)

            Assert.Empty(arguments.Errors)
            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=arguments.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=0, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions.Count)
        End Sub

        <Fact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_GeneralWarnAsErrorPromotesWarningFromRuleSet()
            Dim dir = Temp.CreateDirectory()

            Dim ruleSetSource = "<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Warning"" />
  </Rules>
</RuleSet>
"
            Dim ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource)

            Dim arguments = DefaultParse({"/ruleset:Rules.RuleSet", "/WarnAsError+", "A.vb"}, dir.Path)

            Assert.Empty(arguments.Errors)
            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=arguments.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

        <Fact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_GeneralWarnAsErrorDoesNotPromoteInfoFromRuleSet()
            Dim dir = Temp.CreateDirectory()

            Dim ruleSetSource = "<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Info"" />
  </Rules>
</RuleSet>
"
            Dim ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource)

            Dim arguments = DefaultParse({"/ruleset:Rules.RuleSet", "/WarnAsError+", "A.vb"}, dir.Path)

            Assert.Empty(arguments.Errors)
            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=arguments.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Info, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

        <Fact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_SpecificWarnAsErrorPromotesInfoFromRuleSet()
            Dim dir = Temp.CreateDirectory()

            Dim ruleSetSource = "<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Info"" />
  </Rules>
</RuleSet>
"
            Dim ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource)

            Dim arguments = DefaultParse({"/ruleset:Rules.RuleSet", "/WarnAsError+:Test001", "A.vb"}, dir.Path)

            Assert.Empty(arguments.Errors)
            Assert.Equal(expected:=ReportDiagnostic.Default, actual:=arguments.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

        <Fact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_GeneralWarnAsErrorMinusResetsRules()
            Dim dir = Temp.CreateDirectory()

            Dim ruleSetSource = "<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Warning"" />
  </Rules>
</RuleSet>
"
            Dim ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource)

            Dim arguments = DefaultParse({"/ruleset:Rules.RuleSet", "/WarnAsError+", "/WarnAsError-", "A.vb"}, dir.Path)

            Assert.Empty(arguments.Errors)
            Assert.Equal(expected:=ReportDiagnostic.Default, actual:=arguments.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Warn, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

        <Fact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_SpecificWarnAsErrorMinusResetsRules()
            Dim dir = Temp.CreateDirectory()

            Dim ruleSetSource = "<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Warning"" />
  </Rules>
</RuleSet>
"
            Dim ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource)

            Dim arguments = DefaultParse({"/ruleset:Rules.RuleSet", "/WarnAsError+", "/WarnAsError-:Test001", "A.vb"}, dir.Path)

            Assert.Empty(arguments.Errors)
            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=arguments.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Warn, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

        <Fact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_SpecificWarnAsErrorMinusDefaultsRuleNotInRuleSet()
            Dim dir = Temp.CreateDirectory()

            Dim ruleSetSource = "<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Warning"" />
  </Rules>
</RuleSet>
"
            Dim ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource)

            Dim arguments = DefaultParse({"/ruleset:Rules.RuleSet", "/WarnAsError+:Test002", "/WarnAsError-:Test002", "A.vb"}, dir.Path)

            Assert.Empty(arguments.Errors)
            Assert.Equal(expected:=ReportDiagnostic.Default, actual:=arguments.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=2, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Warn, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions("Test001"))
            Assert.Equal(expected:=ReportDiagnostic.Default, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions("Test002"))
        End Sub

        <Fact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_LastGeneralWarnAsErrorTrumpsNoWarn()
            Dim dir = Temp.CreateDirectory()

            Dim ruleSetSource = "<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Warning"" />
  </Rules>
</RuleSet>
"
            Dim ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource)

            Dim arguments = DefaultParse({"/ruleset:Rules.RuleSet", "/NoWarn", "/WarnAsError+", "A.vb"}, dir.Path)

            Assert.Empty(arguments.Errors)
            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=arguments.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

        <Fact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_GeneralNoWarnTrumpsGeneralWarnAsErrorMinus()
            Dim dir = Temp.CreateDirectory()

            Dim ruleSetSource = "<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Warning"" />
  </Rules>
</RuleSet>
"
            Dim ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource)

            Dim arguments = DefaultParse({"/ruleset:Rules.RuleSet", "/WarnAsError+", "/NoWarn", "/WarnAsError-", "A.vb"}, dir.Path)

            Assert.Empty(arguments.Errors)
            Assert.Equal(expected:=ReportDiagnostic.Suppress, actual:=arguments.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Warn, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

        <Fact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_GeneralNoWarnTurnsOffAllButErrors()
            Dim dir = Temp.CreateDirectory()

            Dim ruleSetSource = "<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Error"" />
    <Rule Id=""Test002"" Action=""Warning"" />
    <Rule Id=""Test003"" Action=""Info"" />
  </Rules>
</RuleSet>
"
            Dim ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource)

            Dim arguments = DefaultParse({"/ruleset:Rules.RuleSet", "/NoWarn", "A.vb"}, dir.Path)

            Assert.Empty(arguments.Errors)
            Assert.Equal(expected:=ReportDiagnostic.Suppress, actual:=arguments.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=3, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions("Test001"))
            Assert.Equal(expected:=ReportDiagnostic.Suppress, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions("Test002"))
            Assert.Equal(expected:=ReportDiagnostic.Suppress, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions("Test003"))
        End Sub

        <Fact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_SpecificNoWarnAlwaysWins()
            Dim dir = Temp.CreateDirectory()

            Dim ruleSetSource = "<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Warning"" />
  </Rules>
</RuleSet>
"
            Dim ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource)

            Dim arguments = DefaultParse({"/ruleset:Rules.RuleSet", "/NoWarn:Test001", "/WarnAsError+", "/WarnAsError-:Test001", "A.vb"}, dir.Path)

            Assert.Empty(arguments.Errors)
            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=arguments.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Suppress, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

        <Fact>
        Public Sub ReportAnalyzer()
            Dim args1 = DefaultParse({"/reportanalyzer", "a.vb"}, _baseDirectory)
            Assert.True(args1.ReportAnalyzer)

            Dim args2 = DefaultParse({"", "a.vb"}, _baseDirectory)
            Assert.False(args2.ReportAnalyzer)
        End Sub

        <Fact>
        Public Sub ReportAnalyzerOutput()
            Dim source As String = Temp.CreateFile().WriteAllText(<text>
Class C
End Class
</text>.Value).Path

            Dim vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/reportanalyzer", "/t:library", "/a:" + Assembly.GetExecutingAssembly().Location, source})
            Using outWriter = New StringWriter()
                Dim exitCode = vbc.Run(outWriter, Nothing)
                Assert.Equal(0, exitCode)
                Dim output = outWriter.ToString()
                Assert.Contains(New WarningDiagnosticAnalyzer().ToString(), output, StringComparison.Ordinal)
                Assert.Contains(CodeAnalysisResources.AnalyzerExecutionTimeColumnHeader, output, StringComparison.Ordinal)
            End Using
            CleanupAllGeneratedFiles(source)
        End Sub

        <Fact>
        <WorkItem(1759, "https://github.com/dotnet/roslyn/issues/1759")>
        Public Sub AnalyzerDiagnosticThrowsInGetMessage()
            Dim source As String = Temp.CreateFile().WriteAllText(<text>
Class C
End Class
</text>.Value).Path

            Dim vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/t:library", source},
                                              analyzer:=New AnalyzerThatThrowsInGetMessage)
            Using outWriter = New StringWriter()
                Dim exitCode = vbc.Run(outWriter, Nothing)
                Assert.Equal(0, exitCode)
                Dim output = outWriter.ToString()

                ' Verify that the diagnostic reported by AnalyzerThatThrowsInGetMessage is reported, though it doesn't have the message.
                Assert.Contains(AnalyzerThatThrowsInGetMessage.Rule.Id, output, StringComparison.Ordinal)

                ' Verify that the analyzer exception diagnostic for the exception throw in AnalyzerThatThrowsInGetMessage is also reported.
                Assert.Contains(AnalyzerExecutor.AnalyzerExceptionDiagnosticId, output, StringComparison.Ordinal)
                Assert.Contains(NameOf(NotImplementedException), output, StringComparison.Ordinal)
            End Using
            CleanupAllGeneratedFiles(source)
        End Sub

        <Fact>
        <WorkItem(3707, "https://github.com/dotnet/roslyn/issues/3707")>
        Public Sub AnalyzerExceptionDiagnosticCanBeConfigured()
            Dim source As String = Temp.CreateFile().WriteAllText(<text>
Class C
End Class
</text>.Value).Path

            Dim vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/t:library", $"/warnaserror:{AnalyzerExecutor.AnalyzerExceptionDiagnosticId}", source},
                                              analyzer:=New AnalyzerThatThrowsInGetMessage)
            Using outWriter = New StringWriter()
                Dim exitCode = vbc.Run(outWriter, Nothing)
                Assert.NotEqual(0, exitCode)
                Dim output = outWriter.ToString()

                ' Verify that the analyzer exception diagnostic for the exception throw in AnalyzerThatThrowsInGetMessage is also reported.
                Assert.Contains(AnalyzerExecutor.AnalyzerExceptionDiagnosticId, output, StringComparison.Ordinal)
                Assert.Contains(NameOf(NotImplementedException), output, StringComparison.Ordinal)
            End Using
            CleanupAllGeneratedFiles(source)
        End Sub

        <Fact>
        <WorkItem(4589, "https://github.com/dotnet/roslyn/issues/4589")>
        Public Sub AnalyzerReportsMisformattedDiagnostic()
            Dim source As String = Temp.CreateFile().WriteAllText(<text>
Class C
End Class
</text>.Value).Path

            Dim vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/t:library", source},
                                              analyzer:=New AnalyzerReportingMisformattedDiagnostic)
            Using outWriter = New StringWriter()
                Dim exitCode = vbc.Run(outWriter, Nothing)
                Assert.Equal(0, exitCode)
                Dim output = outWriter.ToString()

                ' Verify that the diagnostic reported by AnalyzerReportingMisformattedDiagnostic is reported with the message format string, instead of the formatted message.
                Assert.Contains(AnalyzerThatThrowsInGetMessage.Rule.Id, output, StringComparison.Ordinal)
                Assert.Contains(AnalyzerThatThrowsInGetMessage.Rule.MessageFormat.ToString(CultureInfo.InvariantCulture), output, StringComparison.Ordinal)
            End Using
            CleanupAllGeneratedFiles(source)
        End Sub

        <Fact>
        Public Sub AdditionalFileDiagnostics()
            Dim dir = Temp.CreateDirectory()
            Dim source = dir.CreateFile("a.vb").WriteAllText(<text>
Class C
End Class
</text>.Value).Path

            Dim additionalFile = dir.CreateFile("AdditionalFile.txt").WriteAllText(<text>
Additional File Line 1!
Additional File Line 2!
</text>.Value).Path

            Dim nonCompilerInputFile = dir.CreateFile("DummyFile.txt").WriteAllText(<text>
Dummy File Line 1!
</text>.Value).Path

            Dim analyzer = New AdditionalFileDiagnosticAnalyzer(nonCompilerInputFile)
            Dim arguments = {"/nologo", "/preferreduilang:en", "/vbruntime", "/t:library",
            "/additionalfile:" & additionalFile, ' Valid additional text file
            "/additionalfile:" & Assembly.GetExecutingAssembly.Location, ' Non-text file specified as an additional text file
            source}
            Dim vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, arguments, analyzer)

            Using outWriter = New StringWriter()
                Dim exitCode = vbc.Run(outWriter, Nothing)
                Assert.Equal(1, exitCode)
                Dim output = outWriter.ToString()

                AssertOutput(
    String.Format("
AdditionalFile.txt(1) : warning AdditionalFileDiagnostic: Additional File Diagnostic: AdditionalFile
Additional File Line 1!
~~~~~~~~~~             
vbc : warning AdditionalFileDiagnostic: Additional File Diagnostic: {0}
vbc : warning AdditionalFileDiagnostic: Additional File Diagnostic: AdditionalFile
vbc : warning AdditionalFileDiagnostic: Additional File Diagnostic: DummyFile
vbc : warning AdditionalFileDiagnostic: Additional File Diagnostic: NonExistentPath
vbc : error BC2015: the file '{1}' is not a text file
",
        IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly.Location),
        Assembly.GetExecutingAssembly.Location),
    output, fileName:="AdditionalFile.txt")

                CleanupAllGeneratedFiles(source)
                CleanupAllGeneratedFiles(additionalFile)
                CleanupAllGeneratedFiles(nonCompilerInputFile)
            End Using
        End Sub

        <Fact, WorkItem(1093063, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1093063")>
        Public Sub VerifyDiagnosticSeverityNotLocalized()
            Dim source = <![CDATA[
Class A
End Class
]]>
            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/target:exe", fileName})
                vbc.Run(output, Nothing)

                ' If "error" was localized, below assert will fail on PLOC builds. The output would be something like: "!pTCvB!vbc : !FLxft!error 表! BC30420:"
                Assert.Contains("error BC30420:", output.ToString())
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub


        <ConditionalFact(GetType(WindowsOnly))>
        Public Sub SourceFile_BadPath()
            Dim args = DefaultParse({"e:c:\test\test.cs", "/t:library"}, _baseDirectory)
            args.Errors.Verify(Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments("e:c:\test\test.cs").WithLocation(1, 1))
        End Sub

        <ConditionalFact(GetType(WindowsOnly))>
        Public Sub FilePaths()
            Dim args = FullParse("\\unc\path\a.vb b.vb c:\path\c.vb", "e:\temp")
            Assert.Equal({"\\unc\path\a.vb", "e:\temp\b.vb", "c:\path\c.vb"}, args.SourceFiles.Select(Function(x) x.Path))

            args = FullParse("\\unc\path\a.vb ""b.vb"" c:\path\c.vb", "e:\temp")
            Assert.Equal({"\\unc\path\a.vb", "e:\temp\b.vb", "c:\path\c.vb"}, args.SourceFiles.Select(Function(x) x.Path))

            args = FullParse("""b"".vb""", "e:\temp")
            Assert.Equal({"e:\temp\b.vb"}, args.SourceFiles.Select(Function(x) x.Path))
        End Sub

        <ConditionalFact(GetType(WindowsOnly))>
        Public Sub ReferencePathsEx()
            Dim args = FullParse("/nostdlib /vbruntime- /noconfig /r:a.dll,b.dll test.vb", "e:\temp")
            Assert.Equal({"a.dll", "b.dll"}, args.MetadataReferences.Select(Function(x) x.Reference))

            args = FullParse("/nostdlib /vbruntime- /noconfig /r:""a.dll,b.dll"" test.vb", "e:\temp")
            Assert.Equal({"a.dll,b.dll"}, args.MetadataReferences.Select(Function(x) x.Reference))

            args = FullParse("/nostdlib /vbruntime- /noconfig /r:""lib, ex\a.dll"",b.dll test.vb", "e:\temp")
            Assert.Equal({"lib, ex\a.dll", "b.dll"}, args.MetadataReferences.Select(Function(x) x.Reference))

            args = FullParse("/nostdlib /vbruntime- /noconfig /r:""lib, ex\a.dll"" test.vb", "e:\temp")
            Assert.Equal({"lib, ex\a.dll"}, args.MetadataReferences.Select(Function(x) x.Reference))
        End Sub

        <ConditionalFact(GetType(WindowsOnly))>
        Public Sub ParseAssemblyReferences()

            Dim parseCore =
            Sub(value As String, paths As String())
                Dim list As New List(Of Diagnostic)
                Dim references = VisualBasicCommandLineParser.ParseAssemblyReferences("", value, list, embedInteropTypes:=False)
                Assert.Equal(0, list.Count)
                Assert.Equal(paths, references.Select(Function(r) r.Reference))
            End Sub

            parseCore("""a.dll""", New String() {"a.dll"})
            parseCore("a,b", New String() {"a", "b"})
            parseCore("""a,b""", New String() {"a,b"})

            ' This is an intentional deviation from the native compiler.  BCL docs on MSDN, MSBuild and the C# compiler 
            ' treat a semicolon as a separator.  VB compiler was the lone holdout here.  Rather than deviate we decided
            ' to unify the behavior.
            parseCore("a;b", New String() {"a", "b"})

            parseCore("""a;b""", New String() {"a;b"})

            ' Note this case can only happen when it is the last option on the command line.  When done
            ' in another position the command line splitting routine would continue parsing all the text
            ' after /r:"a as it resides in an unterminated quote.
            parseCore("""a", New String() {"a"})

            parseCore("a""mid""b", New String() {"amidb"})
        End Sub

        <Fact>
        Public Sub PublicSign()
            Dim args As VisualBasicCommandLineArguments
            Dim baseDir = "c:\test"
            Dim parse = Function(x As String) FullParse(x, baseDir)

            args = parse("/publicsign a.exe")
            Assert.True(args.CompilationOptions.PublicSign)

            args = parse("/publicsign+ a.exe")
            Assert.True(args.CompilationOptions.PublicSign)

            args = parse("/publicsign- a.exe")
            Assert.False(args.CompilationOptions.PublicSign)

            args = parse("a.exe")
            Assert.False(args.CompilationOptions.PublicSign)
        End Sub

        <WorkItem(8360, "https://github.com/dotnet/roslyn/issues/8360")>
        <Fact>
        Public Sub PublicSign_KeyFileRelativePath()
            Dim parsedArgs = FullParse("/publicsign /keyfile:test.snk a.cs", _baseDirectory)
            Assert.Equal(Path.Combine(_baseDirectory, "test.snk"), parsedArgs.CompilationOptions.CryptoKeyFile)
            parsedArgs.Errors.Verify()
        End Sub

        <WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")>
        <Fact>
        Public Sub PublicSignWithEmptyKeyPath()
            Dim parsedArgs = FullParse("/publicsign /keyfile: a.cs", _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("keyfile", ":<file>").WithLocation(1, 1))
        End Sub

        <WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")>
        <Fact>
        Public Sub PublicSignWithEmptyKeyPath2()
            Dim parsedArgs = FullParse("/publicsign /keyfile:"""" a.cs", _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("keyfile", ":<file>").WithLocation(1, 1))
        End Sub

        <ConditionalFact(GetType(WindowsOnly))>
        Public Sub CommandLineMisc()
            Dim args As VisualBasicCommandLineArguments
            Dim baseDir = "c:\test"
            Dim parse = Function(x As String) FullParse(x, baseDir)

            args = parse("/out:""a.exe""")
            Assert.Equal("a.exe", args.OutputFileName)

            args = parse("/out:""a-b.exe""")
            Assert.Equal("a-b.exe", args.OutputFileName)

            args = parse("/out:""a,b.exe""")
            Assert.Equal("a,b.exe", args.OutputFileName)

            ' The \ here causes " to be treated as a quote, not as an escaping construct
            args = parse("a\""b c""\d.cs")
            Assert.Equal({"c:\test\a""b", "c:\test\c\d.cs"}, args.SourceFiles.Select(Function(x) x.Path))

            args = parse("a\\""b c""\d.cs")
            Assert.Equal({"c:\test\a\b c\d.cs"}, args.SourceFiles.Select(Function(x) x.Path))

            args = parse("/nostdlib /vbruntime- /r:""a.dll"",""b.dll"" c.cs")
            Assert.Equal({"a.dll", "b.dll"}, args.MetadataReferences.Select(Function(x) x.Reference))

            args = parse("/nostdlib /vbruntime- /r:""a-s.dll"",""b-s.dll"" c.cs")
            Assert.Equal({"a-s.dll", "b-s.dll"}, args.MetadataReferences.Select(Function(x) x.Reference))

            args = parse("/nostdlib /vbruntime- /r:""a,s.dll"",""b,s.dll"" c.cs")
            Assert.Equal({"a,s.dll", "b,s.dll"}, args.MetadataReferences.Select(Function(x) x.Reference))
        End Sub

        <WorkItem(7588, "https://github.com/dotnet/roslyn/issues/7588")>
        <Fact()>
        Public Sub Version()
            Dim folderName = Temp.CreateDirectory().ToString()
            Dim expected As String = $"{s_compilerVersion} ({s_compilerShortCommitHash})"

            Dim argss = {
            "/version",
            "a.cs /version /preferreduilang:en",
            "/version /nologo",
            "/version /help"}

            For Each args In argss
                Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, args, startFolder:=folderName)
                Assert.Equal(expected, output.Trim())
            Next
        End Sub

        <Fact>
        Public Sub RefOut()
            Dim dir = Temp.CreateDirectory()
            Dim refDir = dir.CreateDirectory("ref")

            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText("
Public Class C
    ''' <summary>Main method</summary>
    Public Shared Sub Main()
        System.Console.Write(""Hello"")
    End Sub
    ''' <summary>Private method</summary>
    Private Shared Sub PrivateMethod()
        System.Console.Write(""Private"")
    End Sub
End Class")

            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path,
            {"/define:_MYTYPE=""Empty"" ", "/nologo", "/out:a.exe", "/refout:ref/a.dll", "/doc:doc.xml", "/deterministic", "a.vb"})

                Dim exitCode = vbc.Run(outWriter)
                Assert.Equal(0, exitCode)

                Dim exe = Path.Combine(dir.Path, "a.exe")
                Assert.True(File.Exists(exe))

                MetadataReaderUtils.VerifyPEMetadata(exe,
                {"TypeDefinition:<Module>", "TypeDefinition:C"},
                {"MethodDefinition:Void C.Main()", "MethodDefinition:Void C..ctor()", "MethodDefinition:Void C.PrivateMethod()"},
                {"CompilationRelaxationsAttribute", "RuntimeCompatibilityAttribute", "DebuggableAttribute", "STAThreadAttribute"}
                )

                Dim doc = Path.Combine(dir.Path, "doc.xml")
                Assert.True(File.Exists(doc))

                Dim content = File.ReadAllText(doc)
                Dim expectedDoc =
"<?xml version=""1.0""?>
<doc>
<assembly>
<name>
a
</name>
</assembly>
<members>
<member name=""M:C.Main"">
 <summary>Main method</summary>
</member>
<member name=""M:C.PrivateMethod"">
 <summary>Private method</summary>
</member>
</members>
</doc>"
                Assert.Equal(expectedDoc, content.Trim())

                Dim output = ProcessUtilities.RunAndGetOutput(exe, startFolder:=dir.Path)
                Assert.Equal("Hello", output.Trim())

                Dim refDll = Path.Combine(refDir.Path, "a.dll")
                Assert.True(File.Exists(refDll))

                ' The types and members that are included needs further refinement.
                ' See issue https://github.com/dotnet/roslyn/issues/17612
                MetadataReaderUtils.VerifyPEMetadata(refDll,
                {"TypeDefinition:<Module>", "TypeDefinition:C"},
                {"MethodDefinition:Void C.Main()", "MethodDefinition:Void C..ctor()"},
                {"CompilationRelaxationsAttribute", "RuntimeCompatibilityAttribute", "DebuggableAttribute", "STAThreadAttribute", "ReferenceAssemblyAttribute"}
                )

                ' Clean up temp files
                CleanupAllGeneratedFiles(dir.Path)
                CleanupAllGeneratedFiles(refDir.Path)
            End Using
        End Sub

        <Fact>
        Public Sub RefOutWithError()
            Dim dir = Temp.CreateDirectory().CreateDirectory("ref")
            Dim src = dir.CreateFile("a.vb").WriteAllText(
"Class C
    Public Shared Sub Main()
        Bad()
    End Sub
End Class")

            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                Dim csc = New MockVisualBasicCompiler(Nothing, dir.Path, {"/define:_MYTYPE=""Empty"" ", "/nologo", "/out:a.dll", "/refout:ref/a.dll", "/deterministic", "a.vb"})

                Dim exitCode = csc.Run(outWriter)
                Assert.Equal(1, exitCode)

                Dim vb = Path.Combine(dir.Path, "a.vb")

                Dim dll = Path.Combine(dir.Path, "a.dll")
                Assert.False(File.Exists(dll))

                Dim refDll = Path.Combine(dir.Path, Path.Combine("ref", "a.dll"))
                Assert.False(File.Exists(refDll))

                Assert.Equal(
$"{vb}(3) : error BC30451: 'Bad' is not declared. It may be inaccessible due to its protection level.

        Bad()
        ~~~",
outWriter.ToString().Trim())

                ' Clean up temp files
                CleanupAllGeneratedFiles(dir.Path)
            End Using
        End Sub

        <Fact>
        Public Sub RefOnly()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb").WriteAllText(
"Class C
    ''' <summary>Main method</summary>
    Public Shared Sub Main()
        Bad()
    End Sub
    ''' <summary>Field</summary>
    Private Dim field As Integer

    ''' <summary>Field</summary>
    Private Structure S
        ''' <summary>Struct Field</summary>
        Private Dim field As Integer
    End Structure
End Class")

            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                Dim csc = New MockVisualBasicCompiler(Nothing, dir.Path, {"/define:_MYTYPE=""Empty"" ", "/nologo", "/out:a.dll", "/refonly", "/debug", "/deterministic", "/doc:doc.xml", "a.vb"})

                Dim exitCode = csc.Run(outWriter)
                Assert.Equal(0, exitCode)

                Dim refDll = Path.Combine(dir.Path, "a.dll")
                Assert.True(File.Exists(refDll))

                ' The types and members that are included needs further refinement.
                ' See issue https://github.com/dotnet/roslyn/issues/17612
                MetadataReaderUtils.VerifyPEMetadata(refDll,
                {"TypeDefinition:<Module>", "TypeDefinition:C", "TypeDefinition:S"},
                {"MethodDefinition:Void C.Main()", "MethodDefinition:Void C..ctor()"},
                {"CompilationRelaxationsAttribute", "RuntimeCompatibilityAttribute", "DebuggableAttribute", "STAThreadAttribute", "ReferenceAssemblyAttribute"}
                )

                Dim pdb = Path.Combine(dir.Path, "a.pdb")
                Assert.False(File.Exists(pdb))

                Dim doc = Path.Combine(dir.Path, "doc.xml")
                Assert.True(File.Exists(doc))

                Dim content = File.ReadAllText(doc)
                Dim expectedDoc =
"<?xml version=""1.0""?>
<doc>
<assembly>
<name>
a
</name>
</assembly>
<members>
<member name=""M:C.Main"">
 <summary>Main method</summary>
</member>
<member name=""F:C.field"">
 <summary>Field</summary>
</member>
<member name=""T:C.S"">
 <summary>Field</summary>
</member>
<member name=""F:C.S.field"">
 <summary>Struct Field</summary>
</member>
</members>
</doc>"
                Assert.Equal(expectedDoc, content.Trim())

                ' Clean up temp files
                CleanupAllGeneratedFiles(dir.Path)
            End Using
        End Sub

        <WorkItem(13681, "https://github.com/dotnet/roslyn/issues/13681")>
        <Theory()>
        <InlineData("/t:exe", "/out:goo.dll", "goo.dll", "goo.dll.exe")>                                'Output with known but different extension
        <InlineData("/t:exe", "/out:goo.dLL", "goo.dLL", "goo.dLL.exe")>                                'Output with known but different extension (different casing)
        <InlineData("/t:library", "/out:goo.exe", "goo.exe", "goo.exe.dll")>                            'Output with known but different extension
        <InlineData("/t:library", "/out:goo.eXe", "goo.eXe", "goo.eXe.dll")>                            'Output with known but different extension (different casing)
        <InlineData("/t:module", "/out:goo.dll", "goo.dll", "goo.dll.netmodule")>                       'Output with known but different extension
        <InlineData("/t:winmdobj", "/out:goo.netmodule", "goo.netmodule", "goo.netmodule.winmdobj")>    'Output with known but different extension
        <InlineData("/t:exe", "/out:goo.netmodule", "goo.netmodule", "goo.netmodule.exe")>              'Output with known but different extension
        <InlineData("/t:library", "/out:goo.txt", "goo.txt.dll", "goo.dll")>                            'Output with unknown extension (.txt)
        <InlineData("/t:exe", "/out:goo.md", "goo.md.exe", "goo.exe")>                                  'Output with unknown extension (.md)
        <InlineData("/t:exe", "/out:goo", "goo.exe", "goo")>                                            'Output without extension
        <InlineData("/t:library", "/out:goo", "goo.dll", "goo")>                                        'Output without extension
        <InlineData("/t:module", "/out:goo", "goo.netmodule", "goo")>                                   'Output without extension
        <InlineData("/t:winmdobj", "/out:goo", "goo.winmdobj", "goo")>                                  'Output without extension
        <InlineData("/t:exe", "/out:goo.exe", "goo.exe", "goo.exe.exe")>                                'Output with correct extension (.exe)
        <InlineData("/t:library", "/out:goo.dll", "goo.dll", "goo.dll.dll")>                            'Output with correct extension (.dll)
        <InlineData("/t:module", "/out:goo.netmodule", "goo.netmodule", "goo.netmodule.netmodule")>     'Output with correct extension (.netmodule)
        <InlineData("/t:module", "/out:goo.NetModule", "goo.NetModule", "goo.NetModule.netmodule")>     'Output with correct extension (.netmodule) (different casing)
        <InlineData("/t:winmdobj", "/out:goo.winmdobj", "goo.winmdobj", "goo.winmdobj.winmdobj")>       'Output with correct extension (.winmdobj)
        Public Sub OutputingFilesWithDifferentExtensions(targetArg As String, outArg As String, expectedFile As String, unexpectedFile As String)
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Module Program
    Sub Main(args As String())
    End Sub
End Module
]]>
                </file>
            </compilation>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim sourceFile = dir.CreateFile(fileName)
            sourceFile.WriteAllText(source.Value)

            Using output As New StringWriter()

                Assert.Equal(0, New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, targetArg, outArg}).Run(output, Nothing))
                Assert.True(File.Exists(Path.Combine(dir.Path, expectedFile)), "Expected to find: " & expectedFile)
                Assert.False(File.Exists(Path.Combine(dir.Path, unexpectedFile)), "Didn't expect to find: " & unexpectedFile)

                CleanupAllGeneratedFiles(sourceFile.Path)
            End Using
        End Sub

        <Fact>
        Public Sub IOFailure_DisposeOutputFile()
            Dim srcPath = MakeTrivialExe(Temp.CreateDirectory().Path)
            Dim exePath = Path.Combine(Path.GetDirectoryName(srcPath), "test.exe")
            Dim csc = New MockVisualBasicCompiler(_baseDirectory, {"/nologo", "/preferreduilang:en", $"/out:{exePath}", srcPath})
            csc.FileOpen = Function(filePath, mode, access, share)
                               If filePath = exePath Then
                                   Return New TestStream(backingStream:=New MemoryStream(), dispose:=Sub() Throw New IOException("Fake IOException"))
                               End If

                               Return File.Open(filePath, mode, access, share)
                           End Function

            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                Assert.Equal(1, csc.Run(outWriter))
                Assert.Equal($"vbc : error BC2012: can't open '{exePath}' for writing: Fake IOException{Environment.NewLine}", outWriter.ToString())
            End Using
        End Sub

        <Fact>
        Public Sub IOFailure_DisposePdbFile()
            Dim srcPath = MakeTrivialExe(Temp.CreateDirectory().Path)
            Dim exePath = Path.Combine(Path.GetDirectoryName(srcPath), "test.exe")
            Dim pdbPath = Path.ChangeExtension(exePath, "pdb")
            Dim csc = New MockVisualBasicCompiler(_baseDirectory, {"/nologo", "/preferreduilang:en", "/debug", $"/out:{exePath}", srcPath})
            csc.FileOpen = Function(filePath, mode, access, share)
                               If filePath = pdbPath Then
                                   Return New TestStream(backingStream:=New MemoryStream(), dispose:=Sub() Throw New IOException("Fake IOException"))
                               End If

                               Return File.Open(filePath, mode, access, share)
                           End Function

            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                Assert.Equal(1, csc.Run(outWriter))
                Assert.Equal($"vbc : error BC2012: can't open '{pdbPath}' for writing: Fake IOException{Environment.NewLine}", outWriter.ToString())
            End Using
        End Sub

        <Fact>
        Public Sub IOFailure_DisposeXmlFile()
            Dim srcPath = MakeTrivialExe(Temp.CreateDirectory().Path)
            Dim xmlPath = Path.Combine(Path.GetDirectoryName(srcPath), "test.xml")
            Dim csc = New MockVisualBasicCompiler(_baseDirectory, {"/nologo", "/preferreduilang:en", $"/doc:{xmlPath}", srcPath})
            csc.FileOpen = Function(filePath, mode, access, share)
                               If filePath = xmlPath Then
                                   Return New TestStream(backingStream:=New MemoryStream(), dispose:=Sub() Throw New IOException("Fake IOException"))
                               End If

                               Return File.Open(filePath, mode, access, share)
                           End Function

            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                Assert.Equal(1, csc.Run(outWriter))
                Assert.Equal($"vbc : error BC2012: can't open '{xmlPath}' for writing: Fake IOException{Environment.NewLine}", outWriter.ToString())
            End Using
        End Sub

        <Theory>
        <InlineData("portable")>
        <InlineData("full")>
        Public Sub IOFailure_DisposeSourceLinkFile(format As String)
            Dim srcPath = MakeTrivialExe(Temp.CreateDirectory().Path)
            Dim sourceLinkPath = Path.Combine(Path.GetDirectoryName(srcPath), "test.json")
            Dim csc = New MockVisualBasicCompiler(_baseDirectory, {"/nologo", "/preferreduilang:en", "/debug:" & format, $"/sourcelink:{sourceLinkPath}", srcPath})
            csc.FileOpen = Function(filePath, mode, access, share)
                               If filePath = sourceLinkPath Then
                                   Return New TestStream(
                               backingStream:=New MemoryStream(Encoding.UTF8.GetBytes("
{
  ""documents"": {
     ""f:/build/*"" : ""https://raw.githubusercontent.com/my-org/my-project/1111111111111111111111111111111111111111/*""
  }
}
")),
                               dispose:=Sub() Throw New IOException("Fake IOException"))
                               End If

                               Return File.Open(filePath, mode, access, share)
                           End Function

            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                Assert.Equal(1, csc.Run(outWriter))
                Assert.Equal($"vbc : error BC2012: can't open '{sourceLinkPath}' for writing: Fake IOException{Environment.NewLine}", outWriter.ToString())
            End Using
        End Sub

        <Fact>
        Public Sub CompilingCodeWithInvalidPreProcessorSymbolsShouldProvideDiagnostics()
            Dim parsedArgs = DefaultParse({"/define:1", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ConditionalCompilationConstantNotValid).WithArguments("Identifier expected.", "1 ^^ ^^ ").WithLocation(1, 1))
        End Sub

        <Fact>
        Public Sub CompilingCodeWithInvalidLanguageVersionShouldProvideDiagnostics()
            Dim parsedArgs = DefaultParse({"/langversion:1000", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("langversion", "1000").WithLocation(1, 1))
        End Sub

        <WorkItem(406649, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=406649")>
        <ConditionalFact(GetType(IsEnglishLocal))>
        Public Sub MissingCompilerAssembly()
            Dim dir = Temp.CreateDirectory()
            Dim vbcPath = dir.CopyFile(s_basicCompilerExecutable).Path
            dir.CopyFile(GetType(Compilation).Assembly.Location)

            ' Missing Microsoft.CodeAnalysis.VisualBasic.dll.
            Dim result = ProcessUtilities.Run(vbcPath, arguments:="/nologo /t:library unknown.vb", workingDirectory:=dir.Path)
            Assert.Equal(1, result.ExitCode)
            Assert.Equal(
            $"Could not load file or assembly '{GetType(VisualBasicCompilation).Assembly.FullName}' or one of its dependencies. The system cannot find the file specified.",
            result.Output.Trim())

            ' Missing System.Collections.Immutable.dll.
            dir.CopyFile(GetType(VisualBasicCompilation).Assembly.Location)
            result = ProcessUtilities.Run(vbcPath, arguments:="/nologo /t:library unknown.vb", workingDirectory:=dir.Path)
            Assert.Equal(1, result.ExitCode)
            Assert.Equal(
            $"Could not load file or assembly '{GetType(ImmutableArray).Assembly.FullName}' or one of its dependencies. The system cannot find the file specified.",
            result.Output.Trim())
        End Sub

        <ConditionalFact(GetType(WindowsOnly))>
        <WorkItem(21935, "https://github.com/dotnet/roslyn/issues/21935")>
        Public Sub PdbPathNotEmittedWithoutPdb()
            Dim dir = Temp.CreateDirectory()

            Dim src = MakeTrivialExe(directory:=dir.Path)
            Dim args = {"/nologo", src, "/out:a.exe", "/debug-"}
            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)

                Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path, args)
                Dim exitCode = vbc.Run(outWriter)
                Assert.Equal(0, exitCode)

                Dim exePath = Path.Combine(dir.Path, "a.exe")
                Assert.True(File.Exists(exePath))
                Using peStream = File.OpenRead(exePath)
                    Using peReader = New PEReader(peStream)
                        Dim debugDirectory = peReader.PEHeaders.PEHeader.DebugTableDirectory
                        Assert.Equal(0, debugDirectory.Size)
                        Assert.Equal(0, debugDirectory.RelativeVirtualAddress)
                    End Using
                End Using
            End Using
        End Sub

        <Fact>
        Public Sub StrongNameProviderWithCustomTempPath()
            Dim tempDir = Temp.CreateDirectory()
            Dim workingDir = Temp.CreateDirectory()
            workingDir.CreateFile("a.vb")

            Dim vbc = New MockVisualBasicCompiler(Nothing, New BuildPaths("", workingDir.Path, Nothing, tempDir.Path), {"/features:UseLegacyStrongNameProvider", "/nostdlib", "a.vb"})
            Using writer As New StringWriter()
                Dim comp = vbc.CreateCompilation(writer, New TouchedFileLogger(), errorLogger:=Nothing)
                Dim desktopProvider = Assert.IsType(Of DesktopStrongNameProvider)(comp.Options.StrongNameProvider)
                Using inputStream = Assert.IsType(Of DesktopStrongNameProvider.TempFileStream)(desktopProvider.CreateInputStream())
                    Assert.Equal(tempDir.Path, Path.GetDirectoryName(inputStream.Path))
                End Using
            End Using

        End Sub

        Private Function MakeTrivialExe(Optional directory As String = Nothing) As String
            Return Temp.CreateFile(directory:=directory, prefix:="", extension:=".vb").WriteAllText("
Class Program
    Public Shared Sub Main()
    End Sub
End Class").Path
        End Function
    End Class

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend MustInherit Class MockAbstractDiagnosticAnalyzer
        Inherits DiagnosticAnalyzer

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterCompilationStartAction(
                Sub(startContext As CompilationStartAnalysisContext)
                    startContext.RegisterCompilationEndAction(AddressOf AnalyzeCompilation)
                    CreateAnalyzerWithinCompilation(startContext)
                End Sub)
        End Sub

        Public MustOverride Sub CreateAnalyzerWithinCompilation(context As CompilationStartAnalysisContext)
        Public MustOverride Sub AnalyzeCompilation(context As CompilationAnalysisContext)
    End Class

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class HiddenDiagnosticAnalyzer
        Inherits MockAbstractDiagnosticAnalyzer

        Friend Shared ReadOnly Hidden01 As DiagnosticDescriptor = New DiagnosticDescriptor("Hidden01", "", "Throwing a diagnostic for #ExternalSource", "", DiagnosticSeverity.Hidden, isEnabledByDefault:=True)

        Public Overrides Sub CreateAnalyzerWithinCompilation(context As CompilationStartAnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.ExternalSourceDirectiveTrivia)
        End Sub

        Public Overrides Sub AnalyzeCompilation(context As CompilationAnalysisContext)
        End Sub

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Hidden01)
            End Get
        End Property

        Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
            context.ReportDiagnostic(Diagnostic.Create(Hidden01, context.Node.GetLocation()))
        End Sub
    End Class

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class InfoDiagnosticAnalyzer
        Inherits MockAbstractDiagnosticAnalyzer

        Friend Shared ReadOnly Info01 As DiagnosticDescriptor = New DiagnosticDescriptor("Info01", "", "Throwing a diagnostic for #Enable", "", DiagnosticSeverity.Info, isEnabledByDefault:=True)
        Friend Shared ReadOnly Info02 As DiagnosticDescriptor = New DiagnosticDescriptor("Info02", "", "Throwing a diagnostic for something else", "", DiagnosticSeverity.Info, isEnabledByDefault:=True)

        Public Overrides Sub CreateAnalyzerWithinCompilation(context As CompilationStartAnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.EnableWarningDirectiveTrivia)
        End Sub

        Public Overrides Sub AnalyzeCompilation(context As CompilationAnalysisContext)
        End Sub

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Info01, Info02)
            End Get
        End Property

        Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
            context.ReportDiagnostic(Diagnostic.Create(Info01, context.Node.GetLocation()))
        End Sub
    End Class

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class WarningDiagnosticAnalyzer
        Inherits MockAbstractDiagnosticAnalyzer

        Friend Shared ReadOnly Warning01 As DiagnosticDescriptor = New DiagnosticDescriptor("Warning01", "", "Throwing a diagnostic for types declared", "", DiagnosticSeverity.Warning, isEnabledByDefault:=True)
        Friend Shared ReadOnly Warning02 As DiagnosticDescriptor = New DiagnosticDescriptor("Warning02", "", "Throwing a diagnostic for something else", "", DiagnosticSeverity.Warning, isEnabledByDefault:=True)
        Friend Shared ReadOnly Warning03 As DiagnosticDescriptor = New DiagnosticDescriptor("Warning03", "", "Throwing a diagnostic for types declared", "", DiagnosticSeverity.Warning, isEnabledByDefault:=True)

        Public Overrides Sub CreateAnalyzerWithinCompilation(context As CompilationStartAnalysisContext)
            context.RegisterSymbolAction(AddressOf AnalyzeSymbol, SymbolKind.NamedType)
        End Sub

        Public Overrides Sub AnalyzeCompilation(context As CompilationAnalysisContext)
        End Sub

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Warning01, Warning02, Warning03)
            End Get
        End Property

        Public Sub AnalyzeSymbol(context As SymbolAnalysisContext)
            context.ReportDiagnostic(Diagnostic.Create(Warning01, context.Symbol.Locations.First()))
            context.ReportDiagnostic(Diagnostic.Create(Warning03, context.Symbol.Locations.First()))
        End Sub
    End Class

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class ErrorDiagnosticAnalyzer
        Inherits MockAbstractDiagnosticAnalyzer

        Friend Shared ReadOnly Error01 As DiagnosticDescriptor = New DiagnosticDescriptor("Error01", "", "Throwing a diagnostic for #Disable", "", DiagnosticSeverity.Error, isEnabledByDefault:=True)

        Public Overrides Sub CreateAnalyzerWithinCompilation(context As CompilationStartAnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.DisableWarningDirectiveTrivia)
        End Sub

        Public Overrides Sub AnalyzeCompilation(context As CompilationAnalysisContext)
        End Sub

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Error01)
            End Get
        End Property

        Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
            context.ReportDiagnostic(Diagnostic.Create(Error01, context.Node.GetLocation()))
        End Sub
    End Class

    Friend Class AdditionalFileDiagnosticAnalyzer
        Inherits MockAbstractDiagnosticAnalyzer

        Friend Shared ReadOnly Rule As DiagnosticDescriptor = New DiagnosticDescriptor("AdditionalFileDiagnostic", "", "Additional File Diagnostic: {0}", "", DiagnosticSeverity.Warning, isEnabledByDefault:=True)
        Private ReadOnly _nonCompilerInputFile As String

        Public Sub New(nonCompilerInputFile As String)
            _nonCompilerInputFile = nonCompilerInputFile
        End Sub

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub AnalyzeCompilation(context As CompilationAnalysisContext)
        End Sub

        Public Overrides Sub CreateAnalyzerWithinCompilation(context As CompilationStartAnalysisContext)
            context.RegisterCompilationEndAction(AddressOf CompilationEndAction)
        End Sub

        Private Sub CompilationEndAction(context As CompilationAnalysisContext)
            ' Diagnostic reported on additionals file, with valid span.
            For Each additionalFile In context.Options.AdditionalFiles
                ReportDiagnostic(additionalFile.Path, context)
            Next

            ' Diagnostic reported on an additional file, but with an invalid span.
            ReportDiagnostic(context.Options.AdditionalFiles.First().Path, context, New TextSpan(0, 1000000)) ' Overflow span

            ' Diagnostic reported on a file which is not an input for the compiler.
            ReportDiagnostic(_nonCompilerInputFile, context)

            ' Diagnostic reported on a non-existent file.
            ReportDiagnostic("NonExistentPath", context)
        End Sub

        Private Sub ReportDiagnostic(path As String, context As CompilationAnalysisContext, Optional span As TextSpan = Nothing)
            If span = Nothing Then
                span = New TextSpan(0, 11)
            End If

            Dim linePosSpan = New LinePositionSpan(New LinePosition(0, 0), New LinePosition(0, span.End))
            Dim diagLocation = Location.Create(path, span, linePosSpan)
            Dim diag = Diagnostic.Create(Rule, diagLocation, IO.Path.GetFileNameWithoutExtension(path))
            context.ReportDiagnostic(diag)
        End Sub
    End Class
End Namespace

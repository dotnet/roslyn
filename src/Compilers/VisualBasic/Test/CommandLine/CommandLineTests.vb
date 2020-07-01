﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.IO.MemoryMappedFiles
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

        Private Shared ReadOnly s_basicCompilerExecutable As String = Path.Combine(
            Path.GetDirectoryName(GetType(CommandLineTests).Assembly.Location),
            Path.Combine("dependency", "vbc.exe"))
        Private Shared ReadOnly s_DotnetCscRun As String = If(ExecutionConditionUtil.IsMono, "mono", String.Empty)

        Private ReadOnly _baseDirectory As String = TempRoot.Root
        Private Shared ReadOnly s_defaultSdkDirectory As String = RuntimeEnvironment.GetRuntimeDirectory()
        Private Shared ReadOnly s_compilerVersion As String = CommonCompiler.GetProductVersion(GetType(CommandLineTests))

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

        <Fact>
        Public Sub SimpleAnalyzerConfig()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("test.vb").WriteAllText("
Class C
     Sub M()
        Dim x As Integer
    End Sub
End Class")
            Dim additionalFile = dir.CreateFile("file.txt")
            Dim analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText("
[*.vb]
dotnet_diagnostic.bc42024.severity = none")
            Dim cmd = New MockVisualBasicCompiler(Nothing, dir.Path, {
                "/nologo",
                "/t:library",
                "/preferreduilang:en",
                "/analyzerconfig:" + analyzerConfig.Path,
                src.Path})

            Assert.Equal(analyzerConfig.Path, Assert.Single(cmd.Arguments.AnalyzerConfigPaths))

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim exitCode = cmd.Run(outWriter)
            Assert.Equal(0, exitCode)
            Assert.Equal("", outWriter.ToString())

            Assert.Null(cmd.AnalyzerOptions)
        End Sub

        <Fact>
        Public Sub AnalyzerConfigWithOptions()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("test.vb").WriteAllText("
Class C
     Sub M()
        Dim x As Integer
    End Sub
End Class")
            Dim additionalFile = dir.CreateFile("file.txt")
            Dim analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText("
[*.vb]
dotnet_diagnostic.bc42024.severity = none
dotnet_diagnostic.warning01.severity = none
dotnet_diagnostic.Warning03.severity = none
my_option = my_val

[*.txt]
dotnet_diagnostic.bc42024.severity = none
my_option2 = my_val2")
            Dim cmd = New MockVisualBasicCompiler(Nothing, dir.Path, {
                "/nologo",
                "/t:library",
                "/preferreduilang:en",
                "/analyzerconfig:" + analyzerConfig.Path,
                "/analyzer:" + Assembly.GetExecutingAssembly().Location,
                "/nowarn:42376",
                "/additionalfile:" + additionalFile.Path,
                src.Path})

            Assert.Equal(analyzerConfig.Path, Assert.Single(cmd.Arguments.AnalyzerConfigPaths))

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim exitCode = cmd.Run(outWriter)
            Assert.Equal(0, exitCode)
            Assert.Equal("", outWriter.ToString())

            Dim comp = cmd.Compilation
            Dim tree = comp.SyntaxTrees.Single()
            AssertEx.SetEqual({
                KeyValuePairUtil.Create("bc42024", ReportDiagnostic.Suppress),
                KeyValuePairUtil.Create("warning01", ReportDiagnostic.Suppress),
                KeyValuePairUtil.Create("warning03", ReportDiagnostic.Suppress)
                }, tree.DiagnosticOptions)

            Dim provider = cmd.AnalyzerOptions.AnalyzerConfigOptionsProvider
            Dim options = provider.GetOptions(tree)
            Assert.NotNull(options)
            Dim val As String = Nothing
            Assert.True(options.TryGetValue("my_option", val))
            Assert.Equal("my_val", val)
            Assert.False(options.TryGetValue("my_option2", Nothing))
            Assert.False(options.TryGetValue("dotnet_diagnostic.bc42024.severity", Nothing))

            options = provider.GetOptions(cmd.AnalyzerOptions.AdditionalFiles.Single())
            Assert.NotNull(options)
            Assert.True(options.TryGetValue("my_option2", val))
            Assert.Equal("my_val2", val)
            Assert.False(options.TryGetValue("my_option", Nothing))
            Assert.False(options.TryGetValue("dotnet_diagnostic.bc42024.severity", Nothing))
        End Sub

        <Fact>
        Public Sub AnalyzerConfigBadSeverity()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("test.vb").WriteAllText("
Class C
     Sub M()
        Dim x As Integer
    End Sub
End Class")
            Dim analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText("
[*.vb]
dotnet_diagnostic.BC42024.severity = garbage")
            Dim cmd = New MockVisualBasicCompiler(Nothing, dir.Path, {
                "/nologo",
                "/t:library",
                "/preferreduilang:en",
                "/analyzerconfig:" + analyzerConfig.Path,
                src.Path})

            Assert.Equal(analyzerConfig.Path, Assert.Single(cmd.Arguments.AnalyzerConfigPaths))

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim exitCode = cmd.Run(outWriter)
            Assert.Equal(0, exitCode)
            Assert.Equal(
$"vbc : warning InvalidSeverityInAnalyzerConfig: The diagnostic 'bc42024' was given an invalid severity 'garbage' in the analyzer config file at '{analyzerConfig.Path}'.
{src.Path}(4) : warning BC42024: Unused local variable: 'x'.

        Dim x As Integer
            ~           
", outWriter.ToString())
        End Sub

        <Fact>
        Public Sub AnalyzerConfigsInSameDir()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("test.cs").WriteAllText("
Class C
     Sub M()
        Dim x As Integer
    End Sub
End Class")
            Dim configText = "
[*.cs]
dotnet_diagnostic.cs0169.severity = suppress"

            Dim analyzerConfig1 = dir.CreateFile("analyzerconfig1").WriteAllText(configText)
            Dim analyzerConfig2 = dir.CreateFile("analyzerconfig2").WriteAllText(configText)

            Dim cmd = New MockVisualBasicCompiler(Nothing, dir.Path, {
                "/nologo",
                "/t:library",
                "/preferreduilang:en",
                "/analyzerconfig:" + analyzerConfig1.Path,
                "/analyzerconfig:" + analyzerConfig2.Path,
                src.Path
            })

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim exitCode = cmd.Run(outWriter)
            Assert.Equal(1, exitCode)
            Assert.Equal(
                $"vbc : error BC42500: Multiple analyzer config files cannot be in the same directory ('{dir.Path}').",
                outWriter.ToString().TrimEnd())
        End Sub

        <Fact>
        <WorkItem(34101, "https://github.com/dotnet/roslyn/issues/34101")>
        Public Sub SuppressedWarnAsErrorsStillEmit()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("temp.vb").WriteAllText("
#Disable Warning BC42302
Module Module1
    Sub Main()
        Dim x = 42 ''' <test />
    End Sub
End Module")
            Const docName As String = "doc.xml"

            Dim cmd = New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/errorlog:errorlog", $"/doc:{docName}", "/warnaserror", src.Path})

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim exitCode = cmd.Run(outWriter)
            Assert.Equal("", outWriter.ToString())
            Assert.Equal(0, exitCode)

            Dim exePath = Path.Combine(dir.Path, "temp.exe")
            Assert.True(File.Exists(exePath))
        End Sub

        <Fact>
        Public Sub XmlMemoryMapped()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("temp.cs").WriteAllText("
Class C
End Class")
            Dim docName As String = "doc.xml"

            Dim cmd = New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/t:library", "/preferreduilang:en", $"/doc:{docName}", src.Path})

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim exitCode = cmd.Run(outWriter)
            Assert.Equal(0, exitCode)
            Assert.Equal("", outWriter.ToString())

            Dim xmlPath = Path.Combine(dir.Path, docName)
            Using fileStream = New FileStream(xmlPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                Using mmf = MemoryMappedFile.CreateFromFile(fileStream, "xmlMap", 0, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen:=True)
                    exitCode = cmd.Run(outWriter)
                    Assert.Equal(1, exitCode)
                    Assert.StartsWith($"vbc : error BC2012: can't open '{xmlPath}' for writing:", outWriter.ToString())
                End Using
            End Using
        End Sub

        <Fact>
        <WorkItem(946954, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/946954")>
        Public Sub CompilerBinariesAreAnyCPU()
            Assert.Equal(ProcessorArchitecture.MSIL, AssemblyName.GetAssemblyName(s_basicCompilerExecutable).ProcessorArchitecture)
        End Sub

        <Fact, WorkItem(546322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546322")>
        Public Sub NowarnWarnaserrorTest()
            Dim src As String = Temp.CreateFile().WriteAllText(<text>
Class C
End Class
</text>.Value).Path
            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/t:library", "/nowarn", "/warnaserror-", src})
            Assert.Equal(cmd.Arguments.CompilationOptions.GeneralDiagnosticOption, ReportDiagnostic.Suppress)

            cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/t:library", "/nowarn", "/warnaserror", src})
            Assert.Equal(cmd.Arguments.CompilationOptions.GeneralDiagnosticOption, ReportDiagnostic.Error)

            cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/t:library", "/nowarn", "/warnaserror+", src})
            Assert.Equal(cmd.Arguments.CompilationOptions.GeneralDiagnosticOption, ReportDiagnostic.Error)

            cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/t:library", "/warnaserror-", "/nowarn", src})
            Assert.Equal(cmd.Arguments.CompilationOptions.GeneralDiagnosticOption, ReportDiagnostic.Suppress)

            cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/t:library", "/warnaserror", "/nowarn", src})
            Assert.Equal(cmd.Arguments.CompilationOptions.GeneralDiagnosticOption, ReportDiagnostic.Suppress)

            cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/t:library", "/warnaserror+", "/nowarn", src})
            Assert.Equal(cmd.Arguments.CompilationOptions.GeneralDiagnosticOption, ReportDiagnostic.Suppress)


            CleanupAllGeneratedFiles(src)
        End Sub

        <Fact>
        <WorkItem(21508, "https://github.com/dotnet/roslyn/issues/21508")>
        Public Sub ArgumentStartWithDashAndContainingSlash()
            Dim args As VisualBasicCommandLineArguments
            Dim folder = Temp.CreateDirectory()

            args = DefaultParse({"-debug+/debug:portable"}, folder.Path)
            args.Errors.AssertTheseDiagnostics(<errors>
BC2007: unrecognized option '-debug+/debug:portable'; ignored
BC2008: no input sources specified
                                               </errors>)
        End Sub

        <WorkItem(545247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545247")>
        <Fact()>
        Public Sub CommandLineCompilationWithQuotedMainArgument()
            ' Arguments with quoted rootnamespace and main type are unquoted when
            ' the arguments are read in by the command line compiler.
            Dim src As String = Temp.CreateFile().WriteAllText(<text>
Module Module1
    Sub Main()
    
    End Sub
End Module
</text>.Value).Path

            Dim output As New StringWriter()
            Dim vbc As New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/target:exe", "/rootnamespace:""test""", "/main:""test.Module1""", src})

            Dim exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Equal("", output.ToString().Trim())
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
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source)

            Dim cmd = New MockVisualBasicCompiler(dir.Path, {"/nologo", "a.vb", "/keyfile:key.snk"})
            Dim comp = cmd.CreateCompilation(TextWriter.Null, New TouchedFileLogger(), NullErrorLogger.Instance, Nothing)

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
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source)

            Dim cmd = New MockVisualBasicCompiler(dir.Path, {"/nologo", "a.vb", "/keycontainer:aaa"})
            Dim comp = cmd.CreateCompilation(TextWriter.Null, New TouchedFileLogger(), NullErrorLogger.Instance, Nothing)

            Assert.True(TypeOf comp.Options.StrongNameProvider Is DesktopStrongNameProvider)
        End Sub

        <Fact>
        Public Sub CreateCompilationWithStrongNameFallbackCommand()
            Dim source = "
Public Class C
    Public Shared Sub Main()
    End Sub
End Class"

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source)

            Dim cmd = New MockVisualBasicCompiler(dir.Path, {"/nologo", "a.vb", "/features:UseLegacyStrongNameProvider"})
            Dim comp = cmd.CreateCompilation(TextWriter.Null, New TouchedFileLogger(), NullErrorLogger.Instance, Nothing)

            Assert.True(TypeOf comp.Options.StrongNameProvider Is DesktopStrongNameProvider)
        End Sub

        <Fact>
        Public Sub ParseQuotedMainTypeAndRootnamespace()
            'These options are always unquoted when parsed in VisualBasicCommandLineParser.Parse.

            Dim args = DefaultParse({"/rootnamespace:Test", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal("Test", args.CompilationOptions.RootNamespace)

            args = DefaultParse({"/main:Test", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal("Test", args.CompilationOptions.MainTypeName)

            args = DefaultParse({"/main:""Test""", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal("Test", args.CompilationOptions.MainTypeName)

            args = DefaultParse({"/rootnamespace:""Test""", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal("Test", args.CompilationOptions.RootNamespace)

            args = DefaultParse({"/rootnamespace:""test""", "/main:""test.Module1""", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal("test.Module1", args.CompilationOptions.MainTypeName)
            Assert.Equal("test", args.CompilationOptions.RootNamespace)

            ' Use of Cyrillic namespace
            args = DefaultParse({"/rootnamespace:""решения""", "/main:""решения.Module1""", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal("решения.Module1", args.CompilationOptions.MainTypeName)
            Assert.Equal("решения", args.CompilationOptions.RootNamespace)

        End Sub

        <WorkItem(722561, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/722561")>
        <Fact>
        Public Sub Bug_722561()
            Dim src As String = Temp.CreateFile().WriteAllText(<text>
Public Class C
End Class
</text>.Value).Path

            ' Previous versions of the compiler used to report warnings (BC2026, BC2014)
            ' whenever an unrecognized warning code was supplied via /nowarn or /warnaserror.
            ' We no longer generate a warning in such cases.
            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/t:library", "/nowarn:-1", src})
            Dim writer As New StringWriter()
            Dim result = cmd.Run(writer, Nothing)

            Assert.Equal(String.Empty, writer.ToString.Trim)

            cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/t:library", "/nowarn:-12345678901234567890", src})
            writer = New StringWriter()
            result = cmd.Run(writer, Nothing)

            Assert.Equal(String.Empty, writer.ToString.Trim)

            cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/t:library", "/nowarn:-1234567890123456789", src})
            writer = New StringWriter()
            result = cmd.Run(writer, Nothing)

            Assert.Equal(String.Empty, writer.ToString.Trim)

            CleanupAllGeneratedFiles(src)
        End Sub

        <Fact>
        Public Sub VbcTest()
            Dim output As StringWriter = New StringWriter()

            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/preferreduilang:en"})
            cmd.Run(output, Nothing)

            Assert.True(output.ToString().StartsWith(s_logoLine1, StringComparison.Ordinal), "vbc should print logo and help if no args specified")
        End Sub

        <Fact>
        Public Sub VbcNologo_1()
            Dim src As String = Temp.CreateFile().WriteAllText(<text>
Class C
End Class
</text>.Value).Path

            Dim output As StringWriter = New StringWriter()

            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/t:library", src})
            Dim exitCode = cmd.Run(output, Nothing)

            Assert.Equal(0, exitCode)
            Assert.Equal("", output.ToString().Trim())


            CleanupAllGeneratedFiles(src)
        End Sub

        <Fact>
        Public Sub VbcNologo_1a()
            Dim src As String = Temp.CreateFile().WriteAllText(<text>
Class C
End Class
</text>.Value).Path

            Dim output As StringWriter = New StringWriter()

            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo+", "/t:library", src})
            Dim exitCode = cmd.Run(output, Nothing)

            Assert.Equal(0, exitCode)
            Assert.Equal("", output.ToString().Trim())


            CleanupAllGeneratedFiles(src)
        End Sub

        <Fact>
        Public Sub VbcNologo_2()
            Dim src As String = Temp.CreateFile().WriteAllText(<text>
Class C
End Class
</text>.Value).Path

            Dim output As StringWriter = New StringWriter()

            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/t:library", "/preferreduilang:en", src})
            Dim exitCode = cmd.Run(output, Nothing)

            Assert.Equal(0, exitCode)
            Dim patched As String = Regex.Replace(output.ToString().Trim(), "version \d+\.\d+\.\d+(-[\d\w]+)*", "version A.B.C-d")
            patched = ReplaceCommitHash(patched)
            Assert.Equal(<text>
Microsoft (R) Visual Basic Compiler version A.B.C-d (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.
</text>.Value.Replace(vbLf, vbCrLf).Trim,
                patched)

            CleanupAllGeneratedFiles(src)
        End Sub

        <Theory,
            InlineData("Microsoft (R) Visual Basic Compiler version A.B.C-d (<developer build>)",
                       "Microsoft (R) Visual Basic Compiler version A.B.C-d (HASH)"),
            InlineData("Microsoft (R) Visual Basic Compiler version A.B.C-d (ABCDEF01)",
                       "Microsoft (R) Visual Basic Compiler version A.B.C-d (HASH)"),
            InlineData("Microsoft (R) Visual Basic Compiler version A.B.C-d (abcdef90)",
                       "Microsoft (R) Visual Basic Compiler version A.B.C-d (HASH)"),
            InlineData("Microsoft (R) Visual Basic Compiler version A.B.C-d (12345678)",
                       "Microsoft (R) Visual Basic Compiler version A.B.C-d (HASH)")>
        Public Sub TestReplaceCommitHash(orig As String, expected As String)
            Assert.Equal(expected, ReplaceCommitHash(orig))
        End Sub

        Private Shared Function ReplaceCommitHash(s As String) As String
            Return Regex.Replace(s, "(\((<developer build>|[a-fA-F0-9]{8})\))", "(HASH)")
        End Function

        <Fact>
        Public Sub VbcNologo_2a()
            Dim src As String = Temp.CreateFile().WriteAllText(<text>
Class C
End Class
</text>.Value).Path

            Dim output As StringWriter = New StringWriter()

            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo-", "/preferreduilang:en", "/t:library", src})
            Dim exitCode = cmd.Run(output, Nothing)

            Assert.Equal(0, exitCode)
            Dim patched As String = Regex.Replace(output.ToString().Trim(), "version \d+\.\d+\.\d+(-[\w\d]+)*", "version A.B.C-d")
            patched = ReplaceCommitHash(patched)
            Assert.Equal(<text>
Microsoft (R) Visual Basic Compiler version A.B.C-d (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.
</text>.Value.Replace(vbLf, vbCrLf).Trim,
                patched)

            CleanupAllGeneratedFiles(src)
        End Sub

        <Fact()>
        Public Sub VbcUtf8Output_WithRedirecting_Off()
            Dim src As String = Temp.CreateFile().WriteAllText("♚", New System.Text.UTF8Encoding(False)).Path

            Dim tempOut = Temp.CreateFile()

            Dim output = ProcessUtilities.RunAndGetOutput("cmd", "/C """ & s_basicCompilerExecutable & """ /nologo /preferreduilang:en /t:library " & src & " > " & tempOut.Path, expectedRetCode:=1)
            Assert.Equal("", output.Trim())

            Assert.Equal(<text>
SRC.VB(1) : error BC30037: Character is not valid.

?
~
</text>.Value.Trim().Replace(vbLf, vbCrLf), tempOut.ReadAllText().Trim().Replace(src, "SRC.VB"))

            CleanupAllGeneratedFiles(src)
        End Sub

        <Fact()>
        Public Sub VbcUtf8Output_WithRedirecting_On()
            Dim src As String = Temp.CreateFile().WriteAllText("♚", New System.Text.UTF8Encoding(False)).Path

            Dim tempOut = Temp.CreateFile()

            Dim output = ProcessUtilities.RunAndGetOutput("cmd", "/C """ & s_basicCompilerExecutable & """ /utf8output /nologo /preferreduilang:en /t:library " & src & " > " & tempOut.Path, expectedRetCode:=1)
            Assert.Equal("", output.Trim())

            Assert.Equal(<text>
SRC.VB(1) : error BC30037: Character is not valid.

♚
~
</text>.Value.Trim().Replace(vbLf, vbCrLf), tempOut.ReadAllText().Trim().Replace(src, "SRC.VB"))


            CleanupAllGeneratedFiles(src)
        End Sub

        <Fact>
        Public Sub VbcCompile_WithSourceCodeRedirectedViaStandardInput_ProducesRunnableProgram()
            Dim result As ProcessResult
            Dim tempDir As String = Temp.CreateDirectory().Path

            If RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then
                Dim sourceFile = Path.GetTempFileName()
                File.WriteAllText(sourceFile, "
Module Program
    Sub Main()
        System.Console.WriteLine(""Hello World!"")
    End Sub
End Module")
                result = ProcessUtilities.Run("cmd", $"/C {s_basicCompilerExecutable} /nologo /t:exe - < {sourceFile}", workingDirectory:=tempDir)

                File.Delete(sourceFile)
            Else
                result = ProcessUtilities.Run("/usr/bin/env", $"sh -c ""echo \
Module Program                                                               \
    Sub Main\(\)                                                             \
        System.Console.WriteLine\(\\\""Hello World\!\\\""\)                  \
    End Sub                                                                  \
End Module | {s_basicCompilerExecutable} /nologo /t:exe -""", workingDirectory:=tempDir,
                redirectStandardInput:=True)
                ' we are testing shell's piped/redirected stdin behavior explicitly
                ' instead of using Process.StandardInput.Write(), so we set
                ' redirectStandardInput to true, which implies that isatty of child
                ' process is false and thereby Console.IsInputRedirected will return
                ' true in vbc code.
            End If

            Assert.False(result.ContainsErrors, $"Compilation error(s) occurred: {result.Output} {result.Errors}")

            Dim output As String = If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                ProcessUtilities.RunAndGetOutput("cmd.exe", $"/C ""{s_DotnetCscRun} -.exe""", expectedRetCode:=0, startFolder:=tempDir),
                ProcessUtilities.RunAndGetOutput("sh", $"-c ""{s_DotnetCscRun} -.exe""", expectedRetCode:=0, startFolder:=tempDir))

            Assert.Equal("Hello World!", output.Trim())
        End Sub

        <Fact>
        Public Sub VbcCompile_WithSourceCodeRedirectedViaStandardInput_ProducesLibrary()
            Dim name = Guid.NewGuid().ToString() & ".dll"
            Dim tempDir As String = Temp.CreateDirectory().Path
            Dim result As ProcessResult

            If RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then
                Dim sourceFile = Path.GetTempFileName()
                File.WriteAllText(sourceFile, "
Class A
    public Function GetVal() As A
        Return Nothing
    End Function
End Class")
                result = ProcessUtilities.Run("cmd", $"/C {s_basicCompilerExecutable} /nologo /t:library /out:{name} - < {sourceFile}", workingDirectory:=tempDir)

                File.Delete(sourceFile)
            Else
                result = ProcessUtilities.Run("/usr/bin/env", $"sh -c ""echo \
Class A                                                                      \
    Public Function GetVal\(\) As A                                          \
        Return Nothing                                                       \
    End Function                                                             \
End Class | {s_basicCompilerExecutable} /nologo /t:library /out:{name} -""", workingDirectory:=tempDir,
                redirectStandardInput:=True)
                ' we are testing shell's piped/redirected stdin behavior explicitly
                ' instead of using Process.StandardInput.Write(), so we set
                ' redirectStandardInput to true, which implies that isatty of child
                ' process is false and thereby Console.IsInputRedirected will return
                ' true in vbc code.
            End If

            Assert.False(result.ContainsErrors, $"Compilation error(s) occurred: {result.Output} {result.Errors}")

            Dim assemblyName = System.Reflection.AssemblyName.GetAssemblyName(Path.Combine(tempDir, name))
            Assert.Equal(name.Replace(".dll", ", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                         assemblyName.ToString())
        End Sub

        <Fact>
        Public Sub VbcCompile_WithRedirectedInputIndicatorAndStandardInputNotRedirected_ReportsBC56032()
            If Console.IsInputRedirected Then
                ' [applicable to both Windows and Unix]
                ' if our parent (xunit) process itself has input redirected, we cannot test this
                ' error case because our child process will inherit it and we cannot achieve what
                ' we are aiming for: isatty(0):true and thereby Console.IsInputerRedirected:false in
                ' child. running this case will make StreamReader to hang (waiting for input, that
                ' we do not propagate: parent.In->child.In).
                '
                ' note: in Unix we can "close" fd0 by appending `0>&-` in the `sh -c` command below,
                ' but that will also not impact the result of isatty(), and in turn causes a different
                ' compiler error.
                Return
            End If

            Dim tempDir As String = Temp.CreateDirectory().Path
            Dim result As ProcessResult = If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                ProcessUtilities.Run("cmd", $"/C ""{s_basicCompilerExecutable} /nologo /t:exe -""", workingDirectory:=tempDir),
                ProcessUtilities.Run("/usr/bin/env", $"sh -c ""{s_basicCompilerExecutable} /nologo /t:exe -""", workingDirectory:=tempDir))

            Assert.True(result.ContainsErrors)
            Assert.Contains(CInt(ERRID.ERR_StdInOptionProvidedButConsoleInputIsNotRedirected).ToString(), result.Output)
        End Sub

        <Fact()>
        Public Sub ResponseFiles1()
            Dim rsp As String = Temp.CreateFile().WriteAllText(<text>
/r:System.dll
/nostdlib
/vbruntime-
# this is ignored
System.Console.WriteLine(&quot;*?&quot;);  # this is error
a.vb
</text>.Value).Path
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
            Dim rsp As String = Temp.CreateFile().WriteAllText(<text>
/r:System.dll
/rootnamespace:"Hello"
a.vb
</text>.Value).Path
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

        <Fact>
        Public Sub ParseInstrumentTestNames()
            Dim args As VisualBasicCommandLineArguments

            args = DefaultParse({}, _baseDirectory)
            Assert.True(args.EmitOptions.InstrumentationKinds.SequenceEqual({}))

            args = DefaultParse({"/instrument", "a.vb"}, _baseDirectory)
            args.Errors.Verify({Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("instrument", ":<string>").WithLocation(1, 1)})
            Assert.True(args.EmitOptions.InstrumentationKinds.SequenceEqual({}))

            args = DefaultParse({"/instrument:""""", "a.vb"}, _baseDirectory)
            args.Errors.Verify({Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("instrument", ":<string>").WithLocation(1, 1)})
            Assert.True(args.EmitOptions.InstrumentationKinds.SequenceEqual({}))

            args = DefaultParse({"/instrument:", "a.vb"}, _baseDirectory)
            args.Errors.Verify({Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("instrument", ":<string>").WithLocation(1, 1)})
            Assert.True(args.EmitOptions.InstrumentationKinds.SequenceEqual({}))

            args = DefaultParse({"/instrument:", "Test.Flag.Name", "a.vb"}, _baseDirectory)
            args.Errors.Verify({Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("instrument", ":<string>").WithLocation(1, 1)})
            Assert.True(args.EmitOptions.InstrumentationKinds.SequenceEqual({}))

            args = DefaultParse({"/instrument:InvalidOption", "a.vb"}, _baseDirectory)
            args.Errors.Verify({Diagnostic(ERRID.ERR_InvalidInstrumentationKind).WithArguments("InvalidOption").WithLocation(1, 1)})
            Assert.True(args.EmitOptions.InstrumentationKinds.SequenceEqual({}))

            args = DefaultParse({"/instrument:None", "a.vb"}, _baseDirectory)
            args.Errors.Verify({Diagnostic(ERRID.ERR_InvalidInstrumentationKind).WithArguments("None").WithLocation(1, 1)})
            Assert.True(args.EmitOptions.InstrumentationKinds.SequenceEqual({}))

            args = DefaultParse({"/instrument:""TestCoverage,InvalidOption""", "a.vb"}, _baseDirectory)
            args.Errors.Verify({Diagnostic(ERRID.ERR_InvalidInstrumentationKind).WithArguments("InvalidOption").WithLocation(1, 1)})
            Assert.True(args.EmitOptions.InstrumentationKinds.SequenceEqual({InstrumentationKind.TestCoverage}))

            args = DefaultParse({"/instrument:TestCoverage", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.True(args.EmitOptions.InstrumentationKinds.SequenceEqual({InstrumentationKind.TestCoverage}))

            args = DefaultParse({"/instrument:""TestCoverage""", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.True(args.EmitOptions.InstrumentationKinds.SequenceEqual({InstrumentationKind.TestCoverage}))

            args = DefaultParse({"/instrument:""TESTCOVERAGE""", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.True(args.EmitOptions.InstrumentationKinds.SequenceEqual({InstrumentationKind.TestCoverage}))

            args = DefaultParse({"/instrument:TestCoverage,TestCoverage", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.True(args.EmitOptions.InstrumentationKinds.SequenceEqual({InstrumentationKind.TestCoverage}))

            args = DefaultParse({"/instrument:TestCoverage", "/instrument:TestCoverage", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.True(args.EmitOptions.InstrumentationKinds.SequenceEqual({InstrumentationKind.TestCoverage}))
        End Sub

        <Fact>
        Public Sub ResponseFiles2()
            Dim rsp As String = Temp.CreateFile().WriteAllText(<text>
    /r:System
    /r:System.Core
    /r:System.Data
    /r:System.Data.DataSetExtensions
    /r:System.Xml
    /r:System.Xml.Linq
    /imports:System
    /imports:System.Collections.Generic
    /imports:System.Linq
    /imports:System.Text</text>.Value).Path
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

        <Fact, WorkItem(546028, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546028")>
        Public Sub Win32ResourceArguments()
            Dim args As String() = {"/win32manifest:..\here\there\everywhere\nonexistent"}
            Dim parsedArgs = DefaultParse(args, _baseDirectory)
            Dim compilation = CreateCompilationWithMscorlib40(New VisualBasicSyntaxTree() {})
            Dim errors As IEnumerable(Of DiagnosticInfo) = Nothing
            CommonCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, errors)
            Assert.Equal(1, errors.Count())
            Assert.Equal(DirectCast(ERRID.ERR_UnableToReadUacManifest2, Integer), errors.First().Code)
            Assert.Equal(2, errors.First().Arguments.Count())
            args = {"/Win32icon:\bogus"}
            parsedArgs = DefaultParse(args, _baseDirectory)
            CommonCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, errors)
            Assert.Equal(1, errors.Count())
            Assert.Equal(DirectCast(ERRID.ERR_UnableToOpenResourceFile1, Integer), errors.First().Code)
            Assert.Equal(2, errors.First().Arguments.Count())
            args = {"/Win32Resource:\bogus"}
            parsedArgs = DefaultParse(args, _baseDirectory)
            CommonCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, errors)
            Assert.Equal(1, errors.Count())
            Assert.Equal(DirectCast(ERRID.ERR_UnableToOpenResourceFile1, Integer), errors.First().Code)
            Assert.Equal(2, errors.First().Arguments.Count())

            args = {"/win32manifest:goo.win32data:bar.win32data2"}
            parsedArgs = DefaultParse(args, _baseDirectory)
            CommonCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, errors)
            Assert.Equal(1, errors.Count())
            Assert.Equal(DirectCast(ERRID.ERR_UnableToReadUacManifest2, Integer), errors.First().Code)
            Assert.Equal(2, errors.First().Arguments.Count())
            args = {"/Win32icon:goo.win32data:bar.win32data2"}
            parsedArgs = DefaultParse(args, _baseDirectory)
            CommonCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, errors)
            Assert.Equal(1, errors.Count())
            Assert.Equal(DirectCast(ERRID.ERR_UnableToOpenResourceFile1, Integer), errors.First().Code)
            Assert.Equal(2, errors.First().Arguments.Count())
            args = {"/Win32Resource:goo.win32data:bar.win32data2"}
            parsedArgs = DefaultParse(args, _baseDirectory)
            CommonCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, errors)
            Assert.Equal(1, errors.Count())
            Assert.Equal(DirectCast(ERRID.ERR_UnableToOpenResourceFile1, Integer), errors.First().Code)
            Assert.Equal(2, errors.First().Arguments.Count())
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

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim exitCode = New MockVisualBasicCompiler(Nothing, baseDir,
            {
                "/nologo",
                "/preferreduilang:en",
                "/win32resource:" + badres,
                source
            }).Run(outWriter)

            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC30136: Error creating Win32 resources: Unrecognized resource file format.", outWriter.ToString().Trim())

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
            CheckWin32ResourceOptions({"/win32resource"}, Nothing, Nothing, Nothing, False,
                                      Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32resource", ":<file>"))
            CheckWin32ResourceOptions({"/win32resource:"}, Nothing, Nothing, Nothing, False,
                                      Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32resource", ":<file>"))
            CheckWin32ResourceOptions({"/win32resource: "}, Nothing, Nothing, Nothing, False,
                                      Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32resource", ":<file>"))

            CheckWin32ResourceOptions({"/win32icon"}, Nothing, Nothing, Nothing, False,
                                      Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32icon", ":<file>"))
            CheckWin32ResourceOptions({"/win32icon:"}, Nothing, Nothing, Nothing, False,
                                      Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32icon", ":<file>"))
            CheckWin32ResourceOptions({"/win32icon: "}, Nothing, Nothing, Nothing, False,
                                      Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32icon", ":<file>"))

            CheckWin32ResourceOptions({"/win32manifest"}, Nothing, Nothing, Nothing, False,
                                      Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32manifest", ":<file>"))
            CheckWin32ResourceOptions({"/win32manifest:"}, Nothing, Nothing, Nothing, False,
                                      Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32manifest", ":<file>"))
            CheckWin32ResourceOptions({"/win32manifest: "}, Nothing, Nothing, Nothing, False,
                                      Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32manifest", ":<file>"))

            CheckWin32ResourceOptions({"/nowin32manifest"}, Nothing, Nothing, Nothing, True)
            CheckWin32ResourceOptions({"/nowin32manifest:"}, Nothing, Nothing, Nothing, False,
                                      Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/nowin32manifest:"))
            CheckWin32ResourceOptions({"/nowin32manifest: "}, Nothing, Nothing, Nothing, False,
                                      Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/nowin32manifest:"))
        End Sub

        <Fact>
        Public Sub Win32ResourceOptions_Combinations()
            ' last occurrence wins
            CheckWin32ResourceOptions({"/win32resource:r", "/win32resource:s"}, "s", Nothing, Nothing, False)
            ' illegal
            CheckWin32ResourceOptions({"/win32resource:r", "/win32icon:i"}, "r", "i", Nothing, False,
                                      Diagnostic(ERRID.ERR_IconFileAndWin32ResFile))
            ' documented as illegal, but works in dev10
            CheckWin32ResourceOptions({"/win32resource:r", "/win32manifest:m"}, "r", Nothing, "m", False,
                                      Diagnostic(ERRID.ERR_CantHaveWin32ResAndManifest))
            ' fine
            CheckWin32ResourceOptions({"/win32resource:r", "/nowin32manifest"}, "r", Nothing, Nothing, True)


            ' illegal
            CheckWin32ResourceOptions({"/win32icon:i", "/win32resource:r"}, "r", "i", Nothing, False,
                                      Diagnostic(ERRID.ERR_IconFileAndWin32ResFile))
            ' last occurrence wins
            CheckWin32ResourceOptions({"/win32icon:i", "/win32icon:j"}, Nothing, "j", Nothing, False)
            ' fine
            CheckWin32ResourceOptions({"/win32icon:i", "/win32manifest:m"}, Nothing, "i", "m", False)
            ' fine
            CheckWin32ResourceOptions({"/win32icon:i", "/nowin32manifest"}, Nothing, "i", Nothing, True)


            ' documented as illegal, but works in dev10
            CheckWin32ResourceOptions({"/win32manifest:m", "/win32resource:r"}, "r", Nothing, "m", False,
                                      Diagnostic(ERRID.ERR_CantHaveWin32ResAndManifest))
            ' fine
            CheckWin32ResourceOptions({"/win32manifest:m", "/win32icon:i"}, Nothing, "i", "m", False)
            ' last occurrence wins
            CheckWin32ResourceOptions({"/win32manifest:m", "/win32manifest:n"}, Nothing, Nothing, "n", False)
            ' illegal
            CheckWin32ResourceOptions({"/win32manifest:m", "/nowin32manifest"}, Nothing, Nothing, "m", True,
                                      Diagnostic(ERRID.ERR_ConflictingManifestSwitches))


            ' fine
            CheckWin32ResourceOptions({"/nowin32manifest", "/win32resource:r"}, "r", Nothing, Nothing, True)
            ' fine
            CheckWin32ResourceOptions({"/nowin32manifest", "/win32icon:i"}, Nothing, "i", Nothing, True)
            ' illegal
            CheckWin32ResourceOptions({"/nowin32manifest", "/win32manifest:m"}, Nothing, Nothing, "m", True,
                                      Diagnostic(ERRID.ERR_ConflictingManifestSwitches))
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
            diags.Verify(Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments("D:rive\relative\path"))
            diags.Clear()
            Assert.Null(desc)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "inva\l*d?path,someName,public", _baseDirectory, diags, embedded:=False)
            diags.Verify(Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments("inva\l*d?path"))
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

            Dim longI = New String("i"c, 260)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("", String.Format("{0},e,private", longI), _baseDirectory, diags, embedded:=False)
            ' // error BC2032: File name 'iiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
            diags.Verify(Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments("iiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii").WithLocation(1, 1))
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

        <Fact>
        Public Sub ManagedResourceOptions_SimpleErrors()
            Dim parsedArgs = DefaultParse({"/resource:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("resource", ":<resinfo>"))

            parsedArgs = DefaultParse({"/resource: ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("resource", ":<resinfo>"))

            parsedArgs = DefaultParse({"/resource", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("resource", ":<resinfo>"))

            parsedArgs = DefaultParse({"/RES+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/RES+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/res-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/res-:")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/linkresource:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("linkresource", ":<resinfo>"))

            parsedArgs = DefaultParse({"/linkresource: ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("linkresource", ":<resinfo>"))

            parsedArgs = DefaultParse({"/linkresource", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("linkresource", ":<resinfo>"))

            parsedArgs = DefaultParse({"/linkRES+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/linkRES+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/linkres-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/linkres-:")) ' TODO: Dev11 reports ERR_ArgumentRequired
        End Sub

        <Fact>
        Public Sub ModuleManifest()
            Dim parsedArgs = DefaultParse({"/win32manifest:blah", "/target:module", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.WRN_IgnoreModuleManifest))

            ' Illegal, but not clobbered.
            Assert.Equal("blah", parsedArgs.Win32Manifest)
        End Sub

        <Fact>
        Public Sub ArgumentParsing()
            Dim parsedArgs = InteractiveParse({"a + b"}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(True, parsedArgs.SourceFiles.Any())
            parsedArgs = InteractiveParse({"a + b; c"}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(True, parsedArgs.SourceFiles.Any())
            parsedArgs = InteractiveParse({"/help"}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.Equal(True, parsedArgs.DisplayHelp)
            Assert.Equal(False, parsedArgs.SourceFiles.Any())
            parsedArgs = InteractiveParse({"/version"}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.True(parsedArgs.DisplayVersion)
            Assert.Equal(False, parsedArgs.SourceFiles.Any())
            parsedArgs = InteractiveParse({"/version", "c"}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.True(parsedArgs.DisplayVersion)
            Assert.Equal(True, parsedArgs.SourceFiles.Any())
            parsedArgs = InteractiveParse({"/version:something"}, _baseDirectory)
            Assert.Equal(True, parsedArgs.Errors.Any())
            Assert.False(parsedArgs.DisplayVersion)
            parsedArgs = InteractiveParse({"/?"}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.Equal(True, parsedArgs.DisplayHelp)
            Assert.Equal(False, parsedArgs.SourceFiles.Any())
            parsedArgs = InteractiveParse({"@dd"}, _baseDirectory)
            Assert.Equal(True, parsedArgs.Errors.Any())
            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(False, parsedArgs.SourceFiles.Any())
            parsedArgs = InteractiveParse({"c /define:DEBUG"}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(True, parsedArgs.SourceFiles.Any())
            parsedArgs = InteractiveParse({"\\"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments(".exe"))

            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(True, parsedArgs.SourceFiles.Any())
            parsedArgs = InteractiveParse({"""/r d.dll"""}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(True, parsedArgs.SourceFiles.Any())
            parsedArgs = InteractiveParse({"/r: d.dll"}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(False, parsedArgs.SourceFiles.Any())
        End Sub

        <Fact>
        Public Sub LangVersion()
            Dim parsedArgs = DefaultParse({"/langversion:9", "a.VB"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic9, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:9.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic9, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:10", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic10, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:10.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic10, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:11", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic11, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:11.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic11, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:12", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic12, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:12.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic12, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:14", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic14, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:14.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic14, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:15", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic15, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:15.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic15, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:15.3", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic15_3, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:15.5", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic15_5, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:16", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic16, parsedArgs.ParseOptions.LanguageVersion)

            ' The canary check is a reminder that this test needs to be updated when a language version is added
            LanguageVersionAdded_Canary()

            parsedArgs = DefaultParse({"/langVERSION:default", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.Default, parsedArgs.ParseOptions.SpecifiedLanguageVersion)
            Assert.Equal(LanguageVersion.Default.MapSpecifiedToEffectiveVersion(), parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:latest", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.Latest, parsedArgs.ParseOptions.SpecifiedLanguageVersion)
            Assert.Equal(LanguageVersion.Latest.MapSpecifiedToEffectiveVersion(), parsedArgs.ParseOptions.LanguageVersion)

            ' default: "current version"
            parsedArgs = DefaultParse({"a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.Default.MapSpecifiedToEffectiveVersion(), parsedArgs.ParseOptions.LanguageVersion)

            ' overriding
            parsedArgs = DefaultParse({"/langVERSION:10", "/langVERSION:9.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic9, parsedArgs.ParseOptions.LanguageVersion)

            ' errors
            parsedArgs = DefaultParse({"/langVERSION", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("langversion", ":<number>"))
            Assert.Equal(LanguageVersion.Default.MapSpecifiedToEffectiveVersion(), parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/langVERSION+")) ' TODO: Dev11 reports ERR_ArgumentRequired
            Assert.Equal(LanguageVersion.Default.MapSpecifiedToEffectiveVersion(), parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("langversion", ":<number>"))
            Assert.Equal(LanguageVersion.Default.MapSpecifiedToEffectiveVersion(), parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:8", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("langversion", "8"))
            Assert.Equal(LanguageVersion.Default.MapSpecifiedToEffectiveVersion(), parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = DefaultParse({"/langVERSION:" & (LanguageVersion.VisualBasic12 + 1), "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("langversion", CStr(LanguageVersion.VisualBasic12 + 1)))
            Assert.Equal(LanguageVersion.Default.MapSpecifiedToEffectiveVersion(), parsedArgs.ParseOptions.LanguageVersion)
        End Sub

        <Fact>
        Public Sub DelaySign()
            Dim parsedArgs = DefaultParse({"/delaysign", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.NotNull(parsedArgs.CompilationOptions.DelaySign)
            Assert.Equal(True, parsedArgs.CompilationOptions.DelaySign)

            parsedArgs = DefaultParse({"/delaysign+", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.NotNull(parsedArgs.CompilationOptions.DelaySign)
            Assert.Equal(True, parsedArgs.CompilationOptions.DelaySign)

            parsedArgs = DefaultParse({"/DELAYsign-", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.NotNull(parsedArgs.CompilationOptions.DelaySign)
            Assert.Equal(False, parsedArgs.CompilationOptions.DelaySign)

            parsedArgs = DefaultParse({"/delaysign:-", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("delaysign"))

            parsedArgs = InteractiveParse({"/d:a=1"}, _baseDirectory) ' test default value
            parsedArgs.Errors.Verify()
            Assert.Null(parsedArgs.CompilationOptions.DelaySign)
        End Sub

        <WorkItem(546113, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546113")>
        <Fact>
        Public Sub OutputVerbose()
            Dim parsedArgs = DefaultParse({"/verbose", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Verbose, parsedArgs.OutputLevel)

            parsedArgs = DefaultParse({"/verbose+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Verbose, parsedArgs.OutputLevel)

            parsedArgs = DefaultParse({"/verbose-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Normal, parsedArgs.OutputLevel)

            parsedArgs = DefaultParse({"/VERBOSE:-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/VERBOSE:-"))

            parsedArgs = DefaultParse({"/verbose-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("verbose"))

            parsedArgs = DefaultParse({"/verbose+:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("verbose"))

            parsedArgs = DefaultParse({"/verbOSE:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/verbOSE:"))

            parsedArgs = InteractiveParse({"/d:a=1"}, _baseDirectory) ' test default value
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Normal, parsedArgs.OutputLevel)

            parsedArgs = DefaultParse({"/quiet", "/verbose", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Verbose, parsedArgs.OutputLevel)

            parsedArgs = DefaultParse({"/quiet", "/verbose-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Normal, parsedArgs.OutputLevel)
        End Sub

        <WorkItem(546113, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546113")>
        <Fact>
        Public Sub OutputQuiet()
            Dim parsedArgs = DefaultParse({"/quiet", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Quiet, parsedArgs.OutputLevel)

            parsedArgs = DefaultParse({"/quiet+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Quiet, parsedArgs.OutputLevel)

            parsedArgs = DefaultParse({"/quiet-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Normal, parsedArgs.OutputLevel)

            parsedArgs = DefaultParse({"/QUIET:-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/QUIET:-"))

            parsedArgs = DefaultParse({"/quiet-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("quiet"))

            parsedArgs = DefaultParse({"/quiet+:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("quiet"))

            parsedArgs = DefaultParse({"/quiET:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/quiET:"))

            parsedArgs = InteractiveParse({"/d:a=1"}, _baseDirectory) ' test default value
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Normal, parsedArgs.OutputLevel)

            parsedArgs = DefaultParse({"/verbose", "/quiet", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Quiet, parsedArgs.OutputLevel)

            parsedArgs = DefaultParse({"/verbose", "/quiet-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Normal, parsedArgs.OutputLevel)
        End Sub

        <Fact>
        Public Sub Optimize()
            Dim parsedArgs = DefaultParse({"/optimize", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OptimizationLevel.Release, parsedArgs.CompilationOptions.OptimizationLevel)

            parsedArgs = DefaultParse({"a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OptimizationLevel.Debug, parsedArgs.CompilationOptions.OptimizationLevel) ' default

            parsedArgs = DefaultParse({"/OPTIMIZE+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OptimizationLevel.Release, parsedArgs.CompilationOptions.OptimizationLevel)

            parsedArgs = DefaultParse({"/optimize-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OptimizationLevel.Debug, parsedArgs.CompilationOptions.OptimizationLevel)

            parsedArgs = DefaultParse({"/optimize-", "/optimize+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OptimizationLevel.Release, parsedArgs.CompilationOptions.OptimizationLevel)

            parsedArgs = DefaultParse({"/OPTIMIZE:", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optimize"))

            parsedArgs = DefaultParse({"/OPTIMIZE+:", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optimize"))

            parsedArgs = DefaultParse({"/optimize-:", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optimize"))
        End Sub

        <WorkItem(5417, "DevDiv")>
        <Fact>
        Public Sub Deterministic()
            Dim ParsedArgs = DefaultParse({"a.vb"}, _baseDirectory)
            ParsedArgs.Errors.Verify()
            Assert.Equal(False, ParsedArgs.CompilationOptions.Deterministic)

            ParsedArgs = DefaultParse({"/deterministic+", "a.vb"}, _baseDirectory)
            ParsedArgs.Errors.Verify()
            Assert.Equal(True, ParsedArgs.CompilationOptions.Deterministic)

            ParsedArgs = DefaultParse({"/deterministic", "a.vb"}, _baseDirectory)
            ParsedArgs.Errors.Verify()
            Assert.Equal(True, ParsedArgs.CompilationOptions.Deterministic)

            ParsedArgs = DefaultParse({"/DETERMINISTIC+", "a.vb"}, _baseDirectory)
            ParsedArgs.Errors.Verify()
            Assert.Equal(True, ParsedArgs.CompilationOptions.Deterministic)

            ParsedArgs = DefaultParse({"/deterministic-", "a.vb"}, _baseDirectory)
            ParsedArgs.Errors.Verify()
            Assert.Equal(False, ParsedArgs.CompilationOptions.Deterministic)
        End Sub

        <WorkItem(546301, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546301")>
        <Fact>
        Public Sub Parallel()
            Dim parsedArgs = DefaultParse({"/parallel", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.CompilationOptions.ConcurrentBuild)

            parsedArgs = DefaultParse({"/p", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.CompilationOptions.ConcurrentBuild)

            parsedArgs = DefaultParse({"a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.CompilationOptions.ConcurrentBuild) ' default

            parsedArgs = DefaultParse({"/PARALLEL+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.CompilationOptions.ConcurrentBuild)

            parsedArgs = DefaultParse({"/PARALLEL-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.CompilationOptions.ConcurrentBuild)

            parsedArgs = DefaultParse({"/PArallel-", "/PArallel+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.CompilationOptions.ConcurrentBuild)

            parsedArgs = DefaultParse({"/parallel:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("parallel"))

            parsedArgs = DefaultParse({"/parallel+:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("parallel"))

            parsedArgs = DefaultParse({"/parallel-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("parallel"))

            parsedArgs = DefaultParse({"/P+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.CompilationOptions.ConcurrentBuild)

            parsedArgs = DefaultParse({"/P-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.CompilationOptions.ConcurrentBuild)

            parsedArgs = DefaultParse({"/P-", "/P+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.CompilationOptions.ConcurrentBuild)

            parsedArgs = DefaultParse({"/p:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("p"))

            parsedArgs = DefaultParse({"/p+:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("p"))

            parsedArgs = DefaultParse({"/p-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("p"))
        End Sub

        <Fact>
        Public Sub SubsystemVersionTests()
            Dim parsedArgs = DefaultParse({"/subsystemversion:4.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(SubsystemVersion.Create(4, 0), parsedArgs.EmitOptions.SubsystemVersion)

            ' wrongly supported subsystem version. CompilationOptions data will be faithful to the user input.
            ' It is normalized at the time of emit.
            parsedArgs = DefaultParse({"/subsystemversion:0.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify() ' no error in Dev11
            Assert.Equal(SubsystemVersion.Create(0, 0), parsedArgs.EmitOptions.SubsystemVersion)

            parsedArgs = DefaultParse({"/subsystemversion:0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify() ' no error in Dev11
            Assert.Equal(SubsystemVersion.Create(0, 0), parsedArgs.EmitOptions.SubsystemVersion)

            parsedArgs = DefaultParse({"/subsystemversion:3.99", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify() ' no warning in Dev11
            Assert.Equal(SubsystemVersion.Create(3, 99), parsedArgs.EmitOptions.SubsystemVersion)

            parsedArgs = DefaultParse({"/subsystemversion:4.0", "/subsystemversion:5.333", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(SubsystemVersion.Create(5, 333), parsedArgs.EmitOptions.SubsystemVersion)

            parsedArgs = DefaultParse({"/subsystemversion:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("subsystemversion", ":<version>"))

            parsedArgs = DefaultParse({"/subsystemversion", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("subsystemversion", ":<version>"))

            parsedArgs = DefaultParse({"/subsystemversion-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/subsystemversion-")) ' TODO: Dev11 reports ERRID.ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/subsystemversion: ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("subsystemversion", ":<version>"))

            parsedArgs = DefaultParse({"/subsystemversion: 4.1", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments(" 4.1"))

            parsedArgs = DefaultParse({"/subsystemversion:4 .0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments("4 .0"))

            parsedArgs = DefaultParse({"/subsystemversion:4. 0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments("4. 0"))

            parsedArgs = DefaultParse({"/subsystemversion:.", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments("."))

            parsedArgs = DefaultParse({"/subsystemversion:4.", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments("4."))

            parsedArgs = DefaultParse({"/subsystemversion:.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments(".0"))

            parsedArgs = DefaultParse({"/subsystemversion:4.2 ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = DefaultParse({"/subsystemversion:4.65536", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments("4.65536"))

            parsedArgs = DefaultParse({"/subsystemversion:65536.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments("65536.0"))

            parsedArgs = DefaultParse({"/subsystemversion:-4.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments("-4.0"))

            ' TODO: incompatibilities: versions lower than '6.2' and 'arm', 'winmdobj', 'appcontainer'
        End Sub

        <Fact>
        Public Sub Codepage()
            Dim parsedArgs = DefaultParse({"/CodePage:1200", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("Unicode", parsedArgs.Encoding.EncodingName)

            parsedArgs = DefaultParse({"/CodePage:1200", "/CodePage:65001", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("Unicode (UTF-8)", parsedArgs.Encoding.EncodingName)

            ' errors 
            parsedArgs = DefaultParse({"/codepage:0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadCodepage).WithArguments("0"))

            parsedArgs = DefaultParse({"/codepage:abc", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadCodepage).WithArguments("abc"))

            parsedArgs = DefaultParse({"/codepage:-5", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadCodepage).WithArguments("-5"))

            parsedArgs = DefaultParse({"/codepage: ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("codepage", ":<number>"))

            parsedArgs = DefaultParse({"/codepage:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("codepage", ":<number>"))

            parsedArgs = DefaultParse({"/codepage+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/codepage+")) ' Dev11 reports ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/codepage", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("codepage", ":<number>"))
        End Sub

        <Fact, WorkItem(24735, "https://github.com/dotnet/roslyn/issues/24735")>
        Public Sub ChecksumAlgorithm()
            Dim parsedArgs As VisualBasicCommandLineArguments

            parsedArgs = DefaultParse({"/checksumAlgorithm:sHa1", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(SourceHashAlgorithm.Sha1, parsedArgs.ChecksumAlgorithm)
            Assert.Equal(HashAlgorithmName.SHA256, parsedArgs.EmitOptions.PdbChecksumAlgorithm)

            parsedArgs = DefaultParse({"/checksumAlgorithm:sha256", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(SourceHashAlgorithm.Sha256, parsedArgs.ChecksumAlgorithm)
            Assert.Equal(HashAlgorithmName.SHA256, parsedArgs.EmitOptions.PdbChecksumAlgorithm)

            parsedArgs = DefaultParse({"a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(SourceHashAlgorithm.Sha256, parsedArgs.ChecksumAlgorithm)
            Assert.Equal(HashAlgorithmName.SHA256, parsedArgs.EmitOptions.PdbChecksumAlgorithm)

            ' error
            parsedArgs = DefaultParse({"/checksumAlgorithm:256", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadChecksumAlgorithm).WithArguments("256"))

            parsedArgs = DefaultParse({"/checksumAlgorithm:sha-1", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadChecksumAlgorithm).WithArguments("sha-1"))

            parsedArgs = DefaultParse({"/checksumAlgorithm:sha", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadChecksumAlgorithm).WithArguments("sha"))

            parsedArgs = DefaultParse({"/checksumAlgorithm: ", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("checksumalgorithm", ":<algorithm>"))

            parsedArgs = DefaultParse({"/checksumAlgorithm:", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("checksumalgorithm", ":<algorithm>"))

            parsedArgs = DefaultParse({"/checksumAlgorithm", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("checksumalgorithm", ":<algorithm>"))

            parsedArgs = DefaultParse({"/checksumAlgorithm+", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/checksumAlgorithm+"))
        End Sub

        <Fact>
        Public Sub MainTypeName()
            Dim parsedArgs = DefaultParse({"/main:A.B.C", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("A.B.C", parsedArgs.CompilationOptions.MainTypeName)

            ' overriding the value
            parsedArgs = DefaultParse({"/Main:A.B.C", "/M:X.Y.Z", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("X.Y.Z", parsedArgs.CompilationOptions.MainTypeName)

            parsedArgs = DefaultParse({"/MAIN: ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("main", ":<class>"))
            Assert.Null(parsedArgs.CompilationOptions.MainTypeName) ' EDMAURER Dev11 accepts and MainTypeName is " "

            ' errors 
            parsedArgs = DefaultParse({"/maiN:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("main", ":<class>"))

            parsedArgs = DefaultParse({"/m", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("m", ":<class>"))

            parsedArgs = DefaultParse({"/m+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/m+")) ' Dev11 reports ERR_ArgumentRequired

            ' incompatibilities ignored by Dev11
            parsedArgs = DefaultParse({"/MAIN:XYZ", "/t:library", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("XYZ", parsedArgs.CompilationOptions.MainTypeName)
            Assert.Equal(OutputKind.DynamicallyLinkedLibrary, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = DefaultParse({"/MAIN:XYZ", "/t:module", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputKind.NetModule, parsedArgs.CompilationOptions.OutputKind)
        End Sub

        <Fact>
        Public Sub OptionCompare()
            Dim parsedArgs = InteractiveParse({"/optioncompare"}, _baseDirectory)
            Assert.Equal(1, parsedArgs.Errors.Length)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("optioncompare", ":binary|text"))
            Assert.Equal(False, parsedArgs.CompilationOptions.OptionCompareText)

            parsedArgs = InteractiveParse({"/optioncompare:text", "/optioncompare"}, _baseDirectory)
            Assert.Equal(1, parsedArgs.Errors.Length)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("optioncompare", ":binary|text"))
            Assert.Equal(True, parsedArgs.CompilationOptions.OptionCompareText)

            parsedArgs = InteractiveParse({"/opTioncompare:Text", "/optioncomparE:bINARY"}, _baseDirectory)
            Assert.Equal(0, parsedArgs.Errors.Length)
            Assert.Equal(False, parsedArgs.CompilationOptions.OptionCompareText)

            parsedArgs = InteractiveParse({"/d:a=1"}, _baseDirectory) ' test default value
            Assert.Equal(0, parsedArgs.Errors.Length)
            Assert.Equal(False, parsedArgs.CompilationOptions.OptionCompareText)
        End Sub

        <Fact>
        Public Sub OptionExplicit()
            Dim parsedArgs = InteractiveParse({"/optiONexplicit"}, _baseDirectory)
            Assert.Equal(0, parsedArgs.Errors.Length)
            Assert.Equal(True, parsedArgs.CompilationOptions.OptionExplicit)

            parsedArgs = InteractiveParse({"/optiONexplicit:+"}, _baseDirectory)
            Assert.Equal(1, parsedArgs.Errors.Length)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optionexplicit"))
            Assert.Equal(True, parsedArgs.CompilationOptions.OptionExplicit)

            parsedArgs = InteractiveParse({"/optiONexplicit-:"}, _baseDirectory)
            Assert.Equal(1, parsedArgs.Errors.Length)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optionexplicit"))

            parsedArgs = InteractiveParse({"/optionexplicit+", "/optiONexplicit-:"}, _baseDirectory)
            Assert.Equal(1, parsedArgs.Errors.Length)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optionexplicit"))

            parsedArgs = InteractiveParse({"/optionexplicit+", "/optiONexplicit-", "/optiONexpliCIT+"}, _baseDirectory)
            Assert.Equal(0, parsedArgs.Errors.Length)
            Assert.Equal(True, parsedArgs.CompilationOptions.OptionExplicit)

            parsedArgs = InteractiveParse({"/d:a=1"}, _baseDirectory) ' test default value
            Assert.Equal(0, parsedArgs.Errors.Length)
            Assert.Equal(True, parsedArgs.CompilationOptions.OptionExplicit)
        End Sub

        <Fact>
        Public Sub OptionInfer()
            Dim parsedArgs = InteractiveParse({"/optiONinfer"}, _baseDirectory)
            Assert.Equal(0, parsedArgs.Errors.Length)
            Assert.Equal(True, parsedArgs.CompilationOptions.OptionInfer)

            parsedArgs = InteractiveParse({"/OptionInfer:+"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optioninfer"))

            parsedArgs = InteractiveParse({"/OPTIONinfer-:"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optioninfer"))

            parsedArgs = InteractiveParse({"/optioninfer+", "/optioninFER-:"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optioninfer"))

            parsedArgs = InteractiveParse({"/optioninfer+", "/optioninfeR-", "/OptionInfer+"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.CompilationOptions.OptionInfer)

            parsedArgs = InteractiveParse({"/d:a=1"}, _baseDirectory) ' test default value
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.CompilationOptions.OptionInfer)
        End Sub

        Private ReadOnly s_VBC_VER As Double = PredefinedPreprocessorSymbols.CurrentVersionNumber

        <Fact>
        Public Sub LanguageVersionAdded_Canary()
            ' When a new version is added, this test will break. This list must be checked:
            ' - update the "UpgradeProject" codefixer (not yet supported in VB)
            ' - update the IDE drop-down for selecting Language Version (not yet supported in VB)
            ' - update project-system to recognize the new value and pass it through
            ' - update all the tests that call this canary
            ' - update the command-line documentation (CommandLine.md)
            AssertEx.SetEqual({"default", "9", "10", "11", "12", "14", "15", "15.3", "15.5", "16", "latest"},
                System.Enum.GetValues(GetType(LanguageVersion)).Cast(Of LanguageVersion)().Select(Function(v) v.ToDisplayString()))
            ' For minor versions, the format should be "x.y", such as "15.3"
        End Sub

        <Fact>
        Public Sub LanguageVersion_GetErrorCode()
            Dim versions = System.Enum.GetValues(GetType(LanguageVersion)).
                Cast(Of LanguageVersion)().
                Except({LanguageVersion.Default, LanguageVersion.Latest}).
                Select(Function(v) v.GetErrorName())

            Dim errorCodes = {
                "9.0",
                "10.0",
                "11.0",
                "12.0",
                "14.0",
                "15.0",
                "15.3",
                "15.5",
                "16"
             }

            AssertEx.SetEqual(versions, errorCodes)

            ' The canary check is a reminder that this test needs to be updated when a language version is added
            LanguageVersionAdded_Canary()
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
            Assert.Equal(LanguageVersion.VisualBasic16, LanguageVersion.VisualBasic16.MapSpecifiedToEffectiveVersion())
            Assert.Equal(LanguageVersion.VisualBasic16, LanguageVersion.Default.MapSpecifiedToEffectiveVersion())
            Assert.Equal(LanguageVersion.VisualBasic16, LanguageVersion.Latest.MapSpecifiedToEffectiveVersion())

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
            InlineData("16", True, LanguageVersion.VisualBasic16),
            InlineData("DEFAULT", True, LanguageVersion.Default),
            InlineData("default", True, LanguageVersion.Default),
            InlineData("LATEST", True, LanguageVersion.Latest),
            InlineData("latest", True, LanguageVersion.Latest),
            InlineData(Nothing, False, LanguageVersion.Default),
            InlineData("bad", False, LanguageVersion.Default)>
        Public Sub LanguageVersion_TryParseDisplayString(input As String, success As Boolean, expected As LanguageVersion)
            Dim version As LanguageVersion
            Assert.Equal(success, TryParse(input, version))
            Assert.Equal(expected, version)

            ' The canary check is a reminder that this test needs to be updated when a language version is added
            LanguageVersionAdded_Canary()
        End Sub

        <Fact>
        Public Sub LanguageVersion_ListLangVersions()
            Dim dir = Temp.CreateDirectory()
            Dim outWriter As New StringWriter()
            Dim exitCode As Integer = New MockVisualBasicCompiler(Nothing, dir.ToString(), {"/langversion:?"}).Run(outWriter, Nothing)
            Assert.Equal(0, exitCode)

            Dim actual = outWriter.ToString()
            Dim expected = [Enum].GetValues(GetType(LanguageVersion)).Cast(Of LanguageVersion)().Select(Function(v) v.ToDisplayString())
            Dim acceptableSurroundingChar = {CChar(vbCr), CChar(vbLf), "("c, ")"c, " "c}

            For Each v In expected
                Dim foundIndex = actual.IndexOf(v)
                Assert.True(foundIndex > 0, $"Missing version '{v}'")
                Assert.True(Array.IndexOf(acceptableSurroundingChar, actual(foundIndex - 1)) >= 0)
                Assert.True(Array.IndexOf(acceptableSurroundingChar, actual(foundIndex + v.Length)) >= 0)
            Next
        End Sub

        <Fact>
        Public Sub TestDefine()
            TestDefines({"/D:a=True,b=1", "a.vb"},
                        {"a", True},
                        {"b", 1},
                        {"TARGET", "exe"},
                        {"VBC_VER", s_VBC_VER})

            TestDefines({"/D:a=True,b=1", "/define:a=""123"",b=False", "a.vb"},
                        {"a", "123"},
                        {"b", False},
                        {"TARGET", "exe"},
                        {"VBC_VER", s_VBC_VER})

            TestDefines({"/D:a=""\\\\a"",b=""\\\\\b""", "a.vb"},
                        {"a", "\\\\a"},
                        {"b", "\\\\\b"},
                        {"TARGET", "exe"},
                        {"VBC_VER", s_VBC_VER})

            TestDefines({"/define:DEBUG", "a.vb"},
                        {"DEBUG", True},
                        {"TARGET", "exe"},
                        {"VBC_VER", s_VBC_VER})

            TestDefines({"/D:TARGET=True,VBC_VER=1", "a.vb"},
                        {"TARGET", True},
                        {"VBC_VER", 1})
        End Sub

        Private Sub TestDefines(args As IEnumerable(Of String), ParamArray symbols As Object()())
            Dim parsedArgs = DefaultParse(args, _baseDirectory)
            Assert.False(parsedArgs.Errors.Any)
            Assert.Equal(symbols.Length, parsedArgs.ParseOptions.PreprocessorSymbols.Length)
            Dim sortedDefines = parsedArgs.ParseOptions.
                                PreprocessorSymbols.Select(
                                    Function(d) New With {d.Key, d.Value}).OrderBy(Function(o) o.Key)

            For i = 0 To symbols.Length - 1
                Assert.Equal(symbols(i)(0), sortedDefines(i).Key)
                Assert.Equal(symbols(i)(1), sortedDefines(i).Value)
            Next
        End Sub

        <Fact>
        Public Sub OptionStrict()
            Dim parsedArgs = DefaultParse({"/optionStrict", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(VisualBasic.OptionStrict.On, parsedArgs.CompilationOptions.OptionStrict)

            parsedArgs = DefaultParse({"/optionStrict+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(VisualBasic.OptionStrict.On, parsedArgs.CompilationOptions.OptionStrict)

            parsedArgs = DefaultParse({"/optionStrict-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(VisualBasic.OptionStrict.Off, parsedArgs.CompilationOptions.OptionStrict)

            parsedArgs = DefaultParse({"/OptionStrict:cusTom", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(VisualBasic.OptionStrict.Custom, parsedArgs.CompilationOptions.OptionStrict)

            parsedArgs = DefaultParse({"/OptionStrict:cusTom", "/optionstrict-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(VisualBasic.OptionStrict.Off, parsedArgs.CompilationOptions.OptionStrict)

            parsedArgs = DefaultParse({"/optionstrict-", "/OptionStrict:cusTom", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(VisualBasic.OptionStrict.Custom, parsedArgs.CompilationOptions.OptionStrict)

            parsedArgs = DefaultParse({"/optionstrict:", "/OptionStrict:cusTom", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("optionstrict", ":custom"))

            parsedArgs = DefaultParse({"/optionstrict:xxx", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("optionstrict", ":custom"))
        End Sub

        <WorkItem(546319, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546319")>
        <WorkItem(546318, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546318")>
        <WorkItem(685392, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/685392")>
        <Fact>
        Public Sub RootNamespace()
            Dim parsedArgs = DefaultParse({"/rootnamespace:One.Two.Three", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("One.Two.Three", parsedArgs.CompilationOptions.RootNamespace)

            parsedArgs = DefaultParse({"/rootnamespace:One Two Three", "/rootnamespace:One.Two.Three", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("One.Two.Three", parsedArgs.CompilationOptions.RootNamespace)

            parsedArgs = DefaultParse({"/rootnamespace:""One.Two.Three""", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("One.Two.Three", parsedArgs.CompilationOptions.RootNamespace)

            parsedArgs = DefaultParse({"/rootnamespace", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("rootnamespace", ":<string>"))

            parsedArgs = DefaultParse({"/rootnamespace:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("rootnamespace", ":<string>"))

            parsedArgs = DefaultParse({"/rootnamespace+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/rootnamespace+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/rootnamespace-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/rootnamespace-:")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/rootnamespace:+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("+"))

            parsedArgs = DefaultParse({"/rootnamespace: ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("rootnamespace", ":<string>"))

            parsedArgs = DefaultParse({"/rootnamespace: A.B.C", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments(" A.B.C"))

            parsedArgs = DefaultParse({"/rootnamespace:[abcdef", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("[abcdef"))

            parsedArgs = DefaultParse({"/rootnamespace:abcdef]", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("abcdef]"))

            parsedArgs = DefaultParse({"/rootnamespace:[[abcdef]]", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("[[abcdef]]"))

            parsedArgs = DefaultParse({"/rootnamespace:[global]", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("[global]", parsedArgs.CompilationOptions.RootNamespace)

            parsedArgs = DefaultParse({"/rootnamespace:goo.[global].bar", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("goo.[global].bar", parsedArgs.CompilationOptions.RootNamespace)

            parsedArgs = DefaultParse({"/rootnamespace:goo.[bar]", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("goo.[bar]", parsedArgs.CompilationOptions.RootNamespace)

            parsedArgs = DefaultParse({"/rootnamespace:goo$", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("goo$"))

            parsedArgs = DefaultParse({"/rootnamespace:I(", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("I("))

            parsedArgs = DefaultParse({"/rootnamespace:_", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("_"))

            parsedArgs = DefaultParse({"/rootnamespace:[_]", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("[_]"))

            parsedArgs = DefaultParse({"/rootnamespace:__.___", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("__.___", parsedArgs.CompilationOptions.RootNamespace)

            parsedArgs = DefaultParse({"/rootnamespace:[", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("["))

            parsedArgs = DefaultParse({"/rootnamespace:]", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("]"))

            parsedArgs = DefaultParse({"/rootnamespace:[]", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("[]"))
        End Sub

        <Fact>
        Public Sub Link_SimpleTests()
            Dim parsedArgs = DefaultParse({"/link:a", "/link:b,,,,c", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertEx.Equal({"a", "b", "c"},
                           parsedArgs.MetadataReferences.
                                      Where(Function(res) res.Properties.EmbedInteropTypes).
                                      Select(Function(res) res.Reference))

            parsedArgs = DefaultParse({"/Link: ,,, b ,,", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertEx.Equal({" ", " b "},
                           parsedArgs.MetadataReferences.
                                      Where(Function(res) res.Properties.EmbedInteropTypes).
                                      Select(Function(res) res.Reference))

            parsedArgs = DefaultParse({"/l:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("l", ":<file_list>"))

            parsedArgs = DefaultParse({"/L", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("l", ":<file_list>"))

            parsedArgs = DefaultParse({"/l+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/l+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = DefaultParse({"/link-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/link-:")) ' TODO: Dev11 reports ERR_ArgumentRequired
        End Sub

        <Fact>
        Public Sub Recurse_SimpleTests()
            Dim dir = Temp.CreateDirectory()
            Dim file1 = dir.CreateFile("a.vb")
            Dim file2 = dir.CreateFile("b.vb")
            Dim file3 = dir.CreateFile("c.txt")
            Dim file4 = dir.CreateDirectory("d1").CreateFile("d.txt")
            Dim file5 = dir.CreateDirectory("d2").CreateFile("e.vb")

            file1.WriteAllText("")
            file2.WriteAllText("")
            file3.WriteAllText("")
            file4.WriteAllText("")
            file5.WriteAllText("")

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
            Dim file1 = folder.CreateFile("a.cs")
            Dim file2 = folder.CreateFile("b.vb")
            Dim file3 = folder.CreateFile("c.cpp")
            Dim file4 = folder.CreateDirectory("A").CreateFile("A_d.txt")
            Dim file5 = folder.CreateDirectory("B").CreateFile("B_e.vb")
            Dim file6 = folder.CreateDirectory("C").CreateFile("B_f.cs")

            file1.WriteAllText("")
            file2.WriteAllText("")
            file3.WriteAllText("")
            file4.WriteAllText("")
            file5.WriteAllText("")
            file6.WriteAllText("")

            Dim outWriter As New StringWriter()
            Dim exitCode As Integer = New MockVisualBasicCompiler(Nothing, folder.Path, {"/nologo", "/preferreduilang:en", "/t:library", "/recurse:.", "b.vb", "/out:abc.dll"}).Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC2014: the value '.' is invalid for option 'recurse'", outWriter.ToString().Trim())

            outWriter = New StringWriter()
            exitCode = New MockVisualBasicCompiler(Nothing, folder.Path, {"/nologo", "/preferreduilang:en", "/t:library", "/recurse:. ", "b.vb", "/out:abc.dll"}).Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC2014: the value '.' is invalid for option 'recurse'", outWriter.ToString().Trim())

            outWriter = New StringWriter()
            exitCode = New MockVisualBasicCompiler(Nothing, folder.Path, {"/nologo", "/preferreduilang:en", "/t:library", "/recurse:   . ", "/out:abc.dll"}).Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC2014: the value '   .' is invalid for option 'recurse'|vbc : error BC2008: no input sources specified", outWriter.ToString().Trim().Replace(vbCrLf, "|"))

            outWriter = New StringWriter()
            exitCode = New MockVisualBasicCompiler(Nothing, folder.Path, {"/nologo", "/preferreduilang:en", "/t:library", "/recurse:./.", "/out:abc.dll"}).Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC2014: the value './.' is invalid for option 'recurse'|vbc : error BC2008: no input sources specified", outWriter.ToString().Trim().Replace(vbCrLf, "|"))

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
            Dim outWriter = New StringWriter()
            Dim exitCode = New MockVisualBasicCompiler(Nothing, folder.Path, {"/nologo", "/preferreduilang:en", "/t:exe", "/out:abc.exe"}).Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC2008: no input sources specified", outWriter.ToString().Trim().Replace(vbCrLf, "|"))
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

        <Fact>
        Public Sub ParseAnalyzers()
            Dim parsedArgs = DefaultParse({"/a:goo.dll", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(1, parsedArgs.AnalyzerReferences.Length)
            Assert.Equal("goo.dll", parsedArgs.AnalyzerReferences(0).FilePath)

            parsedArgs = DefaultParse({"/analyzer:goo.dll", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(1, parsedArgs.AnalyzerReferences.Length)
            Assert.Equal("goo.dll", parsedArgs.AnalyzerReferences(0).FilePath)

            parsedArgs = DefaultParse({"/analyzer:""goo.dll""", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(1, parsedArgs.AnalyzerReferences.Length)
            Assert.Equal("goo.dll", parsedArgs.AnalyzerReferences(0).FilePath)

            parsedArgs = DefaultParse({"/a:goo.dll,bar.dll", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(2, parsedArgs.AnalyzerReferences.Length)
            Assert.Equal("goo.dll", parsedArgs.AnalyzerReferences(0).FilePath)
            Assert.Equal("bar.dll", parsedArgs.AnalyzerReferences(1).FilePath)

            parsedArgs = DefaultParse({"/a:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("a", ":<file_list>"))

            parsedArgs = DefaultParse({"/a", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("a", ":<file_list>"))
        End Sub

        <Fact>
        Public Sub Analyzers_Missing()
            Dim source = "Imports System"
            Dim dir = Temp.CreateDirectory()

            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/preferreduilang:en", "/t:library", "/a:missing.dll", "a.vb"})
            Dim exitCode = vbc.Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC2017: could not find library 'missing.dll'", outWriter.ToString().Trim())

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <Fact>
        Public Sub Analyzers_Empty()
            Dim source = "Imports System"
            Dim dir = Temp.CreateDirectory()

            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/preferreduilang:en", "/t:library", "/a:" + GetType(Object).Assembly.Location, "a.vb"})
            Dim exitCode = vbc.Run(outWriter, Nothing)
            Assert.Equal(0, exitCode)
            Assert.DoesNotContain("warning", outWriter.ToString())

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <Fact>
        Public Sub Analyzers_Found()
            Dim source = "Imports System " + vbCrLf + "Public Class Tester" + vbCrLf + "End Class"

            Dim dir = Temp.CreateDirectory()

            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)
            ' This assembly has a MockDiagnosticAnalyzer type which should get run by this compilation.
            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/preferreduilang:en", "/t:library", "/a:" + Assembly.GetExecutingAssembly().Location, "a.vb"})
            Dim exitCode = vbc.Run(outWriter, Nothing)
            Assert.Equal(0, exitCode)
            ' Diagnostic cannot instantiate
            Assert.True(outWriter.ToString().Contains("warning BC42376"))
            ' Diagnostic is thrown
            Assert.True(outWriter.ToString().Contains("a.vb(2) : warning Warning01: Throwing a diagnostic for types declared"))
            Assert.True(outWriter.ToString().Contains("a.vb(2) : warning Warning03: Throwing a diagnostic for types declared"))

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <Fact>
        Public Sub Analyzers_WithRuleSet()
            Dim source = "Imports System " + vbCrLf + "Public Class Tester" + vbCrLf + "End Class"

            Dim dir = Temp.CreateDirectory()

            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)

            Dim rulesetSource = <?xml version="1.0" encoding="utf-8"?>
                                <RuleSet Name="Ruleset1" Description="Test" ToolsVersion="12.0">
                                    <Rules AnalyzerId="Microsoft.Analyzers.ManagedCodeAnalysis" RuleNamespace="Microsoft.Rules.Managed">
                                        <Rule Id="Warning01" Action="Error"/>
                                        <Rule Id="Test02" Action="Warning"/>
                                        <Rule Id="Warning03" Action="None"/>
                                    </Rules>
                                </RuleSet>

            Dim ruleSetFile = CreateRuleSetFile(rulesetSource)

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/t:library", "/a:" + Assembly.GetExecutingAssembly().Location, "a.vb", "/ruleset:" + ruleSetFile.Path})
            Dim exitCode = vbc.Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            ' Diagnostic cannot instantiate
            Assert.True(outWriter.ToString().Contains("warning BC42376"))
            '' Diagnostic thrown as error
            'Assert.True(outWriter.ToString().Contains("error Warning01"))
            ' Diagnostic is suppressed
            Assert.False(outWriter.ToString().Contains("warning Warning03"))

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <Fact>
        Public Sub Analyzers_CommandLineOverridesRuleset1()
            Dim source = "Imports System " + vbCrLf + "Public Class Tester" + vbCrLf + "End Class"

            Dim dir = Temp.CreateDirectory()

            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)

            Dim rulesetSource = <?xml version="1.0" encoding="utf-8"?>
                                <RuleSet Name="Ruleset1" Description="Test" ToolsVersion="12.0">
                                    <IncludeAll Action="Warning"/>
                                </RuleSet>

            Dim ruleSetFile = CreateRuleSetFile(rulesetSource)

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path,
                                                  {
                                                        "/nologo", "/preferreduilang:en", "/preferreduilang:en", "/t:library",
                                                        "/a:" + Assembly.GetExecutingAssembly().Location, "a.vb",
                                                        "/ruleset:" & ruleSetFile.Path, "/warnaserror", "/nowarn:42376"
                                                  })
            Dim exitCode = vbc.Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            ' Diagnostics thrown as error: command line always overrides ruleset.
            Dim output = outWriter.ToString()
            Assert.Contains("error Warning01", output, StringComparison.Ordinal)
            Assert.Contains("error Warning03", output, StringComparison.Ordinal)

            outWriter = New StringWriter(CultureInfo.InvariantCulture)
            vbc = New MockVisualBasicCompiler(Nothing, dir.Path,
                                                  {
                                                        "/nologo", "/preferreduilang:en", "/t:library",
                                                        "/a:" + Assembly.GetExecutingAssembly().Location, "a.vb",
                                                        "/warnaserror+", "/ruleset:" & ruleSetFile.Path, "/nowarn:42376"
                                                  })
            exitCode = vbc.Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            ' Diagnostics thrown as error: command line always overrides ruleset.
            output = outWriter.ToString()
            Assert.Contains("error Warning01", output, StringComparison.Ordinal)
            Assert.Contains("error Warning03", output, StringComparison.Ordinal)

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <Fact>
        Public Sub Analyzer_CommandLineOverridesRuleset2()
            Dim source = "Imports System " + vbCrLf + "Public Class Tester" + vbCrLf + "End Class"

            Dim dir = Temp.CreateDirectory()

            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)

            Dim rulesetSource = <?xml version="1.0" encoding="utf-8"?>
                                <RuleSet Name="Ruleset1" Description="Test" ToolsVersion="12.0">
                                    <Rules AnalyzerId="Microsoft.Analyzers.ManagedCodeAnalysis" RuleNamespace="Microsoft.Rules.Managed">
                                        <Rule Id="Warning01" Action="Error"/>
                                        <Rule Id="Warning03" Action="Warning"/>
                                    </Rules>
                                </RuleSet>

            Dim ruleSetFile = CreateRuleSetFile(rulesetSource)

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path,
                                                  {
                                                        "/nologo", "/t:library",
                                                        "/a:" + Assembly.GetExecutingAssembly().Location, "a.vb",
                                                        "/ruleset:" & ruleSetFile.Path, "/nowarn"
                                                  })
            Dim exitCode = vbc.Run(outWriter, Nothing)
            Assert.Equal(0, exitCode)
            ' Diagnostics suppressed: command line always overrides ruleset.
            Dim output = outWriter.ToString()
            Assert.DoesNotContain("Warning01", output, StringComparison.Ordinal)
            Assert.DoesNotContain("BC31072", output, StringComparison.Ordinal)
            Assert.DoesNotContain("Warning03", output, StringComparison.Ordinal)

            outWriter = New StringWriter(CultureInfo.InvariantCulture)
            vbc = New MockVisualBasicCompiler(Nothing, dir.Path,
                                                  {
                                                        "/nologo", "/t:library",
                                                        "/a:" + Assembly.GetExecutingAssembly().Location, "a.vb",
                                                        "/nowarn", "/ruleset:" & ruleSetFile.Path
                                                  })
            exitCode = vbc.Run(outWriter, Nothing)
            Assert.Equal(0, exitCode)
            ' Diagnostics suppressed: command line always overrides ruleset.
            output = outWriter.ToString()
            Assert.DoesNotContain("Warning01", output, StringComparison.Ordinal)
            Assert.DoesNotContain("BC31072", output, StringComparison.Ordinal)
            Assert.DoesNotContain("Warning03", output, StringComparison.Ordinal)

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <Fact>
        Public Sub Analyzers_WithRuleSetIncludeAll()
            Dim source = "Imports System \r\n Public Class Tester \r\n Public Sub Goo() \r\n Dim x As Integer \r\n End Sub \r\n End Class"

            Dim dir = Temp.CreateDirectory()

            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)

            Dim rulesetSource = <?xml version="1.0" encoding="utf-8"?>
                                <RuleSet Name="Ruleset1" Description="Test" ToolsVersion="12.0">
                                    <IncludeAll Action="Error"/>
                                    <Rules AnalyzerId="Microsoft.Analyzers.ManagedCodeAnalysis" RuleNamespace="Microsoft.Rules.Managed">
                                        <Rule Id="Warning01" Action="Error"/>
                                        <Rule Id="Test02" Action="Warning"/>
                                        <Rule Id="Warning03" Action="None"/>
                                    </Rules>
                                </RuleSet>

            Dim ruleSetFile = CreateRuleSetFile(rulesetSource)

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/t:library", "/a:" + Assembly.GetExecutingAssembly().Location, "a.vb", "/ruleset:" + ruleSetFile.Path})
            Dim exitCode = vbc.Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            ' Compiler warnings as errors
            Assert.True(outWriter.ToString().Contains("error BC42376"))
            ' User diagnostics not thrown due to compiler errors
            Assert.False(outWriter.ToString().Contains("Warning01"))
            Assert.False(outWriter.ToString().Contains("Warning03"))

            CleanupAllGeneratedFiles(file.Path)
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

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
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

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/debug:portable", "/sourcelink:sl.json", "a.vb"})
            Dim exitCode As Integer = vbc.Run(outWriter)
            Assert.Equal(0, exitCode)

            Dim pdbStream = File.OpenRead(Path.Combine(dir.Path, "a.pdb"))
            Using mdProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream)
                Dim blob = mdProvider.GetMetadataReader().GetSourceLinkBlob()
                AssertEx.Equal(File.ReadAllBytes(sl.Path), blob)
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

            parsedArgs = DefaultParse({"/embed:a.vb,b.vb", "/debug:portable", "a.vb", "b.vb", "c.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertEx.Equal(
                {"a.vb", "b.vb"}.Select(Function(f) Path.Combine(_baseDirectory, f)),
                parsedArgs.EmbeddedFiles.Select(Function(f) f.Path))

            parsedArgs = DefaultParse({"/embed:""a,b.vb""", "/debug:portable", "a,b.vb", "c.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertEx.Equal(
                {"a,b.vb"}.Select(Function(f) Path.Combine(_baseDirectory, f)),
                parsedArgs.EmbeddedFiles.Select(Function(f) f.Path))

            parsedArgs = DefaultParse({"/embed:\""a,b.vb\""", "/debug:portable", "a,b.vb", "c.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertEx.Equal(
                {"a,b.vb"}.Select(Function(f) Path.Combine(_baseDirectory, f)),
                parsedArgs.EmbeddedFiles.Select(Function(f) f.Path))

            parsedArgs = DefaultParse({"/embed:\""""a.vb,b.vb""\""", "/debug:portable", "a.vb", "b.vb", "c.vb"}, _baseDirectory)
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
            Dim src = dir.CreateFile("embed.vb")
            Dim src2 = dir.CreateFile("embed2.vb")
            Dim txt = dir.CreateFile("embed.xyz")

            src.WriteAllText(embed_vb)
            src2.WriteAllText(embed2_vb)
            txt.WriteAllText(embed_xyz)

            Dim expectedEmbeddedMap = New Dictionary(Of String, String)()
            If expectedEmbedded.Contains("embed.vb") Then
                expectedEmbeddedMap.Add(src.Path, embed_vb)
            End If

            If expectedEmbedded.Contains("embed2.vb") Then
                expectedEmbeddedMap.Add(src2.Path, embed2_vb)
            End If

            If expectedEmbedded.Contains("embed.xyz") Then
                expectedEmbeddedMap.Add(txt.Path, embed_xyz)
            End If

            Dim output = New StringWriter(CultureInfo.InvariantCulture)
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

            Assert.Empty(expectedEmbeddedMap)
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
                    If sourceBlob.Array Is Nothing Then
                        Continue For
                    End If

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
            Dim s = PathUtilities.DirectorySeparatorStr

            Dim parsedArgs = DefaultParse({"/pathmap:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/pathmap:").WithLocation(1, 1)
            )
            Assert.Equal(ImmutableArray.Create(Of KeyValuePair(Of String, String))(), parsedArgs.PathMap)

            parsedArgs = DefaultParse({"/pathmap:K1=V1", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(KeyValuePairUtil.Create("K1" & s, "V1" & s), parsedArgs.PathMap(0))

            parsedArgs = DefaultParse({$"/pathmap:abc{s}=/", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(KeyValuePairUtil.Create("abc" & s, "/"), parsedArgs.PathMap(0))

            parsedArgs = DefaultParse({"/pathmap:K1=V1,K2=V2", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(KeyValuePairUtil.Create("K1" & s, "V1" & s), parsedArgs.PathMap(0))
            Assert.Equal(KeyValuePairUtil.Create("K2" & s, "V2" & s), parsedArgs.PathMap(1))

            parsedArgs = DefaultParse({"/pathmap:,", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(ImmutableArray.Create(Of KeyValuePair(Of String, String))(), parsedArgs.PathMap)

            parsedArgs = DefaultParse({"/pathmap:,,", "a.vb"}, _baseDirectory)
            Assert.Equal(1, parsedArgs.Errors.Count())
            Assert.Equal(ERRID.ERR_InvalidPathMap, parsedArgs.Errors(0).Code)

            parsedArgs = DefaultParse({"/pathmap:,,,", "a.vb"}, _baseDirectory)
            Assert.Equal(1, parsedArgs.Errors.Count())
            Assert.Equal(ERRID.ERR_InvalidPathMap, parsedArgs.Errors(0).Code)

            parsedArgs = DefaultParse({"/pathmap:k=,=v", "a.vb"}, _baseDirectory)
            Assert.Equal(2, parsedArgs.Errors.Count())
            Assert.Equal(ERRID.ERR_InvalidPathMap, parsedArgs.Errors(0).Code)
            Assert.Equal(ERRID.ERR_InvalidPathMap, parsedArgs.Errors(1).Code)

            parsedArgs = DefaultParse({"/pathmap:k=v=bad", "a.vb"}, _baseDirectory)
            Assert.Equal(1, parsedArgs.Errors.Count())
            Assert.Equal(ERRID.ERR_InvalidPathMap, parsedArgs.Errors(0).Code)

            parsedArgs = DefaultParse({"/pathmap:k=", "a.vb"}, _baseDirectory)
            Assert.Equal(1, parsedArgs.Errors.Count())
            Assert.Equal(ERRID.ERR_InvalidPathMap, parsedArgs.Errors(0).Code)

            parsedArgs = DefaultParse({"/pathmap:=v", "a.vb"}, _baseDirectory)
            Assert.Equal(1, parsedArgs.Errors.Count())
            Assert.Equal(ERRID.ERR_InvalidPathMap, parsedArgs.Errors(0).Code)

            parsedArgs = DefaultParse({"/pathmap:""supporting spaces=is hard""", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(KeyValuePairUtil.Create("supporting spaces" & s, "is hard" & s), parsedArgs.PathMap(0))

            parsedArgs = DefaultParse({"/pathmap:""K 1=V 1"",""K 2=V 2""", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(KeyValuePairUtil.Create("K 1" & s, "V 1" & s), parsedArgs.PathMap(0))
            Assert.Equal(KeyValuePairUtil.Create("K 2" & s, "V 2" & s), parsedArgs.PathMap(1))

            parsedArgs = DefaultParse({"/pathmap:""K 1""=""V 1"",""K 2""=""V 2""", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(KeyValuePairUtil.Create("K 1" & s, "V 1" & s), parsedArgs.PathMap(0))
            Assert.Equal(KeyValuePairUtil.Create("K 2" & s, "V 2" & s), parsedArgs.PathMap(1))

            parsedArgs = DefaultParse({"/pathmap:""a ==,,b""=""1,,== 2"",""x ==,,y""=""3 4"",", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(KeyValuePairUtil.Create("a =,b" & s, "1,= 2" & s), parsedArgs.PathMap(0))
            Assert.Equal(KeyValuePairUtil.Create("x =,y" & s, "3 4" & s), parsedArgs.PathMap(1))
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
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({pathmapArg, "a.vb"}, TempRoot.Root, RuntimeEnvironment.GetRuntimeDirectory(), Nothing)
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
            Assert.Equal(New KeyValuePair(Of String, String)("C:\temp/goo" + sep, "/temp\goo" + sep), Parse({"/pathmap:C:\temp/goo=/temp\goo", "a.vb"}).PathMap(0))
            Assert.Equal(New KeyValuePair(Of String, String)("noslash" + sep, "withoutslash" + sep), Parse({"/pathmap:noslash=withoutslash", "a.vb"}).PathMap(0))
            Dim doublemap = Parse({"/pathmap:/temp=/goo,/temp/=/bar", "a.vb"}).PathMap
            Assert.Equal(New KeyValuePair(Of String, String)("/temp/", "/goo/"), doublemap(0))
            Assert.Equal(New KeyValuePair(Of String, String)("/temp/", "/bar/"), doublemap(1))
        End Sub

        <Fact>
        Public Sub NothingBaseDirectoryNotAddedToKeyFileSearchPaths()
            Dim args As VisualBasicCommandLineArguments = VisualBasicCommandLineParser.Default.Parse(New String() {}, Nothing, RuntimeEnvironment.GetRuntimeDirectory())
            AssertEx.Equal(ImmutableArray.Create(Of String)(), args.KeyFileSearchPaths)
        End Sub

        <Fact>
        <WorkItem(29252, "https://github.com/dotnet/roslyn/issues/29252")>
        Public Sub SdkPathArg()
            Dim parentDir = Temp.CreateDirectory()

            Dim sdkDir = parentDir.CreateDirectory("sdk")
            Dim sdkPath = sdkDir.Path

            Dim parser = VisualBasicCommandLineParser.Default.Parse({$"-sdkPath:{sdkPath}"}, parentDir.Path, Nothing)
            AssertEx.Equal(ImmutableArray.Create(sdkPath), parser.ReferencePaths)
        End Sub

        <Fact>
        <WorkItem(29252, "https://github.com/dotnet/roslyn/issues/29252")>
        Public Sub SdkPathNoArg()
            Dim parentDir = Temp.CreateDirectory()
            Dim parser = VisualBasicCommandLineParser.Default.Parse({"file.vb", "-sdkPath", $"-out:{parentDir.Path}"}, parentDir.Path, Nothing)
            parser.Errors.Verify(
                Diagnostic(ERRID.ERR_ArgumentRequired, arguments:={"sdkpath", ":<path>"}).WithLocation(1, 1),
                Diagnostic(ERRID.WRN_CannotFindStandardLibrary1).WithArguments("System.dll").WithLocation(1, 1),
                Diagnostic(ERRID.ERR_LibNotFound).WithArguments("Microsoft.VisualBasic.dll").WithLocation(1, 1))
        End Sub

        <Fact>
        <WorkItem(29252, "https://github.com/dotnet/roslyn/issues/29252")>
        Public Sub SdkPathFollowedByNoSdkPath()
            Dim parentDir = Temp.CreateDirectory()
            Dim parser = VisualBasicCommandLineParser.Default.Parse({"file.vb", $"-out:{parentDir.Path}", "-sdkPath:path/to/sdk", "/noSdkPath"}, parentDir.Path, Nothing)
            AssertEx.Equal(ImmutableArray(Of String).Empty, parser.ReferencePaths)
        End Sub

        <Fact>
        <WorkItem(29252, "https://github.com/dotnet/roslyn/issues/29252")>
        Public Sub NoSdkPathFollowedBySdkPath()
            Dim parentDir = Temp.CreateDirectory()
            Dim sdkDir = parentDir.CreateDirectory("sdk")
            Dim parser = VisualBasicCommandLineParser.Default.Parse({"file.vb", $"-out:{parentDir.Path}", "/noSdkPath", $"-sdkPath:{sdkDir.Path}"}, parentDir.Path, Nothing)
            AssertEx.Equal(ImmutableArray.Create(sdkDir.Path), parser.ReferencePaths)
        End Sub

        <Fact>
        <WorkItem(29252, "https://github.com/dotnet/roslyn/issues/29252")>
        Public Sub NoSdkPathReferenceSystemDll()
            Dim source = "
Module M
End Module
"
            Dim dir = Temp.CreateDirectory()

            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/preferreduilang:en", "/nosdkpath", "/t:library", "a.vb"})
            Dim exitCode = vbc.Run(outWriter, Nothing)
            Dim output = outWriter.ToString().Trim()
            Assert.Equal(1, exitCode)
            Assert.Contains("vbc : error BC2017: could not find library 'Microsoft.VisualBasic.dll'", output)

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <CompilerTrait(CompilerFeature.Determinism)>
        <Fact>
        Public Sub PathMapPdbDeterminism()
            Dim assertPdbEmit =
                Sub(dir As TempDirectory, pePdbPath As String, extraArgs As String())

                    Dim source =
                        <compilation>
Imports System
Module Program
    Sub Main()
    End Sub
End Module
                        </compilation>

                    Dim src = dir.CreateFile("a.vb").WriteAllText(source.Value)
                    Dim pdbPath = Path.Combine(dir.Path, "a.pdb")
                    Dim defaultArgs = {"/nologo", "/debug", "a.vb"}
                    Dim isDeterministic = extraArgs.Contains("/deterministic")
                    Dim args = defaultArgs.Concat(extraArgs).ToArray()
                    Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)

                    Dim vbc = New MockVisualBasicCompiler(dir.Path, args)
                    Dim exitCode = vbc.Run(outWriter)
                    Assert.Equal(0, exitCode)

                    Dim exePath = Path.Combine(dir.Path, "a.exe")
                    Assert.True(File.Exists(exePath))
                    Assert.True(File.Exists(pdbPath))

                    Using peStream = File.OpenRead(exePath)
                        PdbValidation.ValidateDebugDirectory(peStream, Nothing, pePdbPath, hashAlgorithm:=Nothing, hasEmbeddedPdb:=False, isDeterministic)
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
                    Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments("C:""My Folder\MyBinary.dll").WithLocation(1, 1))

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
                Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments(currentDrive + ":a.vb"))

            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory)

            ' UNC
            parsedArgs = DefaultParse({"/out:\\b", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments("\\b"))

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
                Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments("a.b" & vbNullChar & "b"))

            Assert.Equal("a.exe", parsedArgs.OutputFileName)
            Assert.Equal("a", parsedArgs.CompilationName)
            Assert.Equal("a.exe", parsedArgs.CompilationOptions.ModuleName)

            ' Temp Skip: Unicode?
            ' parsedArgs = DefaultParse({"/out:a" & ChrW(&HD800) & "b.dll", "a.vb"}, _baseDirectory)
            ' parsedArgs.Errors.Verify(
            '    Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments("a" & ChrW(&HD800) & "b.dll"))

            ' Assert.Equal("a.exe", parsedArgs.OutputFileName)
            ' Assert.Equal("a", parsedArgs.CompilationName)
            ' Assert.Equal("a.exe", parsedArgs.CompilationOptions.ModuleName)

            ' Temp Skip: error message changed (path)
            'parsedArgs = DefaultParse({"/out:"" a.dll""", "a.vb"}, _baseDirectory)
            'parsedArgs.Errors.Verify(
            '    Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments(" a.dll"))

            'Assert.Equal("a.exe", parsedArgs.OutputFileName)
            'Assert.Equal("a", parsedArgs.CompilationName)
            'Assert.Equal("a.exe", parsedArgs.CompilationOptions.ModuleName)

            ' Dev11 reports BC2012: can't open 'a<>.z' for writing
            parsedArgs = DefaultParse({"/out:""a<>.dll""", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments("a<>.dll"))

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
                Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments(".exe"))

            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/t:exe", "/out:.exe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments(".exe"))

            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/t:library", "/out:.dll", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments(".dll"))

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
                Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments(".exe"))

            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/t:exe", ".vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments(".exe"))

            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/t:library", ".vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments(".dll"))

            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/t:module", ".vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal(".netmodule", parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Equal(".netmodule", parsedArgs.CompilationOptions.ModuleName)
        End Sub

        <Fact>
        Public Sub ParseOut2()
            ' exe
            Dim parsedArgs = DefaultParse({"/out:.x", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal(".x", parsedArgs.CompilationName)
            Assert.Equal(".x.exe", parsedArgs.OutputFileName)
            Assert.Equal(".x.exe", parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/target:winexe", "/out:.x.eXe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal(".x", parsedArgs.CompilationName)
            Assert.Equal(".x.eXe", parsedArgs.OutputFileName)
            Assert.Equal(".x.eXe", parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/target:winexe", "/out:.exe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments(".exe"))

            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            ' dll
            parsedArgs = DefaultParse({"/target:library", "/out:.x", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal(".x", parsedArgs.CompilationName)
            Assert.Equal(".x.dll", parsedArgs.OutputFileName)
            Assert.Equal(".x.dll", parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/target:library", "/out:.X.Dll", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal(".X", parsedArgs.CompilationName)
            Assert.Equal(".X.Dll", parsedArgs.OutputFileName)
            Assert.Equal(".X.Dll", parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/target:library", "/out:.dll", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments(".dll"))

            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            ' module
            parsedArgs = DefaultParse({"/target:module", "/out:.x", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Null(parsedArgs.CompilationName)
            Assert.Equal(".x", parsedArgs.OutputFileName)
            Assert.Equal(".x", parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/target:module", "/out:x.dll", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Null(parsedArgs.CompilationName)
            Assert.Equal("x.dll", parsedArgs.OutputFileName)
            Assert.Equal("x.dll", parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/target:module", "/out:.x.netmodule", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Null(parsedArgs.CompilationName)
            Assert.Equal(".x.netmodule", parsedArgs.OutputFileName)
            Assert.Equal(".x.netmodule", parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = DefaultParse({"/target:module", "/out:x", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Null(parsedArgs.CompilationName)
            Assert.Equal("x.netmodule", parsedArgs.OutputFileName)
            Assert.Equal("x.netmodule", parsedArgs.CompilationOptions.ModuleName)
        End Sub

        <Fact, WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")>
        Public Sub ConsistentErrorMessageWhenProvidingNoKeyFile()
            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/keyfile:", "/target:library", "/nologo", "/preferreduilang:en", "a.vb"})
            Dim exitCode = vbc.Run(outWriter)

            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC2006: option 'keyfile' requires ':<file>'", outWriter.ToString().Trim())
        End Sub

        <Fact, WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")>
        Public Sub ConsistentErrorMessageWhenProvidingEmptyKeyFile()
            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/keyfile:""""", "/target:library", "/nologo", "/preferreduilang:en", "a.vb"})
            Dim exitCode = vbc.Run(outWriter)

            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC2006: option 'keyfile' requires ':<file>'", outWriter.ToString().Trim())
        End Sub

        <Fact, WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")>
        Public Sub ConsistentErrorMessageWhenProvidingNoKeyFile_PublicSign()
            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/keyfile:", "/publicsign", "/target:library", "/nologo", "/preferreduilang:en", "a.vb"})
            Dim exitCode = vbc.Run(outWriter)

            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC2006: option 'keyfile' requires ':<file>'", outWriter.ToString().Trim())
        End Sub

        <Fact, WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")>
        Public Sub ConsistentErrorMessageWhenProvidingEmptyKeyFile_PublicSign()
            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/keyfile:""""", "/publicsign", "/target:library", "/nologo", "/preferreduilang:en", "a.vb"})
            Dim exitCode = vbc.Run(outWriter)

            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC2006: option 'keyfile' requires ':<file>'", outWriter.ToString().Trim())
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
    <text>
''' &lt;summary&gt;ABC...XYZ&lt;/summary&gt;
Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable,
                                         String.Format("/nologo /doc:{1}\src.xml /t:library {0}",
                                                       src.ToString(),
                                                       dir.ToString()),
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
    <text>
''' &lt;summary&gt;ABC...XYZ&lt;/summary&gt;
Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim xml = dir.CreateFile("a.xml")
            xml.WriteAllText("EMPTY")

            Using xmlFileHandle As FileStream = File.Open(xml.ToString(), FileMode.Open, FileAccess.Read, FileShare.Delete Or FileShare.ReadWrite)

                Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, String.Format("/nologo /t:library /doc+ {0}", src.ToString()), startFolder:=dir.ToString(), expectedRetCode:=0)
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
]]>
    </text>,
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
    <text>
''' &lt;summary&gt;ABC&lt;/summary&gt;
Class C: End Class
''' &lt;summary&gt;XYZ&lt;/summary&gt;
Class E: End Class
</text>.Value.Replace(vbLf, vbCrLf))

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
    <text>
''' &lt;summary&gt;ABC&lt;/summary&gt;
Class C: End Class
</text>.Value.Replace(vbLf, vbCrLf))

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
    <text>
''' &lt;summary&gt;ABC...XYZ&lt;/summary&gt;
Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, String.Format("/nologo /t:library /doc:abcdfg.xyz /doc+ {0}", src.ToString()), startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "a.xml")))

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(705148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/705148")>
        Public Sub Bug705148b()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
    <text>
''' &lt;summary&gt;ABC...XYZ&lt;/summary&gt;
Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, String.Format("/nologo /t:library /doc /out:MyXml.dll {0}", src.ToString()), startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "MyXml.xml")))

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(705148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/705148")>
        Public Sub Bug705148c()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
    <text>
''' &lt;summary&gt;ABC...XYZ&lt;/summary&gt;
Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, String.Format("/nologo /t:library /doc:doc.xml /doc+ {0}", src.ToString()), startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "a.xml")))

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(705202, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/705202")>
        Public Sub Bug705202a()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
    <text>
''' &lt;summary&gt;ABC...XYZ&lt;/summary&gt;
Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, String.Format("/nologo /t:library /doc:doc.xml /out:out.dll {0}", src.ToString()), startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "doc.xml")))

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(705202, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/705202")>
        Public Sub Bug705202b()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
    <text>
''' &lt;summary&gt;ABC...XYZ&lt;/summary&gt;
Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, String.Format("/nologo /t:library /doc:doc.xml /doc /out:out.dll {0}", src.ToString()), startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "out.xml")))

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(705202, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/705202")>
        Public Sub Bug705202c()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
    <text>
''' &lt;summary&gt;ABC...XYZ&lt;/summary&gt;
Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, String.Format("/nologo /t:library /doc:doc.xml /out:out.dll /doc+ {0}", src.ToString()), startFolder:=dir.ToString())
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
                Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("errorlog", CommandLineParser.ErrorLogOptionFormat))
            Assert.Null(parsedArgs.ErrorLogOptions)
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics)

            parsedArgs = DefaultParse({"/errorlog", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("errorlog", CommandLineParser.ErrorLogOptionFormat))
            Assert.Null(parsedArgs.ErrorLogOptions)
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics)

            ' Should preserve fully qualified paths
            parsedArgs = DefaultParse({"/errorlog:C:\MyFolder\MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("C:\MyFolder\MyBinary.xml", parsedArgs.ErrorLogOptions.Path)
            Assert.True(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics)

            ' Should handle quotes
            parsedArgs = DefaultParse({"/errorlog:""C:\My Folder\MyBinary.xml""", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("C:\My Folder\MyBinary.xml", parsedArgs.ErrorLogOptions.Path)
            Assert.True(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics)

            ' Quote after a \ is treated as an escape
            parsedArgs = DefaultParse({"/errorlog:C:\""My Folder""\MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                    Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments("C:""My Folder\MyBinary.xml").WithLocation(1, 1))

            ' Should expand partially qualified paths
            parsedArgs = DefaultParse({"/errorlog:MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(Path.Combine(baseDirectory, "MyBinary.xml"), parsedArgs.ErrorLogOptions.Path)

            ' Should expand partially qualified paths
            parsedArgs = DefaultParse({"/errorlog:..\MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("C:\abc\def\MyBinary.xml", parsedArgs.ErrorLogOptions.Path)
            Assert.True(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics)

            ' drive-relative path:
            Dim currentDrive As Char = Directory.GetCurrentDirectory()(0)
            Dim filePath = currentDrive + ":a.xml"
            parsedArgs = DefaultParse({"/errorlog:" + filePath, "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments(filePath))

            Assert.Null(parsedArgs.ErrorLogOptions)
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics)

            ' UNC
            parsedArgs = DefaultParse({"/errorlog:\\server\share\file.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal("\\server\share\file.xml", parsedArgs.ErrorLogOptions.Path)
            Assert.True(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics)

            ' Parses SARIF version.
            parsedArgs = DefaultParse({"/errorlog:C:\MyFolder\MyBinary.xml,version=2", "a.cs"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("C:\MyFolder\MyBinary.xml", parsedArgs.ErrorLogOptions.Path)
            Assert.Equal(SarifVersion.Sarif2, parsedArgs.ErrorLogOptions.SarifVersion)
            Assert.True(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics)

            ' Invalid SARIF version.
            parsedArgs = DefaultParse({"/errorlog:C:\MyFolder\MyBinary.xml,version=42", "a.cs"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.ERR_BadSwitchValue).WithArguments("C:\MyFolder\MyBinary.xml,version=42", "errorlog", CommandLineParser.ErrorLogOptionFormat))
            Assert.Null(parsedArgs.ErrorLogOptions)
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics)

            ' Invalid errorlog qualifier.
            parsedArgs = DefaultParse({"/errorlog:C:\MyFolder\MyBinary.xml,invalid=42", "a.cs"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.ERR_BadSwitchValue).WithArguments("C:\MyFolder\MyBinary.xml,invalid=42", "errorlog", CommandLineParser.ErrorLogOptionFormat))
            Assert.Null(parsedArgs.ErrorLogOptions)
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics)

            ' Too many errorlog qualifiers.
            parsedArgs = DefaultParse({"/errorlog:C:\MyFolder\MyBinary.xml,version=2,version=2", "a.cs"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.ERR_BadSwitchValue).WithArguments("C:\MyFolder\MyBinary.xml,version=2,version=2", "errorlog", CommandLineParser.ErrorLogOptionFormat))
            Assert.Null(parsedArgs.ErrorLogOptions)
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics)
        End Sub

        <Fact>
        Public Sub ParseErrorLogAndOut()
            Const baseDirectory As String = "C:\abc\def\baz"

            ' Can specify separate directories for binary and error log output.
            Dim parsedArgs = DefaultParse({"/errorlog:a\b.xml", "/out:c\d.exe", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal("C:\abc\def\baz\a\b.xml", parsedArgs.ErrorLogOptions.Path)

            Assert.Equal("C:\abc\def\baz\c", parsedArgs.OutputDirectory)
            Assert.Equal("d.exe", parsedArgs.OutputFileName)

            ' error log does not fall back on output directory.
            parsedArgs = DefaultParse({"/errorlog:b.xml", "/out:c\d.exe", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal("C:\abc\def\baz\b.xml", parsedArgs.ErrorLogOptions.Path)

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

        <Fact()>
        Public Sub FileAlignment()
            ' test recognizing all options
            Dim parsedArgs = DefaultParse({"/filealign:512", "a.vb"}, _baseDirectory)
            Assert.Equal(512, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = DefaultParse({"/filealign:1024", "a.vb"}, _baseDirectory)
            Assert.Equal(1024, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = DefaultParse({"/filealign:2048", "a.vb"}, _baseDirectory)
            Assert.Equal(2048, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = DefaultParse({"/filealign:4096", "a.vb"}, _baseDirectory)
            Assert.Equal(4096, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = DefaultParse({"/filealign:8192", "a.vb"}, _baseDirectory)
            Assert.Equal(8192, parsedArgs.EmitOptions.FileAlignment)

            ' test oct values
            parsedArgs = DefaultParse({"/filealign:01000", "a.vb"}, _baseDirectory)
            Assert.Equal(512, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = DefaultParse({"/filealign:02000", "a.vb"}, _baseDirectory)
            Assert.Equal(1024, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = DefaultParse({"/filealign:04000", "a.vb"}, _baseDirectory)
            Assert.Equal(2048, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = DefaultParse({"/filealign:010000", "a.vb"}, _baseDirectory)
            Assert.Equal(4096, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = DefaultParse({"/filealign:020000", "a.vb"}, _baseDirectory)
            Assert.Equal(8192, parsedArgs.EmitOptions.FileAlignment)

            ' test hex values
            parsedArgs = DefaultParse({"/filealign:0x200", "a.vb"}, _baseDirectory)
            Assert.Equal(512, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = DefaultParse({"/filealign:0x400", "a.vb"}, _baseDirectory)
            Assert.Equal(1024, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = DefaultParse({"/filealign:0x800", "a.vb"}, _baseDirectory)
            Assert.Equal(2048, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = DefaultParse({"/filealign:0x1000", "a.vb"}, _baseDirectory)
            Assert.Equal(4096, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = DefaultParse({"/filealign:0x2000", "a.vb"}, _baseDirectory)
            Assert.Equal(8192, parsedArgs.EmitOptions.FileAlignment)

            ' test default (no value)
            parsedArgs = DefaultParse({"/platform:x86", "a.vb"}, _baseDirectory)
            Assert.Equal(0, parsedArgs.EmitOptions.FileAlignment)

            ' test missing 
            parsedArgs = DefaultParse({"/filealign:", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("filealign", ":<number>"))

            ' test illegal
            parsedArgs = DefaultParse({"/filealign:0", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("filealign", "0"))
            parsedArgs = DefaultParse({"/filealign:0x", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("filealign", "0x"))
            parsedArgs = DefaultParse({"/filealign:0x0", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("filealign", "0x0"))
            parsedArgs = DefaultParse({"/filealign:-1", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("filealign", "-1"))
            parsedArgs = DefaultParse({"/filealign:-0x100", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("filealign", "-0x100"))
        End Sub

        <Fact()>
        Public Sub RemoveIntChecks()
            Dim parsedArgs = DefaultParse({"/removeintcheckS", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.False(parsedArgs.CompilationOptions.CheckOverflow)

            parsedArgs = DefaultParse({"/removeintcheckS+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.False(parsedArgs.CompilationOptions.CheckOverflow)

            parsedArgs = DefaultParse({"/removeintcheckS-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.True(parsedArgs.CompilationOptions.CheckOverflow)

            parsedArgs = DefaultParse({"/removeintchecks+", "/removeintchecks-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.True(parsedArgs.CompilationOptions.CheckOverflow)

            parsedArgs = DefaultParse({"/removeintchecks:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("removeintchecks"))

            parsedArgs = DefaultParse({"/removeintchecks:+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("removeintchecks"))

            parsedArgs = DefaultParse({"/removeintchecks+:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("removeintchecks"))
        End Sub

        <Fact()>
        Public Sub BaseAddress()
            ' This test is about what passes the parser. Even if a value was accepted by the parser it might not be considered
            ' as a valid base address later on (e.g. values >0x8000).

            ' test decimal values being treated as hex
            Dim parsedArgs = DefaultParse({"/baseaddress:0", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(0, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = DefaultParse({"/baseaddress:1024", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H1024, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = DefaultParse({"/baseaddress:2048", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H2048, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = DefaultParse({"/baseaddress:4096", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H4096, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = DefaultParse({"/baseaddress:8192", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H8192, ULong), parsedArgs.EmitOptions.BaseAddress)

            ' test hex values being treated as hex
            parsedArgs = DefaultParse({"/baseaddress:0x200", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H200, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = DefaultParse({"/baseaddress:0x400", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H400, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = DefaultParse({"/baseaddress:0x800", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H800, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = DefaultParse({"/baseaddress:0x1000", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H1000, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = DefaultParse({"/baseaddress:0xFFFFFFFFFFFFFFFF", "a.vb"}, _baseDirectory)
            Assert.Equal(ULong.MaxValue, parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = DefaultParse({"/baseaddress:FFFFFFFFFFFFFFFF", "a.vb"}, _baseDirectory)
            Assert.Equal(ULong.MaxValue, parsedArgs.EmitOptions.BaseAddress)

            ' test octal values being treated as hex
            parsedArgs = DefaultParse({"/baseaddress:00", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(0, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = DefaultParse({"/baseaddress:01024", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H1024, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = DefaultParse({"/baseaddress:02048", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H2048, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = DefaultParse({"/baseaddress:04096", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H4096, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = DefaultParse({"/baseaddress:08192", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H8192, ULong), parsedArgs.EmitOptions.BaseAddress)

            ' test default (no value)
            parsedArgs = DefaultParse({"/platform:x86", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(0, ULong), parsedArgs.EmitOptions.BaseAddress)

            ' test missing 
            parsedArgs = DefaultParse({"/baseaddress:", "a.vb"}, _baseDirectory)
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

            Dim outWriter As New StringWriter()
            Dim exitCode As Integer = New MockVisualBasicCompiler(Nothing, subDirectory, {"/nologo", "/t:library", "/out:abc.xyz", src.ToString()}).Run(outWriter, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Equal("", outWriter.ToString().Trim())

            outWriter = New StringWriter()
            exitCode = New MockVisualBasicCompiler(Nothing, baseDirectory, {"/nologo", "/libpath:temp", "/r:abc.xyz.dll", "/t:library", src.ToString()}).Run(outWriter, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Equal("", outWriter.ToString().Trim())

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact()>
        Public Sub UnableWriteOutput()
            Dim tempFolder = Temp.CreateDirectory()
            Dim baseDirectory = tempFolder.ToString()
            Dim subFolder = tempFolder.CreateDirectory("temp.dll")

            Dim src = Temp.CreateFile("a.vb")
            src.WriteAllText("Imports System")

            Dim outWriter As New StringWriter()
            Dim exitCode As Integer = New MockVisualBasicCompiler(Nothing, baseDirectory, {"/nologo", "/preferreduilang:en", "/t:library", "/out:" & subFolder.ToString(), src.ToString()}).Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            Assert.True(outWriter.ToString().Contains("error BC2012: can't open '" & subFolder.ToString() & "' for writing: ")) ' Cannot create a file when that file already exists.

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
    <text>
Imports Microsoft.VisualBasic
Class C
Dim a = vbLf
Dim b = Loc
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, "/nologo /preferreduilang:en /vbruntime /t:library " & src.ToString(), expectedRetCode:=1)
            AssertOutput(
    <text>
src.vb(5) : error BC30455: Argument not specified for parameter 'FileNumber' of 'Public Function Loc(FileNumber As Integer) As Long'.
Dim b = Loc
        ~~~
</text>, output)

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
            AssertOutput(expected.Value, output, fileName)
        End Sub

        Private Sub AssertOutput(expected As String, output As String, Optional fileName As String = "src.vb")
            output = Regex.Replace(output, "^.*" & fileName, fileName, RegexOptions.Multiline)
            output = Regex.Replace(output, "\r\n\s*\r\n", vbCrLf) ' empty strings
            output = output.Trim()
            Assert.Equal(expected.Replace(vbLf, vbCrLf).Trim, output)
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

            Dim outWriter As New StringWriter()
            Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, commandLineArguments.Concat({inputName1, inputName2}).ToArray())
            Dim exitCode As Integer = vbc.Run(outWriter, Nothing)
            If exitCode <> 0 Then
                Console.WriteLine(outWriter.ToString())
                Assert.Equal(0, exitCode)
            End If

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

            Dim outWriter As New StringWriter()
            Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, commandLineArguments.Concat({fileName}).ToArray())
            Return vbc.Run(outWriter, Nothing)
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

            Dim output As New StringWriter()
            Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
            vbc.Run(output, Nothing)

            Dim expected = ReplacePathAndVersionAndHash(result, file).Trim()
            Dim actual = output.ToString().Trim()
            Assert.Equal(expected, actual)

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

            Dim output As New StringWriter()
            Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en", "-imports:System"})
            vbc.Run(output, Nothing)

            Assert.Equal(ReplacePathAndVersionAndHash(result, file), output.ToString())

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

            Dim output As New StringWriter()
            Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
            vbc.Run(output, Nothing)

            Dim expected = ReplacePathAndVersionAndHash(result, file).Trim()
            Dim actual = output.ToString().Trim()
            Assert.Equal(expected, actual)

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

            Dim output As New StringWriter()
            Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
            vbc.Run(output, Nothing)

            Assert.Equal(ReplacePathAndVersionAndHash(result, file), output.ToString())

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

            Dim output As New StringWriter()
            Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
            vbc.Run(output, Nothing)

            Assert.Equal(ReplacePathAndVersionAndHash(result, file), output.ToString())

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

            Dim output As New StringWriter()
            Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
            vbc.Run(output, Nothing)

            Assert.Equal(ReplacePathAndVersionAndHash(result, file), output.ToString())

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

            Dim output As New StringWriter()
            Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
            vbc.Run(output, Nothing)

            Assert.Equal(ReplacePathAndVersionAndHash(result, file), output.ToString())

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

            Dim output As New StringWriter()
            Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
            vbc.Run(output, Nothing)

            Assert.Equal(ReplacePathAndVersionAndHash(result, file), output.ToString())

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        Private Shared Function ReplacePathAndVersionAndHash(result As XElement, file As TempFile) As String
            Return result.Value.Replace("PATH", file.Path).Replace("VERSION (HASH)", s_compilerVersion).Replace(vbLf, vbCrLf)
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

            Dim output As New StringWriter()
            Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/target:exe", "/preferreduilang:en", "/out:sub\a.exe"})
            Dim exitCode = vbc.Run(output, Nothing)

            Assert.Equal(1, exitCode)
            Assert.Contains("error BC2012: can't open '" + dir.Path + "\sub\a.exe' for writing", output.ToString(), StringComparison.Ordinal)

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

            Dim output As New StringWriter()
            Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en", "/target:exe", "/out:sub\"})
            Dim exitCode = vbc.Run(output, Nothing)

            Assert.Equal(1, exitCode)
            Dim message = output.ToString()
            Assert.Contains("error BC2032: File name", message, StringComparison.Ordinal)
            Assert.Contains("sub", message, StringComparison.Ordinal)

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

            Dim output As New StringWriter()
            Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en", "/target:exe", "/out:sub\ "})
            Dim exitCode = vbc.Run(output, Nothing)

            Assert.Equal(1, exitCode)
            Dim message = output.ToString()
            Assert.Contains("error BC2032: File name", message, StringComparison.Ordinal)
            Assert.Contains("sub", message, StringComparison.Ordinal)

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

            Dim output As New StringWriter()
            Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en", "/target:exe", "/out:aaa:\a.exe"})
            Dim exitCode = vbc.Run(output, Nothing)

            Assert.Equal(1, exitCode)
            Assert.Contains("error BC2032: File name 'aaa:\a.exe' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long", output.ToString(), StringComparison.Ordinal)

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

            Dim output As New StringWriter()
            Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en", "/target:exe", "/out: "})
            Dim exitCode = vbc.Run(output, Nothing)

            Assert.Equal(1, exitCode)
            Assert.Contains("error BC2006: option 'out' requires ':<file>'", output.ToString(), StringComparison.Ordinal)

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
            ' The icacls command fails on our Helix machines And it appears to be related to the use of the $ in 
            ' the username. 
            ' https://github.com/dotnet/roslyn/issues/28836
            If StringComparer.OrdinalIgnoreCase.Equals(Environment.UserDomainName, "WORKGROUP") Then
                Return
            End If

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
            Dim output As New StringWriter()
            Dim exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Contains("warning BC2025: ignoring /noconfig option because it was specified in a response file", output.ToString(), StringComparison.Ordinal)

            ' Checks the case with /noconfig inside the response file as along with /nowarn (expect to see warning)
            vbc = New MockVisualBasicCompiler(rsp, _baseDirectory, {source, "/preferreduilang:en", "/nowarn"})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Contains("warning BC2025: ignoring /noconfig option because it was specified in a response file", output.ToString(), StringComparison.Ordinal)

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
            Dim output As New StringWriter()
            Dim exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Contains("warning BC2025: ignoring /noconfig option because it was specified in a response file", output.ToString(), StringComparison.Ordinal)

            ' Checks the case with /NOCONFIG inside the response file as along with /nowarn (expect to see warning)
            vbc = New MockVisualBasicCompiler(rsp, _baseDirectory, {source, "/preferreduilang:en", "/nowarn"})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Contains("warning BC2025: ignoring /noconfig option because it was specified in a response file", output.ToString(), StringComparison.Ordinal)

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
            Dim output As New StringWriter()
            Dim exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Contains("warning BC2025: ignoring /noconfig option because it was specified in a response file", output.ToString(), StringComparison.Ordinal)

            ' Checks the case with -noconfig inside the response file as along with /nowarn (expect to see warning)
            vbc = New MockVisualBasicCompiler(rsp, _baseDirectory, {source, "/preferreduilang:en", "/nowarn"})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Contains("warning BC2025: ignoring /noconfig option because it was specified in a response file", output.ToString(), StringComparison.Ordinal)

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
            Dim output As New StringWriter()
            Dim exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC2017: could not find library 'a='", output.ToString().Trim())

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
            Dim output As New StringWriter()
            Dim exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC2006: option 'define' requires ':<symbol_list>'", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC2006: option 'define' requires ':<symbol_list>'", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define: ", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC2006: option 'define' requires ':<symbol_list>'", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:_,", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC31030: Conditional compilation constant '_ ^^ ^^ ' is not valid: Identifier expected.", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:_a,", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Equal("", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:_ a,", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC31030: Conditional compilation constant '_  ^^ ^^ a' is not valid: Identifier expected.", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:a,_,b", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC31030: Conditional compilation constant '_ ^^ ^^ ' is not valid: Identifier expected.", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:_", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC31030: Conditional compilation constant '_ ^^ ^^ ' is not valid: Identifier expected.", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:_ ", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC31030: Conditional compilation constant '_ ^^ ^^ ' is not valid: Identifier expected.", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:a,_", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC31030: Conditional compilation constant '_ ^^ ^^ ' is not valid: Identifier expected.", output.ToString().Trim())

            CleanupAllGeneratedFiles(source)
        End Sub

        Private Function GetDefaultResponseFilePath() As String
            Return Temp.CreateFile().WriteAllBytes(GetType(CommandLineTests).Assembly.GetManifestResourceStream("vbc.rsp").ReadAllBytes()).Path
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
            Dim output = New StringWriter()
            Dim exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Equal("vbc : warning BC2007: unrecognized option '/blah'; ignored", output.ToString().Trim())
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
            Dim output As New StringWriter()
            Dim exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC31030: Conditional compilation constant 'I ^^ ^^ ' is not valid: End of statement expected.", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/preferreduilang:en", "/define:I*", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC31030: Conditional compilation constant 'I ^^ ^^ ' is not valid: End of statement expected.", output.ToString().Trim())
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

            Dim vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/netcf", source})
            Dim output = New StringWriter()
            Dim exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Equal("", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/bugreport", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Equal("", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/bugreport:test.dmp", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Equal("", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/errorreport", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Equal("", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/errorreport:prompt", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Equal("", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/errorreport:queue", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Equal("", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/errorreport:send", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Equal("", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/errorreport:", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Equal("", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/bugreport:", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Equal("", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/novbruntimeref", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Equal("", output.ToString().Trim())

            ' Just to confirm case insensitive
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/errorreport:PROMPT", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Equal("", output.ToString().Trim())

            CleanupAllGeneratedFiles(source)
        End Sub

        <WorkItem(531263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531263")>
        <Fact>
        Public Sub EmptyFileName()
            Dim outWriter As New StringWriter()
            Dim exitCode = New MockVisualBasicCompiler(Nothing, _baseDirectory, {""}).Run(outWriter, Nothing)
            Assert.NotEqual(0, exitCode)

            ' error BC2032: File name '' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
            Assert.Contains("BC2032", outWriter.ToString(), StringComparison.Ordinal)
        End Sub

        <WorkItem(1119609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1119609")>
        <Fact>
        Public Sub PreferredUILang()
            Dim outWriter As New StringWriter(CultureInfo.InvariantCulture)
            Dim exitCode = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/preferreduilang"}).Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Contains("BC2006", outWriter.ToString(), StringComparison.Ordinal)

            outWriter = New StringWriter(CultureInfo.InvariantCulture)
            exitCode = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/preferreduilang:"}).Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Contains("BC2006", outWriter.ToString(), StringComparison.Ordinal)

            outWriter = New StringWriter(CultureInfo.InvariantCulture)
            exitCode = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/preferreduilang:zz"}).Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Contains("BC2038", outWriter.ToString(), StringComparison.Ordinal)

            outWriter = New StringWriter(CultureInfo.InvariantCulture)
            exitCode = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/preferreduilang:en-zz"}).Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Contains("BC2038", outWriter.ToString(), StringComparison.Ordinal)

            outWriter = New StringWriter(CultureInfo.InvariantCulture)
            exitCode = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/preferreduilang:en-US"}).Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            Assert.DoesNotContain("BC2038", outWriter.ToString(), StringComparison.Ordinal)

            outWriter = New StringWriter(CultureInfo.InvariantCulture)
            exitCode = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/preferreduilang:de"}).Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            Assert.DoesNotContain("BC2038", outWriter.ToString(), StringComparison.Ordinal)

            outWriter = New StringWriter(CultureInfo.InvariantCulture)
            exitCode = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/preferreduilang:de-AT"}).Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            Assert.DoesNotContain("BC2038", outWriter.ToString(), StringComparison.Ordinal)
        End Sub

        <Fact, WorkItem(650083, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/650083")>
        Public Sub ReservedDeviceNameAsFileName()
            ' Source file name
            Dim parsedArgs = DefaultParse({"/t:library", "con.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = DefaultParse({"/out:com1.exe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments("\\.\com1").WithLocation(1, 1))

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
            Dim vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/r:.\com3.dll", "/preferreduilang:en", source})
            Dim output = New StringWriter()
            Dim exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Contains("error BC2017: could not find library '.\com3.dll'", output.ToString(), StringComparison.Ordinal)

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/preferreduilang:en", "/link:prn.dll", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Contains("error BC2017: could not find library 'prn.dll'", output.ToString(), StringComparison.Ordinal)

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"@aux.rsp", "/preferreduilang:en", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Dim errMessage = output.ToString().Trim()
            Assert.Contains("error BC2011: unable to open response file", errMessage, StringComparison.Ordinal)
            Assert.Contains("aux.rsp", errMessage, StringComparison.Ordinal)

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/preferreduilang:en", "/vbruntime:..\con.dll", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Contains("error BC2017: could not find library '..\con.dll'", output.ToString(), StringComparison.Ordinal)

            ' Native VB compiler also ignore invalid lib paths
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/LibPath:lpt1,Lpt2,LPT9", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)

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

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
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
            AssertOutput(expected.Replace(vbCrLf, vbLf), outWriter.ToString())
            CleanupAllGeneratedFiles(file.Path)
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

            args = DefaultParse({"/additionalfile:""web.config,app.manifest""", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(1, args.AdditionalFiles.Length)
            Assert.Equal(Path.Combine(_baseDirectory, "web.config,app.manifest"), args.AdditionalFiles(0).Path)

            args = DefaultParse({"/additionalfile:\""web.config,app.manifest\""", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(1, args.AdditionalFiles.Length)
            Assert.Equal(Path.Combine(_baseDirectory, "web.config,app.manifest"), args.AdditionalFiles(0).Path)

            args = DefaultParse({"/additionalfile:\""""web.config,app.manifest""\""", "a.vb"}, _baseDirectory)
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

        <Fact>
        Public Sub ParseEditorConfig()
            Dim args = DefaultParse({"/analyzerconfig:.editorconfig", "a.vb"}, _baseDirectory)
            args.Errors.AssertNoErrors()
            Assert.Equal(Path.Combine(_baseDirectory, ".editorconfig"), args.AnalyzerConfigPaths.Single())

            args = DefaultParse({"/analyzerconfig:.editorconfig", "a.vb", "/analyzerconfig:subdir\.editorconfig"}, _baseDirectory)
            args.Errors.AssertNoErrors()
            Assert.Equal(2, args.AnalyzerConfigPaths.Length)
            Assert.Equal(Path.Combine(_baseDirectory, ".editorconfig"), args.AnalyzerConfigPaths(0))
            Assert.Equal(Path.Combine(_baseDirectory, "subdir\.editorconfig"), args.AnalyzerConfigPaths(1))

            args = DefaultParse({"/analyzerconfig:.editorconfig", "a.vb", "/analyzerconfig:.editorconfig"}, _baseDirectory)
            args.Errors.AssertNoErrors()
            Assert.Equal(2, args.AnalyzerConfigPaths.Length)
            Assert.Equal(Path.Combine(_baseDirectory, ".editorconfig"), args.AnalyzerConfigPaths(0))
            Assert.Equal(Path.Combine(_baseDirectory, ".editorconfig"), args.AnalyzerConfigPaths(1))

            args = DefaultParse({"/analyzerconfig:..\.editorconfig", "a.vb"}, _baseDirectory)
            args.Errors.AssertNoErrors()
            Assert.Equal(Path.Combine(_baseDirectory, "..\.editorconfig"), args.AnalyzerConfigPaths.Single())

            args = DefaultParse({"/analyzerconfig:.editorconfig;subdir\.editorconfig", "a.vb"}, _baseDirectory)
            args.Errors.AssertNoErrors()
            Assert.Equal(2, args.AnalyzerConfigPaths.Length)
            Assert.Equal(Path.Combine(_baseDirectory, ".editorconfig"), args.AnalyzerConfigPaths(0))
            Assert.Equal(Path.Combine(_baseDirectory, "subdir\.editorconfig"), args.AnalyzerConfigPaths(1))

            args = DefaultParse({"/analyzerconfig:.editorconfig,subdir\.editorconfig", "a.vb"}, _baseDirectory)
            args.Errors.AssertNoErrors()
            Assert.Equal(2, args.AnalyzerConfigPaths.Length)
            Assert.Equal(Path.Combine(_baseDirectory, ".editorconfig"), args.AnalyzerConfigPaths(0))
            Assert.Equal(Path.Combine(_baseDirectory, "subdir\.editorconfig"), args.AnalyzerConfigPaths(1))

            args = DefaultParse({"/analyzerconfig:.editorconfig:.editorconfig", "a.vb"}, _baseDirectory)
            args.Errors.AssertNoErrors()
            Assert.Equal(1, args.AnalyzerConfigPaths.Length)
            Assert.Equal(Path.Combine(_baseDirectory, ".editorconfig:.editorconfig"), args.AnalyzerConfigPaths(0))

            args = DefaultParse({"/analyzerconfig", "a.vb"}, _baseDirectory)
            args.Errors.AssertTheseDiagnostics(
                <errors><![CDATA[
                    BC2006: option 'analyzerconfig' requires ':<file_list>'
                    ]]>
                </errors>)
            Assert.Equal(0, args.AnalyzerConfigPaths.Length)

            args = DefaultParse({"/analyzerconfig:", "a.vb"}, _baseDirectory)
            args.Errors.AssertTheseDiagnostics(
                <errors><![CDATA[
BC2006: option 'analyzerconfig' requires ':<file_list>']]>
                </errors>)
            Assert.Equal(0, args.AnalyzerConfigPaths.Length)
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
                                             Optional expectedErrorCount As Integer = 0,
                                             Optional errorlog As Boolean = False,
                                             Optional analyzers As ImmutableArray(Of DiagnosticAnalyzer) = Nothing) As String
            Dim args = {
                            "/nologo", "/preferreduilang:en", "/t:library",
                            sourceFile.Path
                       }
            If includeCurrentAssemblyAsAnalyzerReference Then
                args = args.Append("/a:" + Assembly.GetExecutingAssembly().Location)
            End If

            If errorlog Then
                args = args.Append("/errorlog:errorlog")
            End If

            If additionalFlags IsNot Nothing Then
                args = args.Append(additionalFlags)
            End If

            Dim vbc = New MockVisualBasicCompiler(Nothing, sourceDir.Path, args, analyzers)
            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim exitCode = vbc.Run(outWriter, Nothing)
            Dim output = outWriter.ToString()

            Dim expectedExitCode = If(expectedErrorCount > 0, 1, 0)
            Assert.True(expectedExitCode = exitCode,
                        String.Format("Expected exit code to be '{0}' was '{1}'.{2}Output:{3}{4}", expectedExitCode, exitCode, Environment.NewLine, Environment.NewLine, output))

            Assert.DoesNotContain(" : hidden", output, StringComparison.Ordinal)

            If expectedInfoCount = 0 Then
                Assert.DoesNotContain(" : info", output, StringComparison.Ordinal)
            Else
                ' Info diagnostics are only logged with /errorlog.
                Assert.True(errorlog)
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
        <WorkItem(42166, "https://github.com/dotnet/roslyn/issues/42166")>
        <CombinatorialData, Theory>
        Public Sub NoWarnAndWarnAsError_InfoDiagnostic(errorlog As Boolean)
            ' NOTE: Info diagnostics are only logged on command line when /errorlog is specified. See https://github.com/dotnet/roslyn/issues/42166 for details.

            ' This assembly has an InfoDiagnosticAnalyzer type which should produce custom info
            ' diagnostics for the #Enable directives present in the compilations created in this test.
            Dim source = "Imports System
#Enable Warning"
            Dim name = "a.vb"

            Dim output = GetOutput(name, source, expectedWarningCount:=1, expectedInfoCount:=If(errorlog, 1, 0), errorlog:=errorlog)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            If errorlog Then
                Assert.Contains("a.vb(2) : info Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)
            End If

            ' TEST: Verify that custom info diagnostic Info01 can be suppressed via /nowarn.
            output = GetOutput(name, source, additionalFlags:={"/nowarn"}, errorlog:=errorlog)

            ' TEST: Verify that custom info diagnostic Info01 can be individually suppressed via /nowarn:.
            output = GetOutput(name, source, additionalFlags:={"/nowarn:Info01"}, expectedWarningCount:=1, errorlog:=errorlog)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that custom info diagnostic Info01 can never be promoted to an error via /warnaserror+.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror+", "/nowarn:42376"}, expectedInfoCount:=If(errorlog, 1, 0), errorlog:=errorlog)
            If errorlog Then
                Assert.Contains("a.vb(2) : info Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)
            End If

            ' TEST: Verify that custom info diagnostic Info01 is still reported as an info when /warnaserror- is used.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror-"}, expectedWarningCount:=1, expectedInfoCount:=If(errorlog, 1, 0), errorlog:=errorlog)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            If errorlog Then
                Assert.Contains("a.vb(2) : info Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)
            End If

            ' TEST: Verify that custom info diagnostic Info01 can be individually promoted to an error via /warnaserror:.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror:info01"}, expectedWarningCount:=1, expectedErrorCount:=1, errorlog:=errorlog)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that custom info diagnostic Info01 is still reported as an info when passed to /warnaserror-:.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror-:info01"}, expectedWarningCount:=1, expectedInfoCount:=If(errorlog, 1, 0), errorlog:=errorlog)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            If errorlog Then
                Assert.Contains("a.vb(2) : info Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)
            End If

            ' TEST: Verify /nowarn: overrides /warnaserror:.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror:Info01", "/nowarn:info01"}, expectedWarningCount:=1, errorlog:=errorlog)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify /nowarn: overrides /warnaserror:.
            output = GetOutput(name, source, additionalFlags:={"/nowarn:INFO01", "/warnaserror:Info01"}, expectedWarningCount:=1, errorlog:=errorlog)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify /nowarn: overrides /warnaserror-:.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror-:Info01", "/nowarn:info01"}, expectedWarningCount:=1, errorlog:=errorlog)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify /nowarn: overrides /warnaserror-:.
            output = GetOutput(name, source, additionalFlags:={"/nowarn:INFO01", "/warnaserror-:Info01"}, expectedWarningCount:=1, errorlog:=errorlog)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify /nowarn overrides /warnaserror:.
            output = GetOutput(name, source, additionalFlags:={"/nowarn", "/warnaserror:Info01"}, errorlog:=errorlog)

            ' TEST: Verify /nowarn overrides /warnaserror:.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror:Info01", "/nowarn"}, errorlog:=errorlog)

            ' TEST: Verify /nowarn overrides /warnaserror-:.
            output = GetOutput(name, source, additionalFlags:={"/nowarn", "/warnaserror-:Info01"}, errorlog:=errorlog)

            ' TEST: Verify /nowarn overrides /warnaserror-:.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror-:Info01", "/nowarn"}, errorlog:=errorlog)

            ' TEST: Sanity test for /nowarn and /nowarn:.
            output = GetOutput(name, source, additionalFlags:={"/nowarn", "/nowarn:Info01"}, errorlog:=errorlog)

            ' TEST: Sanity test for /nowarn and /nowarn:.
            output = GetOutput(name, source, additionalFlags:={"/nowarn:Info01", "/nowarn"}, errorlog:=errorlog)

            ' TEST: Verify that last /warnaserror[+/-]: flag on command line wins.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror+:Info01", "/warnaserror-:info01"}, expectedWarningCount:=1, expectedInfoCount:=If(errorlog, 1, 0), errorlog:=errorlog)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            If errorlog Then
                Assert.Contains("a.vb(2) : info Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)
            End If

            ' TEST: Verify that last /warnaserror[+/-]: flag on command line wins.
            output = GetOutput(name, source, additionalFlags:={"/warnaserror-:Info01", "/warnaserror+:INfo01"}, expectedWarningCount:=1, expectedErrorCount:=1, errorlog:=errorlog)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = GetOutput(name, source, additionalFlags:={"/warnaserror-", "/warnaserror+:info01"}, expectedWarningCount:=1, expectedErrorCount:=1, errorlog:=errorlog)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = GetOutput(name, source, additionalFlags:={"/warnaserror+:InFo01", "/warnaserror+", "/nowarn:42376"}, expectedErrorCount:=1, errorlog:=errorlog)
            Assert.Contains("a.vb(2) : error Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = GetOutput(name, source, additionalFlags:={"/warnaserror+:InfO01", "/warnaserror-"}, expectedWarningCount:=1, expectedErrorCount:=1, errorlog:=errorlog)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(2) : error Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = GetOutput(name, source, additionalFlags:={"/warnaserror+", "/warnaserror-:INfo01", "/nowarn:42376"}, expectedInfoCount:=If(errorlog, 1, 0), errorlog:=errorlog)
            If errorlog Then
                Assert.Contains("a.vb(2) : info Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)
            End If

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = GetOutput(name, source, additionalFlags:={"/warnaserror-", "/warnaserror-:INfo01"}, expectedWarningCount:=1, expectedInfoCount:=If(errorlog, 1, 0), errorlog:=errorlog)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            If errorlog Then
                Assert.Contains("a.vb(2) : info Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)
            End If

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = GetOutput(name, source, additionalFlags:={"/warnaserror-:Info01", "/warnaserror-"}, expectedWarningCount:=1, expectedInfoCount:=If(errorlog, 1, 0), errorlog:=errorlog)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            If errorlog Then
                Assert.Contains("a.vb(2) : info Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)
            End If

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = GetOutput(name, source, additionalFlags:={"/warnaserror+", "/warnaserror+:Info01", "/nowarn:42376"}, expectedErrorCount:=1, errorlog:=errorlog)
            Assert.Contains("a.vb(2) : error Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)

            ' TEST: Verify that specific promotions and suppressions (via /warnaserror[+/-]:) override general ones (i.e. /warnaserror[+/-]).
            output = GetOutput(name, source, additionalFlags:={"/warnaserror+:InFO01", "/warnaserror+", "/nowarn:42376"}, expectedErrorCount:=1, errorlog:=errorlog)
            Assert.Contains("a.vb(2) : error Info01: Throwing a diagnostic for #Enable", output, StringComparison.Ordinal)
        End Sub

        Private Function GetOutput(name As String,
                                   source As String,
                                   Optional includeCurrentAssemblyAsAnalyzerReference As Boolean = True,
                                   Optional additionalFlags As String() = Nothing,
                                   Optional expectedInfoCount As Integer = 0,
                                   Optional expectedWarningCount As Integer = 0,
                                   Optional expectedErrorCount As Integer = 0,
                                   Optional errorlog As Boolean = False) As String
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(name)
            file.WriteAllText(source)
            Dim output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference, additionalFlags, expectedInfoCount, expectedWarningCount, expectedErrorCount, errorlog)
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
            Dim outWriter = New StringWriter()
            Dim exitCode = vbc.Run(outWriter, Nothing)
            Assert.Equal(0, exitCode)
            Dim output = outWriter.ToString()
            Assert.Contains(New WarningDiagnosticAnalyzer().ToString(), output, StringComparison.Ordinal)
            Assert.Contains(CodeAnalysisResources.AnalyzerExecutionTimeColumnHeader, output, StringComparison.Ordinal)
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
            Dim outWriter = New StringWriter()
            Dim exitCode = vbc.Run(outWriter, Nothing)
            Assert.Equal(0, exitCode)
            Dim output = outWriter.ToString()

            ' Verify that the diagnostic reported by AnalyzerThatThrowsInGetMessage is reported, though it doesn't have the message.
            Assert.Contains(AnalyzerThatThrowsInGetMessage.Rule.Id, output, StringComparison.Ordinal)

            ' Verify that the analyzer exception diagnostic for the exception throw in AnalyzerThatThrowsInGetMessage is also reported.
            Assert.Contains(AnalyzerExecutor.AnalyzerExceptionDiagnosticId, output, StringComparison.Ordinal)
            Assert.Contains(NameOf(NotImplementedException), output, StringComparison.Ordinal)
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
            Dim outWriter = New StringWriter()
            Dim exitCode = vbc.Run(outWriter, Nothing)
            Assert.NotEqual(0, exitCode)
            Dim output = outWriter.ToString()

            ' Verify that the analyzer exception diagnostic for the exception throw in AnalyzerThatThrowsInGetMessage is also reported.
            Assert.Contains(AnalyzerExecutor.AnalyzerExceptionDiagnosticId, output, StringComparison.Ordinal)
            Assert.Contains(NameOf(NotImplementedException), output, StringComparison.Ordinal)
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
            Dim outWriter = New StringWriter()
            Dim exitCode = vbc.Run(outWriter, Nothing)
            Assert.Equal(0, exitCode)
            Dim output = outWriter.ToString()

            ' Verify that the diagnostic reported by AnalyzerReportingMisformattedDiagnostic is reported with the message format string, instead of the formatted message.
            Assert.Contains(AnalyzerThatThrowsInGetMessage.Rule.Id, output, StringComparison.Ordinal)
            Assert.Contains(AnalyzerThatThrowsInGetMessage.Rule.MessageFormat.ToString(CultureInfo.InvariantCulture), output, StringComparison.Ordinal)

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

            Dim outWriter = New StringWriter()
            Dim exitCode = vbc.Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            Dim output = outWriter.ToString()

            AssertOutput(
    String.Format(<text>
AdditionalFile.txt(1) : warning AdditionalFileDiagnostic: Additional File Diagnostic: AdditionalFile
Additional File Line 1!
~~~~~~~~~~             
vbc : warning AdditionalFileDiagnostic: Additional File Diagnostic: {0}
vbc : warning AdditionalFileDiagnostic: Additional File Diagnostic: AdditionalFile
vbc : warning AdditionalFileDiagnostic: Additional File Diagnostic: DummyFile
vbc : warning AdditionalFileDiagnostic: Additional File Diagnostic: NonExistentPath
vbc : error BC2015: the file '{1}' is not a text file
</text>.Value.ToString(),
        IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly.Location),
        Assembly.GetExecutingAssembly.Location),
    output, fileName:="AdditionalFile.txt")

            CleanupAllGeneratedFiles(source)
            CleanupAllGeneratedFiles(additionalFile)
            CleanupAllGeneratedFiles(nonCompilerInputFile)
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

            Dim output As New StringWriter()
            Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/target:exe", fileName})
            vbc.Run(output, Nothing)

            ' If "error" was localized, below assert will fail on PLOC builds. The output would be something like: "!pTCvB!vbc : !FLxft!error 表! BC30420:"
            Assert.Contains("error BC30420:", output.ToString())

            CleanupAllGeneratedFiles(file.Path)
        End Sub


        <ConditionalFact(GetType(WindowsOnly))>
        Public Sub SourceFile_BadPath()
            Dim args = DefaultParse({"e:c:\test\test.cs", "/t:library"}, _baseDirectory)
            args.Errors.Verify(Diagnostic(ERRID.FTL_InvalidInputFileName).WithArguments("e:c:\test\test.cs").WithLocation(1, 1))
        End Sub

        <ConditionalFact(GetType(WindowsOnly))>
        Public Sub FilePaths()
            Dim args = FullParse("\\unc\path\a.vb b.vb c:\path\c.vb", "e:\temp")
            Assert.Equal(
                New String() {"\\unc\path\a.vb", "e:\temp\b.vb", "c:\path\c.vb"},
                args.SourceFiles.Select(Function(x) x.Path))

            args = FullParse("\\unc\path\a.vb ""b.vb"" c:\path\c.vb", "e:\temp")
            Assert.Equal(
                New String() {"\\unc\path\a.vb", "e:\temp\b.vb", "c:\path\c.vb"},
                args.SourceFiles.Select(Function(x) x.Path))

            args = FullParse("""b"".vb""", "e:\temp")
            Assert.Equal(
                New String() {"e:\temp\b.vb"},
                args.SourceFiles.Select(Function(x) x.Path))
        End Sub

        <ConditionalFact(GetType(WindowsOnly))>
        Public Sub ReferencePathsEx()
            Dim args = FullParse("/nostdlib /vbruntime- /noconfig /r:a.dll,b.dll test.vb", "e:\temp")
            Assert.Equal(
                New String() {"a.dll", "b.dll"},
                args.MetadataReferences.Select(Function(x) x.Reference))

            args = FullParse("/nostdlib /vbruntime- /noconfig /r:""a.dll,b.dll"" test.vb", "e:\temp")
            Assert.Equal(
                New String() {"a.dll,b.dll"},
                args.MetadataReferences.Select(Function(x) x.Reference))

            args = FullParse("/nostdlib /vbruntime- /noconfig /r:""lib, ex\a.dll"",b.dll test.vb", "e:\temp")
            Assert.Equal(
                New String() {"lib, ex\a.dll", "b.dll"},
                args.MetadataReferences.Select(Function(x) x.Reference))

            args = FullParse("/nostdlib /vbruntime- /noconfig /r:""lib, ex\a.dll"" test.vb", "e:\temp")
            Assert.Equal(
                New String() {"lib, ex\a.dll"},
                args.MetadataReferences.Select(Function(x) x.Reference))
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
            Assert.Equal(
                New String() {"c:\test\a""b", "c:\test\c\d.cs"},
                args.SourceFiles.Select(Function(x) x.Path))

            args = parse("a\\""b c""\d.cs")
            Assert.Equal(
                New String() {"c:\test\a\b c\d.cs"},
                args.SourceFiles.Select(Function(x) x.Path))

            args = parse("/nostdlib /vbruntime- /r:""a.dll"",""b.dll"" c.cs")
            Assert.Equal(
                New String() {"a.dll", "b.dll"},
                args.MetadataReferences.Select(Function(x) x.Reference))

            args = parse("/nostdlib /vbruntime- /r:""a-s.dll"",""b-s.dll"" c.cs")
            Assert.Equal(
                New String() {"a-s.dll", "b-s.dll"},
                args.MetadataReferences.Select(Function(x) x.Reference))

            args = parse("/nostdlib /vbruntime- /r:""a,s.dll"",""b,s.dll"" c.cs")
            Assert.Equal(
                New String() {"a,s.dll", "b,s.dll"},
                args.MetadataReferences.Select(Function(x) x.Reference))
        End Sub

        <WorkItem(7588, "https://github.com/dotnet/roslyn/issues/7588")>
        <Fact()>
        Public Sub Version()
            Dim folderName = Temp.CreateDirectory().ToString()

            Dim argss = {
                "/version",
                "a.cs /version /preferreduilang:en",
                "/version /nologo",
                "/version /help"}

            For Each args In argss
                Dim output = ProcessUtilities.RunAndGetOutput(s_basicCompilerExecutable, args, startFolder:=folderName)
                Assert.Equal(s_compilerVersion, output.Trim())
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

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
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
        End Sub

        <Fact>
        Public Sub RefOutWithError()
            Dim dir = Temp.CreateDirectory()
            dir.CreateDirectory("ref")

            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
"Class C
    Public Shared Sub Main()
        Bad()
    End Sub
End Class")

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path,
                {"/define:_MYTYPE=""Empty"" ", "/nologo", "/out:a.dll", "/refout:ref/a.dll", "/deterministic", "/preferreduilang:en", "a.vb"})

            Dim exitCode = vbc.Run(outWriter)
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
        End Sub

        <Fact>
        Public Sub RefOnly()
            Dim dir = Temp.CreateDirectory()

            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
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

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path,
                {"/define:_MYTYPE=""Empty"" ", "/nologo", "/out:a.dll", "/refonly", "/debug", "/deterministic", "/doc:doc.xml", "a.vb"})

            Dim exitCode = vbc.Run(outWriter)
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

            Dim output As New StringWriter()

            Assert.Equal(0, New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, targetArg, outArg}).Run(output, Nothing))
            Assert.True(File.Exists(Path.Combine(dir.Path, expectedFile)), "Expected to find: " & expectedFile)
            Assert.False(File.Exists(Path.Combine(dir.Path, unexpectedFile)), "Didn't expect to find: " & unexpectedFile)

            CleanupAllGeneratedFiles(sourceFile.Path)
        End Sub

        <Fact>
        Public Sub IOFailure_DisposeOutputFile()
            Dim srcPath = MakeTrivialExe(Temp.CreateDirectory().Path)
            Dim exePath = Path.Combine(Path.GetDirectoryName(srcPath), "test.exe")
            Dim vbc = New MockVisualBasicCompiler(_baseDirectory, {"/nologo", "/preferreduilang:en", $"/out:{exePath}", srcPath})
            vbc.FileOpen = Function(filePath, mode, access, share)
                               If filePath = exePath Then
                                   Return New TestStream(backingStream:=New MemoryStream(), dispose:=Sub() Throw New IOException("Fake IOException"))
                               End If

                               Return File.Open(filePath, mode, access, share)
                           End Function

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Assert.Equal(1, vbc.Run(outWriter))
            Assert.Equal($"vbc : error BC2012: can't open '{exePath}' for writing: Fake IOException{Environment.NewLine}", outWriter.ToString())
        End Sub

        <Fact>
        Public Sub IOFailure_DisposePdbFile()
            Dim srcPath = MakeTrivialExe(Temp.CreateDirectory().Path)
            Dim exePath = Path.Combine(Path.GetDirectoryName(srcPath), "test.exe")
            Dim pdbPath = Path.ChangeExtension(exePath, "pdb")
            Dim vbc = New MockVisualBasicCompiler(_baseDirectory, {"/nologo", "/preferreduilang:en", "/debug", $"/out:{exePath}", srcPath})
            vbc.FileOpen = Function(filePath, mode, access, share)
                               If filePath = pdbPath Then
                                   Return New TestStream(backingStream:=New MemoryStream(), dispose:=Sub() Throw New IOException("Fake IOException"))
                               End If

                               Return File.Open(filePath, mode, access, share)
                           End Function

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Assert.Equal(1, vbc.Run(outWriter))
            Assert.Equal($"vbc : error BC2012: can't open '{pdbPath}' for writing: Fake IOException{Environment.NewLine}", outWriter.ToString())
        End Sub

        <Fact>
        Public Sub IOFailure_DisposeXmlFile()
            Dim srcPath = MakeTrivialExe(Temp.CreateDirectory().Path)
            Dim xmlPath = Path.Combine(Path.GetDirectoryName(srcPath), "test.xml")
            Dim vbc = New MockVisualBasicCompiler(_baseDirectory, {"/nologo", "/preferreduilang:en", $"/doc:{xmlPath}", srcPath})
            vbc.FileOpen = Function(filePath, mode, access, share)
                               If filePath = xmlPath Then
                                   Return New TestStream(backingStream:=New MemoryStream(), dispose:=Sub() Throw New IOException("Fake IOException"))
                               End If

                               Return File.Open(filePath, mode, access, share)
                           End Function

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Assert.Equal(1, vbc.Run(outWriter))
            Assert.Equal($"vbc : error BC2012: can't open '{xmlPath}' for writing: Fake IOException{Environment.NewLine}", outWriter.ToString())
        End Sub

        <Theory>
        <InlineData("portable")>
        <InlineData("full")>
        Public Sub IOFailure_DisposeSourceLinkFile(format As String)
            Dim srcPath = MakeTrivialExe(Temp.CreateDirectory().Path)
            Dim sourceLinkPath = Path.Combine(Path.GetDirectoryName(srcPath), "test.json")
            Dim vbc = New MockVisualBasicCompiler(_baseDirectory, {"/nologo", "/preferreduilang:en", "/debug:" & format, $"/sourcelink:{sourceLinkPath}", srcPath})
            vbc.FileOpen = Function(filePath, mode, access, share)
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

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Assert.Equal(1, vbc.Run(outWriter))
            Assert.Equal($"vbc : error BC2012: can't open '{sourceLinkPath}' for writing: Fake IOException{Environment.NewLine}", outWriter.ToString())
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
            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)

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
        End Sub

        <Fact>
        Public Sub StrongNameProviderWithCustomTempPath()
            Dim tempDir = Temp.CreateDirectory()
            Dim workingDir = Temp.CreateDirectory()
            workingDir.CreateFile("a.vb")

            Dim vbc = New MockVisualBasicCompiler(Nothing, New BuildPaths("", workingDir.Path, Nothing, tempDir.Path),
                              {"/features:UseLegacyStrongNameProvider", "/nostdlib", "a.vb"})
            Dim comp = vbc.CreateCompilation(TextWriter.Null, New TouchedFileLogger(), NullErrorLogger.Instance, Nothing)
            Assert.False(comp.SignUsingBuilder)
        End Sub

        Private Function MakeTrivialExe(Optional directory As String = Nothing) As String
            Return Temp.CreateFile(directory:=directory, prefix:="", extension:=".vb").WriteAllText("
Class Program
    Public Shared Sub Main()
    End Sub
End Class").Path
        End Function

        <Fact>
        <WorkItem(23525, "https://github.com/dotnet/roslyn/issues/23525")>
        Public Sub InvalidPathCharacterInPathMap()
            Dim filePath = Temp.CreateFile().WriteAllText("").Path
            Dim compiler = New MockVisualBasicCompiler(Nothing, _baseDirectory,
            {
                filePath,
                "/debug:embedded",
                "/pathmap:test\\=""",
                "/target:library",
                "/preferreduilang:en"
            })

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim exitCode = compiler.Run(outWriter)
            Assert.Equal(1, exitCode)
            Assert.Contains("vbc : error BC37253: The pathmap option was incorrectly formatted.", outWriter.ToString(), StringComparison.Ordinal)
        End Sub

        <WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/38454")>
        Public Sub TestSuppression_CompilerWarning()
            ' warning BC40008 : 'C' is obsolete
            Dim source = "
Imports System

<Obsolete>
Class C
End Class

Class D
    Inherits C
End Class"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)

            ' Verify that compiler warning BC40008 is reported.
            Dim output = VerifyOutput(dir, file, expectedWarningCount:=1,
                                      includeCurrentAssemblyAsAnalyzerReference:=False)
            Assert.Contains("warning BC40008", output, StringComparison.Ordinal)

            ' Verify that compiler warning BC40008 is suppressed with diagnostic suppressor
            ' and info diagnostic is logged with programmatic suppression information.
            Dim suppressor = New DiagnosticSuppressorForId("BC40008")

            ' Diagnostic '{0}: {1}' was programmatically suppressed by a DiagnosticSuppressor with suppression ID '{2}' and justification '{3}'
            Dim suppressionMessage = String.Format(CodeAnalysisResources.SuppressionDiagnosticDescriptorMessage,
                suppressor.SuppressionDescriptor.SuppressedDiagnosticId,
                New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "C"), Location.None).GetMessage(CultureInfo.InvariantCulture),
                suppressor.SuppressionDescriptor.Id,
                suppressor.SuppressionDescriptor.Justification)

            Dim suppressors = ImmutableArray.Create(Of DiagnosticAnalyzer)(suppressor)
            output = VerifyOutput(dir, file, expectedInfoCount:=1, expectedWarningCount:=0,
                                  includeCurrentAssemblyAsAnalyzerReference:=False,
                                  analyzers:=suppressors)
            Assert.DoesNotContain("warning BC40008", output, StringComparison.Ordinal)
            Assert.Contains("info SP0001", output, StringComparison.Ordinal)
            Assert.Contains(suppressionMessage, output, StringComparison.Ordinal)

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/38454")>
        Public Sub TestSuppression_CompilerWarningAsError()
            ' warning BC40008 : 'C' is obsolete
            Dim source = "
Imports System

<Obsolete>
Class C
End Class

Class D
    Inherits C
End Class"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)

            ' Verify that compiler warning BC40008 is reported.
            Dim output = VerifyOutput(dir, file, expectedWarningCount:=1,
                                      includeCurrentAssemblyAsAnalyzerReference:=False)
            Assert.Contains("warning BC40008", output, StringComparison.Ordinal)

            ' Verify that compiler warning BC40008 is reported as error for /warnaserror.
            output = VerifyOutput(dir, file, expectedErrorCount:=1, additionalFlags:={"/warnaserror+"},
                                  includeCurrentAssemblyAsAnalyzerReference:=False)
            Assert.Contains("error BC40008", output, StringComparison.Ordinal)

            ' Verify that compiler warning BC40008 is suppressed with diagnostic suppressor even with /warnaserror
            ' and info diagnostic is logged with programmatic suppression information.
            Dim suppressor = New DiagnosticSuppressorForId("BC40008")

            Dim suppressors = ImmutableArray.Create(Of DiagnosticAnalyzer)(suppressor)
            output = VerifyOutput(dir, file, expectedInfoCount:=1, expectedWarningCount:=0, expectedErrorCount:=0,
                                  additionalFlags:={"/warnaserror+"},
                                  includeCurrentAssemblyAsAnalyzerReference:=False,
                                  analyzers:=suppressors)
            Assert.DoesNotContain($"warning BC40008", output, StringComparison.Ordinal)
            Assert.DoesNotContain($"error BC40008", output, StringComparison.Ordinal)

            ' Diagnostic '{0}: {1}' was programmatically suppressed by a DiagnosticSuppressor with suppression ID '{2}' and justification '{3}'
            Dim suppressionMessage = String.Format(CodeAnalysisResources.SuppressionDiagnosticDescriptorMessage,
                suppressor.SuppressionDescriptor.SuppressedDiagnosticId,
                New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "C"), Location.None).GetMessage(CultureInfo.InvariantCulture),
                suppressor.SuppressionDescriptor.Id,
                suppressor.SuppressionDescriptor.Justification)
            Assert.Contains("info SP0001", output, StringComparison.Ordinal)
            Assert.Contains(suppressionMessage, output, StringComparison.Ordinal)

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")>
        <Fact>
        Public Sub TestNoSuppression_CompilerError()
            ' warning BC30203 : Identifier expected
            Dim source = "
Class
End Class"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)

            ' Verify that compiler error BC30203 is reported.
            Dim output = VerifyOutput(dir, file, expectedErrorCount:=1,
                                      includeCurrentAssemblyAsAnalyzerReference:=False)
            Assert.Contains("error BC30203", output, StringComparison.Ordinal)

            ' Verify that compiler error BC30203 cannot be suppressed with diagnostic suppressor.
            Dim analyzers = ImmutableArray.Create(Of DiagnosticAnalyzer)(New DiagnosticSuppressorForId("BC30203"))
            output = VerifyOutput(dir, file, expectedErrorCount:=1,
                                  includeCurrentAssemblyAsAnalyzerReference:=False,
                                  analyzers:=analyzers)
            Assert.Contains("error BC30203", output, StringComparison.Ordinal)

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/38454")>
        Public Sub TestSuppression_AnalyzerWarning()
            Dim source = "
Class C
End Class"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)

            ' Verify that analyzer warning is reported.
            Dim analyzer = New CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable:=True)
            Dim analyzers = ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer)
            Dim output = VerifyOutput(dir, file, expectedWarningCount:=1,
                                      includeCurrentAssemblyAsAnalyzerReference:=False,
                                      analyzers:=analyzers)
            Assert.Contains($"warning {analyzer.Descriptor.Id}", output, StringComparison.Ordinal)

            ' Verify that analyzer warning is suppressed with diagnostic suppressor
            ' and info diagnostic is logged with programmatic suppression information.
            Dim suppressor = New DiagnosticSuppressorForId(analyzer.Descriptor.Id)

            ' Diagnostic '{0}: {1}' was programmatically suppressed by a DiagnosticSuppressor with suppression ID '{2}' and justification '{3}'
            Dim suppressionMessage = String.Format(CodeAnalysisResources.SuppressionDiagnosticDescriptorMessage,
                suppressor.SuppressionDescriptor.SuppressedDiagnosticId,
                analyzer.Descriptor.MessageFormat,
                suppressor.SuppressionDescriptor.Id,
                suppressor.SuppressionDescriptor.Justification)

            Dim analyzerAndSuppressor = ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer, suppressor)
            output = VerifyOutput(dir, file, expectedInfoCount:=1, expectedWarningCount:=0,
                                  includeCurrentAssemblyAsAnalyzerReference:=False,
                                  analyzers:=analyzerAndSuppressor)
            Assert.DoesNotContain($"warning {analyzer.Descriptor.Id}", output, StringComparison.Ordinal)
            Assert.Contains("info SP0001", output, StringComparison.Ordinal)
            Assert.Contains(suppressionMessage, output, StringComparison.Ordinal)

            ' Verify that analyzer warning is reported as error for /warnaserror.
            output = VerifyOutput(dir, file, expectedErrorCount:=1,
                                  additionalFlags:={"/warnaserror+"},
                                  includeCurrentAssemblyAsAnalyzerReference:=False,
                                  analyzers:=analyzers)
            Assert.Contains($"error {analyzer.Descriptor.Id}", output, StringComparison.Ordinal)

            ' Verify that analyzer warning is suppressed with diagnostic suppressor even with /warnaserror
            ' and info diagnostic is logged with programmatic suppression information.
            output = VerifyOutput(dir, file, expectedInfoCount:=1, expectedWarningCount:=0, expectedErrorCount:=0,
                                  additionalFlags:={"/warnaserror+"},
                                  includeCurrentAssemblyAsAnalyzerReference:=False,
                                  analyzers:=analyzerAndSuppressor)
            Assert.DoesNotContain($"warning {analyzer.Descriptor.Id}", output, StringComparison.Ordinal)
            Assert.Contains("info SP0001", output, StringComparison.Ordinal)
            Assert.Contains(suppressionMessage, output, StringComparison.Ordinal)

            ' Verify that "NotConfigurable" analyzer warning cannot be suppressed with diagnostic suppressor even with /warnaserror.
            analyzer = New CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable:=False)
            suppressor = New DiagnosticSuppressorForId(analyzer.Descriptor.Id)
            analyzerAndSuppressor = ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer, suppressor)
            output = VerifyOutput(dir, file, expectedWarningCount:=1,
                                  includeCurrentAssemblyAsAnalyzerReference:=False,
                                  analyzers:=analyzerAndSuppressor)
            Assert.Contains($"warning {analyzer.Descriptor.Id}", output, StringComparison.Ordinal)

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")>
        <Fact>
        Public Sub TestNoSuppression_AnalyzerError()
            Dim source = "
Class C
End Class"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)

            ' Verify that analyzer error is reported.
            Dim analyzer = New CompilationAnalyzerWithSeverity(DiagnosticSeverity.Error, configurable:=True)
            Dim analyzers = ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer)
            Dim output = VerifyOutput(dir, file, expectedErrorCount:=1,
                                      includeCurrentAssemblyAsAnalyzerReference:=False,
                                      analyzers:=analyzers)
            Assert.Contains($"error {analyzer.Descriptor.Id}", output, StringComparison.Ordinal)

            ' Verify that analyzer error cannot be suppressed with diagnostic suppressor.
            Dim suppressor = New DiagnosticSuppressorForId(analyzer.Descriptor.Id)
            Dim analyzerAndSuppressor = ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer, suppressor)
            output = VerifyOutput(dir, file, expectedErrorCount:=1,
                                  includeCurrentAssemblyAsAnalyzerReference:=False,
                                  analyzers:=analyzerAndSuppressor)
            Assert.Contains($"error {analyzer.Descriptor.Id}", output, StringComparison.Ordinal)

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <Theory>
        <InlineData(True)>
        <InlineData(False)>
        <WorkItem(37779, "https://github.com/dotnet/roslyn/issues/37779")>
        Public Sub CompilerWarnAsErrorDoesNotEmit(ByVal warnAsError As Boolean)
            ' warning BC40008 : 'C' is obsolete
            Dim source = "
Imports System

<Obsolete>
Class C
End Class

Class D
    Inherits C
End Class"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("temp.vb")
            file.WriteAllText(source)

            Dim docName As String = "doc.xml"
            Dim additionalFlags = {$"/doc:{docName}", "/debug:full"}
            If warnAsError Then
                additionalFlags = additionalFlags.Append("/warnaserror").AsArray()
            End If

            Dim expectedErrorCount = If(warnAsError, 1, 0)
            Dim expectedWarningCount = If(Not warnAsError, 1, 0)
            Dim output = VerifyOutput(dir, file,
                                      includeCurrentAssemblyAsAnalyzerReference:=False,
                                      additionalFlags,
                                      expectedErrorCount:=expectedErrorCount,
                                      expectedWarningCount:=expectedWarningCount)

            Dim expectedOutput = If(warnAsError, "error BC40008", "warning BC40008")
            Assert.Contains(expectedOutput, output)

            Dim binaryPath As String = Path.Combine(dir.Path, "temp.dll")
            Assert.True(IO.File.Exists(binaryPath) = Not warnAsError)

            Dim pdbPath As String = Path.Combine(dir.Path, "temp.pdb")
            Assert.True(IO.File.Exists(pdbPath) = Not warnAsError)

            Dim docPath As String = Path.Combine(dir.Path, docName)
            Assert.True(IO.File.Exists(docPath) = Not warnAsError)
        End Sub

        <Theory>
        <InlineData(True)>
        <InlineData(False)>
        <WorkItem(37779, "https://github.com/dotnet/roslyn/issues/37779")>
        Public Sub AnalyzerConfigSeverityEscalationToErrorDoesNotEmit(ByVal analyzerConfigSetToError As Boolean)
            ' warning BC40008 : 'C' is obsolete
            Dim source = "
Imports System

<Obsolete>
Class C
End Class

Class D
    Inherits C
End Class"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("temp.vb")
            file.WriteAllText(source)

            Dim docName As String = "doc.xml"
            Dim additionalFlags = {$"/doc:{docName}", "/debug:full"}

            If analyzerConfigSetToError Then
                Dim analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText("
[*.vb]
dotnet_diagnostic.bc40008.severity = error")

                additionalFlags = additionalFlags.Append("/analyzerconfig:" + analyzerConfig.Path).ToArray()
            End If

            Dim expectedErrorCount = If(analyzerConfigSetToError, 1, 0)
            Dim expectedWarningCount = If(Not analyzerConfigSetToError, 1, 0)
            Dim output = VerifyOutput(dir, file,
                                      includeCurrentAssemblyAsAnalyzerReference:=False,
                                      additionalFlags,
                                      expectedErrorCount:=expectedErrorCount,
                                      expectedWarningCount:=expectedWarningCount)

            Dim expectedOutput = If(analyzerConfigSetToError, "error BC40008", "warning BC40008")
            Assert.Contains(expectedOutput, output)

            Dim binaryPath As String = Path.Combine(dir.Path, "temp.dll")
            Assert.True(IO.File.Exists(binaryPath) = Not analyzerConfigSetToError)

            Dim pdbPath As String = Path.Combine(dir.Path, "temp.pdb")
            Assert.True(IO.File.Exists(pdbPath) = Not analyzerConfigSetToError)

            Dim docPath As String = Path.Combine(dir.Path, docName)
            Assert.True(IO.File.Exists(docPath) = Not analyzerConfigSetToError)
        End Sub

        <Theory>
        <InlineData(True)>
        <InlineData(False)>
        <WorkItem(37779, "https://github.com/dotnet/roslyn/issues/37779")>
        Public Sub RulesetSeverityEscalationToErrorDoesNotEmit(ByVal rulesetSetToError As Boolean)
            ' warning BC40008 : 'C' is obsolete
            Dim source = "
Imports System

<Obsolete>
Class C
End Class

Class D
    Inherits C
End Class"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("temp.vb")
            file.WriteAllText(source)

            Dim docName As String = "doc.xml"
            Dim additionalFlags = {$"/doc:{docName}", "/debug:full"}

            If rulesetSetToError Then
                Dim rulesetSource = <?xml version="1.0" encoding="utf-8"?>
                                    <RuleSet Name="Ruleset1" Description="Test" ToolsVersion="12.0">
                                        <Rules AnalyzerId="Microsoft.CodeAnalysis" RuleNamespace="Microsoft.CodeAnalysis">
                                            <Rule Id="BC40008" Action="Error"/>
                                        </Rules>
                                    </RuleSet>

                Dim ruleSetFile = CreateRuleSetFile(rulesetSource)

                additionalFlags = additionalFlags.Append("/ruleset:" + ruleSetFile.Path).ToArray()
            End If

            Dim expectedErrorCount = If(rulesetSetToError, 1, 0)
            Dim expectedWarningCount = If(Not rulesetSetToError, 1, 0)
            Dim output = VerifyOutput(dir, file,
                                      includeCurrentAssemblyAsAnalyzerReference:=False,
                                      additionalFlags,
                                      expectedErrorCount:=expectedErrorCount,
                                      expectedWarningCount:=expectedWarningCount)

            Dim expectedOutput = If(rulesetSetToError, "error BC40008", "warning BC40008")
            Assert.Contains(expectedOutput, output)

            Dim binaryPath As String = Path.Combine(dir.Path, "temp.dll")
            Assert.True(IO.File.Exists(binaryPath) = Not rulesetSetToError)

            Dim pdbPath As String = Path.Combine(dir.Path, "temp.pdb")
            Assert.True(IO.File.Exists(pdbPath) = Not rulesetSetToError)

            Dim docPath As String = Path.Combine(dir.Path, docName)
            Assert.True(IO.File.Exists(docPath) = Not rulesetSetToError)
        End Sub

        <Theory>
        <InlineData(True)>
        <InlineData(False)>
        <WorkItem(37779, "https://github.com/dotnet/roslyn/issues/37779")>
        Public Sub AnalyzerWarnAsErrorDoesNotEmit(ByVal warnAsError As Boolean)
            Dim source = "
Class C
End Class"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("temp.vb")
            file.WriteAllText(source)

            Dim expectedErrorCount = If(warnAsError, 2, 0)
            Dim expectedWarningCount = If(Not warnAsError, 2, 0)
            Dim analyzer As DiagnosticAnalyzer = New WarningDiagnosticAnalyzer() ' Reports 2 warnings for each named type.
            Dim additionalFlags = If(warnAsError, {"/warnaserror"}, Nothing)
            Dim output = VerifyOutput(dir, file,
                                      includeCurrentAssemblyAsAnalyzerReference:=False,
                                      additionalFlags,
                                      expectedErrorCount:=expectedErrorCount,
                                      expectedWarningCount:=expectedWarningCount,
                                      analyzers:=ImmutableArray.Create(analyzer))

            Dim expectedODiagnosticSeverity = If(warnAsError, "error", "warning")
            Assert.Contains($"{expectedODiagnosticSeverity} {WarningDiagnosticAnalyzer.Warning01.Id}", output)
            Assert.Contains($"{expectedODiagnosticSeverity} {WarningDiagnosticAnalyzer.Warning03.Id}", output)

            Dim binaryPath As String = Path.Combine(dir.Path, "temp.dll")
            Assert.True(IO.File.Exists(binaryPath) = Not warnAsError)
        End Sub

        <WorkItem(42166, "https://github.com/dotnet/roslyn/issues/42166")>
        <CombinatorialData, Theory>
        Public Sub TestAnalyzerFilteringBasedOnSeverity(ByVal defaultSeverity As DiagnosticSeverity, ByVal errorlog As Boolean)
            ' This test verifies that analyzer execution is skipped at build time for the following:
            '   1. Analyzer reporting Hidden diagnostics
            '   2. Analyzer reporting Info diagnostics, when /errorlog is not specified
            Dim analyzerShouldBeSkipped = defaultSeverity = DiagnosticSeverity.Hidden OrElse
                defaultSeverity = DiagnosticSeverity.Info AndAlso Not errorlog

            ' We use an analyzer that throws an exception on every analyzer callback.
            ' So an AD0001 analyzer exception diagnostic is reported if analyzer executed, otherwise not.
            Dim analyzer = New NamedTypeAnalyzerWithConfigurableEnabledByDefault(isEnabledByDefault:=True, defaultSeverity, throwOnAllNamedTypes:=True)

            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("test.vb").WriteAllText("
Class C
End Class")
            Dim args = {"/nologo", "/t:library", "/preferreduilang:en", src.Path}
            If errorlog Then
                args = args.Append("/errorlog:errorlog")
            End If

            Dim cmd = New MockVisualBasicCompiler(Nothing, dir.Path, args, analyzer)
            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim exitCode = cmd.Run(outWriter)
            Assert.Equal(0, exitCode)
            Dim output = outWriter.ToString()

            If analyzerShouldBeSkipped Then
                Assert.Empty(output)
            Else
                Assert.Contains("warning AD0001: Analyzer 'Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers+NamedTypeAnalyzerWithConfigurableEnabledByDefault' threw an exception of type 'System.NotImplementedException'", output, StringComparison.Ordinal)
            End If
        End Sub
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

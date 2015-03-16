﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports System.Reflection.PortableExecutable
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Test.Utilities.SharedResourceHelpers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities
Imports Xunit
Imports ProprietaryTestResources = Microsoft.CodeAnalysis.Test.Resources.Proprietary

Namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine.UnitTests
    Partial Public Class CommandLineTests
        Inherits BasicTestBase

        Private ReadOnly _baseDirectory As String = TempRoot.Root
        Private Shared ReadOnly BasicCompilerExecutable As String = GetType(Vbc).Assembly.Location

        <Fact>
        <WorkItem(946954)>
        Sub CompilerBinariesAreAnyCPU()
            Assert.Equal(ProcessorArchitecture.MSIL, AssemblyName.GetAssemblyName(BasicCompilerExecutable).ProcessorArchitecture)
        End Sub

        <Fact, WorkItem(546322, "DevDiv")>
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

        <WorkItem(545247, "DevDiv")>
        <Fact()>
        Public Sub CommandLineCompilationWithQuotedMainArgument()
            ' Arguments with quoted rootnamespace and maintype are unquoted when
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
        Public Sub ParseQuotedMainTypeAndRootnamespace()
            'These options are always unquoted when parsed in VisualBasicCommandLineParser.Parse.

            Dim args = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:Test", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal("Test", args.CompilationOptions.RootNamespace)

            args = VisualBasicCommandLineParser.Default.Parse({"/main:Test", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal("Test", args.CompilationOptions.MainTypeName)

            args = VisualBasicCommandLineParser.Default.Parse({"/main:""Test""", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal("Test", args.CompilationOptions.MainTypeName)

            args = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:""Test""", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal("Test", args.CompilationOptions.RootNamespace)

            args = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:""test""", "/main:""test.Module1""", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal("test.Module1", args.CompilationOptions.MainTypeName)
            Assert.Equal("test", args.CompilationOptions.RootNamespace)

            ' Use of Cyrillic namespace
            args = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:""решения""", "/main:""решения.Module1""", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal("решения.Module1", args.CompilationOptions.MainTypeName)
            Assert.Equal("решения", args.CompilationOptions.RootNamespace)

        End Sub

        <WorkItem(722561, "DevDiv")>
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

            Assert.True(output.ToString().StartsWith(LogoLine1, StringComparison.Ordinal), "vbc should print logo and help if no args specified")
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
            Assert.Equal(<text>
Microsoft (R) Visual Basic Compiler version A.B.C.D
Copyright (C) Microsoft Corporation. All rights reserved.
</text>.Value.Replace(vbLf, vbCrLf).Trim,
                Regex.Replace(output.ToString().Trim(), "version \d+\.\d+\.\d+(\.\d+)?", "version A.B.C.D"))
            ' Privately queued builds have 3-part version numbers instead of 4.  Since we're throwing away the version number,
            ' making the last part optional will fix this.


            CleanupAllGeneratedFiles(src)
        End Sub

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
            Assert.Equal(<text>
Microsoft (R) Visual Basic Compiler version A.B.C.D
Copyright (C) Microsoft Corporation. All rights reserved.
</text>.Value.Replace(vbLf, vbCrLf).Trim,
                Regex.Replace(output.ToString().Trim(), "version \d+\.\d+\.\d+(\.\d+)?", "version A.B.C.D"))
            ' Privately queued builds have 3-part version numbers instead of 4.  Since we're throwing away the version number,
            ' making the last part optional will fix this.


            CleanupAllGeneratedFiles(src)
        End Sub

        <Fact()>
        Public Sub VbcUtf8Output_WithRedirecting_Off()
            Dim src As String = Temp.CreateFile().WriteAllText("♚", New System.Text.UTF8Encoding(False)).Path

            Dim tempOut = Temp.CreateFile()

            Dim output = RunAndGetOutput("cmd", "/C """ & BasicCompilerExecutable & """ /nologo /preferreduilang:en /t:library " & src & " > " & tempOut.Path, expectedRetCode:=1)
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

            Dim output = RunAndGetOutput("cmd", "/C """ & BasicCompilerExecutable & """ /utf8output /nologo /preferreduilang:en /t:library " & src & " > " & tempOut.Path, expectedRetCode:=1)
            Assert.Equal("", output.Trim())

            Assert.Equal(<text>
SRC.VB(1) : error BC30037: Character is not valid.

♚
~
</text>.Value.Trim().Replace(vbLf, vbCrLf), tempOut.ReadAllText().Trim().Replace(src, "SRC.VB"))


            CleanupAllGeneratedFiles(src)
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

        <WorkItem(685392, "DevDiv")>
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
            Dim args = VisualBasicCommandLineParser.Default.Parse({"/imports: System ,System.Xml ,System.Linq", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            AssertEx.Equal({"System", "System.Xml", "System.Linq"}, args.CompilationOptions.GlobalImports.Select(Function(import) import.Clause.ToString()))

            args = VisualBasicCommandLineParser.Default.Parse({"/impORt: System,,,,,", "/IMPORTs:,,,Microsoft.VisualBasic,,System.IO", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            AssertEx.Equal({"System", "Microsoft.VisualBasic", "System.IO"}, args.CompilationOptions.GlobalImports.Select(Function(import) import.Clause.ToString()))

            args = VisualBasicCommandLineParser.Default.Parse({"/impORt: System, ,, ,,", "a.vb"}, _baseDirectory)
            args.Errors.Verify(Diagnostic(ERRID.ERR_ExpectedIdentifier),
                               Diagnostic(ERRID.ERR_ExpectedIdentifier))

            args = VisualBasicCommandLineParser.Default.Parse({"/impORt:", "a.vb"}, _baseDirectory)
            args.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("import", ":<str>"))

            args = VisualBasicCommandLineParser.Default.Parse({"/impORts:", "a.vb"}, _baseDirectory)
            args.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("imports", ":<import_list>"))

            args = VisualBasicCommandLineParser.Default.Parse({"/imports", "a.vb"}, _baseDirectory)
            args.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("imports", ":<import_list>"))

            args = VisualBasicCommandLineParser.Default.Parse({"/imports+", "a.vb"}, _baseDirectory)
            args.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/imports+")) ' TODO: Dev11 reports ERR_ArgumentRequired
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

        <Fact, WorkItem(546028, "DevDiv")>
        Public Sub Win32ResourceArguments()
            Dim args As String() = {"/win32manifest:..\here\there\everywhere\nonexistent"}
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse(args, _baseDirectory)
            Dim compilation = CreateCompilationWithMscorlib(New VisualBasicSyntaxTree() {})
            Dim errors As IEnumerable(Of DiagnosticInfo) = Nothing
            CommonCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, errors)
            Assert.Equal(1, errors.Count())
            Assert.Equal(DirectCast(ERRID.ERR_UnableToReadUacManifest2, Integer), errors.First().Code)
            Assert.Equal(2, errors.First().Arguments.Count())
            args = {"/Win32icon:\bogus"}
            parsedArgs = VisualBasicCommandLineParser.Default.Parse(args, _baseDirectory)
            CommonCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, errors)
            Assert.Equal(1, errors.Count())
            Assert.Equal(DirectCast(ERRID.ERR_UnableToOpenResourceFile1, Integer), errors.First().Code)
            Assert.Equal(2, errors.First().Arguments.Count())
            args = {"/Win32Resource:\bogus"}
            parsedArgs = VisualBasicCommandLineParser.Default.Parse(args, _baseDirectory)
            CommonCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, errors)
            Assert.Equal(1, errors.Count())
            Assert.Equal(DirectCast(ERRID.ERR_UnableToOpenResourceFile1, Integer), errors.First().Code)
            Assert.Equal(2, errors.First().Arguments.Count())

            args = {"/win32manifest:foo.win32data:bar.win32data2"}
            parsedArgs = VisualBasicCommandLineParser.Default.Parse(args, _baseDirectory)
            CommonCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, errors)
            Assert.Equal(1, errors.Count())
            Assert.Equal(DirectCast(ERRID.ERR_UnableToReadUacManifest2, Integer), errors.First().Code)
            Assert.Equal(2, errors.First().Arguments.Count())
            args = {"/Win32icon:foo.win32data:bar.win32data2"}
            parsedArgs = VisualBasicCommandLineParser.Default.Parse(args, _baseDirectory)
            CommonCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, errors)
            Assert.Equal(1, errors.Count())
            Assert.Equal(DirectCast(ERRID.ERR_UnableToOpenResourceFile1, Integer), errors.First().Code)
            Assert.Equal(2, errors.First().Arguments.Count())
            args = {"/Win32Resource:foo.win32data:bar.win32data2"}
            parsedArgs = VisualBasicCommandLineParser.Default.Parse(args, _baseDirectory)
            CommonCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, errors)
            Assert.Equal(1, errors.Count())
            Assert.Equal(DirectCast(ERRID.ERR_UnableToOpenResourceFile1, Integer), errors.First().Code)
            Assert.Equal(2, errors.First().Arguments.Count())
        End Sub

        <Fact>
        Public Sub Win32IconContainsGarbage()
            Dim tmpFileName As String = Temp.CreateFile().WriteAllBytes(New Byte() {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}).Path
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/win32icon:" + tmpFileName}, _baseDirectory)
            Dim compilation = CreateCompilationWithMscorlib(New VisualBasicSyntaxTree() {})
            Dim errors As IEnumerable(Of DiagnosticInfo) = Nothing
            CommonCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, errors)
            Assert.Equal(1, errors.Count())
            Assert.Equal(DirectCast(ERRID.ERR_ErrorCreatingWin32ResourceFile, Integer), errors.First().Code)
            Assert.Equal(1, errors.First().Arguments.Count())


            CleanupAllGeneratedFiles(tmpFileName)
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

            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/win32resource", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32resource", ":<file>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/win32resource+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/win32resource+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/win32resource-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/win32resource-")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/win32icon", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32icon", ":<file>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/win32icon+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/win32icon+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/win32icon-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/win32icon-")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/win32manifest", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("win32manifest", ":<file>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/win32manifest+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/win32manifest+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/win32manifest-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/win32manifest-")) ' TODO: Dev11 reports ERR_ArgumentRequired

        End Sub

        Private Sub CheckWin32ResourceOptions(args As String(), expectedResourceFile As String, expectedIcon As String, expectedManifest As String, expectedNoManifest As Boolean, ParamArray diags As DiagnosticDescription())
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse(args.Concat({"Test.vb"}), _baseDirectory)
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

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "\somepath\someFile.foo.bar", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("someFile.foo.bar", desc.FileName)
            Assert.Equal("someFile.foo.bar", desc.ResourceName)
            Assert.True(desc.IsPublic)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "\somepath\someFile.foo.bar,someName", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("someFile.foo.bar", desc.FileName)
            Assert.Equal("someName", desc.ResourceName)
            Assert.True(desc.IsPublic)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "\somepath\someFile.foo.bar,someName,public", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("someFile.foo.bar", desc.FileName)
            Assert.Equal("someName", desc.ResourceName)
            Assert.True(desc.IsPublic)

            ' use file name in place of missing resource name
            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "\somepath\someFile.foo.bar,,private", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("someFile.foo.bar", desc.FileName)
            Assert.Equal("someFile.foo.bar", desc.ResourceName)
            Assert.False(desc.IsPublic)

            ' quoted accessibility is fine
            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "\somepath\someFile.foo.bar,,""private""", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("someFile.foo.bar", desc.FileName)
            Assert.Equal("someFile.foo.bar", desc.ResourceName)
            Assert.False(desc.IsPublic)

            ' leading commas are ignored...
            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", ",,\somepath\someFile.foo.bar,,private", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("someFile.foo.bar", desc.FileName)
            Assert.Equal("someFile.foo.bar", desc.ResourceName)
            Assert.False(desc.IsPublic)

            ' ...as long as there's no whitespace between them
            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", ", ,\somepath\someFile.foo.bar,,private", _baseDirectory, diags, embedded:=False)
            diags.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("resource", " "))
            diags.Clear()
            Assert.Null(desc)

            ' trailing commas are ignored...
            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "\somepath\someFile.foo.bar,,private", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("someFile.foo.bar", desc.FileName)
            Assert.Equal("someFile.foo.bar", desc.ResourceName)
            Assert.False(desc.IsPublic)

            ' ...even if there's whitespace between them
            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "\somepath\someFile.foo.bar,,private, ,", _baseDirectory, diags, embedded:=False)
            diags.Verify()
            diags.Clear()
            Assert.Equal("someFile.foo.bar", desc.FileName)
            Assert.Equal("someFile.foo.bar", desc.ResourceName)
            Assert.False(desc.IsPublic)

            desc = VisualBasicCommandLineParser.ParseResourceDescription("resource", "\somepath\someFile.foo.bar,someName,publi", _baseDirectory, diags, embedded:=False)
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
            Dim resourceDescription As resourceDescription

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/resource:a", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.DisplayHelp)
            resourceDescription = parsedArgs.ManifestResources.Single()
            Assert.Null(resourceDescription.FileName) ' since embedded
            Assert.Equal("a", resourceDescription.ResourceName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/res:b", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.DisplayHelp)
            resourceDescription = parsedArgs.ManifestResources.Single()
            Assert.Null(resourceDescription.FileName) ' since embedded
            Assert.Equal("b", resourceDescription.ResourceName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/linkresource:c", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.DisplayHelp)
            resourceDescription = parsedArgs.ManifestResources.Single()
            Assert.Equal("c", resourceDescription.FileName)
            Assert.Equal("c", resourceDescription.ResourceName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/linkres:d", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.DisplayHelp)
            resourceDescription = parsedArgs.ManifestResources.Single()
            Assert.Equal("d", resourceDescription.FileName)
            Assert.Equal("d", resourceDescription.ResourceName)
        End Sub

        <Fact>
        Public Sub ManagedResourceOptions_SimpleErrors()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/resource:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("resource", ":<resinfo>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/resource: ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("resource", ":<resinfo>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/resource", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("resource", ":<resinfo>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/RES+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/RES+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/res-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/res-:")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/linkresource:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("linkresource", ":<resinfo>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/linkresource: ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("linkresource", ":<resinfo>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/linkresource", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("linkresource", ":<resinfo>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/linkRES+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/linkRES+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/linkres-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/linkres-:")) ' TODO: Dev11 reports ERR_ArgumentRequired
        End Sub

        <Fact>
        Public Sub ModuleManifest()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/win32manifest:blah", "/target:module", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.WRN_IgnoreModuleManifest))

            ' Illegal, but not clobbered.
            Assert.Equal("blah", parsedArgs.Win32Manifest)
        End Sub

        <Fact>
        Public Sub ArgumentParsing()
            Dim parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"a + b"}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(True, parsedArgs.SourceFiles.Any())
            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"a + b; c"}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(True, parsedArgs.SourceFiles.Any())
            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/help"}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.Equal(True, parsedArgs.DisplayHelp)
            Assert.Equal(False, parsedArgs.SourceFiles.Any())
            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/?"}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.Equal(True, parsedArgs.DisplayHelp)
            Assert.Equal(False, parsedArgs.SourceFiles.Any())
            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"c.vbx  /langversion:10"}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(True, parsedArgs.SourceFiles.Any())
            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"c.vbx", "/langversion:-1"}, _baseDirectory)
            Assert.Equal(1, parsedArgs.Errors.Length)
            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(1, parsedArgs.SourceFiles.Length)
            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"c.vbx  /r:d /r:d.dll"}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(True, parsedArgs.SourceFiles.Any())
            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"@dd"}, _baseDirectory)
            Assert.Equal(True, parsedArgs.Errors.Any())
            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(False, parsedArgs.SourceFiles.Any())
            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"c /define:DEBUG"}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(True, parsedArgs.SourceFiles.Any())
            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"\\"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(".exe"))

            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(True, parsedArgs.SourceFiles.Any())
            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"c.vbx", "/r:d.dll", "/define:DEGUG"}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(True, parsedArgs.SourceFiles.Any())
            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"""/r d.dll"""}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(True, parsedArgs.SourceFiles.Any())
            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/r: d.dll"}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            Assert.Equal(False, parsedArgs.DisplayHelp)
            Assert.Equal(False, parsedArgs.SourceFiles.Any())
        End Sub

        <Fact>
        Public Sub LangVersion()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/langversion:9", "a.VB"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic9, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/langVERSION:9.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic9, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/langVERSION:10", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic10, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/langVERSION:10.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic10, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/langVERSION:11", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic11, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/langVERSION:11.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic11, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/langVERSION:12", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic12, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/langVERSION:12.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic12, parsedArgs.ParseOptions.LanguageVersion)

            ' default: "current version"
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic14, parsedArgs.ParseOptions.LanguageVersion)

            ' overriding
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/langVERSION:10", "/langVERSION:9.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(LanguageVersion.VisualBasic9, parsedArgs.ParseOptions.LanguageVersion)

            ' errors
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/langVERSION", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("langversion", ":<number>"))
            Assert.Equal(LanguageVersion.VisualBasic14, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/langVERSION+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/langVERSION+")) ' TODO: Dev11 reports ERR_ArgumentRequired
            Assert.Equal(LanguageVersion.VisualBasic14, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/langVERSION:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("langversion", ":<number>"))
            Assert.Equal(LanguageVersion.VisualBasic14, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/langVERSION:8", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("langversion", "8"))
            Assert.Equal(LanguageVersion.VisualBasic14, parsedArgs.ParseOptions.LanguageVersion)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/langVERSION:" & (LanguageVersion.VisualBasic12 + 1), "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("langversion", CStr(LanguageVersion.VisualBasic12 + 1)))
            Assert.Equal(LanguageVersion.VisualBasic14, parsedArgs.ParseOptions.LanguageVersion)
        End Sub

        <Fact>
        Public Sub DelaySign()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/delaysign", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.NotNull(parsedArgs.CompilationOptions.DelaySign)
            Assert.Equal(True, parsedArgs.CompilationOptions.DelaySign)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/delaysign+", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.NotNull(parsedArgs.CompilationOptions.DelaySign)
            Assert.Equal(True, parsedArgs.CompilationOptions.DelaySign)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/DELAYsign-", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.NotNull(parsedArgs.CompilationOptions.DelaySign)
            Assert.Equal(False, parsedArgs.CompilationOptions.DelaySign)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/delaysign:-", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("delaysign"))

            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/d:a=1"}, _baseDirectory) ' test default value
            parsedArgs.Errors.Verify()
            Assert.Null(parsedArgs.CompilationOptions.DelaySign)
        End Sub

        <WorkItem(546113, "DevDiv")>
        <Fact>
        Public Sub OutputVerbose()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/verbose", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Verbose, parsedArgs.OutputLevel)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/verbose+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Verbose, parsedArgs.OutputLevel)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/verbose-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Normal, parsedArgs.OutputLevel)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/VERBOSE:-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/VERBOSE:-"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/verbose-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("verbose"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/verbose+:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("verbose"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/verbOSE:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/verbOSE:"))

            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/d:a=1"}, _baseDirectory) ' test default value
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Normal, parsedArgs.OutputLevel)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/quiet", "/verbose", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Verbose, parsedArgs.OutputLevel)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/quiet", "/verbose-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Normal, parsedArgs.OutputLevel)
        End Sub

        <WorkItem(546113, "DevDiv")>
        <Fact>
        Public Sub OutputQuiet()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/quiet", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Quiet, parsedArgs.OutputLevel)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/quiet+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Quiet, parsedArgs.OutputLevel)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/quiet-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Normal, parsedArgs.OutputLevel)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/QUIET:-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/QUIET:-"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/quiet-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("quiet"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/quiet+:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("quiet"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/quiET:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/quiET:"))

            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/d:a=1"}, _baseDirectory) ' test default value
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Normal, parsedArgs.OutputLevel)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/verbose", "/quiet", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Quiet, parsedArgs.OutputLevel)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/verbose", "/quiet-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputLevel.Normal, parsedArgs.OutputLevel)
        End Sub

        <Fact>
        Public Sub Optimize()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/optimize", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OptimizationLevel.Release, parsedArgs.CompilationOptions.OptimizationLevel)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OptimizationLevel.Debug, parsedArgs.CompilationOptions.OptimizationLevel) ' default

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/OPTIMIZE+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OptimizationLevel.Release, parsedArgs.CompilationOptions.OptimizationLevel)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/optimize-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OptimizationLevel.Debug, parsedArgs.CompilationOptions.OptimizationLevel)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/optimize-", "/optimize+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OptimizationLevel.Release, parsedArgs.CompilationOptions.OptimizationLevel)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/OPTIMIZE:", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optimize"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/OPTIMIZE+:", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optimize"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/optimize-:", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optimize"))
        End Sub

        <WorkItem(546301, "DevDiv")>
        <Fact>
        Public Sub Parallel()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/parallel", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.CompilationOptions.ConcurrentBuild)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/p", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.CompilationOptions.ConcurrentBuild)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.CompilationOptions.ConcurrentBuild) ' default

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/PARALLEL+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.CompilationOptions.ConcurrentBuild)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/PARALLEL-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.CompilationOptions.ConcurrentBuild)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/PArallel-", "/PArallel+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.CompilationOptions.ConcurrentBuild)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/parallel:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("parallel"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/parallel+:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("parallel"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/parallel-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("parallel"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/P+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.CompilationOptions.ConcurrentBuild)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/P-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.CompilationOptions.ConcurrentBuild)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/P-", "/P+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.CompilationOptions.ConcurrentBuild)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/p:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("p"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/p+:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("p"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/p-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("p"))
        End Sub

        <Fact>
        Public Sub SubsystemVersionTests()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion:4.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(SubsystemVersion.Create(4, 0), parsedArgs.EmitOptions.SubsystemVersion)

            ' wrongly supported subsystem version. CompilationOptions data will be faithful to the user input.
            ' It is normalized at the time of emit.
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion:0.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify() ' no error in Dev11
            Assert.Equal(SubsystemVersion.Create(0, 0), parsedArgs.EmitOptions.SubsystemVersion)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion:0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify() ' no error in Dev11
            Assert.Equal(SubsystemVersion.Create(0, 0), parsedArgs.EmitOptions.SubsystemVersion)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion:3.99", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify() ' no warning in Dev11
            Assert.Equal(SubsystemVersion.Create(3, 99), parsedArgs.EmitOptions.SubsystemVersion)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion:4.0", "/subsystemversion:5.333", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(SubsystemVersion.Create(5, 333), parsedArgs.EmitOptions.SubsystemVersion)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("subsystemversion", ":<version>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("subsystemversion", ":<version>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/subsystemversion-")) ' TODO: Dev11 reports ERRID.ERR_ArgumentRequired

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion: ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("subsystemversion", ":<version>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion: 4.1", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments(" 4.1"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion:4 .0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments("4 .0"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion:4. 0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments("4. 0"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion:.", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments("."))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion:4.", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments("4."))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion:.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments(".0"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion:4.2 ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion:4.65536", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments("4.65536"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion:65536.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments("65536.0"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/subsystemversion:-4.0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments("-4.0"))

            ' TODO: incompatibilities: versions lower than '6.2' and 'arm', 'winmdobj', 'appcontainer'
        End Sub

        <Fact>
        Public Sub Codepage()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/CodePage:1200", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("Unicode", parsedArgs.Encoding.EncodingName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/CodePage:1200", "/CodePage:65001", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("Unicode (UTF-8)", parsedArgs.Encoding.EncodingName)

            ' errors 
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/codepage:0", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadCodepage).WithArguments("0"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/codepage:abc", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadCodepage).WithArguments("abc"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/codepage:-5", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadCodepage).WithArguments("-5"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/codepage: ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("codepage", ":<number>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/codepage:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("codepage", ":<number>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/codepage+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/codepage+")) ' Dev11 reports ERR_ArgumentRequired

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/codepage", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("codepage", ":<number>"))
        End Sub

        <Fact>
        Public Sub ChecksumAlgorithm()
            Dim parsedArgs As VisualBasicCommandLineArguments

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/checksumAlgorithm:sHa1", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(SourceHashAlgorithm.Sha1, parsedArgs.ChecksumAlgorithm)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/checksumAlgorithm:sha256", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(SourceHashAlgorithm.Sha256, parsedArgs.ChecksumAlgorithm)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(SourceHashAlgorithm.Sha1, parsedArgs.ChecksumAlgorithm)

            ' error
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/checksumAlgorithm:256", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadChecksumAlgorithm).WithArguments("256"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/checksumAlgorithm:sha-1", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadChecksumAlgorithm).WithArguments("sha-1"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/checksumAlgorithm:sha", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadChecksumAlgorithm).WithArguments("sha"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/checksumAlgorithm: ", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("checksumalgorithm", ":<algorithm>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/checksumAlgorithm:", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("checksumalgorithm", ":<algorithm>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/checksumAlgorithm", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("checksumalgorithm", ":<algorithm>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/checksumAlgorithm+", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/checksumAlgorithm+"))
        End Sub

        <Fact>
        Public Sub MainTypeName()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/main:A.B.C", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("A.B.C", parsedArgs.CompilationOptions.MainTypeName)

            ' overriding the value
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/Main:A.B.C", "/M:X.Y.Z", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("X.Y.Z", parsedArgs.CompilationOptions.MainTypeName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/MAIN: ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("main", ":<class>"))
            Assert.Null(parsedArgs.CompilationOptions.MainTypeName) ' EDMAURER Dev11 accepts and MainTypeName is " "

            ' errors 
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/maiN:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("main", ":<class>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/m", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("m", ":<class>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/m+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/m+")) ' Dev11 reports ERR_ArgumentRequired

            ' incompatibilities ignored by Dev11
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/MAIN:XYZ", "/t:library", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("XYZ", parsedArgs.CompilationOptions.MainTypeName)
            Assert.Equal(OutputKind.DynamicallyLinkedLibrary, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/MAIN:XYZ", "/t:module", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputKind.NetModule, parsedArgs.CompilationOptions.OutputKind)
        End Sub

        <Fact>
        Public Sub OptionCompare()
            Dim parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/optioncompare"}, _baseDirectory)
            Assert.Equal(1, parsedArgs.Errors.Length)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("optioncompare", ":binary|text"))
            Assert.Equal(False, parsedArgs.CompilationOptions.OptionCompareText)

            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/optioncompare:text", "/optioncompare"}, _baseDirectory)
            Assert.Equal(1, parsedArgs.Errors.Length)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("optioncompare", ":binary|text"))
            Assert.Equal(True, parsedArgs.CompilationOptions.OptionCompareText)

            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/opTioncompare:Text", "/optioncomparE:bINARY"}, _baseDirectory)
            Assert.Equal(0, parsedArgs.Errors.Length)
            Assert.Equal(False, parsedArgs.CompilationOptions.OptionCompareText)

            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/d:a=1"}, _baseDirectory) ' test default value
            Assert.Equal(0, parsedArgs.Errors.Length)
            Assert.Equal(False, parsedArgs.CompilationOptions.OptionCompareText)
        End Sub

        <Fact>
        Public Sub OptionExplicit()
            Dim parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/optiONexplicit"}, _baseDirectory)
            Assert.Equal(0, parsedArgs.Errors.Length)
            Assert.Equal(True, parsedArgs.CompilationOptions.OptionExplicit)

            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/optiONexplicit:+"}, _baseDirectory)
            Assert.Equal(1, parsedArgs.Errors.Length)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optionexplicit"))
            Assert.Equal(True, parsedArgs.CompilationOptions.OptionExplicit)

            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/optiONexplicit-:"}, _baseDirectory)
            Assert.Equal(1, parsedArgs.Errors.Length)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optionexplicit"))

            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/optionexplicit+", "/optiONexplicit-:"}, _baseDirectory)
            Assert.Equal(1, parsedArgs.Errors.Length)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optionexplicit"))

            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/optionexplicit+", "/optiONexplicit-", "/optiONexpliCIT+"}, _baseDirectory)
            Assert.Equal(0, parsedArgs.Errors.Length)
            Assert.Equal(True, parsedArgs.CompilationOptions.OptionExplicit)

            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/d:a=1"}, _baseDirectory) ' test default value
            Assert.Equal(0, parsedArgs.Errors.Length)
            Assert.Equal(True, parsedArgs.CompilationOptions.OptionExplicit)
        End Sub

        <Fact>
        Public Sub OptionInfer()
            Dim parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/optiONinfer"}, _baseDirectory)
            Assert.Equal(0, parsedArgs.Errors.Length)
            Assert.Equal(True, parsedArgs.CompilationOptions.OptionInfer)

            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/OptionInfer:+"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optioninfer"))

            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/OPTIONinfer-:"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optioninfer"))

            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/optioninfer+", "/optioninFER-:"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("optioninfer"))

            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/optioninfer+", "/optioninfeR-", "/OptionInfer+"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.CompilationOptions.OptionInfer)

            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/d:a=1"}, _baseDirectory) ' test default value
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.CompilationOptions.OptionInfer)
        End Sub

        Const VBC_VER As Double = PredefinedPreprocessorSymbols.CurrentVersionNumber

        <Fact>
        Public Sub TestDefine()
            TestDefines({"/D:a=True,b=1", "a.vb"},
                        {"a", True},
                        {"b", 1},
                        {"TARGET", "exe"},
                        {"VBC_VER", VBC_VER})

            TestDefines({"/D:a=True,b=1", "/define:a=""123"",b=False", "a.vb"},
                        {"a", "123"},
                        {"b", False},
                        {"TARGET", "exe"},
                        {"VBC_VER", VBC_VER})

            TestDefines({"/D:a=""\\\\a"",b=""\\\\\b""", "a.vb"},
                        {"a", "\\\\a"},
                        {"b", "\\\\\b"},
                        {"TARGET", "exe"},
                        {"VBC_VER", VBC_VER})

            TestDefines({"/define:DEBUG", "a.vb"},
                        {"DEBUG", True},
                        {"TARGET", "exe"},
                        {"VBC_VER", VBC_VER})

            TestDefines({"/D:TARGET=True,VBC_VER=1", "a.vb"},
                        {"TARGET", True},
                        {"VBC_VER", 1})
        End Sub

        Private Sub TestDefines(args As IEnumerable(Of String), ParamArray symbols As Object()())
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse(args, _baseDirectory)
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
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/optionStrict", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(VisualBasic.OptionStrict.On, parsedArgs.CompilationOptions.OptionStrict)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/optionStrict+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(VisualBasic.OptionStrict.On, parsedArgs.CompilationOptions.OptionStrict)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/optionStrict-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(VisualBasic.OptionStrict.Off, parsedArgs.CompilationOptions.OptionStrict)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/OptionStrict:cusTom", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(VisualBasic.OptionStrict.Custom, parsedArgs.CompilationOptions.OptionStrict)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/OptionStrict:cusTom", "/optionstrict-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(VisualBasic.OptionStrict.Off, parsedArgs.CompilationOptions.OptionStrict)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/optionstrict-", "/OptionStrict:cusTom", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(VisualBasic.OptionStrict.Custom, parsedArgs.CompilationOptions.OptionStrict)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/optionstrict:", "/OptionStrict:cusTom", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("optionstrict", ":custom"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/optionstrict:xxx", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("optionstrict", ":custom"))
        End Sub

        <WorkItem(546319, "DevDiv")>
        <WorkItem(546318, "DevDiv")>
        <WorkItem(685392, "DevDiv")>
        <Fact>
        Public Sub RootNamespace()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:One.Two.Three", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("One.Two.Three", parsedArgs.CompilationOptions.RootNamespace)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:One Two Three", "/rootnamespace:One.Two.Three", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("One.Two.Three", parsedArgs.CompilationOptions.RootNamespace)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:""One.Two.Three""", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("One.Two.Three", parsedArgs.CompilationOptions.RootNamespace)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("rootnamespace", ":<string>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("rootnamespace", ":<string>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/rootnamespace+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/rootnamespace-:")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("+"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace: ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("rootnamespace", ":<string>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace: A.B.C", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments(" A.B.C"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:[abcdef", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("[abcdef"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:abcdef]", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("abcdef]"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:[[abcdef]]", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("[[abcdef]]"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:[global]", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("[global]", parsedArgs.CompilationOptions.RootNamespace)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:foo.[global].bar", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("foo.[global].bar", parsedArgs.CompilationOptions.RootNamespace)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:foo.[bar]", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("foo.[bar]", parsedArgs.CompilationOptions.RootNamespace)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:foo$", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("foo$"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:I(", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("I("))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:_", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("_"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:[_]", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("[_]"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:__.___", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("__.___", parsedArgs.CompilationOptions.RootNamespace)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:[", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("["))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:]", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("]"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/rootnamespace:[]", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_BadNamespaceName1).WithArguments("[]"))
        End Sub

        <Fact>
        Public Sub Link_SimpleTests()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/link:a", "/link:b,,,,c", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertEx.Equal({"a", "b", "c"},
                           parsedArgs.MetadataReferences.
                                      Where(Function(res) res.Properties.EmbedInteropTypes).
                                      Select(Function(res) res.Reference))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/Link: ,,, b ,,", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertEx.Equal({" ", " b "},
                           parsedArgs.MetadataReferences.
                                      Where(Function(res) res.Properties.EmbedInteropTypes).
                                      Select(Function(res) res.Reference))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/l:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("l", ":<file_list>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/L", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("l", ":<file_list>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/l+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/l+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/link-:", "a.vb"}, _baseDirectory)
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

            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/recurse:" & dir.ToString() & "\*.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertEx.Equal({"{DIR}\a.vb", "{DIR}\b.vb", "{DIR}\d2\e.vb"}, parsedArgs.SourceFiles.Select(Function(file) file.Path.Replace(dir.ToString(), "{DIR}")))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"*.vb"}, dir.ToString())
            parsedArgs.Errors.Verify()
            AssertEx.Equal({"{DIR}\a.vb", "{DIR}\b.vb"}, parsedArgs.SourceFiles.Select(Function(file) file.Path.Replace(dir.ToString(), "{DIR}")))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/reCURSE:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("recurse", ":<wildcard>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/RECURSE: ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("recurse", ":<wildcard>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/recurse", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("recurse", ":<wildcard>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/recurse+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/recurse+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/recurse-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/recurse-:")) ' TODO: Dev11 reports ERR_ArgumentRequired

            CleanupAllGeneratedFiles(file1.Path)
            CleanupAllGeneratedFiles(file2.Path)
            CleanupAllGeneratedFiles(file3.Path)
            CleanupAllGeneratedFiles(file4.Path)
            CleanupAllGeneratedFiles(file5.Path)
        End Sub

        <WorkItem(545991, "DevDiv")>
        <WorkItem(546009, "DevDiv")>
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
            Assert.Equal("vbc : error BC2014: the value '   .' is invalid for option 'recurse'", outWriter.ToString().Trim().Replace(vbCrLf, "|"))

            outWriter = New StringWriter()
            exitCode = New MockVisualBasicCompiler(Nothing, folder.Path, {"/nologo", "/preferreduilang:en", "/t:library", "/recurse:./.", "/out:abc.dll"}).Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC2014: the value './.' is invalid for option 'recurse'", outWriter.ToString().Trim().Replace(vbCrLf, "|"))

            Dim args As VisualBasicCommandLineArguments
            Dim resolvedSourceFiles As String()

            args = VisualBasicCommandLineParser.Default.Parse({"/recurse:*.cp*", "/recurse:b\*.v*", "/out:a.dll"}, folder.Path)
            args.Errors.Verify()
            resolvedSourceFiles = args.SourceFiles.Select(Function(f) f.Path).ToArray()
            AssertEx.Equal({folder.Path + "\c.cpp", folder.Path + "\b\B_e.vb"}, resolvedSourceFiles)

            args = VisualBasicCommandLineParser.Default.Parse({"/recurse:.\\\\\\*.vb", "/out:a.dll"}, folder.Path)
            args.Errors.Verify()
            resolvedSourceFiles = args.SourceFiles.Select(Function(f) f.Path).ToArray()
            Assert.Equal(2, resolvedSourceFiles.Length)

            args = VisualBasicCommandLineParser.Default.Parse({"/recurse:.////*.vb", "/out:a.dll"}, folder.Path)
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

        <WorkItem(948285, "DevDiv")>
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
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/nostdlib", "/vbruntime-", "/r:a", "/REFERENCE:b,,,,c", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertEx.Equal({"a", "b", "c"},
                           parsedArgs.MetadataReferences.
                                      Where(Function(res) Not res.Properties.EmbedInteropTypes AndAlso Not res.Reference.EndsWith("mscorlib.dll", StringComparison.Ordinal)).
                                      Select(Function(res) res.Reference))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/Reference: ,,, b ,,", "/nostdlib", "/vbruntime-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertEx.Equal({" ", " b "},
                           parsedArgs.MetadataReferences.
                                      Where(Function(res) Not res.Properties.EmbedInteropTypes AndAlso Not res.Reference.EndsWith("mscorlib.dll", StringComparison.Ordinal)).
                                      Select(Function(res) res.Reference))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/r:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("r", ":<file_list>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/R", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("r", ":<file_list>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/reference+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/reference+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/reference-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/reference-:")) ' TODO: Dev11 reports ERR_ArgumentRequired
        End Sub

        <Fact>
        Public Sub ParseAnalyzers()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/a:foo.dll", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(1, parsedArgs.AnalyzerReferences.Length)
            Assert.Equal("foo.dll", parsedArgs.AnalyzerReferences(0).FilePath)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/analyzer:foo.dll", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(1, parsedArgs.AnalyzerReferences.Length)
            Assert.Equal("foo.dll", parsedArgs.AnalyzerReferences(0).FilePath)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/analyzer:""foo.dll""", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(1, parsedArgs.AnalyzerReferences.Length)
            Assert.Equal("foo.dll", parsedArgs.AnalyzerReferences(0).FilePath)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/a:foo.dll,bar.dll", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(2, parsedArgs.AnalyzerReferences.Length)
            Assert.Equal("foo.dll", parsedArgs.AnalyzerReferences(0).FilePath)
            Assert.Equal("bar.dll", parsedArgs.AnalyzerReferences(1).FilePath)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/a:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("a", ":<file_list>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/a", "a.vb"}, _baseDirectory)
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
            Assert.Equal("vbc : warning BC42377: The assembly " + GetType(Object).Assembly.Location + " does not contain any analyzers.", outWriter.ToString().Trim())


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
            Dim source = "Imports System \r\n Public Class Tester \r\n Public Sub Foo() \r\n Dim x As Integer \r\n End Sub \r\n End Class"

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
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse(New String() {"/ruleset:" + file.Path, "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify()
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
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse(New String() {"/ruleset:" + """" + file.Path + """", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify()
        End Sub

        <Fact>
        Public Sub RulesetSwitchParseErrors()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse(New String() {"/ruleset", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("ruleset", ":<file>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse(New String() {"/ruleset", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("ruleset", ":<file>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse(New String() {"/ruleset:blah", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.ERR_CantReadRulesetFile).WithArguments(Path.Combine(TempRoot.Root, "blah"), "File not found."))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse(New String() {"/ruleset:blah;blah.ruleset", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(
            Diagnostic(ERRID.ERR_CantReadRulesetFile).WithArguments(Path.Combine(TempRoot.Root, "blah;blah.ruleset"), "File not found."))

            Dim file = CreateRuleSetFile(New XDocument())
            parsedArgs = VisualBasicCommandLineParser.Default.Parse(New String() {"/ruleset:" + file.Path, "a.cs"}, _baseDirectory)
            'parsedArgs.Errors.Verify(
            '   Diagnostic(ERRID.ERR_CantReadRulesetFile).WithArguments(file.Path, "Root element is missing."))
            Dim err = parsedArgs.Errors.Single()

            Assert.Equal(ERRID.ERR_CantReadRulesetFile, err.Code)
            Assert.Equal(2, err.Arguments.Count)
            Assert.Equal(file.Path, DirectCast(err.Arguments(0), String))
            Dim currentUICultureName  = Thread.CurrentThread.CurrentUICulture.Name
            If currentUICultureName.Length = 0 OrElse currentUICultureName.StartsWith("en", StringComparison.OrdinalIgnoreCase) Then
                Assert.Equal(err.Arguments(1), "Root element is missing.")
            End If
        End Sub

        <Fact>
        Public Sub Target_SimpleTests()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:exe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputKind.ConsoleApplication, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/t:module", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputKind.NetModule, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:library", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputKind.DynamicallyLinkedLibrary, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/TARGET:winexe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputKind.WindowsApplication, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:winmdobj", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputKind.WindowsRuntimeMetadata, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:appcontainerexe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputKind.WindowsRuntimeApplication, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:winexe", "/T:exe", "/target:module", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(OutputKind.NetModule, parsedArgs.CompilationOptions.OutputKind)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/t", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("t", ":exe|winexe|library|module|appcontainerexe|winmdobj"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("target", ":exe|winexe|library|module|appcontainerexe|winmdobj"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:xyz", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("target", "xyz"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/T+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/T+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/TARGET-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/TARGET-:")) ' TODO: Dev11 reports ERR_ArgumentRequired
        End Sub

        <Fact>
        Public Sub Utf8Output()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/utf8output", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.Utf8Output)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/utf8output+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(True, parsedArgs.Utf8Output)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/utf8output-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.Utf8Output)

            ' default
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/nologo", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.Utf8Output)

            ' overriding
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/utf8output+", "/utf8output-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(False, parsedArgs.Utf8Output)

            ' errors
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/utf8output:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("utf8output"))

        End Sub

        <Fact>
        Public Sub Debug()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/debug-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/debug", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/debug+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/debug+", "/debug-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/debug:full", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/debug:FULL", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/debug:pdbonly", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/debug:PDBONLY", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/debug:full", "/debug:pdbonly", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/debug:pdbonly", "/debug:full", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/debug:pdbonly", "/debug-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/debug:pdbonly", "/debug-", "/debug", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/debug:pdbonly", "/debug-", "/debug+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/debug:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("debug", ":pdbonly|full"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/debug:+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("debug", "+"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/debug:invalid", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("debug", "invalid"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/debug-:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("debug"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/pdb:something", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/pdb:something"))
        End Sub

        <WorkItem(540891, "DevDiv")>
        <Fact>
        Public Sub ParseOut()
            Const baseDirectory As String = "C:\abc\def\baz"

            ' Should preserve fully qualified paths
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/out:C:\MyFolder\MyBinary.dll", "/t:library", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("MyBinary", parsedArgs.CompilationName)
            Assert.Equal("MyBinary.dll", parsedArgs.OutputFileName)
            Assert.Equal("MyBinary.dll", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal("C:\MyFolder", parsedArgs.OutputDirectory)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/out:C:\""My Folder""\MyBinary.dll", "/t:library", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("MyBinary", parsedArgs.CompilationName)
            Assert.Equal("MyBinary.dll", parsedArgs.OutputFileName)
            Assert.Equal("MyBinary.dll", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal("C:\My Folder", parsedArgs.OutputDirectory)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/out:MyBinary.dll", "/t:library", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("MyBinary", parsedArgs.CompilationName)
            Assert.Equal("MyBinary.dll", parsedArgs.OutputFileName)
            Assert.Equal("MyBinary.dll", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/out:Ignored.dll", "/out:MyBinary.dll", "/t:library", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("MyBinary", parsedArgs.CompilationName)
            Assert.Equal("MyBinary.dll", parsedArgs.OutputFileName)
            Assert.Equal("MyBinary.dll", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/out:..\MyBinary.dll", "/t:library", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("MyBinary", parsedArgs.CompilationName)
            Assert.Equal("MyBinary.dll", parsedArgs.OutputFileName)
            Assert.Equal("MyBinary.dll", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal("C:\abc\def", parsedArgs.OutputDirectory)

            ' not specified: exe
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("a", parsedArgs.CompilationName)
            Assert.Equal("a.exe", parsedArgs.OutputFileName)
            Assert.Equal("a.exe", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory)

            ' not specified: dll
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:library", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("a", parsedArgs.CompilationName)
            Assert.Equal("a.dll", parsedArgs.OutputFileName)
            Assert.Equal("a.dll", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory)

            ' not specified: module
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:module", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Null(parsedArgs.CompilationName)
            Assert.Equal("a.netmodule", parsedArgs.OutputFileName)
            Assert.Equal("a.netmodule", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory)

            ' not specified: appcontainerexe
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:appcontainerexe", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("a", parsedArgs.CompilationName)
            Assert.Equal("a.exe", parsedArgs.OutputFileName)
            Assert.Equal("a.exe", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory)

            ' not specified: winmdobj
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:winmdobj", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("a", parsedArgs.CompilationName)
            Assert.Equal("a.winmdobj", parsedArgs.OutputFileName)
            Assert.Equal("a.winmdobj", parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory)

            ' drive-relative path:
            Dim currentDrive As Char = Directory.GetCurrentDirectory()(0)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({currentDrive + ":a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(currentDrive + ":a.vb"))

            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory)

            ' UNC
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/out:\\b", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments("\\b"))

            Assert.Equal("a.exe", parsedArgs.OutputFileName)
            Assert.Equal("a", parsedArgs.CompilationName)
            Assert.Equal("a.exe", parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/out:\\server\share\file.exe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal("\\server\share", parsedArgs.OutputDirectory)
            Assert.Equal("file.exe", parsedArgs.OutputFileName)
            Assert.Equal("file", parsedArgs.CompilationName)
            Assert.Equal("file.exe", parsedArgs.CompilationOptions.ModuleName)

            ' invalid name
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/out:a.b" & vbNullChar & "b", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments("a.b" & vbNullChar & "b"))

            Assert.Equal("a.exe", parsedArgs.OutputFileName)
            Assert.Equal("a", parsedArgs.CompilationName)
            Assert.Equal("a.exe", parsedArgs.CompilationOptions.ModuleName)

            ' Temp Skip: Unicode?
            ' parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/out:a" & ChrW(&HD800) & "b.dll", "a.vb"}, _baseDirectory)
            ' parsedArgs.Errors.Verify(
            '    Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments("a" & ChrW(&HD800) & "b.dll"))

            ' Assert.Equal("a.exe", parsedArgs.OutputFileName)
            ' Assert.Equal("a", parsedArgs.CompilationName)
            ' Assert.Equal("a.exe", parsedArgs.CompilationOptions.ModuleName)

            ' Temp Skip: error message changed (path)
            'parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/out:"" a.dll""", "a.vb"}, _baseDirectory)
            'parsedArgs.Errors.Verify(
            '    Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(" a.dll"))

            'Assert.Equal("a.exe", parsedArgs.OutputFileName)
            'Assert.Equal("a", parsedArgs.CompilationName)
            'Assert.Equal("a.exe", parsedArgs.CompilationOptions.ModuleName)

            ' Dev11 reports BC2012: can't open 'a<>.z' for writing
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/out:""a<>.dll""", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments("a<>.dll"))

            Assert.Equal("a.exe", parsedArgs.OutputFileName)
            Assert.Equal("a", parsedArgs.CompilationName)
            Assert.Equal("a.exe", parsedArgs.CompilationOptions.ModuleName)

            ' bad value
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/out", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("out", ":<file>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/OUT:", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("out", ":<file>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/out+", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/out+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/out-:", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/out-:")) ' TODO: Dev11 reports ERR_ArgumentRequired

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/out:.exe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(".exe"))

            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/t:exe", "/out:.exe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(".exe"))

            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/t:library", "/out:.dll", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(".dll"))

            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/t:module", "/out:.netmodule", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal(".netmodule", parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Equal(".netmodule", parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({".vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(".exe"))

            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/t:exe", ".vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(".exe"))

            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/t:library", ".vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(".dll"))

            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/t:module", ".vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal(".netmodule", parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationName)
            Assert.Equal(".netmodule", parsedArgs.CompilationOptions.ModuleName)
        End Sub

        <Fact>
        Public Sub ParseOut2()
            ' exe
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/out:.x", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal(".x", parsedArgs.CompilationName)
            Assert.Equal(".x.exe", parsedArgs.OutputFileName)
            Assert.Equal(".x.exe", parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:winexe", "/out:.x.eXe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal(".x", parsedArgs.CompilationName)
            Assert.Equal(".x.eXe", parsedArgs.OutputFileName)
            Assert.Equal(".x.eXe", parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:winexe", "/out:.exe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(".exe"))

            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            ' dll
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:library", "/out:.x", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal(".x", parsedArgs.CompilationName)
            Assert.Equal(".x.dll", parsedArgs.OutputFileName)
            Assert.Equal(".x.dll", parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:library", "/out:.X.Dll", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal(".X", parsedArgs.CompilationName)
            Assert.Equal(".X.Dll", parsedArgs.OutputFileName)
            Assert.Equal(".X.Dll", parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:library", "/out:.dll", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(".dll"))

            Assert.Null(parsedArgs.CompilationName)
            Assert.Null(parsedArgs.OutputFileName)
            Assert.Null(parsedArgs.CompilationOptions.ModuleName)

            ' module
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:module", "/out:.x", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Null(parsedArgs.CompilationName)
            Assert.Equal(".x", parsedArgs.OutputFileName)
            Assert.Equal(".x", parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:module", "/out:x.dll", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Null(parsedArgs.CompilationName)
            Assert.Equal("x.dll", parsedArgs.OutputFileName)
            Assert.Equal("x.dll", parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:module", "/out:.x.netmodule", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Null(parsedArgs.CompilationName)
            Assert.Equal(".x.netmodule", parsedArgs.OutputFileName)
            Assert.Equal(".x.netmodule", parsedArgs.CompilationOptions.ModuleName)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/target:module", "/out:x", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Null(parsedArgs.CompilationName)
            Assert.Equal("x.netmodule", parsedArgs.OutputFileName)
            Assert.Equal("x.netmodule", parsedArgs.CompilationOptions.ModuleName)
        End Sub

        <Fact, WorkItem(531020, "DevDiv")>
        Public Sub ParseDocBreak1()
            Const baseDirectory As String = "C:\abc\def\baz"

            ' In dev11, this appears to be equivalent to /doc- (i.e. don't parse and don't output).
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:""""", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("doc", ":<file>"))
            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)
        End Sub

        <Fact, WorkItem(705173, "DevDiv")>
        Public Sub Ensure_UTF8_Explicit_Prefix_In_Documentation_Comment_File()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("src.vb")
            src.WriteAllText(
    <text>
''' &lt;summary&gt;ABC...XYZ&lt;/summary&gt;
Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = RunAndGetOutput(BasicCompilerExecutable,
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

        <Fact, WorkItem(733242, "DevDiv")>
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

                Dim output = RunAndGetOutput(BasicCompilerExecutable, String.Format("/nologo /t:library /doc+ {0}", src.ToString()), startFolder:=dir.ToString(), expectedRetCode:=0)
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

        <Fact, WorkItem(768605, "DevDiv")>
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

            Dim output = RunAndGetOutput(BasicCompilerExecutable, String.Format("/nologo /t:library /doc+ {0}", src.ToString()), startFolder:=dir.ToString(), expectedRetCode:=0)
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

            output = RunAndGetOutput(BasicCompilerExecutable, String.Format("/nologo /t:library /doc+ {0}", src.ToString()), startFolder:=dir.ToString(), expectedRetCode:=0)
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

        <Fact, WorkItem(705148, "DevDiv")>
        Public Sub Bug705148a()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
    <text>
''' &lt;summary&gt;ABC...XYZ&lt;/summary&gt;
Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = RunAndGetOutput(BasicCompilerExecutable, String.Format("/nologo /t:library /doc:abcdfg.xyz /doc+ {0}", src.ToString()), startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "a.xml")))

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(705148, "DevDiv")>
        Public Sub Bug705148b()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
    <text>
''' &lt;summary&gt;ABC...XYZ&lt;/summary&gt;
Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = RunAndGetOutput(BasicCompilerExecutable, String.Format("/nologo /t:library /doc /out:MyXml.dll {0}", src.ToString()), startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "MyXml.xml")))

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(705148, "DevDiv")>
        Public Sub Bug705148c()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
    <text>
''' &lt;summary&gt;ABC...XYZ&lt;/summary&gt;
Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = RunAndGetOutput(BasicCompilerExecutable, String.Format("/nologo /t:library /doc:doc.xml /doc+ {0}", src.ToString()), startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "a.xml")))

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(705202, "DevDiv")>
        Public Sub Bug705202a()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
    <text>
''' &lt;summary&gt;ABC...XYZ&lt;/summary&gt;
Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = RunAndGetOutput(BasicCompilerExecutable, String.Format("/nologo /t:library /doc:doc.xml /out:out.dll {0}", src.ToString()), startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "doc.xml")))

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(705202, "DevDiv")>
        Public Sub Bug705202b()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
    <text>
''' &lt;summary&gt;ABC...XYZ&lt;/summary&gt;
Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = RunAndGetOutput(BasicCompilerExecutable, String.Format("/nologo /t:library /doc:doc.xml /doc /out:out.dll {0}", src.ToString()), startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "out.xml")))

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(705202, "DevDiv")>
        Public Sub Bug705202c()
            Dim dir = Temp.CreateDirectory()
            Dim src = dir.CreateFile("a.vb")
            src.WriteAllText(
    <text>
''' &lt;summary&gt;ABC...XYZ&lt;/summary&gt;
Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf))

            Dim output = RunAndGetOutput(BasicCompilerExecutable, String.Format("/nologo /t:library /doc:doc.xml /out:out.dll /doc+ {0}", src.ToString()), startFolder:=dir.ToString())
            AssertOutput(<text></text>, output)

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "out.xml")))

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact, WorkItem(531021, "DevDiv")>
        Public Sub ParseDocBreak2()

            ' In dev11, if you give an invalid file name, the documentation comments
            ' are parsed but writing the XML file fails with (warning!) BC42311.
            Const baseDirectory As String = "C:\abc\def\baz"

            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:"" """, "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.WRN_XMLCannotWriteToXMLDocFile2).WithArguments(" ", "The system cannot find the path specified"))
            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:"" \ """, "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.WRN_XMLCannotWriteToXMLDocFile2).WithArguments(" \ ", "The system cannot find the path specified"))
            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            ' UNC
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:\\b", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.WRN_XMLCannotWriteToXMLDocFile2).WithArguments("\\b", "The system cannot find the path specified"))

            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode) ' Even though the format was incorrect

            ' invalid name:
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:a.b" + ChrW(0) + "b", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.WRN_XMLCannotWriteToXMLDocFile2).WithArguments("a.b" + ChrW(0) + "b", "The system cannot find the path specified"))

            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode) ' Even though the format was incorrect

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:a" + ChrW(55296) + "b.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.WRN_XMLCannotWriteToXMLDocFile2).WithArguments("a" + ChrW(55296) + "b.xml", "The system cannot find the path specified"))

            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode) ' Even though the format was incorrect

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:""a<>.xml""", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.WRN_XMLCannotWriteToXMLDocFile2).WithArguments("a<>.xml", "The system cannot find the path specified"))

            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode) ' Even though the format was incorrect
        End Sub

        <Fact>
        Public Sub ParseDoc()
            Const baseDirectory As String = "C:\abc\def\baz"

            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("doc", ":<file>"))
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(Path.Combine(baseDirectory, "a.xml"), parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc+", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(Path.Combine(baseDirectory, "a.xml"), parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc-", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.None, parsedArgs.ParseOptions.DocumentationMode)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc+:abc.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("doc"))
            Assert.Equal(Path.Combine(baseDirectory, "a.xml"), parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc-:a.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("doc"))
            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.None, parsedArgs.ParseOptions.DocumentationMode)

            ' Should preserve fully qualified paths
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:C:\MyFolder\MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("C:\MyFolder\MyBinary.xml", parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            ' Should handle quotes
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:C:\""My Folder""\MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("C:\My Folder\MyBinary.xml", parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            ' Should expand partially qualified paths
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(Path.Combine(baseDirectory, "MyBinary.xml"), parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            ' Should expand partially qualified paths
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:..\MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("C:\abc\def\MyBinary.xml", parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)

            ' drive-relative path:
            Dim currentDrive As Char = Directory.GetCurrentDirectory()(0)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:" + currentDrive + ":a.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.WRN_XMLCannotWriteToXMLDocFile2).WithArguments(currentDrive + ":a.xml", "The system cannot find the path specified"))

            Assert.Null(parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode) ' Even though the format was incorrect

            ' UNC
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:\\server\share\file.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal("\\server\share\file.xml", parsedArgs.DocumentationPath)
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)
        End Sub

        <Fact>
        Public Sub ParseDocAndOut()
            Const baseDirectory As String = "C:\abc\def\baz"

            ' Can specify separate directories for binary and XML output.
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:a\b.xml", "/out:c\d.exe", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal("C:\abc\def\baz\a\b.xml", parsedArgs.DocumentationPath)

            Assert.Equal("C:\abc\def\baz\c", parsedArgs.OutputDirectory)
            Assert.Equal("d.exe", parsedArgs.OutputFileName)

            ' XML does not fall back on output directory.
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:b.xml", "/out:c\d.exe", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal("C:\abc\def\baz\b.xml", parsedArgs.DocumentationPath)

            Assert.Equal("C:\abc\def\baz\c", parsedArgs.OutputDirectory)
            Assert.Equal("d.exe", parsedArgs.OutputFileName)
        End Sub

        <Fact>
        Public Sub ParseDocMultiple()
            Const baseDirectory As String = "C:\abc\def\baz"

            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc+", "/doc-", "/doc+", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)
            Assert.Equal(Path.Combine(baseDirectory, "a.xml"), parsedArgs.DocumentationPath)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc-", "/doc+", "/doc-", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(DocumentationMode.None, parsedArgs.ParseOptions.DocumentationMode)
            Assert.Null(parsedArgs.DocumentationPath)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:a.xml", "/doc-", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(DocumentationMode.None, parsedArgs.ParseOptions.DocumentationMode)
            Assert.Null(parsedArgs.DocumentationPath)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:abc.xml", "/doc+", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)
            Assert.Equal(Path.Combine(baseDirectory, "a.xml"), parsedArgs.DocumentationPath)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc-", "/doc:a.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)
            Assert.Equal(Path.Combine(baseDirectory, "a.xml"), parsedArgs.DocumentationPath)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc+", "/doc:a.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode)
            Assert.Equal(Path.Combine(baseDirectory, "a.xml"), parsedArgs.DocumentationPath)
        End Sub

        <Fact>
        Public Sub ParseErrorLog()
            Const baseDirectory As String = "C:\abc\def\baz"

            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/errorlog:", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("errorlog", ":<file>"))
            Assert.Null(parsedArgs.ErrorLogPath)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/errorlog", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("errorlog", ":<file>"))
            Assert.Null(parsedArgs.ErrorLogPath)

            ' Should preserve fully qualified paths
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/errorlog:C:\MyFolder\MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("C:\MyFolder\MyBinary.xml", parsedArgs.ErrorLogPath)

            ' Should handle quotes
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/errorlog:C:\""My Folder""\MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("C:\My Folder\MyBinary.xml", parsedArgs.ErrorLogPath)

            ' Should expand partially qualified paths
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/errorlog:MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(Path.Combine(baseDirectory, "MyBinary.xml"), parsedArgs.ErrorLogPath)

            ' Should expand partially qualified paths
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/errorlog:..\MyBinary.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("C:\abc\def\MyBinary.xml", parsedArgs.ErrorLogPath)

            ' drive-relative path:
            Dim currentDrive As Char = Directory.GetCurrentDirectory()(0)
            Dim filePath = currentDrive + ":a.xml"
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/errorlog:" + filePath, "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify(
                Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments(filePath))

            Assert.Null(parsedArgs.ErrorLogPath)

            ' UNC
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/errorlog:\\server\share\file.xml", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal("\\server\share\file.xml", parsedArgs.ErrorLogPath)
        End Sub

        <Fact>
        Public Sub ParseErrorLogAndOut()
            Const baseDirectory As String = "C:\abc\def\baz"

            ' Can specify separate directories for binary and error log output.
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/errorlog:a\b.xml", "/out:c\d.exe", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal("C:\abc\def\baz\a\b.xml", parsedArgs.ErrorLogPath)

            Assert.Equal("C:\abc\def\baz\c", parsedArgs.OutputDirectory)
            Assert.Equal("d.exe", parsedArgs.OutputFileName)

            ' error log does not fall back on output directory.
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/errorlog:b.xml", "/out:c\d.exe", "a.vb"}, baseDirectory)
            parsedArgs.Errors.Verify()

            Assert.Equal("C:\abc\def\baz\b.xml", parsedArgs.ErrorLogPath)

            Assert.Equal("C:\abc\def\baz\c", parsedArgs.OutputDirectory)
            Assert.Equal("d.exe", parsedArgs.OutputFileName)
        End Sub

        <Fact>
        Public Sub KeyContainerAndKeyFile()
            ' KEYCONTAINER
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/KeyContainer:key-cont-name", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("key-cont-name", parsedArgs.CompilationOptions.CryptoKeyContainer)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/KEYcontainer", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("keycontainer", ":<string>"))
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyContainer)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/keycontainer-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/keycontainer-"))
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyContainer)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/keycontainer:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("keycontainer", ":<string>"))
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyContainer)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/keycontainer: ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("keycontainer", ":<string>"))
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyContainer)

            ' KEYFILE
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/keyfile:\somepath\s""ome Fil""e.foo.bar", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("\somepath\some File.foo.bar", parsedArgs.CompilationOptions.CryptoKeyFile)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/keyFile", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("keyfile", ":<file>"))
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyFile)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/keyfile-", "a.cs"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/keyfile-"))
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyFile)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/keyfile: ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("keyfile", ":<file>"))
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyFile)

            ' default value
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(Nothing, parsedArgs.CompilationOptions.CryptoKeyContainer)
            Assert.Equal(Nothing, parsedArgs.CompilationOptions.CryptoKeyFile)

            ' keyfile/keycontainer conflicts 
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/keycontainer:a", "/keyfile:b", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(Nothing, parsedArgs.CompilationOptions.CryptoKeyContainer)
            Assert.Equal("b", parsedArgs.CompilationOptions.CryptoKeyFile)

            ' keyfile/keycontainer conflicts 
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/keyfile:b", "/keycontainer:a", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal("a", parsedArgs.CompilationOptions.CryptoKeyContainer)
            Assert.Equal(Nothing, parsedArgs.CompilationOptions.CryptoKeyFile)

        End Sub

        <Fact>
        Public Sub ReferencePaths()
            Dim parsedArgs As VisualBasicCommandLineArguments
            parsedArgs = VisualBasicCommandLineParser.Interactive.Parse({"/rp:a;b", "/referencePath:c", "a.vb"}, _baseDirectory)
            Assert.Equal(False, parsedArgs.Errors.Any())
            AssertEx.Equal({RuntimeEnvironment.GetRuntimeDirectory(),
                            Path.Combine(_baseDirectory, "a"),
                            Path.Combine(_baseDirectory, "b"),
                            Path.Combine(_baseDirectory, "c")},
                           parsedArgs.ReferencePaths,
                           StringComparer.OrdinalIgnoreCase)
        End Sub

        <Fact, WorkItem(530088, "DevDiv")>
        Public Sub Platform()
            ' test recognizing all options
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/platform:X86", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CodeAnalysis.Platform.X86, parsedArgs.CompilationOptions.Platform)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/platform:x64", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CodeAnalysis.Platform.X64, parsedArgs.CompilationOptions.Platform)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/platform:itanium", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CodeAnalysis.Platform.Itanium, parsedArgs.CompilationOptions.Platform)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/platform:anycpu", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CodeAnalysis.Platform.AnyCpu, parsedArgs.CompilationOptions.Platform)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/platform:anycpu32bitpreferred", "/t:exe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CodeAnalysis.Platform.AnyCpu32BitPreferred, parsedArgs.CompilationOptions.Platform)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/platform:anycpu32bitpreferred", "/t:appcontainerexe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CodeAnalysis.Platform.AnyCpu32BitPreferred, parsedArgs.CompilationOptions.Platform)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/platform:arm", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CodeAnalysis.Platform.Arm, parsedArgs.CompilationOptions.Platform)

            ' test default (AnyCPU)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/debug-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CodeAnalysis.Platform.AnyCpu, parsedArgs.CompilationOptions.Platform)

            ' test missing 
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/platform:", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("platform", ":<string>"))
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/platform", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("platform", ":<string>"))
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/platform+", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/platform+")) ' TODO: Dev11 reports ERR_ArgumentRequired

            ' test illegal input
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/platform:abcdef", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("platform", "abcdef"))

            ' test overriding
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/platform:anycpu32bitpreferred", "/platform:anycpu", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(CodeAnalysis.Platform.AnyCpu, parsedArgs.CompilationOptions.Platform)

            ' test illegal
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/platform:anycpu32bitpreferred", "/t:library", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_LibAnycpu32bitPreferredConflict).WithArguments("Platform", "AnyCpu32BitPreferred").WithLocation(1, 1))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/platform:anycpu", "/platform:anycpu32bitpreferred", "/target:winmdobj", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_LibAnycpu32bitPreferredConflict).WithArguments("Platform", "AnyCpu32BitPreferred").WithLocation(1, 1))
        End Sub

        <Fact()>
        Public Sub FileAlignment()
            ' test recognizing all options
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:512", "a.vb"}, _baseDirectory)
            Assert.Equal(512, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:1024", "a.vb"}, _baseDirectory)
            Assert.Equal(1024, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:2048", "a.vb"}, _baseDirectory)
            Assert.Equal(2048, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:4096", "a.vb"}, _baseDirectory)
            Assert.Equal(4096, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:8192", "a.vb"}, _baseDirectory)
            Assert.Equal(8192, parsedArgs.EmitOptions.FileAlignment)

            ' test oct values
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:01000", "a.vb"}, _baseDirectory)
            Assert.Equal(512, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:02000", "a.vb"}, _baseDirectory)
            Assert.Equal(1024, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:04000", "a.vb"}, _baseDirectory)
            Assert.Equal(2048, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:010000", "a.vb"}, _baseDirectory)
            Assert.Equal(4096, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:020000", "a.vb"}, _baseDirectory)
            Assert.Equal(8192, parsedArgs.EmitOptions.FileAlignment)

            ' test hex values
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:0x200", "a.vb"}, _baseDirectory)
            Assert.Equal(512, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:0x400", "a.vb"}, _baseDirectory)
            Assert.Equal(1024, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:0x800", "a.vb"}, _baseDirectory)
            Assert.Equal(2048, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:0x1000", "a.vb"}, _baseDirectory)
            Assert.Equal(4096, parsedArgs.EmitOptions.FileAlignment)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:0x2000", "a.vb"}, _baseDirectory)
            Assert.Equal(8192, parsedArgs.EmitOptions.FileAlignment)

            ' test default (no value)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/platform:x86", "a.vb"}, _baseDirectory)
            Assert.Equal(0, parsedArgs.EmitOptions.FileAlignment)

            ' test missing 
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("filealign", ":<number>"))

            ' test illegal
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:0", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("filealign", "0"))
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:0x", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("filealign", "0x"))
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:0x0", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("filealign", "0x0"))
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:-1", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("filealign", "-1"))
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/filealign:-0x100", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("filealign", "-0x100"))
        End Sub

        <Fact()>
        Public Sub RemoveIntChecks()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/removeintcheckS", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.False(parsedArgs.CompilationOptions.CheckOverflow)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/removeintcheckS+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.False(parsedArgs.CompilationOptions.CheckOverflow)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/removeintcheckS-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.True(parsedArgs.CompilationOptions.CheckOverflow)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/removeintchecks+", "/removeintchecks-", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.True(parsedArgs.CompilationOptions.CheckOverflow)

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/removeintchecks:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("removeintchecks"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/removeintchecks:+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("removeintchecks"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/removeintchecks+:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("removeintchecks"))
        End Sub

        <Fact()>
        Public Sub BaseAddress()
            ' This test is about what passes the parser. Even if a value was accepted by the parser it might not be considered
            ' as a valid base address later on (e.g. values >0x8000).

            ' test decimal values being treated as hex
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/baseaddress:0", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(0, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/baseaddress:1024", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H1024, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/baseaddress:2048", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H2048, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/baseaddress:4096", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H4096, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/baseaddress:8192", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H8192, ULong), parsedArgs.EmitOptions.BaseAddress)

            ' test hex values being treated as hex
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/baseaddress:0x200", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H200, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/baseaddress:0x400", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H400, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/baseaddress:0x800", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H800, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/baseaddress:0x1000", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H1000, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/baseaddress:0xFFFFFFFFFFFFFFFF", "a.vb"}, _baseDirectory)
            Assert.Equal(ULong.MaxValue, parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/baseaddress:FFFFFFFFFFFFFFFF", "a.vb"}, _baseDirectory)
            Assert.Equal(ULong.MaxValue, parsedArgs.EmitOptions.BaseAddress)

            ' test octal values being treated as hex
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/baseaddress:00", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(0, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/baseaddress:01024", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H1024, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/baseaddress:02048", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H2048, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/baseaddress:04096", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H4096, ULong), parsedArgs.EmitOptions.BaseAddress)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/baseaddress:08192", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(&H8192, ULong), parsedArgs.EmitOptions.BaseAddress)

            ' test default (no value)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/platform:x86", "a.vb"}, _baseDirectory)
            Assert.Equal(CType(0, ULong), parsedArgs.EmitOptions.BaseAddress)

            ' test missing 
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/baseaddress:", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("baseaddress", ":<number>"))

            ' test illegal
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/baseaddress:0x10000000000000000", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("baseaddress", "0x10000000000000000"))
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/BASEADDRESS:-1", "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("baseaddress", "-1"))
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/BASEADDRESS:" + ULong.MaxValue.ToString, "a.vb"}, _baseDirectory)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("baseaddress", ULong.MaxValue.ToString))
        End Sub

        <Fact()>
        Public Sub BinaryFile()
            Dim binaryPath = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path
            Dim outWriter As New StringWriter()
            Dim exitCode As Integer = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/preferreduilang:en", binaryPath}).Run(outWriter, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC2015: the file '" + binaryPath + "' is not a text file", outWriter.ToString.Trim())

            CleanupAllGeneratedFiles(binaryPath)
        End Sub

        <Fact()>
        Public Sub AddModule()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/nostdlib", "/vbruntime-", "/addMODULE:c:\;d:\x\y\z,abc;;", "a.vb"}, _baseDirectory)
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

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/ADDMODULE", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("addmodule", ":<file_list>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/addmodule:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("addmodule", ":<file_list>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/addmodule+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/addmodule+")) ' TODO: Dev11 reports ERR_ArgumentRequired
        End Sub

        <Fact()>
        Public Sub LibPathsAndLibEnvVariable()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/libpath:c:\;d:\x\y\z;abc;;", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertReferencePathsEqual(parsedArgs.ReferencePaths, Nothing, "c:\", "d:\x\y\z", Path.Combine(_baseDirectory, "abc"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/libpath:c:\Windows", "/libpath:abc\def; ; ; ", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertReferencePathsEqual(parsedArgs.ReferencePaths, Nothing, "c:\Windows", Path.Combine(_baseDirectory, "abc\def"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/libpath", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("libpath", ":<path_list>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/libpath:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("libpath", ":<path_list>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/libpath+", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/libpath+")) ' TODO: Dev11 reports ERR_ArgumentRequired
        End Sub

        <Fact(), WorkItem(546005, "DevDiv")>
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
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/libpath:c:lib2", "/sdkpath:<>;d:\sdk1", "/vbruntime*", "/nostdlib", "a.vb"}, _baseDirectory)

            ' invalid paths are ignored
            parsedArgs.Errors.Verify()
            AssertReferencePathsEqual(parsedArgs.ReferencePaths, "d:\sdk1")

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/sdkpath:c:\Windows", "/sdkpath:d:\Windows", "/vbruntime*", "/nostdlib", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertReferencePathsEqual(parsedArgs.ReferencePaths, "d:\Windows")

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/sdkpath:""c:\Windows;d:\blah""", "a.vb"}, _baseDirectory)   'Entire path string is wrapped in quotes
            AssertReferencePathsEqual(parsedArgs.ReferencePaths, "c:\Windows", "d:\blah")

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/libpath:""c:\Windows;d:\blah""", "/sdkpath:c:\lib2", "a.vb"}, _baseDirectory)   'Entire path string is wrapped in quotes
            AssertReferencePathsEqual(parsedArgs.ReferencePaths, "c:\lib2", "c:\Windows", "d:\blah")

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/sdkpath", "/vbruntime*", "/nostdlib", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("sdkpath", ":<path>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/sdkpath:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("sdkpath", ":<path>"))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/sdkpath+", "/vbruntime*", "/nostdlib", "a.vb"}, _baseDirectory)
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

            Dim output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /preferreduilang:en /vbruntime /t:library " & src.ToString(), expectedRetCode:=1)
            AssertOutput(
    <text>
src.vb(5) : error BC30455: Argument not specified for parameter 'FileNumber' of 'Public Function Loc(FileNumber As Integer) As Long'.
Dim b = Loc
        ~~~
</text>, output)

            output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /preferreduilang:en /vbruntime+ /t:library " & src.ToString(), expectedRetCode:=1)
            AssertOutput(
    <text>
src.vb(5) : error BC30455: Argument not specified for parameter 'FileNumber' of 'Public Function Loc(FileNumber As Integer) As Long'.
Dim b = Loc
        ~~~
</text>, output)

            output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /preferreduilang:en /vbruntime* /t:library /r:System.dll " & src.ToString(), expectedRetCode:=1)
            AssertOutput(
    <text>
src.vb(5) : error BC30451: 'Loc' is not declared. It may be inaccessible due to its protection level.
Dim b = Loc
        ~~~
</text>, output)

            output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /preferreduilang:en /vbruntime+ /vbruntime:abc /vbruntime* /t:library /r:System.dll " & src.ToString(), expectedRetCode:=1)
            AssertOutput(
    <text>
src.vb(5) : error BC30451: 'Loc' is not declared. It may be inaccessible due to its protection level.
Dim b = Loc
        ~~~
</text>, output)

            output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /preferreduilang:en /vbruntime+ /vbruntime:abc /t:library " & src.ToString(), expectedRetCode:=1)
            AssertOutput(
    <text>
vbc : error BC2017: could not find library 'abc'
</text>, output)

            Dim newVbCore = dir.CreateFile("Microsoft.VisualBasic.dll")
            newVbCore.WriteAllBytes(File.ReadAllBytes(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "Microsoft.VisualBasic.dll")))

            output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /preferreduilang:en /vbruntime:" & newVbCore.ToString() & " /t:library " & src.ToString(), expectedRetCode:=1)
            AssertOutput(
    <text>
src.vb(5) : error BC30455: Argument not specified for parameter 'FileNumber' of 'Public Function Loc(FileNumber As Integer) As Long'.
Dim b = Loc
        ~~~
</text>, output)


            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <WorkItem(997208)>
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

            Dim output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /preferreduilang:en /nostdlib /r:mscorlib.dll /vbruntime- /t:library /d:_MyType=\""Empty\"" " & src.ToString(), expectedRetCode:=1)
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

            CreateCompilationWithMscorlibAndVBRuntime(<compilation><file/></compilation>, opt).GetDiagnostics().Verify(Diagnostic(ERRID.ERR_VBCoreNetModuleConflict))

            opt = opt.WithOutputKind(OutputKind.DynamicallyLinkedLibrary)
            opt.Errors.Verify()

            CreateCompilationWithMscorlibAndVBRuntime(<compilation><file/></compilation>, opt).GetDiagnostics().Verify()
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

            Dim output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /preferreduilang:en /nostdlib /sdkpath:l:\x /t:library " & src.ToString(), expectedRetCode:=1, startFolder:=dir.Path)
            AssertOutput(
    <text>
        vbc : error BC2017: could not find library 'Microsoft.VisualBasic.dll'
        </text>, output)

            output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /preferreduilang:en /nostdlib /r:mscorlib.dll /vbruntime- /sdkpath:c:folder /t:library " & src.ToString(), expectedRetCode:=1, startFolder:=dir.Path)
            AssertOutput(
    <text> 
vbc : error BC2017: could not find library 'mscorlib.dll'
</text>, output)

            output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /preferreduilang:en /nostdlib /sdkpath:" & dir.Path & " /t:library " & src.ToString(), expectedRetCode:=1, startFolder:=dir.Path)
            AssertOutput(
    <text>
vbc : error BC2017: could not find library 'Microsoft.VisualBasic.dll'
</text>, output.Replace(dir.Path, "{SDKPATH}"))

            ' Create 'System.Runtime.dll'
            Dim sysRuntime = dir.CreateFile("System.Runtime.dll")
            sysRuntime.WriteAllBytes(File.ReadAllBytes(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "System.Runtime.dll")))

            output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /preferreduilang:en /nostdlib /sdkpath:" & dir.Path & " /t:library " & src.ToString(), expectedRetCode:=1, startFolder:=dir.Path)
            AssertOutput(
    <text>
vbc : error BC2017: could not find library 'Microsoft.VisualBasic.dll'
</text>, output.Replace(dir.Path, "{SDKPATH}"))

            ' trash in 'System.Runtime.dll'
            sysRuntime.WriteAllBytes({0, 1, 2, 3, 4, 5})

            output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /preferreduilang:en /nostdlib /sdkpath:" & dir.Path & " /t:library " & src.ToString(), expectedRetCode:=1, startFolder:=dir.Path)
            AssertOutput(
    <text>
vbc : error BC2017: could not find library 'Microsoft.VisualBasic.dll'
</text>, output.Replace(dir.Path, "{SDKPATH}"))

            ' Create 'mscorlib.dll'
            Dim msCorLib = dir.CreateFile("mscorlib.dll")
            msCorLib.WriteAllBytes(File.ReadAllBytes(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "mscorlib.dll")))

            ' NOT: both libraries exist, but 'System.Runtime.dll' is invalid, so we need to pick up 'mscorlib.dll'
            output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /nostdlib /sdkpath:" & dir.Path & " /t:library /vbruntime* /r:" & Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "System.dll") & " " & src.ToString(), startFolder:=dir.Path)
            AssertOutput(<text></text>, output.Replace(dir.Path, "{SDKPATH}")) ' SUCCESSFUL BUILD with 'mscorlib.dll' and embedded VbCore

            File.Delete(sysRuntime.Path)

            ' NOTE: only 'mscorlib.dll' exists
            output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /nostdlib /sdkpath:" & dir.Path & " /t:library /vbruntime* /r:" & Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "System.dll") & " " & src.ToString(), startFolder:=dir.Path)
            AssertOutput(<text></text>, output.Replace(dir.Path, "{SDKPATH}"))

            File.Delete(msCorLib.Path)

            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <WorkItem(598158, "DevDiv")>
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
            output = RunAndGetOutput(BasicCompilerExecutable, cmdNoStdLibNoRuntime, startFolder:=dir.Path, expectedRetCode:=1)
            AssertOutput(<text>vbc : error BC2017: could not find library 'mscorlib.dll'</text>, output.Replace(dir.Path, "{SDKPATH}"))

            ' Create '<dir>\fldr2\mscorlib.dll'
            Dim msCorLib = subFolder2.CreateFile("mscorlib.dll")
            msCorLib.WriteAllBytes(File.ReadAllBytes(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "mscorlib.dll")))

            ' NOTE: only 'mscorlib.dll' exists
            output = RunAndGetOutput(BasicCompilerExecutable, cmdNoStdLibNoRuntime, startFolder:=dir.Path)
            AssertOutput(<text></text>, output.Replace(dir.Path, "{SDKPATH}"))

            output = RunAndGetOutput(BasicCompilerExecutable, cmd, startFolder:=dir.Path, expectedRetCode:=1)
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

            Dim output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /preferreduilang:en /nostdlib /t:library " & src.ToString(), startFolder:=dir.Path, expectedRetCode:=1)
            Assert.Contains("error BC30002: Type 'Global.System.ComponentModel.EditorBrowsable' is not defined.", output, StringComparison.Ordinal)

            output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /nostdlib /define:_MYTYPE=\""Empty\"" /t:library " & src.ToString(), startFolder:=dir.Path)
            AssertOutput(<text></text>, output)

            output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /preferreduilang:en /nostdlib /sdkpath:x:\ /vbruntime- /define:_MYTYPE=\""Empty\"" /t:library " & src.ToString(), expectedRetCode:=1, startFolder:=dir.Path)
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

            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/libpath:c:\lib2;", "@" & file.ToString(), "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            AssertReferencePathsEqual(parsedArgs.ReferencePaths, Nothing, Path.GetDirectoryName(file.ToString()) + "\", "c:\lib2")

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
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/highentropyva", "a.vb"}, _baseDirectory)
            Assert.True(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/highentropyva+", "a.vb"}, _baseDirectory)
            Assert.True(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/highentropyva-", "a.vb"}, _baseDirectory)
            Assert.False(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace)
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/highentropyva:+", "a.vb"}, _baseDirectory)
            Assert.False(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/highentropyva:+"))
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/highentropyva:", "a.vb"}, _baseDirectory)
            Assert.False(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace)
            Verify(parsedArgs.Errors, Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/highentropyva:"))
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/highentropyva+ /highentropyva-", "a.vb"}, _baseDirectory)
            Assert.False(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace)
        End Sub

        <Fact>
        Public Sub Win32ResQuotes()
            Dim responseFile As String() = {
                " /win32resource:d:\\""abc def""\a""b c""d\a.res"
            }

            Dim args = VisualBasicCommandLineParser.Default.Parse(VisualBasicCommandLineParser.ParseResponseLines(responseFile), "c:\")
            Assert.Equal("d:\abc def\ab cd\a.res", args.Win32ResourceFile)

            responseFile = {
                " /win32icon:d:\\""abc def""\a""b c""d\a.ico"
            }

            args = VisualBasicCommandLineParser.Default.Parse(VisualBasicCommandLineParser.ParseResponseLines(responseFile), "c:\")
            Assert.Equal("d:\abc def\ab cd\a.ico", args.Win32Icon)

            responseFile = {
                " /win32manifest:d:\\""abc def""\a""b c""d\a.manifest"
            }

            args = VisualBasicCommandLineParser.Default.Parse(VisualBasicCommandLineParser.ParseResponseLines(responseFile), "c:\")
            Assert.Equal("d:\abc def\ab cd\a.manifest", args.Win32Manifest)
        End Sub

        <Fact>
        Public Sub ResourceOnlyCompile()
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/resource:foo.vb,ed", "/out:e.dll"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/resource:foo.vb,ed"}, _baseDirectory)
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

        <WorkItem(545773, "DevDiv")>
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
                commandLineArguments:={"/target:library", "/out:foo"},
                expectedOutputName:="foo.dll")
        End Sub

        <WorkItem(545773, "DevDiv")>
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
                commandLineArguments:={"/target:library", "/out:foo. "},
                expectedOutputName:="foo.dll")
        End Sub

        <WorkItem(545773, "DevDiv")>
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
                commandLineArguments:={"/target:library", "/out:foo.a"},
                expectedOutputName:="foo.a.dll")
        End Sub

        <WorkItem(545773, "DevDiv")>
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
                commandLineArguments:={"/target:module", "/out:foo.a"},
                expectedOutputName:="foo.a")
        End Sub

        <WorkItem(545773, "DevDiv")>
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
                commandLineArguments:={"/target:module", "/out:foo.a . . . . "},
                expectedOutputName:="foo.a")
        End Sub

        <WorkItem(545773, "DevDiv")>
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
                commandLineArguments:={"/target:module", "/out:foo. . . . . "},
                expectedOutputName:="foo.netmodule")
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
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)

            ' Test for /warnaserrors
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/warnaserror", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Error, parsedArgs.CompilationOptions.GeneralDiagnosticOption)

            ' Test for /warnaserrors+
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/warnaserror+", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Error, parsedArgs.CompilationOptions.GeneralDiagnosticOption)

            ' Test for /warnaserrors:
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/warnaserror:", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)

            ' Test for /warnaserrors:42024,42025
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/warnaserror:42024,42025", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)
            AssertSpecificDiagnostics({42024, 42025}, {ReportDiagnostic.Error, ReportDiagnostic.Error}, parsedArgs)

            ' Test for /warnaserrors+:
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/warnaserror+:", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)

            ' Test for /warnaserrors+:42024,42025
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/warnaserror+:42024,42025", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)
            AssertSpecificDiagnostics({42024, 42025}, {ReportDiagnostic.Error, ReportDiagnostic.Error}, parsedArgs)

            ' Test for /warnaserrors-
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/warnaserror-", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)

            ' Test for /warnaserrors-:
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/warnaserror-:", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)

            ' Test for /warnaserrors-:42024,42025
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/warnaserror-:42024,42025", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)
            AssertSpecificDiagnostics({42024, 42025}, {ReportDiagnostic.Default, ReportDiagnostic.Default}, parsedArgs)

            ' Test for /nowarn
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/nowarn", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Suppress, parsedArgs.CompilationOptions.GeneralDiagnosticOption)

            ' Test for /nowarn:
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/nowarn:", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)

            ' Test for /nowarn:42024,42025
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/nowarn:42024,42025", "a.vb"}, _baseDirectory)
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption)
            AssertSpecificDiagnostics({42024, 42025}, {ReportDiagnostic.Suppress, ReportDiagnostic.Suppress}, parsedArgs)
        End Sub

        <Fact()>
        Public Sub WarningsErrors()
            ' Previous versions of the compiler used to report warnings (BC2026, BC2014)
            ' whenever an unrecognized warning code was supplied via /nowarn or /warnaserror.
            ' We no longer generate a warning in such cases.

            ' Test for /warnaserrors:1
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/warnaserror:1", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            ' Test for /warnaserrors:abc
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/warnaserror:abc", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            ' Test for /nowarn:1
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/nowarn:1", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            ' Test for /nowarn:abc
            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/nowarn:abc", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
        End Sub

        <WorkItem(545025, "DevDiv")>
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

        <WorkItem(545214, "DevDiv")>
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

    Function foo()
    End Function
End Module
                    </file>
                </compilation>

            Dim result =
                    <file name="output">Microsoft (R) Visual Basic Compiler version VERSION
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
PATH(11) : warning BC42105: Function 'foo' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.

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

            Dim version As String = FileVersionInfo.GetVersionInfo(GetType(VisualBasicCompiler).Assembly.Location).FileVersion
            Dim expected = result.Value.Replace("PATH", file.Path).Replace("VERSION", version).Replace(vbLf, vbCrLf).Trim()
            Dim actual = output.ToString().Trim()
            Assert.Equal(expected, actual)


            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545214, "DevDiv")>
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
        Dim d As delegateType = AddressOf a.Foo
    End Sub

    <Extension()> _
    Public Function Foo(ByVal x As ArgIterator) as Integer
	Return 1
    End Function
End Module
]]>
                    </file>
                </compilation>

            Dim result =
                    <file name="output">Microsoft (R) Visual Basic Compiler version VERSION
Copyright (C) Microsoft Corporation. All rights reserved.

PATH(9) : error BC36640: Instance of restricted type 'ArgIterator' cannot be used in a lambda expression.

        Dim d As delegateType = AddressOf a.Foo
                                          ~    
</file>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Dim output As New StringWriter()
            Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en", "-imports:System"})
            vbc.Run(output, Nothing)

            Dim version As String = FileVersionInfo.GetVersionInfo(GetType(VisualBasicCompiler).Assembly.Location).FileVersion
            Assert.Equal(result.Value.Replace("PATH", file.Path).Replace("VERSION", version).Replace(vbLf, vbCrLf), output.ToString())

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545214, "DevDiv")>
        <Fact()>
        Public Sub ErrorMessageWithSquiggles_03()
            ' It verifies the case where the squiggles covers the error span with tabs in it.
            Dim source = "Module Module1" + vbCrLf +
                         "  Sub Main()" + vbCrLf +
                         "      Dim x As Integer = ""a" + vbTab + vbTab + vbTab + "b""c ' There is a tab in the string." + vbCrLf +
                         "  End Sub" + vbCrLf +
                         "End Module" + vbCrLf

            Dim result = <file name="output">Microsoft (R) Visual Basic Compiler version VERSION
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

            Dim version As String = FileVersionInfo.GetVersionInfo(GetType(VisualBasicCompiler).Assembly.Location).FileVersion
            Dim expected = result.Value.Replace("PATH", file.Path).Replace("VERSION", version).Replace(vbLf, vbCrLf).Trim()
            Dim actual = output.ToString().Trim()
            Assert.Equal(expected, actual)

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545214, "DevDiv")>
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
                    <file name="output">Microsoft (R) Visual Basic Compiler version VERSION
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

            Dim version As String = FileVersionInfo.GetVersionInfo(GetType(VisualBasicCompiler).Assembly.Location).FileVersion
            Assert.Equal(result.Value.Replace("PATH", file.Path).Replace("VERSION", version).Replace(vbLf, vbCrLf), output.ToString())

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545214, "DevDiv")>
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
                    <file name="output">Microsoft (R) Visual Basic Compiler version VERSION
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

            Dim version As String = FileVersionInfo.GetVersionInfo(GetType(VisualBasicCompiler).Assembly.Location).FileVersion
            Assert.Equal(result.Value.Replace("PATH", file.Path).Replace("VERSION", version).Replace(vbLf, vbCrLf), output.ToString())

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545214, "DevDiv")>
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
                    <file name="output">Microsoft (R) Visual Basic Compiler version VERSION
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

            Dim version As String = FileVersionInfo.GetVersionInfo(GetType(VisualBasicCompiler).Assembly.Location).FileVersion
            Assert.Equal(result.Value.Replace("PATH", file.Path).Replace("VERSION", version).Replace(vbLf, vbCrLf), output.ToString())

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545214, "DevDiv")>
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
                    <file name="output">Microsoft (R) Visual Basic Compiler version VERSION
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

            Dim version As String = FileVersionInfo.GetVersionInfo(GetType(VisualBasicCompiler).Assembly.Location).FileVersion
            Assert.Equal(result.Value.Replace("PATH", file.Path).Replace("VERSION", version).Replace(vbLf, vbCrLf), output.ToString())

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(531606, "DevDiv")>
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
    <file name="output">Microsoft (R) Visual Basic Compiler version VERSION
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

            Dim version As String = FileVersionInfo.GetVersionInfo(GetType(VisualBasicCompiler).Assembly.Location).FileVersion
            Assert.Equal(result.Value.Replace("PATH", file.Path).Replace("VERSION", version).Replace(vbLf, vbCrLf), output.ToString())

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545247, "DevDiv")>
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

        <WorkItem(545247, "DevDiv")>
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

        <WorkItem(545247, "DevDiv")>
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

        <WorkItem(545247, "DevDiv")>
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

        <WorkItem(545247, "DevDiv")>
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
            ' Class <UTF8 Cyryllic Character>
            ' End Class
            Dim source() As Byte = {
                                    &H43, &H6C, &H61, &H73, &H73, &H20, &HD0, &H96, &HD, &HA,
                                    &H45, &H6E, &H64, &H20, &H43, &H6C, &H61, &H73, &H73
                                   }

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllBytes(source)

            Dim output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /t:library " & file.ToString(), startFolder:=dir.Path)
            Assert.Equal("", output) ' Autodetected UTF8, NO ERROR

            output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /preferreduilang:en /t:library /codepage:20127 " & file.ToString(), expectedRetCode:=1, startFolder:=dir.Path) ' 20127: US-ASCII
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
                Case outputKind.ConsoleApplication
                    outputFileName = "Test.exe"
                    target = "exe"
                Case outputKind.WindowsApplication
                    outputFileName = "Test.exe"
                    target = "winexe"
                Case outputKind.DynamicallyLinkedLibrary
                    outputFileName = "Test.dll"
                    target = "library"
                Case outputKind.NetModule
                    outputFileName = "Test.netmodule"
                    target = "module"
                Case outputKind.WindowsRuntimeMetadata
                    outputFileName = "Test.winmdobj"
                    target = "winmdobj"
                Case outputKind.WindowsRuntimeApplication
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
            Dim resourceId As String = If(outputKind = outputKind.DynamicallyLinkedLibrary, "#2", "#1")

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

        <WorkItem(530221, "DevDiv")>
        <Fact()>
        Public Sub Bug15538()
            Dim folder = Temp.CreateDirectory()
            Dim source As String = folder.CreateFile("src.vb").WriteAllText("").Path
            Dim ref As String = folder.CreateFile("ref.dll").WriteAllText("").Path

            Try
                Dim output = RunAndGetOutput("cmd", "/C icacls " & ref & " /inheritance:r /Q")
                Assert.Equal("Successfully processed 1 files; Failed processing 0 files", output.Trim())

                output = RunAndGetOutput("cmd", "/C icacls " & ref & " /deny %USERDOMAIN%\%USERNAME%:(r,WDAC) /Q")
                Assert.Equal("Successfully processed 1 files; Failed processing 0 files", output.Trim())

                output = RunAndGetOutput("cmd", "/C """ & BasicCompilerExecutable & """ /nologo /preferreduilang:en /r:" & ref & " /t:library " & source, expectedRetCode:=1)
                Assert.True(output.StartsWith("vbc : error BC31011: Unable to load referenced library '" & ref & "': Access to the path '" & ref & "' is denied.", StringComparison.Ordinal))

            Finally
                Dim output = RunAndGetOutput("cmd", "/C icacls " & ref & " /reset /Q")
                Assert.Equal("Successfully processed 1 files; Failed processing 0 files", output.Trim())
                File.Delete(ref)
            End Try

            CleanupAllGeneratedFiles(source)
        End Sub

        <WorkItem(544926, "DevDiv")>
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

        <WorkItem(544926, "DevDiv")>
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

        <WorkItem(544926, "DevDiv")>
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

        <WorkItem(544926, "DevDiv")>
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

        <WorkItem(545832, "DevDiv")>
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

        <WorkItem(546031, "DevDiv")>
        <WorkItem(546032, "DevDiv")>
        <WorkItem(546033, "DevDiv")>
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
            Assert.Equal("vbc : error BC31030: Project-level conditional compilation constant '_ ^^ ^^ ' is not valid: Identifier expected.", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:_a,", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)
            Assert.Equal("", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:_ a,", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC31030: Project-level conditional compilation constant '_  ^^ ^^ a' is not valid: Identifier expected.", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:a,_,b", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC31030: Project-level conditional compilation constant '_ ^^ ^^ ' is not valid: Identifier expected.", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:_", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC31030: Project-level conditional compilation constant '_ ^^ ^^ ' is not valid: Identifier expected.", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:_ ", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC31030: Project-level conditional compilation constant '_ ^^ ^^ ' is not valid: Identifier expected.", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"-nologo", "/preferreduilang:en", "/t:libraRY", "/define:a,_", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC31030: Project-level conditional compilation constant '_ ^^ ^^ ' is not valid: Identifier expected.", output.ToString().Trim())

            CleanupAllGeneratedFiles(source)
        End Sub

        Private Function GetDefaultResponseFilePath() As String
            Return Temp.CreateFile().WriteAllBytes(CommandLineTestResources.vbc_rsp).Path
        End Function

        <Fact(Skip:="972948")>
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

        <Fact(Skip:="972948")>
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

        <Fact(), WorkItem(546114, "DevDiv")>
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

        <WorkItem(546305, "DevDiv")>
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
            Assert.Equal("vbc : error BC31030: Project-level conditional compilation constant 'I ^^ ^^ ' is not valid: End of statement expected.", output.ToString().Trim())

            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/nologo", "/preferreduilang:en", "/define:I*", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(1, exitCode)
            Assert.Equal("vbc : error BC31030: Project-level conditional compilation constant 'I ^^ ^^ ' is not valid: End of statement expected.", output.ToString().Trim())
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

        <WorkItem(531263, "DevDiv")>
        <Fact>
        Public Sub EmptyFileName()
            Dim outWriter As New StringWriter()
            Dim exitCode = New MockVisualBasicCompiler(Nothing, _baseDirectory, {""}).Run(outWriter, Nothing)
            Assert.NotEqual(0, exitCode)

            ' error BC2032: File name '' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
            Assert.Contains("BC2032", outWriter.ToString(), StringComparison.Ordinal)
        End Sub

        <WorkItem(1119609, "DevDiv")>
        <Fact(Skip:="1119609")>
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

        <Fact, WorkItem(650083, "DevDiv")>
        Public Sub ReservedDeviceNameAsFileName()
            ' Source file name
            Dim parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/t:library", "con.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/out:com1.exe", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.FTL_InputFileNameTooLong).WithArguments("\\.\com1").WithLocation(1, 1))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/doc:..\lpt2.xml", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_XMLCannotWriteToXMLDocFile2).WithArguments("..\lpt2.xml", "The system cannot find the path specified").WithLocation(1, 1))

            parsedArgs = VisualBasicCommandLineParser.Default.Parse({"/SdkPath:..\aux", "com.vb"}, _baseDirectory)
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

            ' Native VB compiler also ignore invalid lib pathes
            vbc = New MockVisualBasicCompiler(Nothing, _baseDirectory, {"/LibPath:lpt1,Lpt2,LPT9", source})
            output = New StringWriter()
            exitCode = vbc.Run(output, Nothing)
            Assert.Equal(0, exitCode)

            CleanupAllGeneratedFiles(source)
        End Sub

        <Fact(Skip:="574361"), WorkItem(574361, "DevDiv")>
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

            Dim output = RunAndGetOutput(BasicCompilerExecutable, "/nologo /t:library /langversion:9 " & src.ToString(), expectedRetCode:=1, startFolder:=dir.Path)
            AssertOutput(
    <text><![CDATA[
error BC36716: Visual Basic 9.0 does not support auto-implemented properties.

        Public Property Prop() As Integer
                        ~~~~
error BC36716: Visual Basic 9.0 does not support implicit line continuation.

        <CompilerGenerated()>
                            ~
error BC36716: Visual Basic 9.0 does not support auto-implemented properties.

        Public Property Scen1() As <CompilerGenerated()> Func(Of String)
                        ~~~~~
error BC36716: Visual Basic 9.0 does not support implicit line continuation.

        <CLSCompliant(False), Obsolete("obsolete message!")>
                                                           ~
error BC36716: Visual Basic 9.0 does not support implicit line continuation.

        <AttrInThisAsmAttribute()>
                                 ~
error BC36716: Visual Basic 9.0 does not support auto-implemented properties.

        Public Property Scen2() As String
                        ~~~~~
]]>
    </text>, output)


            CleanupAllGeneratedFiles(src.Path)
        End Sub

        <Fact>
        Public Sub DiagnosticFormatting()
            Dim source = "
Class C
    Sub Main()
        Foo(0)
#ExternalSource(""c:\temp\a\1.vb"", 10)
        Foo(1)
#End ExternalSource
#ExternalSource(""C:\a\..\b.vb"", 20)
        Foo(2)
#End ExternalSource
#ExternalSource(""C:\a\../B.vb"", 30)
        Foo(3)
#End ExternalSource
#ExternalSource(""../b.vb"", 40)
        Foo(4)
#End ExternalSource
#ExternalSource(""..\b.vb"", 50)
        Foo(5)
#End ExternalSource
#ExternalSource(""C:\X.vb"", 60)
        Foo(6)
#End ExternalSource
#ExternalSource(""C:\x.vb"", 70)
        Foo(7)
#End ExternalSource
#ExternalSource(""      "", 90)
		Foo(9)
#End ExternalSource
#ExternalSource(""C:\*.vb"", 100)
		Foo(10)
#End ExternalSource
#ExternalSource("""", 110)
		Foo(11)
#End ExternalSource
        Foo(12)
#ExternalSource(""***"", 140)
        Foo(14)
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
    file.Path & "(4) : error BC30451: 'Foo' is not declared. It may be inaccessible due to its protection level.
        Foo(0)
        ~~~   
c:\temp\a\1.vb(10) : error BC30451: 'Foo' is not declared. It may be inaccessible due to its protection level.
        Foo(1)
        ~~~   
C:\b.vb(20) : error BC30451: 'Foo' is not declared. It may be inaccessible due to its protection level.
        Foo(2)
        ~~~   
C:\B.vb(30) : error BC30451: 'Foo' is not declared. It may be inaccessible due to its protection level.
        Foo(3)
        ~~~   
" & Path.GetFullPath(Path.Combine(dir.Path, "..\b.vb")) & "(40) : error BC30451: 'Foo' is not declared. It may be inaccessible due to its protection level.
        Foo(4)
        ~~~   
" & Path.GetFullPath(Path.Combine(dir.Path, "..\b.vb")) & "(50) : error BC30451: 'Foo' is not declared. It may be inaccessible due to its protection level.
        Foo(5)
        ~~~   
C:\X.vb(60) : error BC30451: 'Foo' is not declared. It may be inaccessible due to its protection level.
        Foo(6)
        ~~~   
C:\x.vb(70) : error BC30451: 'Foo' is not declared. It may be inaccessible due to its protection level.
        Foo(7)
        ~~~   
      (90) : error BC30451: 'Foo' is not declared. It may be inaccessible due to its protection level.
        Foo(9)
        ~~~   
C:\*.vb(100) : error BC30451: 'Foo' is not declared. It may be inaccessible due to its protection level.
        Foo(10)
        ~~~    
(110) : error BC30451: 'Foo' is not declared. It may be inaccessible due to its protection level.
        Foo(11)
        ~~~    
" & file.Path & "(35) : error BC30451: 'Foo' is not declared. It may be inaccessible due to its protection level.
        Foo(12)
        ~~~    
***(140) : error BC30451: 'Foo' is not declared. It may be inaccessible due to its protection level.
        Foo(14)
        ~~~    
"
            AssertOutput(expected.Replace(vbCrLf, vbLf), outWriter.ToString())
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <Fact>
        Public Sub ParseFeatures()
            Dim args = VisualBasicCommandLineParser.Default.Parse({"/features:Test", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal("Test", args.CompilationOptions.Features.Single())

            args = VisualBasicCommandLineParser.Default.Parse({"/features:Test", "a.vb", "/Features:Experiment"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(2, args.CompilationOptions.Features.Length)
            Assert.Equal("Test", args.CompilationOptions.Features(0))
            Assert.Equal("Experiment", args.CompilationOptions.Features(1))

            args = VisualBasicCommandLineParser.Default.Parse({"/features:Test:false,Key:value", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal("Test:false,Key:value", args.CompilationOptions.Features.Single())

            ' We don't do any rigorous validation of /features arguments...

            args = VisualBasicCommandLineParser.Default.Parse({"/features", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Empty(args.CompilationOptions.Features)

            args = VisualBasicCommandLineParser.Default.Parse({"/features:,", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(",", args.CompilationOptions.Features.Single())

            args = VisualBasicCommandLineParser.Default.Parse({"/features:Test,", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal("Test,", args.CompilationOptions.Features.Single())
        End Sub

        <Fact>
        Public Sub ParseAdditionalFile()
            Dim args = VisualBasicCommandLineParser.Default.Parse({"/additionalfile:web.config", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(Path.Combine(_baseDirectory, "web.config"), args.AdditionalFiles.Single().Path)

            args = VisualBasicCommandLineParser.Default.Parse({"/additionalfile:web.config", "a.vb", "/additionalfile:app.manifest"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(2, args.AdditionalFiles.Length)
            Assert.Equal(Path.Combine(_baseDirectory, "web.config"), args.AdditionalFiles(0).Path)
            Assert.Equal(Path.Combine(_baseDirectory, "app.manifest"), args.AdditionalFiles(1).Path)

            args = VisualBasicCommandLineParser.Default.Parse({"/additionalfile:web.config", "a.vb", "/additionalfile:web.config"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(2, args.AdditionalFiles.Length)
            Assert.Equal(Path.Combine(_baseDirectory, "web.config"), args.AdditionalFiles(0).Path)
            Assert.Equal(Path.Combine(_baseDirectory, "web.config"), args.AdditionalFiles(1).Path)

            args = VisualBasicCommandLineParser.Default.Parse({"/additionalfile:..\web.config", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(Path.Combine(_baseDirectory, "..\web.config"), args.AdditionalFiles.Single().Path)

            Dim baseDir = Temp.CreateDirectory()
            baseDir.CreateFile("web1.config")
            baseDir.CreateFile("web2.config")
            baseDir.CreateFile("web3.config")

            args = VisualBasicCommandLineParser.Default.Parse({"/additionalfile:web*.config", "a.vb"}, baseDir.Path)
            args.Errors.Verify()
            Assert.Equal(3, args.AdditionalFiles.Length)
            Assert.Equal(Path.Combine(baseDir.Path, "web1.config"), args.AdditionalFiles(0).Path)
            Assert.Equal(Path.Combine(baseDir.Path, "web2.config"), args.AdditionalFiles(1).Path)
            Assert.Equal(Path.Combine(baseDir.Path, "web3.config"), args.AdditionalFiles(2).Path)

            args = VisualBasicCommandLineParser.Default.Parse({"/additionalfile:web.config;app.manifest", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(2, args.AdditionalFiles.Length)
            Assert.Equal(Path.Combine(_baseDirectory, "web.config"), args.AdditionalFiles(0).Path)
            Assert.Equal(Path.Combine(_baseDirectory, "app.manifest"), args.AdditionalFiles(1).Path)

            args = VisualBasicCommandLineParser.Default.Parse({"/additionalfile:web.config,app.manifest", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(2, args.AdditionalFiles.Length)
            Assert.Equal(Path.Combine(_baseDirectory, "web.config"), args.AdditionalFiles(0).Path)
            Assert.Equal(Path.Combine(_baseDirectory, "app.manifest"), args.AdditionalFiles(1).Path)

            args = VisualBasicCommandLineParser.Default.Parse({"/additionalfile:web.config:app.manifest", "a.vb"}, _baseDirectory)
            args.Errors.Verify()
            Assert.Equal(1, args.AdditionalFiles.Length)
            Assert.Equal(Path.Combine(_baseDirectory, "web.config:app.manifest"), args.AdditionalFiles(0).Path)

            args = VisualBasicCommandLineParser.Default.Parse({"/additionalfile", "a.vb"}, _baseDirectory)
            args.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("additionalfile", ":<file_list>"))
            Assert.Equal(0, args.AdditionalFiles.Length)

            args = VisualBasicCommandLineParser.Default.Parse({"/additionalfile:", "a.vb"}, _baseDirectory)
            args.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("additionalfile", ":<file_list>"))
            Assert.Equal(0, args.AdditionalFiles.Length)
        End Sub

        Private Shared Sub Verify(actual As IEnumerable(Of Diagnostic), ParamArray expected As DiagnosticDescription())
            actual.Verify(expected)
        End Sub

        Const LogoLine1 As String = "Microsoft (R) Visual Basic Compiler version"
        Const LogoLine2 As String = "Copyright (C) Microsoft Corporation. All rights reserved."

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
                                             Optional includeCurrentAssemblyAsAnalyzerReferecne As Boolean = True,
                                             Optional additionalFlags As String() = Nothing,
                                             Optional expectedInfoCount As Integer = 0,
                                             Optional expectedWarningCount As Integer = 0,
                                             Optional expectedErrorCount As Integer = 0) As String
            Dim args = {
                            "/nologo", "/preferreduilang:en", "/t:library",
                            sourceFile.Path
                       }
            If includeCurrentAssemblyAsAnalyzerReferecne Then
                args = args.Append("/a:" + Assembly.GetExecutingAssembly().Location)
            End If
            If additionalFlags IsNot Nothing Then
                args = args.Append(additionalFlags)
            End If

            Dim vbc = New MockVisualBasicCompiler(Nothing, sourceDir.Path, args)
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

        <WorkItem(899050)>
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

        <WorkItem(899050)>
        <WorkItem(981677)>
        <Fact>
        Public Sub NoWarnAndWarnAsError_HiddenDiagnostic()
            ' This assembly has a HiddenDiagnosticAnalyzer type which should produce custom hidden
            ' diagnostics for #ExternalSouce directives present in the compilations created in this test.
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

            ' TEST: Santiy test for /nowarn and /nowarn:.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn", "/nowarn:Hidden01"})

            ' TEST: Santiy test for /nowarn and /nowarn:.
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

        <WorkItem(899050)>
        <WorkItem(981677)>
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

            ' TEST: Santiy test for /nowarn and /nowarn:.
            output = GetOutput(name, source, additionalFlags:={"/nowarn", "/nowarn:Info01"})

            ' TEST: Santiy test for /nowarn and /nowarn:.
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
                          Optional includeCurrentAssemblyAsAnalyzerReferecne As Boolean = True,
                          Optional additionalFlags As String() = Nothing,
                          Optional expectedInfoCount As Integer = 0,
                          Optional expectedWarningCount As Integer = 0,
                          Optional expectedErrorCount As Integer = 0) As String
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(name)
            file.WriteAllText(source)
            Dim output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReferecne, additionalFlags, expectedInfoCount, expectedWarningCount, expectedErrorCount)
            CleanupAllGeneratedFiles(file.Path)
            Return output
        End Function

        <WorkItem(899050)>
        <WorkItem(981677)>
        <WorkItem(998069)>
        <WorkItem(998724)>
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
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror"}, expectedErrorCount:=1)
            Assert.Contains("error BC42376", output, StringComparison.Ordinal)

            ' TEST: Verify that compiler warning BC42024 as well as custom warning diagnostics Warning01 and Warning03 can be promoted to errors via /warnaserror+.
            ' This doesn't work currently - promoting compiler warning BC42024 to an error causes us to no longer report any custom warning diagnostics as errors (Bug 998069).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+"}, expectedErrorCount:=1)
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
            ' This doesn't work correctly currently - promoting compiler warning BC42024 to an error causes us to no longer report any custom warning diagnostics as errors (Bug 998069).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror+:bc42024"}, expectedWarningCount:=1, expectedErrorCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
            Assert.Contains("a.vb(4) : error BC42024: Unused local variable: 'x'.", output, StringComparison.Ordinal)

            ' TEST: Verify that custom warning diagnostics Warning01 and Warning03 as well as compiler warning BC42024 can be individually promoted to errors via /warnaserror:.
            ' This doesn't work currently - promoting compiler warning BC42024 to an error causes us to no longer report any custom warning diagnostics as errors (Bug 998069).
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror:warning01,Warning03,bc42024,58000"}, expectedWarningCount:=1, expectedErrorCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
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

            ' TEST: Santiy test for /nowarn and /nowarn:.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn", "/nowarn:Something,042024,Warning01,Warning03,42376"})

            ' TEST: Santiy test for /nowarn: and /nowarn.
            output = VerifyOutput(dir, file, additionalFlags:={"/nowarn:Something,042024,Warning01,Warning03,42376", "/nowarn"})

            ' TEST: Verify that last /warnaserror[+/-] flag on command line wins.
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-", "/warnaserror+"}, expectedErrorCount:=1)
            Assert.Contains("error BC42376", output, StringComparison.Ordinal)

            ' Note: Old native compiler behaved strangely for the below case.
            ' When /warnaserror+ and /warnaserror- appeared on the same command line, natvie compiler would allow /warnaserror+ to win always
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
            output = VerifyOutput(dir, file, additionalFlags:={"/warnaserror-", "/warnaserror+:warning01,Warning03,bc42024,58000"}, expectedWarningCount:=1, expectedErrorCount:=1)
            Assert.Contains("warning BC42376", output, StringComparison.Ordinal)
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

        <WorkItem(899050)>
        <WorkItem(981677)>
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

        <WorkItem(981677)>
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

            Dim output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReferecne:=False, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            ' TEST: Verify that compiler error BC30311 can't be suppressed via /nowarn.
            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReferecne:=False, additionalFlags:={"/nowarn"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            ' TEST: Verify that compiler error BC30311 can't be suppressed via /nowarn:.
            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReferecne:=False, additionalFlags:={"/nowarn:30311"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReferecne:=False, additionalFlags:={"/nowarn:BC30311"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReferecne:=False, additionalFlags:={"/nowarn:bc30311"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            ' TEST: Verify that nothing bad happens when using /warnaserror[+/-] when compiler error BC30311 is present.
            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReferecne:=False, additionalFlags:={"/warnaserror"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReferecne:=False, additionalFlags:={"/warnaserror+"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReferecne:=False, additionalFlags:={"/warnaserror-"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            ' TEST: Verify that nothing bad happens if someone passes BC30311 to /warnaserror[+/-]:.
            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReferecne:=False, additionalFlags:={"/warnaserror:30311"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReferecne:=False, additionalFlags:={"/warnaserror+:BC30311"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReferecne:=False, additionalFlags:={"/warnaserror+:bc30311"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReferecne:=False, additionalFlags:={"/warnaserror-:30311"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReferecne:=False, additionalFlags:={"/warnaserror-:BC30311"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReferecne:=False, additionalFlags:={"/warnaserror-:bc30311"}, expectedErrorCount:=1)
            Assert.Contains("a.vb(4) : error BC30311: Value of type 'Exception' cannot be converted to 'Integer'.", output, StringComparison.Ordinal)

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <Fact, WorkItem(1091972, "DevDiv"), WorkItem(444, "CodePlex")>
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

            Dim output = RunAndGetOutput(BasicCompilerExecutable, String.Format("/nologo /doc:doc.xml /out:out.exe /resource:doc.xml {0}", src.ToString()), startFolder:=dir.ToString())
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

            output = RunAndGetOutput(Path.Combine(dir.ToString(), "out.exe"), startFolder:=dir.ToString())
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

            Dim arguments = VisualBasicCommandLineParser.Default.Parse({"/ruleset:Rules.RuleSet", "/WarnAsError+", "A.vb"}, dir.Path)

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

            Dim arguments = VisualBasicCommandLineParser.Default.Parse({"/ruleset:Rules.RuleSet", "/WarnAsError+", "A.vb"}, dir.Path)

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

            Dim arguments = VisualBasicCommandLineParser.Default.Parse({"/ruleset:Rules.RuleSet", "/WarnAsError+", "A.vb"}, dir.Path)

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

            Dim arguments = VisualBasicCommandLineParser.Default.Parse({"/ruleset:Rules.RuleSet", "/WarnAsError+:Test001", "A.vb"}, dir.Path)

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

            Dim arguments = VisualBasicCommandLineParser.Default.Parse({"/ruleset:Rules.RuleSet", "/WarnAsError+", "/WarnAsError-", "A.vb"}, dir.Path)

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

            Dim arguments = VisualBasicCommandLineParser.Default.Parse({"/ruleset:Rules.RuleSet", "/WarnAsError+", "/WarnAsError-:Test001", "A.vb"}, dir.Path)

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

            Dim arguments = VisualBasicCommandLineParser.Default.Parse({"/ruleset:Rules.RuleSet", "/WarnAsError+:Test002", "/WarnAsError-:Test002", "A.vb"}, dir.Path)

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

            Dim arguments = VisualBasicCommandLineParser.Default.Parse({"/ruleset:Rules.RuleSet", "/NoWarn", "/WarnAsError+", "A.vb"}, dir.Path)

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

            Dim arguments = VisualBasicCommandLineParser.Default.Parse({"/ruleset:Rules.RuleSet", "/WarnAsError+", "/NoWarn", "/WarnAsError-", "A.vb"}, dir.Path)

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

            Dim arguments = VisualBasicCommandLineParser.Default.Parse({"/ruleset:Rules.RuleSet", "/NoWarn", "A.vb"}, dir.Path)

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

            Dim arguments = VisualBasicCommandLineParser.Default.Parse({"/ruleset:Rules.RuleSet", "/NoWarn:Test001", "/WarnAsError+", "/WarnAsError-:Test001", "A.vb"}, dir.Path)

            Assert.Empty(arguments.Errors)
            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=arguments.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Suppress, actual:=arguments.CompilationOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

    End Class

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    MustInherit Class MockAbstractDiagnosticAnalyzer
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
    Class HiddenDiagnosticAnalyzer
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
    Class InfoDiagnosticAnalyzer
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
    Class WarningDiagnosticAnalyzer
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
    Class ErrorDiagnosticAnalyzer
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
End Namespace

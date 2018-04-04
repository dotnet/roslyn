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

    End Class

End Namespace

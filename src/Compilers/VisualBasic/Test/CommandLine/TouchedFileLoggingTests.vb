' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CompilerServer
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities.SharedResourceHelpers
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine.UnitTests

    Public Class TouchedFileLoggingTests
        Inherits BasicTestBase

        Private Shared ReadOnly s_libDirectory As String = Environment.GetEnvironmentVariable("LIB")

        Private ReadOnly _baseDirectory As String = TempRoot.Root

        Private ReadOnly _helloWorldCS As String = <text>
Imports System
Class C
    Shared Sub Main(args As String())
        Console.WriteLine("Hello, world")
    End Sub
End Class
</text>.Value

        <Fact>
        Public Sub TrivialSourceFileOnlyVbc()
            Dim hello = Temp.CreateFile().WriteAllText(_helloWorldCS).Path
            Dim touchedDir = Temp.CreateDirectory()
            Dim touchedBase = Path.Combine(touchedDir.Path, "touched")

            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory,
                {"/nologo",
                 "/touchedfiles:" + touchedBase,
                 hello})
            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)

            Dim expectedReads As List(Of String) = Nothing
            Dim expectedWrites As List(Of String) = Nothing
            BuildTouchedFiles(cmd,
                              Path.ChangeExtension(hello, "exe"),
                              expectedReads,
                              expectedWrites)

            Dim exitCode = cmd.Run(outWriter, Nothing)
            Assert.Equal("", outWriter.ToString().Trim())
            Assert.Equal(0, exitCode)

            AssertTouchedFilesEqual(expectedReads,
                                    expectedWrites,
                                    touchedBase)

            CleanupAllGeneratedFiles(hello)
        End Sub

        <Fact>
        Public Sub StrongNameKeyVbc()
            Dim hello = Temp.CreateFile().WriteAllText(_helloWorldCS).Path
            Dim snkPath = Temp.CreateFile("TestKeyPair_", ".snk").WriteAllBytes(TestResources.General.snKey).Path
            Dim touchedDir = Temp.CreateDirectory()
            Dim touchedBase = Path.Combine(touchedDir.Path, "touched")

            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)
            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory,
                {"/nologo",
                 "/touchedfiles:" + touchedBase,
                 "/keyfile:" + snkPath,
                 hello})

            Dim expectedReads As List(Of String) = Nothing
            Dim expectedWrites As List(Of String) = Nothing
            BuildTouchedFiles(cmd,
                              Path.ChangeExtension(hello, "exe"),
                              expectedReads,
                              expectedWrites)
            expectedReads.Add(snkPath)

            Dim exitCode = cmd.Run(outWriter, Nothing)

            Assert.Equal(String.Empty, outWriter.ToString().Trim())
            Assert.Equal(0, exitCode)

            AssertTouchedFilesEqual(expectedReads,
                                    expectedWrites,
                                    touchedBase)

            CleanupAllGeneratedFiles(hello)
            CleanupAllGeneratedFiles(snkPath)
        End Sub

        <Fact>
        Public Sub XmlDocumentFileVbc()
            Dim sourcePath = Temp.CreateFile().WriteAllText(
<text><![CDATA[
''' <summary>
''' A subtype of <see cref="object" />.
''' </summary>
Public Class C
End Class
]]></text>.Value).Path
            Dim xml = Temp.CreateFile()
            Dim touchedDir = Temp.CreateDirectory()
            Dim touchedBase = Path.Combine(touchedDir.Path, "touched")

            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory,
                {"/nologo",
                 "/target:library",
                 "/doc:" + xml.Path,
                 "/touchedfiles:" + touchedBase,
                 sourcePath})
            ' Build touched files
            Dim expectedReads As List(Of String) = Nothing
            Dim expectedWrites As List(Of String) = Nothing
            BuildTouchedFiles(cmd,
                              Path.ChangeExtension(sourcePath, "dll"),
                              expectedReads,
                              expectedWrites)
            expectedWrites.Add(xml.Path)

            Dim writer = New StringWriter(CultureInfo.InvariantCulture)
            Dim exitCode = cmd.Run(writer, Nothing)
            Assert.Equal(String.Empty, writer.ToString().Trim())
            Assert.Equal(0, exitCode)
            Dim expectedDoc = <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
{0}
</name>
</assembly>
<members>
<member name="T:C">
 <summary>
 A subtype of <see cref="T:System.Object" />.
 </summary>
</member>
</members>
</doc>]]>.Value.Trim()
            expectedDoc = String.Format(expectedDoc,
                                        Path.GetFileNameWithoutExtension(sourcePath))
            expectedDoc = expectedDoc.Replace(vbLf, vbCrLf)
            Assert.Equal(expectedDoc, xml.ReadAllText().Trim())

            AssertTouchedFilesEqual(expectedReads,
                                    expectedWrites,
                                    touchedBase)

            CleanupAllGeneratedFiles(sourcePath)
            CleanupAllGeneratedFiles(xml.Path)
        End Sub

        <Fact>
        Public Sub TrivialMetadataCaching()
            Dim folderList As New List(Of String)
            Dim filelist As New List(Of String)

            For i = 0 To 2 - 1
                Dim source1 = Temp.CreateFile().WriteAllText(_helloWorldCS).Path
                Dim touchedDir = Temp.CreateDirectory()
                Dim touchedBase = Path.Combine(touchedDir.Path, "touched")
                filelist.Add(source1)
                folderList.Add(touchedDir.Path)

                Dim outWriter = New StringWriter()
                Dim cmd = New VisualBasicCompilerServer(
                    DesktopCompilerServerHost.SharedAssemblyReferenceProvider,
                    {"/nologo",
                     "/touchedfiles:" + touchedBase,
                     source1},
                    Nothing,
                    _baseDirectory,
                    RuntimeEnvironment.GetRuntimeDirectory(),
                    s_libDirectory,
                    New TestAnalyzerAssemblyLoader())
                Dim expectedReads As List(Of String) = Nothing
                Dim expectedWrites As List(Of String) = Nothing
                BuildTouchedFiles(cmd,
                                  Path.ChangeExtension(source1, "exe"),
                                  expectedReads,
                                  expectedWrites)

                Dim exitCode = cmd.Run(outWriter, Nothing)
                Assert.Equal(String.Empty, outWriter.ToString().Trim())
                Assert.Equal(0, exitCode)

                AssertTouchedFilesEqual(expectedReads,
                                        expectedWrites,
                                        touchedBase)
            Next

            For Each f In filelist
                CleanupAllGeneratedFiles(f)
            Next
        End Sub

        ''' <summary>
        ''' Builds the expected base of touched files.
        ''' Adds a hook for temporary file creation as well,
        ''' so this method must be called before the execution of
        ''' Vbc.Run.
        ''' </summary>
        ''' <param name="cmd"></param>
        Private Shared Sub BuildTouchedFiles(cmd As VisualBasicCompiler,
                                                  outputPath As String,
                                                  <Out> ByRef expectedReads As List(Of String),
                                                  <Out> ByRef expectedWrites As List(Of String))
            expectedReads = cmd.Arguments.MetadataReferences.Select(Function(r) r.Reference).ToList()

            Dim coreLibrary = cmd.Arguments.DefaultCoreLibraryReference
            If coreLibrary.HasValue Then
                expectedReads.Add(coreLibrary.GetValueOrDefault().Reference)
            End If

            For Each file In cmd.Arguments.SourceFiles
                expectedReads.Add(file.Path)
            Next

            Dim writes = New List(Of String)
            writes.Add(outputPath)
            expectedWrites = writes
        End Sub

        Private Shared Sub AssertTouchedFilesEqual(expectedReads As List(Of String),
                                                   expectedWrites As List(Of String),
                                                   touchedFilesBase As String)
            Dim touchedReadPath = touchedFilesBase + ".read"
            Dim touchedWritesPath = touchedFilesBase + ".write"

            Dim expected = expectedReads.Select(Function(s) s.ToUpperInvariant()).OrderBy(Function(s) s)
            Assert.Equal(String.Join(vbCrLf, expected),
                         File.ReadAllText(touchedReadPath).Trim())

            expected = expectedWrites.Select(Function(s) s.ToUpperInvariant()).OrderBy(Function(s) s)
            Assert.Equal(String.Join(vbCrLf, expected),
                         File.ReadAllText(touchedWritesPath).Trim())
        End Sub

        Private Class TestAnalyzerAssemblyLoader
            Implements IAnalyzerAssemblyLoader

            Public Sub AddDependencyLocation(fullPath As String) Implements IAnalyzerAssemblyLoader.AddDependencyLocation
                Throw New NotImplementedException()
            End Sub

            Public Function LoadFromPath(fullPath As String) As Assembly Implements IAnalyzerAssemblyLoader.LoadFromPath
                Throw New NotImplementedException()
            End Function
        End Class

    End Class
End Namespace

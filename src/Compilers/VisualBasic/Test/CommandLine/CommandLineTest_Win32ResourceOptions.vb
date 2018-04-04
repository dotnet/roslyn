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

        <Theory,
            InlineData({"/resource:a", "a.vb"}, False, Nothing, "a"),
            InlineData({"/res:b", "a.vb"}, False, Nothing, "b"),
            InlineData({"/linkresource:c", "a.vb"}, False, "c", "c"),
            InlineData({"/linkres:d", "a.vb"}, False, "d", "d")>
        Public Sub ManagedResourceOptions(Args() As String, Expected_DisplayHelp As Boolean, Expected_DescriptionFileName As String, Expected_DescriptionResourceName As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(Expected_DisplayHelp, parsedArgs.DisplayHelp)
            Dim resourceDescription = parsedArgs.ManifestResources.Single()
            Assert.Equal(Expected_DescriptionFileName, resourceDescription.FileName) ' since embedded
            Assert.Equal(Expected_DescriptionResourceName, resourceDescription.ResourceName)
        End Sub

        Shared Iterator Function ManagedResourceOptions_SimpleErrors_Data() As IEnumerable(Of Object())
            Yield {"/resource:", "a.vb", Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("resource", ":<resinfo>")}
            Yield {"/resource: ", "a.vb", Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("resource", ":<resinfo>")}
            Yield {"/resource", "a.vb", Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("resource", ":<resinfo>")}
            Yield {"/linkresource:", "a.vb", Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("linkresource", ":<resinfo>")}
            Yield {"/linkresource: ", "a.vb", Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("linkresource", ":<resinfo>")}
            Yield {"/linkresource", "a.vb", Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("linkresource", ":<resinfo>")}
            'Yield {"/RES+", "a.vb", Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/RES+")} ' TODO: Dev11 reports ERR_ArgumentRequired
            'Yield {"/res-:", "a.vb", Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/res-:")} ' TODO: Dev11 reports ERR_ArgumentRequired
            'Yield {"/linkRES+", "a.vb", Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/linkRES+")} ' TODO: Dev11 reports ERR_ArgumentRequired
            'Yield {"/linkres-:", "a.vb", Diagnostic(ERRID.WRN_BadSwitch).WithArguments("/linkres-:")} ' TODO: Dev11 reports ERR_ArgumentRequired
        End Function

        <Theory>
        <InlineData({"/resource:", "a.vb"}, {"resource", ":<resinfo>"})>
        <InlineData({"/resource: ", "a.vb"}, {"resource", ":<resinfo>"})>
        <InlineData({"/resource", "a.vb"}, {"resource", ":<resinfo>"})>
        <InlineData({"/linkresource:", "a.vb"}, {"linkresource", ":<resinfo>"})>
        <InlineData({"/linkresource: ", "a.vb"}, {"linkresource", ":<resinfo>"})>
        <InlineData({"/linkresource", "a.vb"}, {"linkresource", ":<resinfo>"})>
        Public Sub ManagedResourceOptions_SimpleErrors_ERR_ArgumentRequired(Args() As String, WithArgs() As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments(WithArgs))
        End Sub

        <Theory>
        <InlineData({"/RES+", "a.vb"}, {"/RES+"})> ' TODO: Dev11 reports ERR_ArgumentRequired
        <InlineData({"/res-:", "a.vb"}, {"/res-:"})> ' TODO: >Dev11 reports ERR_ArgumentRequired
        <InlineData({"/linkRES+", "a.vb"}, {"/linkRES+"})> ' TODO: Dev11 reports ERR_ArgumentRequired
        <InlineData({"/linkres-:", "a.vb"}, {"/linkres-:"})> ' TODO: Dev11 reports ERR_ArgumentRequired
        Public Sub ManagedResourceOptions_SimpleErrors_WRN_BadSwitch(Args() As String, WithArgs() As String)
            Dim parsedArgs = DefaultParse(Args, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.WRN_BadSwitch).WithArguments(WithArgs))
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

    End Class

End Namespace

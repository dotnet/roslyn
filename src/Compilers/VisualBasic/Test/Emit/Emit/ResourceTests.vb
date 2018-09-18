' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit

    Public Class ResourceTests
        Inherits BasicTestBase

        <DllImport("kernel32.dll", SetLastError:=True)> Public Shared Function _
        LoadLibraryEx(lpFileName As String, hFile As IntPtr, dwFlags As UInteger) As IntPtr
        End Function
        <DllImport("kernel32.dll", SetLastError:=True)> Public Shared Function _
        FreeLibrary(hFile As IntPtr) As Boolean
        End Function


        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
        Public Sub DefaultVersionResource()
            Dim source =
<compilation name="Win32VerNoAttrs">
    <file>
Public Class Main
    Public Shared Sub Main()
    End Sub
End Class
    </file>
</compilation>

            Dim c1 = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe)
            Dim exe = Temp.CreateFile()
            Using output As FileStream = exe.Open()
                c1.Emit(output, win32Resources:=c1.CreateDefaultWin32Resources(True, False, Nothing, Nothing))
            End Using

            c1 = Nothing
            'Open as data
            Dim [lib] As IntPtr = IntPtr.Zero
            Dim versionData As String
            Dim mftData As String
            Try
                [lib] = LoadLibraryEx(exe.Path, IntPtr.Zero, &H2)
                If [lib] = IntPtr.Zero Then
                    Throw New Win32Exception(Marshal.GetLastWin32Error())
                End If

                'the manifest and version primitives are tested elsewhere. This is to test that the default
                'values are passed to the primitives that assemble the resources.
                Dim size As UInteger
                Dim versionRsrc As IntPtr = Win32Res.GetResource([lib], "#1", "#16", size)
                versionData = Win32Res.VersionResourceToXml(versionRsrc)
                Dim mftSize As UInteger
                Dim mftRsrc As IntPtr = Win32Res.GetResource([lib], "#1", "#24", mftSize)
                mftData = Win32Res.ManifestResourceToXml(mftRsrc, mftSize)
            Finally
                If [lib] <> IntPtr.Zero Then
                    FreeLibrary([lib])
                End If
            End Try

            Dim expected As String =
"<?xml version=""1.0"" encoding=""utf-16""?>" & vbCrLf &
"<VersionResource Size=""612"">" & vbCrLf &
"  <VS_FIXEDFILEINFO FileVersionMS=""00000000"" FileVersionLS=""00000000"" ProductVersionMS=""00000000"" ProductVersionLS=""00000000"" />" & vbCrLf &
"  <KeyValuePair Key=""FileDescription"" Value="" "" />" & vbCrLf &
"  <KeyValuePair Key=""FileVersion"" Value=""0.0.0.0"" />" & vbCrLf &
"  <KeyValuePair Key=""InternalName"" Value=""Win32VerNoAttrs.exe"" />" & vbCrLf &
"  <KeyValuePair Key=""LegalCopyright"" Value="" "" />" & vbCrLf &
"  <KeyValuePair Key=""OriginalFilename"" Value=""Win32VerNoAttrs.exe"" />" & vbCrLf &
"  <KeyValuePair Key=""ProductVersion"" Value=""0.0.0.0"" />" & vbCrLf &
"  <KeyValuePair Key=""Assembly Version"" Value=""0.0.0.0"" />" & vbCrLf &
"</VersionResource>"

            Assert.Equal(versionData, expected)

            expected =
"<?xml version=""1.0"" encoding=""utf-16""?>" & vbCrLf &
"<ManifestResource Size=""490"">" & vbCrLf &
"  <Contents><![CDATA[<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>" & vbCrLf &
"" & vbCrLf &
"<assembly xmlns=""urn:schemas-microsoft-com:asm.v1"" manifestVersion=""1.0"">" & vbCrLf &
"  <assemblyIdentity version=""1.0.0.0"" name=""MyApplication.app""/>" & vbCrLf &
"  <trustInfo xmlns=""urn:schemas-microsoft-com:asm.v2"">" & vbCrLf &
"    <security>" & vbCrLf &
"      <requestedPrivileges xmlns=""urn:schemas-microsoft-com:asm.v3"">" & vbCrLf &
"        <requestedExecutionLevel level=""asInvoker"" uiAccess=""false""/>" & vbCrLf &
"      </requestedPrivileges>" & vbCrLf &
"    </security>" & vbCrLf &
"  </trustInfo>" & vbCrLf &
"</assembly>]]></Contents>" & vbCrLf &
"</ManifestResource>"

            Assert.Equal(expected, mftData)

            'look at the same data through the FileVersion API.
            'If the codepage and resource language information is not
            'written correctly into the internal resource directory of
            'the PE, then GetVersionInfo will fail to find the FileVersionInfo. 
            'Once upon a time in Roslyn, the codepage and lang info was not written correctly.
            Dim fileVer = FileVersionInfo.GetVersionInfo(exe.Path)
            Assert.Equal(" ", fileVer.LegalCopyright)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
        Public Sub ResourcesInCoff()
            'this is to test that resources coming from a COFF can be added to a binary.
            Dim source =
<compilation>
    <file>
Public Class Main
    Public Shared Sub Main()
    End Sub
End Class
    </file>
</compilation>

            Dim c1 = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe)
            Dim exe = Temp.CreateFile()
            Using output As FileStream = exe.Open()
                Dim memStream = New MemoryStream(TestResources.General.nativeCOFFResources)
                c1.Emit(output, win32Resources:=memStream)
            End Using

            c1 = Nothing
            'Open as data
            Dim [lib] As IntPtr = IntPtr.Zero
            Dim versionData As String
            Try
                [lib] = LoadLibraryEx(exe.Path, IntPtr.Zero, &H2)
                If [lib] = IntPtr.Zero Then
                    Throw New Win32Exception(Marshal.GetLastWin32Error())
                End If

                Dim size As UInteger
                Dim rsrc As IntPtr = Win32Res.GetResource([lib], "#1", "#16", size)
                versionData = Win32Res.VersionResourceToXml(rsrc)

                rsrc = Win32Res.GetResource([lib], "#1", "#6", size)
                Assert.NotNull(rsrc)

                rsrc = Win32Res.GetResource([lib], "#1", "#11", size)
                Assert.NotNull(rsrc)

                rsrc = Win32Res.GetResource([lib], "#1", "WEVT_TEMPLATE", size)
                Assert.NotNull(rsrc)
            Finally
                If [lib] <> IntPtr.Zero Then
                    FreeLibrary([lib])
                End If
            End Try

            Dim expected As String =
"<?xml version=""1.0"" encoding=""utf-16""?>" & vbCrLf &
"<VersionResource Size=""1104"">" & vbCrLf &
"  <VS_FIXEDFILEINFO FileVersionMS=""000b0000"" FileVersionLS=""eacc0000"" ProductVersionMS=""000b0000"" ProductVersionLS=""eacc0000"" />" & vbCrLf &
"  <KeyValuePair Key=""CompanyName"" Value=""Microsoft Corporation"" />" & vbCrLf &
"  <KeyValuePair Key=""FileDescription"" Value=""Team Foundation Server Object Model"" />" & vbCrLf &
"  <KeyValuePair Key=""FileVersion"" Value=""11.0.60108.0 built by: TOOLSET_ROSLYN(GNAMBOO-DEV-GNAMBOO)"" />" & vbCrLf &
"  <KeyValuePair Key=""InternalName"" Value=""Microsoft.TeamFoundation.Framework.Server.dll"" />" & vbCrLf &
"  <KeyValuePair Key=""LegalCopyright"" Value=""© Microsoft Corporation. All rights reserved."" />" & vbCrLf &
"  <KeyValuePair Key=""OriginalFilename"" Value=""Microsoft.TeamFoundation.Framework.Server.dll"" />" & vbCrLf &
"  <KeyValuePair Key=""ProductName"" Value=""Microsoft® Visual Studio® 2012"" />" & vbCrLf &
"  <KeyValuePair Key=""ProductVersion"" Value=""11.0.60108.0"" />" & vbCrLf &
"</VersionResource>"

            Assert.Equal(expected, versionData)

            'look at the same data through the FileVersion API.
            'If the codepage and resource language information is not
            'written correctly into the internal resource directory of
            'the PE, then GetVersionInfo will fail to find the FileVersionInfo. 
            'Once upon a time in Roslyn, the codepage and lang info was not written correctly.
            Dim fileVer = FileVersionInfo.GetVersionInfo(exe.Path)
            Assert.Equal("Microsoft Corporation", fileVer.CompanyName)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
        Public Sub FaultyResourceDataProvider()
            Dim c1 = VisualBasicCompilation.Create("goo", references:={MscorlibRef}, options:=TestOptions.ReleaseDll)
            Dim result = c1.Emit(New MemoryStream(),
                                 manifestResources:={New ResourceDescription("r2", "file", Function()
                                                                                               Throw New Exception("bad stuff")
                                                                                           End Function, False)})

            result.Diagnostics.Verify(Diagnostic(ERRID.ERR_UnableToOpenResourceFile1).WithArguments("file", "bad stuff"))

            result = c1.Emit(New MemoryStream(),
                             manifestResources:={New ResourceDescription("r2", "file", Function() Nothing, False)})

            result.Diagnostics.Verify(Diagnostic(ERRID.ERR_UnableToOpenResourceFile1).WithArguments("file", CodeAnalysisResources.ResourceDataProviderShouldReturnNonNullStream))
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly))>
        Public Sub AddManagedResource()
            ' Use a unique guid as a compilation name to prevent conflicts with other assemblies loaded via Assembly.ReflectionOnlyLoad:
            Dim c1 As VisualBasicCompilation = CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation><file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine()
    End Sub
End Module
              </file></compilation>)

            Dim output As New IO.MemoryStream
            Dim resourceFileName = "RoslynResourceFile.goo"

            Dim r1Name As String = "some.dotted.NAME"
            Dim r2Name As String = "another.DoTtEd.NAME"

            Dim arrayOfEmbeddedData() As Byte = {1, 2, 3, 4, 5}
            Dim resourceFileData() As Byte = {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}

            Dim result As EmitResult = c1.Emit(output, manifestResources:=New ResourceDescription(1) _
                {
                    New ResourceDescription(r1Name, Function() New IO.MemoryStream(arrayOfEmbeddedData), True),
                    New ResourceDescription(r2Name, resourceFileName, Function() New IO.MemoryStream(resourceFileData), False)
                })

            Assert.True(result.Success)

            Dim assembly As Assembly = Assembly.ReflectionOnlyLoad(output.ToArray())

            Dim resourceNames As String() = assembly.GetManifestResourceNames()
            Assert.Equal(2, resourceNames.Length)

            Dim rInfo As ManifestResourceInfo = assembly.GetManifestResourceInfo(r1Name)
            Assert.Equal(ResourceLocation.Embedded Or ResourceLocation.ContainedInManifestFile, rInfo.ResourceLocation)

            Dim rData As Stream = assembly.GetManifestResourceStream(r1Name)
            Dim rBytes(CInt(rData.Length - 1)) As Byte
            rData.Read(rBytes, 0, CInt(rData.Length))
            Assert.Equal(arrayOfEmbeddedData, rBytes)

            rInfo = assembly.GetManifestResourceInfo(r2Name)
            Assert.Equal(resourceFileName, rInfo.FileName)
        End Sub

        <Fact>
        Public Sub AddManagedLinkedResourceFail()

            Dim c1 As VisualBasicCompilation = CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation><file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine()
    End Sub
End Module
              </file></compilation>)

            Dim output As New IO.MemoryStream

            Dim r2Name As String = "another.DoTtEd.NAME"

            Dim result As EmitResult = c1.Emit(output, manifestResources:=New ResourceDescription(0) _
                {
                    New ResourceDescription(r2Name, Function()
                                                        Throw New NotSupportedException()
                                                    End Function, False)
                })

            Assert.False(result.Success)
            result.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_UnableToOpenResourceFile1).WithArguments("another.DoTtEd.NAME", New NotSupportedException().Message))
        End Sub

        <Fact>
        Public Sub AddManagedEmbeddedResourceFail()

            Dim c1 As VisualBasicCompilation = CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation><file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine()
    End Sub
End Module
              </file></compilation>)

            Dim output As New IO.MemoryStream

            Dim r1Name As String = "some.dotted.NAME"

            Dim result As EmitResult = c1.Emit(output, manifestResources:=New ResourceDescription(0) _
                {
                    New ResourceDescription(r1Name, Function()
                                                        Throw New NotSupportedException()
                                                    End Function, True)
                })

            Assert.False(result.Success)
            result.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_UnableToOpenResourceFile1).WithArguments("some.dotted.NAME", New NotSupportedException().Message))
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
        Public Sub ResourceWithAttrSettings()
            Dim c1 As VisualBasicCompilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Win32VerAttrs">
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyVersion("1.2.3.4")>
<Assembly: System.Reflection.AssemblyFileVersion("5.6.7.8")>
<Assembly: System.Reflection.AssemblyTitle("One Hundred Years of Solitude")>
<Assembly: System.Reflection.AssemblyDescription("A classic of magical realist literature")>
<Assembly: System.Reflection.AssemblyCompany("MossBrain")>
<Assembly: System.Reflection.AssemblyProduct("Sound Cannon")>
<Assembly: System.Reflection.AssemblyCopyright("circle C")>
<Assembly: System.Reflection.AssemblyTrademark("circle R")>
<Assembly: System.Reflection.AssemblyInformationalVersion("1.2.3garbage")>
Module Module1
    Sub Main()
        System.Console.WriteLine()
    End Sub
End Module
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseExe)

            Dim exeFile = Temp.CreateFile()
            Using output As FileStream = exeFile.Open()
                c1.Emit(output, win32Resources:=c1.CreateDefaultWin32Resources(True, False, Nothing, Nothing))
            End Using

            c1 = Nothing
            Dim versionData As String
            'Open as data
            Dim [lib] As IntPtr = IntPtr.Zero
            Try
                [lib] = LoadLibraryEx(exeFile.Path, IntPtr.Zero, &H2)
                Assert.True([lib] <> IntPtr.Zero, String.Format("LoadLibrary failed with HResult: {0:X}", Marshal.GetLastWin32Error()))

                'the manifest and version primitives are tested elsewhere. This is to test that the default
                'values are passed to the primitives that assemble the resources.
                Dim size As UInteger
                Dim versionRsrc As IntPtr = Win32Res.GetResource([lib], "#1", "#16", size)
                versionData = Win32Res.VersionResourceToXml(versionRsrc)
            Finally
                If [lib] <> IntPtr.Zero Then
                    FreeLibrary([lib])
                End If
            End Try

            Dim expected As String =
"<?xml version=""1.0"" encoding=""utf-16""?>" & vbCrLf &
"<VersionResource Size=""964"">" & vbCrLf &
"  <VS_FIXEDFILEINFO FileVersionMS=""00050006"" FileVersionLS=""00070008"" ProductVersionMS=""00010002"" ProductVersionLS=""00030000"" />" & vbCrLf &
"  <KeyValuePair Key=""Comments"" Value=""A classic of magical realist literature"" />" & vbCrLf &
"  <KeyValuePair Key=""CompanyName"" Value=""MossBrain"" />" & vbCrLf &
"  <KeyValuePair Key=""FileDescription"" Value=""One Hundred Years of Solitude"" />" & vbCrLf &
"  <KeyValuePair Key=""FileVersion"" Value=""5.6.7.8"" />" & vbCrLf &
"  <KeyValuePair Key=""InternalName"" Value=""Win32VerAttrs.exe"" />" & vbCrLf &
"  <KeyValuePair Key=""LegalCopyright"" Value=""circle C"" />" & vbCrLf &
"  <KeyValuePair Key=""LegalTrademarks"" Value=""circle R"" />" & vbCrLf &
"  <KeyValuePair Key=""OriginalFilename"" Value=""Win32VerAttrs.exe"" />" & vbCrLf &
"  <KeyValuePair Key=""ProductName"" Value=""Sound Cannon"" />" & vbCrLf &
"  <KeyValuePair Key=""ProductVersion"" Value=""1.2.3garbage"" />" & vbCrLf &
"  <KeyValuePair Key=""Assembly Version"" Value=""1.2.3.4"" />" & vbCrLf &
"</VersionResource>"

            Assert.Equal(expected, versionData)
        End Sub




        <WorkItem(543501, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543501")>
        <Fact()>
        Public Sub BC31502_DuplicateManifestResourceIdentifier()
            Dim c1 As VisualBasicCompilation = CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation><file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine()
    End Sub
End Module
              </file></compilation>)

            Dim output As New IO.MemoryStream
            Dim dataProvider = Function() New IO.MemoryStream(New Byte() {})

            Dim result As EmitResult = c1.Emit(output, manifestResources:=New ResourceDescription(1) _
                {
                    New ResourceDescription("A", "x.goo", dataProvider, True),
                    New ResourceDescription("A", "y.goo", dataProvider, True)
                })

            ' error BC31502: Resource name 'A' cannot be used more than once.
            result.Diagnostics.Verify(Diagnostic(ERRID.ERR_DuplicateResourceName1).WithArguments("A"))
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly))>
        Public Sub AddResourceToModule()
            Dim source =
<compilation><file name="a.vb">
    </file></compilation>
            Dim c1 As VisualBasicCompilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseModule)

            Dim output As New IO.MemoryStream
            Dim dataProvider = Function() New IO.MemoryStream(New Byte() {})

            Dim r1Name As String = "some.dotted.NAME"
            Dim r2Name As String = "another.DoTtEd.NAME"

            Dim arrayOfEmbeddedData() As Byte = {1, 2, 3, 4, 5}
            Dim resourceFileData() As Byte = {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}

            Dim result As EmitResult = c1.Emit(output, manifestResources:=
                {
                    New ResourceDescription(r1Name, Function() New IO.MemoryStream(arrayOfEmbeddedData), True),
                    New ResourceDescription("A", "y.goo", dataProvider, True)
                })

            Assert.False(result.Success)
            result.Diagnostics.Verify(Diagnostic(ERRID.ERR_ResourceInModule))

            result = c1.Emit(output, manifestResources:=
                {
                    New ResourceDescription("A", "y.goo", dataProvider, True),
                    New ResourceDescription(r1Name, Function() New IO.MemoryStream(arrayOfEmbeddedData), True)
                })

            Assert.False(result.Success)
            result.Diagnostics.Verify(Diagnostic(ERRID.ERR_ResourceInModule))

            result = c1.Emit(output, manifestResources:=
                {
                    New ResourceDescription("A", "y.goo", dataProvider, True)
                })

            Assert.False(result.Success)
            result.Diagnostics.Verify(Diagnostic(ERRID.ERR_ResourceInModule))

            Dim c_mod1 = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseModule)

            Dim output_mod1 = New MemoryStream()
            result = c_mod1.Emit(output_mod1, manifestResources:=
                {
                    New ResourceDescription(r1Name, Function() New IO.MemoryStream(arrayOfEmbeddedData), True)
                })

            Assert.True(result.Success)
            Dim mod1 = ModuleMetadata.CreateFromImage(output_mod1.ToImmutable())
            Dim ref_mod1 = mod1.GetReference()
            Assert.Equal(ManifestResourceAttributes.Public, mod1.Module.GetEmbeddedResourcesOrThrow()(0).Attributes)

            If True Then
                Dim C2 = CreateCompilationWithMscorlib40AndReferences(source, {ref_mod1}, TestOptions.ReleaseDll)
                Dim output2 = New MemoryStream()
                Dim result2 = C2.Emit(output2)

                Assert.True(result2.Success)
                Dim assembly = System.Reflection.Assembly.ReflectionOnlyLoad(output2.ToArray())

                AddHandler assembly.ModuleResolve, Function(sender As Object, e As ResolveEventArgs)
                                                       If (e.Name.Equals(c_mod1.SourceModule.Name)) Then
                                                           Return assembly.LoadModule(e.Name, output_mod1.ToArray())
                                                       End If

                                                       Return Nothing
                                                   End Function

                Dim resourceNames As String() = assembly.GetManifestResourceNames()
                Assert.Equal(1, resourceNames.Length)

                Dim rInfo = assembly.GetManifestResourceInfo(r1Name)
                Assert.Equal(System.Reflection.ResourceLocation.Embedded, rInfo.ResourceLocation)
                Assert.Equal(c_mod1.SourceModule.Name, rInfo.FileName)

                Dim rData = assembly.GetManifestResourceStream(r1Name)
                Dim rBytes = New Byte(CInt(rData.Length) - 1) {}
                rData.Read(rBytes, 0, CInt(rData.Length))
                Assert.Equal(arrayOfEmbeddedData, rBytes)
            End If

            Dim c_mod2 = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseModule)

            Dim output_mod2 = New MemoryStream()
            result = c_mod2.Emit(output_mod2, manifestResources:=
                {
                    New ResourceDescription(r1Name, Function() New MemoryStream(arrayOfEmbeddedData), True),
                    New ResourceDescription(r2Name, Function() New MemoryStream(resourceFileData), True)
                })

            Assert.True(result.Success)
            Dim ref_mod2 = ModuleMetadata.CreateFromImage(output_mod2.ToImmutable()).GetReference()

            If True Then
                Dim C3 = CreateCompilationWithMscorlib40AndReferences(source, {ref_mod2}, TestOptions.ReleaseDll)
                Dim output3 = New MemoryStream()
                Dim result3 = C3.Emit(output3)

                Assert.True(result3.Success)
                Dim assembly = System.Reflection.Assembly.ReflectionOnlyLoad(output3.ToArray())

                AddHandler assembly.ModuleResolve, Function(sender As Object, e As ResolveEventArgs)
                                                       If (e.Name.Equals(c_mod2.SourceModule.Name)) Then
                                                           Return assembly.LoadModule(e.Name, output_mod2.ToArray())
                                                       End If

                                                       Return Nothing
                                                   End Function

                Dim resourceNames As String() = assembly.GetManifestResourceNames()
                Assert.Equal(2, resourceNames.Length)

                Dim rInfo = assembly.GetManifestResourceInfo(r1Name)
                Assert.Equal(ResourceLocation.Embedded, rInfo.ResourceLocation)
                Assert.Equal(c_mod2.SourceModule.Name, rInfo.FileName)

                Dim rData = assembly.GetManifestResourceStream(r1Name)
                Dim rBytes = New Byte(CInt(rData.Length) - 1) {}
                rData.Read(rBytes, 0, CInt(rData.Length))
                Assert.Equal(arrayOfEmbeddedData, rBytes)

                rInfo = assembly.GetManifestResourceInfo(r2Name)
                Assert.Equal(ResourceLocation.Embedded, rInfo.ResourceLocation)
                Assert.Equal(c_mod2.SourceModule.Name, rInfo.FileName)

                rData = assembly.GetManifestResourceStream(r2Name)
                rBytes = New Byte(CInt(rData.Length) - 1) {}
                rData.Read(rBytes, 0, CInt(rData.Length))
                Assert.Equal(resourceFileData, rBytes)
            End If

            Dim c_mod3 = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseModule)

            Dim output_mod3 = New MemoryStream()
            result = c_mod3.Emit(output_mod3, manifestResources:=
                                                       {
                                                           New ResourceDescription(r2Name, Function() New MemoryStream(resourceFileData), False)
                                                       })

            Assert.True(result.Success)
            Dim mod3 = ModuleMetadata.CreateFromImage(output_mod3.ToImmutable())
            Dim ref_mod3 = mod3.GetReference()
            Assert.Equal(ManifestResourceAttributes.Private, mod3.Module.GetEmbeddedResourcesOrThrow()(0).Attributes)

            If True Then
                Dim C4 = CreateCompilationWithMscorlib40AndReferences(source, {ref_mod3}, TestOptions.ReleaseDll)
                Dim output4 = New MemoryStream()
                Dim result4 = C4.Emit(output4, manifestResources:=
                                                       {
                                                           New ResourceDescription(r1Name, Function() New MemoryStream(arrayOfEmbeddedData), False)
                                                       })

                Assert.True(result4.Success)
                Dim assembly = System.Reflection.Assembly.ReflectionOnlyLoad(output4.ToArray())

                AddHandler assembly.ModuleResolve, Function(sender As Object, e As ResolveEventArgs)
                                                       If (e.Name.Equals(c_mod3.SourceModule.Name)) Then
                                                           Return assembly.LoadModule(e.Name, output_mod3.ToArray())
                                                       End If

                                                       Return Nothing
                                                   End Function

                Dim resourceNames As String() = assembly.GetManifestResourceNames()
                Assert.Equal(2, resourceNames.Length)

                Dim rInfo = assembly.GetManifestResourceInfo(r1Name)
                Assert.Equal(ResourceLocation.Embedded Or ResourceLocation.ContainedInManifestFile, rInfo.ResourceLocation)

                Dim rData = assembly.GetManifestResourceStream(r1Name)
                Dim rBytes = New Byte(CInt(rData.Length) - 1) {}
                rData.Read(rBytes, 0, CInt(rData.Length))
                Assert.Equal(arrayOfEmbeddedData, rBytes)

                rInfo = assembly.GetManifestResourceInfo(r2Name)
                Assert.Equal(ResourceLocation.Embedded, rInfo.ResourceLocation)
                Assert.Equal(c_mod3.SourceModule.Name, rInfo.FileName)

                rData = assembly.GetManifestResourceStream(r2Name)
                rBytes = New Byte(CInt(rData.Length) - 1) {}
                rData.Read(rBytes, 0, CInt(rData.Length))
                Assert.Equal(resourceFileData, rBytes)
            End If

            If True Then
                Dim c5 = CreateCompilationWithMscorlib40AndReferences(source, {ref_mod1, ref_mod3}, TestOptions.ReleaseDll)
                Dim output5 = New MemoryStream()
                Dim result5 = c5.Emit(output5)

                Assert.True(result5.Success)
                Dim assembly = System.Reflection.Assembly.ReflectionOnlyLoad(output5.ToArray())

                AddHandler assembly.ModuleResolve, Function(sender As Object, e As ResolveEventArgs)
                                                       If (e.Name.Equals(c_mod1.SourceModule.Name)) Then
                                                           Return assembly.LoadModule(e.Name, output_mod1.ToArray())
                                                       ElseIf (e.Name.Equals(c_mod3.SourceModule.Name)) Then
                                                           Return assembly.LoadModule(e.Name, output_mod3.ToArray())
                                                       End If
                                                       Return Nothing
                                                   End Function

                Dim resourceNames As String() = assembly.GetManifestResourceNames()
                Assert.Equal(2, resourceNames.Length)

                Dim rInfo = assembly.GetManifestResourceInfo(r1Name)
                Assert.Equal(System.Reflection.ResourceLocation.Embedded, rInfo.ResourceLocation)
                Assert.Equal(c_mod1.SourceModule.Name, rInfo.FileName)

                Dim rData = assembly.GetManifestResourceStream(r1Name)
                Dim rBytes = New Byte(CInt(rData.Length) - 1) {}
                rData.Read(rBytes, 0, CInt(rData.Length))
                Assert.Equal(arrayOfEmbeddedData, rBytes)

                rInfo = assembly.GetManifestResourceInfo(r2Name)
                Assert.Equal(System.Reflection.ResourceLocation.Embedded, rInfo.ResourceLocation)
                Assert.Equal(c_mod3.SourceModule.Name, rInfo.FileName)

                rData = assembly.GetManifestResourceStream(r2Name)
                rBytes = New Byte(CInt(rData.Length) - 1) {}
                rData.Read(rBytes, 0, CInt(rData.Length))
                Assert.Equal(resourceFileData, rBytes)
            End If

            If True Then
                Dim c6 = CreateCompilationWithMscorlib40AndReferences(source, {ref_mod1, ref_mod2}, TestOptions.ReleaseDll)
                Dim output6 = New MemoryStream()
                Dim result6 = c6.Emit(output6)

                Assert.False(result6.Success)
                AssertTheseDiagnostics(result6.Diagnostics,
<expected>
BC31502: Resource name 'some.dotted.NAME' cannot be used more than once.
</expected>)

                result6 = c6.Emit(output6, manifestResources:=
                {
                    New ResourceDescription(r2Name, Function() New IO.MemoryStream(resourceFileData), False)
                })

                Assert.False(result6.Success)
                AssertTheseDiagnostics(result6.Diagnostics,
<expected>
BC31502: Resource name 'another.DoTtEd.NAME' cannot be used more than once.
BC31502: Resource name 'some.dotted.NAME' cannot be used more than once.
</expected>)

                c6 = CreateCompilationWithMscorlib40AndReferences(source, {ref_mod1, ref_mod2}, TestOptions.ReleaseModule)
                result6 = c6.Emit(output6, manifestResources:=
                {
                    New ResourceDescription(r2Name, Function() New IO.MemoryStream(resourceFileData), False)
                })

                Assert.True(result6.Success)
            End If

        End Sub

        <WorkItem(543501, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543501")>
        <Fact()>
        Public Sub BC31502_DuplicateManifestResourceIdentifier_EmbeddedResource()
            Dim c1 As VisualBasicCompilation = CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation><file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine()
    End Sub
End Module
              </file></compilation>)

            Dim output As New IO.MemoryStream
            Dim dataProvider = Function() New IO.MemoryStream(New Byte() {})

            Dim result As EmitResult = c1.Emit(output, manifestResources:=New ResourceDescription(1) _
                {
                    New ResourceDescription("A", dataProvider, True),
                    New ResourceDescription("A", Nothing, dataProvider, True, isEmbedded:=True, checkArgs:=True)
                })

            ' error BC31502: Resource name 'A' cannot be used more than once.
            result.Diagnostics.Verify(Diagnostic(ERRID.ERR_DuplicateResourceName1).WithArguments("A"))

            ' file name ignored for embedded manifest resources
            result = c1.Emit(output, manifestResources:=New ResourceDescription(1) _
                {
                    New ResourceDescription("A", "x.goo", dataProvider, True, isEmbedded:=True, checkArgs:=True),
                    New ResourceDescription("A", "x.goo", dataProvider, True, isEmbedded:=False, checkArgs:=True)
                })

            ' error BC31502: Resource name 'A' cannot be used more than once.
            result.Diagnostics.Verify(Diagnostic(ERRID.ERR_DuplicateResourceName1).WithArguments("A"))
        End Sub

        <WorkItem(543501, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543501"), WorkItem(546298, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546298")>
        <Fact()>
        Public Sub BC35003_DuplicateManifestResourceFileName()
            Dim c1 As Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine()
    End Sub
End Module
    </file>
</compilation>)

            Dim output As New IO.MemoryStream
            Dim dataProvider = Function() New IO.MemoryStream(New Byte() {})

            Dim result = c1.Emit(output, manifestResources:=New ResourceDescription(1) _
                {
                    New ResourceDescription("A", "x.goo", dataProvider, True),
                    New ResourceDescription("B", "x.goo", dataProvider, True)
                })

            ' error BC35003: Each linked resource and module must have a unique filename. Filename 'x.goo' is specified more than once in this assembly.
            result.Diagnostics.Verify(Diagnostic(ERRID.ERR_DuplicateResourceFileName1).WithArguments("x.goo"))

            result = c1.Emit(output, manifestResources:=New ResourceDescription(0) _
                {
                    New ResourceDescription("A", "C.dll", dataProvider, True)
                })

            result.Diagnostics.Verify()

            Dim netModule1 = TestReferences.SymbolsTests.netModule.netModule1

            c1 = VisualBasicCompilation.Create("goo", references:={MscorlibRef, netModule1}, options:=TestOptions.ReleaseDll)

            result = c1.Emit(output, manifestResources:=New ResourceDescription(0) _
                {
                    New ResourceDescription("A", "netmodule1.netmodule", dataProvider, True)
                })

            ' Native compiler gives BC30144 which is no longer used in Roslyn
            ' error BC35003: Each linked resource and module must have a unique filename. Filename 'netmodule1.netmodule' is specified more than once in this assembly
            result.Diagnostics.Verify(Diagnostic(ERRID.ERR_DuplicateResourceFileName1).WithArguments("netModule1.netmodule"))

        End Sub

        <WorkItem(543501, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543501")>
        <Fact()>
        Public Sub NoDuplicateManifestResourceFileNameDiagnosticForEmbeddedResources()
            Dim c1 As VisualBasicCompilation = CreateCompilationWithMscorlib40AndVBRuntime(
                              <compilation><file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine()
    End Sub
End Module
              </file></compilation>)

            Dim output As New IO.MemoryStream
            Dim dataProvider = Function() New IO.MemoryStream(New Byte() {})

            Dim result As EmitResult = c1.Emit(output, manifestResources:=New ResourceDescription(1) _
                {
                    New ResourceDescription("A", dataProvider, True),
                    New ResourceDescription("B", Nothing, dataProvider, True, isEmbedded:=True, checkArgs:=True)
                })

            result.Diagnostics.Verify()

            ' file name ignored for embedded manifest resources
            result = c1.Emit(output, manifestResources:=New ResourceDescription(1) _
                {
                    New ResourceDescription("A", "x.goo", dataProvider, True, isEmbedded:=True, checkArgs:=True),
                    New ResourceDescription("B", "x.goo", dataProvider, True, isEmbedded:=False, checkArgs:=True)
                })

            result.Diagnostics.Verify()
        End Sub

        <WorkItem(543501, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543501")>
        <Fact()>
        Public Sub BC31502_BC35003_DuplicateManifestResourceDiagnostics()
            Dim c1 As VisualBasicCompilation = CreateCompilationWithMscorlib40AndVBRuntime(
                                 <compilation><file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine()
    End Sub
End Module
              </file></compilation>)

            Dim output As New IO.MemoryStream
            Dim dataProvider = Function() New IO.MemoryStream(New Byte() {})

            Dim result As EmitResult = c1.Emit(output, manifestResources:=New ResourceDescription(1) _
                {
                    New ResourceDescription("A", "x.goo", dataProvider, True),
                    New ResourceDescription("A", "x.goo", dataProvider, True)
                })

            ' error BC31502: Resource name 'A' cannot be used more than once.
            ' error BC35003: Each linked resource and module must have a unique filename. Filename 'x.goo' is specified more than once in this assembly.
            result.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_DuplicateResourceName1).WithArguments("A"),
                Diagnostic(ERRID.ERR_DuplicateResourceFileName1).WithArguments("x.goo"))

            result = c1.Emit(output, manifestResources:=New ResourceDescription(2) _
                {
                    New ResourceDescription("A", "x.goo", dataProvider, True),
                    New ResourceDescription("B", "x.goo", dataProvider, True),
                    New ResourceDescription("B", "y.goo", dataProvider, True)
                })

            ' error BC35003: Each linked resource andmust have a unique filename. Filename 'x.goo' is specified more than once in this assembly.
            ' error BC31502: Resource name 'B' cannot be used more than once.
            result.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_DuplicateResourceFileName1).WithArguments("x.goo"),
                Diagnostic(ERRID.ERR_DuplicateResourceName1).WithArguments("B"))
        End Sub
    End Class

End Namespace

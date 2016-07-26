Imports <xmlns:wix="http://schemas.microsoft.com/wix/2006/wi">
Imports <xmlns:msbuild="http://schemas.microsoft.com/developer/msbuild/2003">
Imports <xmlns:vsix="http://schemas.microsoft.com/developer/vsx-schema/2011">
Imports <xmlns:netfx="http://schemas.microsoft.com/wix/NetFxExtension">
Imports System.IO.Packaging
Imports System.IO
Imports System.Threading
Imports System.IO.Compression

Public Module BuildDevDivInsertionFiles
    Private Const InsertionFilesDirName = "DevDivInsertionFiles"
    Private Const ExternalApisDirName = "ExternalAPIs"
    Private Const NetFX20DirectoryName = "NetFX20"

    Public Function Main(args As String()) As Integer
        Try
            Dim inputDirectory = Path.GetFullPath(args(0))
            Dim outputDirectory = Path.Combine(inputDirectory, InsertionFilesDirName)
            Dim assemblyVersion = args(1)
            Dim publicKeyToken = args(2)

            Call New Worker(inputDirectory, outputDirectory, assemblyVersion, publicKeyToken).Execute()
            Return 0
        Catch ex As Exception
            Console.Error.WriteLine(ex.ToString())
            Return 1
        End Try
    End Function

    Private Class Worker
        Private ReadOnly BinariesToSkipLocalization As String() = {
            "Microsoft.CodeAnalysis.Elfie.dll"
        }

        Private ReadOnly VsixContentsToSkip As String() = {
            "Microsoft.Data.ConnectionUI.dll",
            "Microsoft.TeamFoundation.TestManagement.Client.dll",
            "Microsoft.TeamFoundation.TestManagement.Common.dll",
            "Microsoft.VisualStudio.CallHierarchy.Package.Definitions.dll",
            "Microsoft.VisualStudio.CodeAnalysis.Sdk.UI.dll",
            "Microsoft.VisualStudio.Data.dll",
            "Microsoft.VisualStudio.QualityTools.OperationalStore.ClientHelper.dll",
            "Microsoft.VisualStudio.QualityTools.WarehouseCommon.dll",
            "Microsoft.VisualStudio.TeamSystem.Common.dll",
            "Microsoft.VisualStudio.TeamSystem.Common.Framework.dll",
            "Microsoft.VisualStudio.TeamSystem.Integration.dll",
            "Newtonsoft.Json.dll",
            "System.Composition.AttributedModel.dll",
            "System.Composition.Runtime.dll",
            "System.Composition.Convention.resources.dll",
            "System.Composition.Hosting.resources.dll",
            "System.Composition.TypedParts.resources.dll",
            "Microsoft.CodeAnalysis.VisualBasic.Scripting.dll",
            "Microsoft.CodeAnalysis.VisualBasic.InteractiveEditorFeatures.dll",
            "Microsoft.VisualStudio.VisualBasic.Repl.dll",
            "Microsoft.VisualStudio.VisualBasic.Repl.pkgdef",
            "VisualBasicInteractive.png",
            "VisualBasicInteractive.rsp",
            "VisualBasicInteractivePackageRegistration.pkgdef",
            "System.Collections.Immutable.dll",                ' Setup authoring: Platform\Components
            "System.Reflection.Metadata.dll",                  ' Setup authoring: Platform\Components
            "Microsoft.DiaSymReader.dll",                      ' Setup authoring: edev\debugger\Components
            "Microsoft.DiaSymReader.PortablePdb.dll"           ' Setup authoring: edev\debugger\Components
        }

        Private ReadOnly CompilerFiles As String() = {
            "Microsoft.CodeAnalysis.dll",
            "Microsoft.CodeAnalysis.CSharp.dll",
            "Microsoft.CodeAnalysis.Scripting.dll",
            "Microsoft.CodeAnalysis.CSharp.Scripting.dll",
            "Microsoft.CodeAnalysis.VisualBasic.dll",
            "Microsoft.DiaSymReader.Native.amd64.dll",
            "Microsoft.DiaSymReader.Native.x86.dll",
            "System.AppContext.dll",
            "System.Console.dll",
            "System.Diagnostics.StackTrace.dll",
            "System.IO.FileSystem.dll",
            "System.IO.FileSystem.Primitives.dll",
            "csc.exe",
            "csc.exe.config",
            "csc.rsp",
            "csi.exe",
            "csi.rsp",
            "vbc.exe",
            "vbc.exe.config",
            "vbc.rsp",
            "VBCSCompiler.exe",
            "VBCSCompiler.exe.config",
            "Microsoft.Build.Tasks.CodeAnalysis.dll",
            "Microsoft.CSharp.Core.targets",
            "Microsoft.VisualBasic.Core.targets"
        }

        Private ReadOnly VsixesToInstall As String() = {
            "Roslyn.VisualStudio.Setup.vsix",
            "ExpressionEvaluatorPackage.vsix",
            "Roslyn.VisualStudio.InteractiveComponents.vsix",
            "Microsoft.VisualStudio.VsInteractiveWindow.vsix",
            "Roslyn.VisualStudio.Setup.Interactive.vsix",
            "Roslyn.VisualStudio.Setup.Next.vsix"
        }

        ' Files copied to Maddog machines running integration tests that are produced from our builds.
        Private ReadOnly IntegrationTestFiles As String() = {
            "xunit.*.dll",
            "Esent.Interop.dll",
            "InteractiveHost.exe",
            "Microsoft.CodeAnalysis.CSharp.dll",
            "Microsoft.CodeAnalysis.CSharp.EditorFeatures.dll",
            "Microsoft.CodeAnalysis.CSharp.Features.dll",
            "Microsoft.CodeAnalysis.CSharp.InteractiveEditorFeatures.dll",
            "Microsoft.CodeAnalysis.CSharp.Scripting.dll",
            "Microsoft.CodeAnalysis.CSharp.Workspaces.dll",
            "Microsoft.CodeAnalysis.dll",
            "Microsoft.CodeAnalysis.EditorFeatures.dll",
            "Microsoft.CodeAnalysis.EditorFeatures.Text.dll",
            "Microsoft.CodeAnalysis.Features.dll",
            "Microsoft.CodeAnalysis.InteractiveEditorFeatures.dll",
            "Microsoft.CodeAnalysis.InteractiveFeatures.dll",
            "Microsoft.CodeAnalysis.Scripting.dll",
            "Microsoft.CodeAnalysis.Test.Resources.Proprietary.dll",
            "Microsoft.CodeAnalysis.VisualBasic.dll",
            "Microsoft.CodeAnalysis.VisualBasic.EditorFeatures.dll",
            "Microsoft.CodeAnalysis.VisualBasic.Features.dll",
            "Microsoft.CodeAnalysis.VisualBasic.InteractiveEditorFeatures.dll",
            "Microsoft.CodeAnalysis.VisualBasic.Workspaces.dll",
            "Microsoft.CodeAnalysis.Workspaces.dll",
            "Microsoft.Diagnostics.Runtime.dll",
            "Microsoft.VisualStudio.CallHierarchy.Package.Definitions.dll",
            "Microsoft.VisualStudio.LanguageServices.CSharp.dll",
            "Microsoft.VisualStudio.LanguageServices.dll",
            "Microsoft.VisualStudio.LanguageServices.Implementation.dll",
            "Microsoft.VisualStudio.LanguageServices.VisualBasic.dll",
            "Microsoft.VisualStudio.Platform.VSEditor.Interop.dll",
            "Roslyn.Compilers.Test.Resources.dll",
            "Roslyn.Hosting.Diagnostics.dll",
            "Roslyn.Services.Test.Utilities.dll",
            "Roslyn.Test.PdbUtilities.dll",
            "Roslyn.Test.Utilities.dll",
            "Roslyn.Test.Utilities.dll.config",
            "Roslyn.VisualStudio.Test.Setup.vsix"
        }

        ' Files needed by Mad dog tests that are produced by our internal builds.
        Private ReadOnly IntegrationTestFilesExtra As String() = {
            "IntegrationTests\*.xml",
            "CodeMarkerListener.dll",
            "CorApi.dll",
            "CorApiRaw.dll",
            "DbgHooksIdl.tlb",
            "DbgHooksJitIdl.tlb",
            "Default.vssettings",
            "DiagnosticMargin.dll",
            "EditorTestApp.exe",
            "EditorTestApp.exe.config",
            "Extensions.txt",
            "GetNewestPid.exe",
            "Handle.exe",
            "Interop.DbgHooksIdl.dll",
            "Interop.DbgHooksJitIdl.dll",
            "KernelTraceControl.dll",
            "MDbgCore.dll",
            "MDbgEng.dll",
            "MDbgExt.dll",
            "MDbgUtility.dll",
            "Microsoft.Diagnostics.Tracing.TraceEvent.dll",
            "Microsoft.Internal.Performance.CodeMarkers.dll",
            "Microsoft.Internal.VisualStudio.DelayTracker.Library.dll",
            "Microsoft.Internal.VisualStudio.DelayTracker.TraceEvent.dll",
            "Microsoft.Internal.VisualStudio.Shell.Interop.10.0.DesignTime.dll",
            "Microsoft.Internal.VisualStudio.Shell.Interop.11.0.DesignTime.dll",
            "Microsoft.Internal.VisualStudio.Shell.Interop.12.0.DesignTime.dll",
            "Microsoft.Test.Apex.Framework.dll",
            "Microsoft.Test.Apex.MSTestIntegration.dll",
            "Microsoft.Test.Apex.OsIntegration.dll",
            "Microsoft.Test.Apex.RemoteCodeInjector.dll",
            "Microsoft.Test.Apex.VisualStudio.dll",
            "Microsoft.Test.Apex.VisualStudio.Debugger.dll",
            "Microsoft.Test.Apex.VisualStudio.Hosting.dll",
            "Microsoft.VisualStudio.Web.Common.TestServices.dll",
            "Microsoft.VisualStudio.Web.Project.TestServices.dll",
            "NativeDebugWrappers.dll",
            "Omni.Common.dll",
            "Omni.Log.dll",
            "Omni.Logging.Extended.dll",
            "Perf-CheckTestFiles.cmd",
            "Perf-Compiler-AssembliesToCopy.txt",
            "Perf-Compiler-AssembliesToNGen.txt",
            "Perf-DailyScorecard.bat",
            "Perf-DeleteOldDirectories.cmd",
            "Perf-DisableIbcCollection.bat",
            "Perf-EnableIbcCollection.bat",
            "Perf-IDE-Assemblies.txt",
            "Perf-InstallRoslyn.cmd",
            "Perf-MakeOptimizationPgos.bat",
            "PerformanceTestLog.xslt",
            "Perf-ProcessRunReports.bat",
            "Perf-ResetRoslynOptions.cmd",
            "Perf-Rolling-RunCompilerTests.bat",
            "Perf-Rolling-RunServicesTests.bat",
            "Perf-Rolling-RunServicesTestsWithServerGC.bat",
            "Perf-RunCompilerTests.bat",
            "Perf-RunOptProf.bat",
            "Perf-RunPgoTraining.bat",
            "Perf-RunServicesTests.bat",
            "Perf-RunTestsInLab.bat",
            "Perf-UninstallRoslyn.cmd",
            "Prism.Monitor.Communication.dll",
            "ProcDump.exe",
            "dbgcore.dll",
            "dbghelp.dll",
            "regtlb.exe",
            "ResourceManagerBasic.dll",
            "Roslyn.Test.Performance.dll",
            "RoslynETAHost.dll",
            "RoslynTaoActions.dll",
            "RPFPlayback.dll",
            "RPFPlaybackWrapperVSTT.dll",
            "RPFUiaManagedPlugin.dll",
            "RunPrism.bat",
            "StrongNameLowjack.bat",
            "Tao.Engine.dll",
            "Tao.exe",
            "Tao.exe.config",
            "Tao.Environment.dll",
            "Tao.Utilities.dll",
            "TaoConfig.txt",
            "TraceEvent.dll",
            "TypingDelayAnalyzer.exe",
            "UISynch.dll",
            "UITechnologyInterfaces.dll"
        }

        ' Files copied to Maddog machines running unit tests that are produced from our open build.
        Private ReadOnly UnitTestFiles As String() = {
            "Microsoft.*.UnitTests*.dll",
            "Roslyn.*.UnitTests*.dll",
            "xunit.*.dll",
            "PerfTests",
            "BasicUndo.dll",
            "Esent.Interop.dll",
            "InteractiveHost.exe",
            "Microsoft.CodeAnalysis.CSharp.dll",
            "Microsoft.CodeAnalysis.CSharp.EditorFeatures.dll",
            "Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.ExpressionCompiler.dll",
            "Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.ResultProvider.dll",
            "Microsoft.CodeAnalysis.CSharp.Features.dll",
            "Microsoft.CodeAnalysis.CSharp.InteractiveEditorFeatures.dll",
            "Microsoft.CodeAnalysis.CSharp.Workspaces.dll",
            "Microsoft.CodeAnalysis.dll",
            "Microsoft.CodeAnalysis.EditorFeatures.dll",
            "Microsoft.CodeAnalysis.EditorFeatures.Text.dll",
            "Microsoft.CodeAnalysis.ExpressionEvaluator.ExpressionCompiler.dll",
            "Microsoft.CodeAnalysis.ExpressionEvaluator.ResultProvider.dll",
            "Microsoft.CodeAnalysis.Features.dll",
            "Microsoft.CodeAnalysis.InteractiveEditorFeatures.dll",
            "Microsoft.CodeAnalysis.InteractiveFeatures.dll",
            "Microsoft.CodeAnalysis.CSharp.Scripting.dll",
            "Microsoft.CodeAnalysis.Scripting.dll",
            "Microsoft.CodeAnalysis.VisualBasic.Scripting.dll",
            "Microsoft.CodeAnalysis.Test.Resources.Proprietary.dll",
            "Microsoft.CodeAnalysis.VisualBasic.dll",
            "Microsoft.CodeAnalysis.VisualBasic.EditorFeatures.dll",
            "Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.ExpressionCompiler.dll",
            "Microsoft.CodeAnalysis.VisualBasic.Features.dll",
            "Microsoft.CodeAnalysis.VisualBasic.InteractiveEditorFeatures.dll",
            "Microsoft.CodeAnalysis.VisualBasic.Workspaces.dll",
            "Microsoft.CodeAnalysis.Workspaces.dll",
            "Microsoft.DiaSymReader.dll",
            "Microsoft.DiaSymReader.Native.amd64.dll",
            "Microsoft.DiaSymReader.Native.x86.dll",
            "Microsoft.DiaSymReader.PortablePdb.dll",
            "Microsoft.VisualStudio.CallHierarchy.Package.Definitions.dll",
            "Microsoft.VisualStudio.Debugger.Engine.dll",
            "Microsoft.VisualStudio.LanguageServices.CSharp.dll",
            "Microsoft.VisualStudio.LanguageServices.dll",
            "Microsoft.VisualStudio.LanguageServices.Implementation.dll",
            "Microsoft.VisualStudio.LanguageServices.SolutionExplorer.dll",
            "Microsoft.VisualStudio.LanguageServices.VisualBasic.dll",
            "Microsoft.VisualStudio.Platform.VSEditor.Interop.dll",
            "Moq.dll",
            "csc.exe",
            "csc.exe.config",
            "csc.rsp",
            "csi.exe",
            "Roslyn.Compilers.CSharp.Test.Utilities.dll",
            "Roslyn.Compilers.Test.Resources.dll",
            "Roslyn.Compilers.VisualBasic.Test.Utilities.dll",
            "Roslyn.ExpressionEvaluator.ExpressionCompiler.Test.Utilities.dll",
            "Roslyn.ExpressionEvaluator.ResultProvider.Test.Utilities.dll",
            "Roslyn.Hosting.Diagnostics.dll",
            "Roslyn.Services.Test.Utilities.dll",
            "Roslyn.Test.PdbUtilities.dll",
            "Roslyn.Test.Utilities.Desktop.dll",
            "Roslyn.Test.Utilities.dll",
            "Roslyn.Test.Utilities.dll.config",
            "Roslyn.Test.Utilities.FX45.dll",
            "vbc.exe",
            "vbc.exe.config",
            "vbc.rsp",
            "vbi.exe",
            "VBCSCompiler.exe",
            "VBCSCompiler.exe.config"
        }

        ' Files copied to Maddog machines running unit tests that are produced from our closed build.
        Private ReadOnly UnitTestFilesExtra As String() = {
            "CorApi.dll",
            "CorApiRaw.dll",
            "MDbgCore.dll",
            "MDbgEng.dll",
            "MDbgExt.dll",
            "MDbgUtility.dll",
            "NativeDebugWrappers.dll",
            "Tao.Engine.dll",
            "Tao.Environment.dll",
            "Tao.Utilities.dll"
        }

        Private ReadOnly inputDirectory As String
        Private ReadOnly outputDirectory As String
        Private ReadOnly assemblyVersion As String
        Private ReadOnly publicKeyToken As String

        Public Sub New(inputDirectory As String, outputDirectory As String, assemblyVersion As String, publicKeyToken As String)
            Me.inputDirectory = inputDirectory
            Me.outputDirectory = outputDirectory
            Me.assemblyVersion = assemblyVersion
            Me.publicKeyToken = publicKeyToken
        End Sub

        Public Sub Execute()
            Retry(Sub()
                      If Directory.Exists(outputDirectory) Then
                          ' Delete everything within it. We'll keep the top-level one around.
                          For Each file In New DirectoryInfo(outputDirectory).GetFiles()
                              file.Delete()
                          Next

                          For Each directory In New DirectoryInfo(outputDirectory).GetDirectories()
                              directory.Delete(recursive:=True)
                          Next
                      End If
                  End Sub)

            ' Build a dependency map
            Dim dependencies = BuildDependencyMap(inputDirectory)

            ' List of files to add to VS.ExternalAPI.Roslyn.nuspec.
            ' Paths are relative to input directory.
            ' Files in DevDivInsertionFiles\ExternalAPIs don't need to be added, they are included in the nuspec using a pattern.
            ' May contain duplicates.
            Dim filesToInsert = New List(Of NugetFileInfo)
            Dim locProjects = New List(Of String)

            ' And now copy over all our core compiler binaries and related files
            ' Build tools setup authoring depends on these files being inserted.
            For Each fileName In CompilerFiles

                Dim dependency As DependencyInfo = Nothing
                If Not dependencies.TryGetValue(fileName, dependency) Then
                    AddXmlDocumentationFile(filesToInsert, fileName)
                    filesToInsert.Add(New NugetFileInfo(fileName))
                End If

                If NeedsLocalization(fileName) Then
                    Dim relativeOutputDir = GetExternalApiDirectory(dependency)
                    GenerateLocProject(fileName, Path.Combine(relativeOutputDir, fileName), locProjects)
                End If
            Next

            ' Copy over the files in the NetFX20 subdirectory (identical, except for references and Authenticode signing).
            ' These are for msvsmon, whose setup authoring is done by the debugger.
            For Each relativePath In Directory.EnumerateFiles(Path.Combine(inputDirectory, NetFX20DirectoryName), "*.ExpressionEvaluator.*.dll", SearchOption.TopDirectoryOnly)
                filesToInsert.Add(New NugetFileInfo(Path.Combine(NetFX20DirectoryName, Path.GetFileName(relativePath)), NetFX20DirectoryName))
            Next

            ProcessVsixFiles(filesToInsert, locProjects, dependencies)

            ' Generate loc project that imports loc projects generated for each localized binary:
            GenerateMainLocProj(locProjects)

            ' Generate Roslyn.nuspec:
            GenerateRoslynNuSpec(filesToInsert)

            ' Generate lists of files that are needed to run unit and integration tests in Maddog:
            Dim insertedFiles = New HashSet(Of String)(filesToInsert.Select(Function(f) f.Path), StringComparer.OrdinalIgnoreCase)
            GenerateTestFileDependencyList(NameOf(UnitTestFiles), ExpandTestDependencies(UnitTestFiles), insertedFiles)
            GenerateTestFileDependencyList(NameOf(UnitTestFilesExtra), UnitTestFilesExtra, insertedFiles)
            GenerateTestFileDependencyList(NameOf(IntegrationTestFiles), ExpandTestDependencies(IntegrationTestFiles), insertedFiles)
            GenerateTestFileDependencyList(NameOf(IntegrationTestFilesExtra), IntegrationTestFilesExtra, insertedFiles)
        End Sub

        Private Shared Function GetExternalApiDirectory(Optional dependency As DependencyInfo = Nothing) As String
            Return If(dependency Is Nothing,
                Path.Combine(ExternalApisDirName, "Roslyn"),
                Path.Combine(ExternalApisDirName, dependency.PackageName, dependency.Target))
        End Function

        Private Class NugetFileInfo
            Implements IEquatable(Of NugetFileInfo)

            Public Path As String
            Public Target As String

            Sub New(path As String, Optional target As String = "")
                Me.Path = path
                Me.Target = target
            End Sub

            Public Overrides Function Equals(obj As Object) As Boolean
                Return IEquatableEquals(TryCast(obj, NugetFileInfo))
            End Function

            Public Overrides Function GetHashCode() As Integer
                Return StringComparer.OrdinalIgnoreCase.GetHashCode(Path) Xor StringComparer.OrdinalIgnoreCase.GetHashCode(Target)
            End Function

            Public Function IEquatableEquals(other As NugetFileInfo) As Boolean Implements IEquatable(Of NugetFileInfo).Equals
                Return other IsNot Nothing AndAlso
                    StringComparer.OrdinalIgnoreCase.Equals(Path, other.Path) AndAlso
                    StringComparer.OrdinalIgnoreCase.Equals(Target, other.Target)
            End Function
        End Class

        Private Class DependencyInfo
            Public Target As String
            Public PackageName As String
            Public PackageVersion As String

            Sub New(target As String, packageName As String, packageVersion As String)
                Me.Target = target
                Me.PackageName = packageName
                Me.PackageVersion = packageVersion
            End Sub
        End Class

        ' If a package contains multiple targets we pick the one that has the highest listed number.
        ' Only platforms used by the packages we insert need to be listed.
        Private Shared m_platformPreference As String() =
        {
            "net20",
            "netcore45",
            "net40",
            "net45",
            "net451",
            "net452",
            "net46",
            "net461",
            "net462",
            "dotnet",
            "dotnet5.1",
            "dotnet5.2",
            "portable-net45+win8",
            "portable-net45+win8+wp8+wpa81",
            "netstandard1.0",
            "netstandard1.1",
            "netstandard1.2",
            "netstandard1.3",
            "netstandard1.4",
            "netstandard1.5",
            "native"
        }

        Private Shared Function GetPlatformId(target As String) As String
            Return Path.GetFileName(target).Replace("%2B", "+")
        End Function

        Private Function BuildDependencyMap(inputDirectory As String) As Dictionary(Of String, DependencyInfo)
            Dim result = New Dictionary(Of String, DependencyInfo)

            For Each nupkgPath In Directory.EnumerateFiles(Path.Combine(inputDirectory, "DevDivPackages", "Dependencies"), "*.nupkg", SearchOption.TopDirectoryOnly)
                Using zip = ZipFile.OpenRead(nupkgPath)
                    Dim packageName As String = Nothing
                    Dim packageVersion As String = Nothing
                    NugetUtilities.ParsePackageFileName(Path.GetFileName(nupkgPath), packageName, packageVersion)

                    For Each entry In zip.Entries
                        If IsExecutableCodeFileName(entry.FullName) Then
                            Dim fileName = Path.GetFileName(entry.FullName)
                            Dim target = Path.GetDirectoryName(entry.FullName)

                            Dim platformPreference = Array.IndexOf(m_platformPreference, GetPlatformId(target))
                            If platformPreference < 0 Then
                                Throw New InvalidOperationException($"Unknown platform: '{GetPlatformId(target)}' (package '{nupkgPath}')")
                            End If

                            Dim existing As DependencyInfo = Nothing
                            If result.TryGetValue(fileName, existing) Then
                                If Not existing.PackageName.Equals(packageName) OrElse Not existing.PackageName.Equals(packageName) Then
                                    Throw New InvalidOperationException($"Multiple packages contain the same file '{fileName}'")
                                End If

                                If platformPreference > Array.IndexOf(m_platformPreference, GetPlatformId(existing.Target)) Then
                                    result(fileName) = New DependencyInfo(target, packageName, packageVersion)
                                End If
                            Else
                                result.Add(fileName, New DependencyInfo(target, packageName, packageVersion))
                            End If
                        End If
                    Next
                End Using
            Next

            Return result
        End Function

        ''' <summary>
        ''' Generate a list of files which were not inserted and place them in the named file.
        ''' </summary>
        Private Sub GenerateTestFileDependencyList(outputFileName As String, fileSpecs As IEnumerable(Of String), insertedFiles As HashSet(Of String))
            File.WriteAllLines(
                Path.Combine(outputDirectory, Path.ChangeExtension(outputFileName, ".txt")),
                fileSpecs.Where(Function(f) Not insertedFiles.Contains(f)))
        End Sub

        ''' <summary>
        ''' Enumerate files specified in the list. The specifications may include file names, directory names, and patterns.
        ''' </summary>
        ''' <param name="fileSpecs">
        ''' If the item contains '*', then it will be treated as a search pattern for the top directory.
        ''' Otherwise, if the item represents a directory, then all files and subdirectories of the item will be copied over.
        ''' 
        ''' This funtion will fail and throw and exception if any of the specified files do not exist on disk.
        ''' </param>
        Private Iterator Function ExpandTestDependencies(fileSpecs As String()) As IEnumerable(Of String)
            For Each spec In fileSpecs
                If spec.Contains("*") Then
                    For Each path In Directory.EnumerateFiles(inputDirectory, spec, SearchOption.TopDirectoryOnly)
                        Yield path.Substring(inputDirectory.Length)
                    Next
                Else
                    Dim inputItem = Path.Combine(inputDirectory, spec)

                    If Directory.Exists(inputItem) Then
                        For Each path In Directory.EnumerateFiles(inputItem, "*.*", SearchOption.AllDirectories)
                            Yield path.Substring(inputDirectory.Length)
                        Next
                    ElseIf File.Exists(inputItem) Then
                        Yield spec
                    Else
                        Throw New FileNotFoundException($"File or directory '{spec}' listed in test dependencies doesn't exist.", spec)
                    End If
                End If
            Next
        End Function

        ''' <summary>
        ''' A simple method to retry an operation. Helpful if some file locking is going on and you want to just try the operation again.
        ''' </summary>
        Private Sub Retry(action As Action)
            For i = 1 To 10
                Try
                    action()
                    Return
                Catch ex As Exception
                    Thread.Sleep(100)
                End Try
            Next
        End Sub

        Private Sub ProcessVsixFiles(filesToInsert As List(Of NugetFileInfo), locProjects As List(Of String), dependencies As Dictionary(Of String, DependencyInfo))
            Dim wsx = <?xml version="1.0" encoding="utf-8"?>
                      <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi" xmlns:netfx="http://schemas.microsoft.com/wix/NetFxExtension">
                      </Wix>

            Dim resWsx = <?xml version="1.0" encoding="utf-8"?>
                         <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi" xmlns:netfx="http://schemas.microsoft.com/wix/NetFxExtension">
                         </Wix>

            Dim processedFiles = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

            ' We build our language service authoring by cracking our .vsixes and pulling out the bits that matter
            For Each vsixFileName In VsixesToInstall
                Dim vsixName As String = Path.GetFileNameWithoutExtension(vsixFileName)
                Using vsix = Package.Open(Path.Combine(inputDirectory, vsixFileName), FileMode.Open, FileAccess.Read, FileShare.Read)
                    For Each vsixPart In vsix.GetParts()

                        ' This part might be metadata for the digital signature. In that case, skip it
                        If vsixPart.ContentType.StartsWith("application/vnd.openxmlformats-package.") Then
                            Continue For
                        End If

                        Dim fileName = Path.GetFileName(vsixPart.Uri.ToString())

                        ' If this is something that we don't need to ship, skip it
                        If VsixContentsToSkip.Contains(fileName) Then
                            Continue For
                        End If

                        ' We'll want to extract this file out somewhere. If it's code, we'll extract it to our top-level
                        ' directory since we don't need duplicates. Otherwise, we'll stick it in a subfolder
                        Dim dependency As DependencyInfo = Nothing
                        Dim relativeOutputDir As String

                        If IsLanguageServiceRegistrationFile(fileName) Then
                            relativeOutputDir = Path.Combine(GetExternalApiDirectory(), "LanguageServiceRegistration", vsixName)
                        ElseIf dependencies.TryGetValue(fileName, dependency) Then
                            relativeOutputDir = GetExternalApiDirectory(dependency)
                        Else
                            relativeOutputDir = GetExternalApiDirectory()
                        End If

                        Dim relativeOutputFilePath = Path.Combine(relativeOutputDir, fileName)

                        If processedFiles.Add(relativeOutputFilePath) Then
                            If IsLanguageServiceRegistrationFile(fileName) Then
                                Dim absoluteOutputFilePath = GetAbsolutePathInOutputDirectory(relativeOutputFilePath)
                                WriteVsixPartToFile(vsixPart, absoluteOutputFilePath)

                                ' We want to rewrite a few of these things from our standard vsix-installable forms
                                Select Case Path.GetExtension(absoluteOutputFilePath)
                                    Case ".pkgdef"
                                        RewritePkgDef(absoluteOutputFilePath)

                                    Case ".vsixmanifest"
                                        RewriteVsixManifest(absoluteOutputFilePath)
                                End Select
                            ElseIf dependency Is Nothing Then
                                If Not File.Exists(Path.Combine(inputDirectory, fileName)) Then
                                    Throw New InvalidOperationException($"File '{vsixPart.Uri}' is contained in '{vsixFileName}' but not present in '{inputDirectory}'.")
                                End If

                                ' paths are relative to input directory:
                                filesToInsert.Add(New NugetFileInfo(fileName))

                                AddXmlDocumentationFile(filesToInsert, fileName)
                            End If

                            ' Now write the setup authoring for it
                            wsx.Root.Add(CreateComponentFragment(vsixName, fileName, relativeOutputFilePath))

                            ' Localization:
                            If NeedsLocalization(fileName) Then
                                Dim resourceFileSourcePath = If(CompilerFiles.Contains(fileName),
                                    $"!(bindpath.binaries.$(var.Chip))\$(var.LocalizationPathModifier)\simship\$(var.Lang)\{relativeOutputDir}\",
                                    $"!(bindpath.binaries.$(var.Chip))\$(var.LocalizationPathModifier)\$(var.Lang)\{relativeOutputDir}\")

                                Dim resourcePath = resourceFileSourcePath + GetAssemblyResourcesDllName(fileName)
                                resWsx.Root.Add(CreateResourceComponentFragment(vsixName, fileName, resourcePath))

                                GenerateLocProject(fileName, relativeOutputFilePath, locProjects)
                            End If
                        End If
                    Next
                End Using
            Next

            ' Collect all the component IDs together
            Dim componentGroupFragment = CreateComponentGroupFragment(wsx)
            wsx.Root.Add(componentGroupFragment)

            Dim resourceComponentGroupFragment = CreateResourceComponentGroupFragment(resWsx)
            resWsx.Root.Add(resourceComponentGroupFragment)

            resWsx.Root.AddFirst(<?if $(var.Lang) != enu ?>)
            resWsx.Root.Add(<?endif?>)

            wsx.Save(GetAbsolutePathInOutputDirectory("SetupAuthoring\Roslyn\RoslynLanguageServices.wxs"), SaveOptions.OmitDuplicateNamespaces)
            resWsx.Save(GetAbsolutePathInOutputDirectory("SetupAuthoring\Roslyn\RoslynLanguageServices_Res.wxs"), SaveOptions.OmitDuplicateNamespaces)
        End Sub

        Private Function NeedsLocalization(fileName As String) As Boolean
            Return IsExecutableCodeFileName(fileName) AndAlso Not BinariesToSkipLocalization.Contains(fileName)
        End Function

        ' XML doc file if exists:
        Private Sub AddXmlDocumentationFile(filesToInsert As List(Of NugetFileInfo), fileName As String)
            If IsExecutableCodeFileName(fileName) Then
                Dim xmlDocFile = Path.ChangeExtension(fileName, ".xml")
                If File.Exists(Path.Combine(inputDirectory, xmlDocFile)) Then
                    ' paths are relative to input directory
                    filesToInsert.Add(New NugetFileInfo(xmlDocFile))
                End If
            End If
        End Sub

        Private Sub WriteVsixPartToFile(vsixPart As PackagePart, path As String)
            Using outputStream = New FileStream(path, FileMode.Create)
                vsixPart.GetStream().CopyTo(outputStream)
            End Using
        End Sub

        ''' <summary>
        ''' Takes a list of paths relative to <see cref="outputDirectory"/> and generates a nuspec file that includes them.
        ''' </summary>
        Private Sub GenerateRoslynNuSpec(filesToInsert As List(Of NugetFileInfo))
            Const PackageName As String = "VS.ExternalAPIs.Roslyn"

            Dim xml = <?xml version="1.0" encoding="utf-8"?>
                      <package xmlns="http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd">
                          <metadata>
                              <id><%= PackageName %></id>
                              <summary>Roslyn binaries for the VS build.</summary>
                              <description>CoreXT package for the VS build.</description>
                              <authors>Managed Languages</authors>
                              <version>0.0</version>
                          </metadata>
                          <files>
                              <file src=<%= Path.Combine(InsertionFilesDirName, ExternalApisDirName, "Roslyn", "**") %> target=""/>
                              <%= filesToInsert.
                                  OrderBy(Function(f) f.Path).
                                  Distinct().
                                  Select(Function(f) <file src=<%= f.Path %> target=<%= f.Target %> xmlns="http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"/>) %>
                          </files>
                      </package>

            xml.Save(GetAbsolutePathInOutputDirectory(PackageName & ".nuspec"), SaveOptions.OmitDuplicateNamespaces)
        End Sub

        Private Sub GenerateMainLocProj(locProjects As List(Of String))
            Dim xml = <?xml version="1.0" encoding="utf-8"?>
                      <Project DefaultTargets="Localize" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
                          <PropertyGroup>
                              <RunFromAll>true</RunFromAll>
                          </PropertyGroup>
                          <!--When running from a top-level project $(RunFromMaster) = true-->
                          <Import Project="$(_NTBINDIR)\tools\devdiv\loc\Loctask\Localization.settings.targets" Condition=" '$(RunFromMaster)' == '' "/>
                          <ItemDefinitionGroup>
                              <LocalizeFile>
                                  <Store>sources</Store>
                              </LocalizeFile>
                          </ItemDefinitionGroup>
                          <%= locProjects.OrderBy(Function(f) f).Distinct().Select(AddressOf MakeLocProjectImportElement) %>
                      </Project>

            xml.Save(GetAbsolutePathInOutputDirectory("Roslyn\all.roslyn.locproj"), SaveOptions.OmitDuplicateNamespaces)
        End Sub

        Private Shared Function MakeLocProjectImportElement(projectFileName As String) As XElement
            Dim locCondition = "Exists('LocProjects\" & projectFileName + "')"

            If IsVisualStudioLanguageServiceComponent(projectFileName) Then
                ' We change our loc condition depending upon if it's a 32-bit binary only or not
                locCondition += " and '$(BuildArchitecture)' == 'i386'"
            End If

            Return <Import Project=<%= "LocProjects\" & projectFileName %> Condition=<%= locCondition %> xmlns="http://schemas.microsoft.com/developer/msbuild/2003"/>
        End Function

        Private Sub RewriteVsixManifest(fileToRewrite As String)
            Dim xml = XDocument.Load(fileToRewrite)
            Dim installationElement = xml.<vsix:PackageManifest>.<vsix:Installation>.Single()

            ' We want to modify the .vsixmanifest to say this was installed via MSI
            installationElement.@InstalledByMsi = "true"

            ' Ensure the VSIX isn't shown in the extension gallery
            installationElement.@SystemComponent = "true"

            ' We build our VSIXes with the experimental flag so you can install them as test extensions. In the real MSI, they're not experimental.
            installationElement.Attribute("Experimental")?.Remove()

            ' Update the path to our MEF/Analyzer Components to be in their new home under PrivateAssemblies
            Dim assets = From asset In xml...<vsix:Asset>
                         Where asset.@Type = "Microsoft.VisualStudio.MefComponent" OrElse
                                asset.@Type = "Microsoft.VisualStudio.Analyzer"

            For Each asset In assets
                asset.@Path = "$RootFolder$Common7\IDE\PrivateAssemblies\" & asset.@Path
            Next
            xml.Save(fileToRewrite)
        End Sub

        Private Function CreateResourceComponentGroupFragment(languageServiceResourcesSetupAuthoring As XDocument) As XElement
            Return <Fragment xmlns="http://schemas.microsoft.com/wix/2006/wi">
                       <ComponentGroup Id="RoslynLanguageServices_Res_$(var.Chip)_$(var.Lang)">
                           <?if $(var.IncludeLanguageSpecificBits) = true?>
                           <%= From component In languageServiceResourcesSetupAuthoring...<wix:Component>
                               Order By component.@Id
                               Select <ComponentRef Id=<%= component.@Id %> xmlns="http://schemas.microsoft.com/wix/2006/wi"/> %>
                           <?endif?>
                       </ComponentGroup>
                   </Fragment>
        End Function

        Private Function CreateComponentGroupFragment(languageServiceSetupAuthoring As XDocument) As XElement
            Return <Fragment xmlns="http://schemas.microsoft.com/wix/2006/wi">
                       <ComponentGroup Id="RoslynLanguageServices_$(var.Chip)">
                           <?if $(var.IncludeLanguageNeutralBits) = true?>
                           <%= From component In languageServiceSetupAuthoring...<wix:Component>
                               Order By component.@Id
                               Select <ComponentRef Id=<%= component.@Id %> xmlns="http://schemas.microsoft.com/wix/2006/wi"/> %>
                           <?endif?>
                       </ComponentGroup>
                   </Fragment>
        End Function

        Private Function CreateResourceComponentFragment(vsixName As String, vsixPartFileName As String, resourcePath As String) As XElement
            Dim resourceId = "Roslyn_" + vsixName + "_" + GetAssemblyResourcesDllName(vsixPartFileName) + "_$(var.Chip)_$(var.Lang)"
            Dim fragment As XElement = <Fragment xmlns="http://schemas.microsoft.com/wix/2006/wi">
                                           <Component Id=<%= resourceId %> Directory="PrivateAssemblies_culture_of_$(var.Lang).3643236F_FC70_11D3_A536_0090278A1BB8">
                                               <File KeyPath="yes" Id=<%= resourceId %> Source=<%= resourcePath %>>
                                                   <netfx:NativeImage Id=<%= "ngen_" + resourceId %> Platform="32bit" Priority="3" AssemblyApplication="[VS_NGEN_EXE_CONFIG_PATH]" Dependencies="no"/>
                                               </File>
                                           </Component>
                                       </Fragment>
            Return fragment
        End Function

        Private Function CreateComponentFragment(vsixName As String, vsixPartFileName As String, relativePartOutputPath As String) As XElement
            Dim id = "Roslyn_" + vsixName + "_" + vsixPartFileName + "_$(var.Chip)"
            If Not IsLanguageServiceRegistrationFile(vsixPartFileName) Then
                Return <Fragment xmlns="http://schemas.microsoft.com/wix/2006/wi">
                           <Component Id=<%= id %> Directory="PrivateAssemblies.3643236F_FC70_11D3_A536_0090278A1BB8">
                               <File KeyPath="yes" Id=<%= id %> Source=<%= "!(bindpath.sources)\" + relativePartOutputPath %>>
                                   <netfx:NativeImage Id=<%= "ngen_" + id %> Platform="32bit" Priority="3" AssemblyApplication="[VS_NGEN_EXE_CONFIG_PATH]" Dependencies="no"/>
                               </File>
                           </Component>
                       </Fragment>
            Else
                Return <Fragment xmlns="http://schemas.microsoft.com/wix/2006/wi">
                           <Component Id=<%= id %> Directory=<%= "CommonRoslynExtensions_" + vsixName + ".3643236F_FC70_11D3_A536_0090278A1BB8" %>>
                               <File KeyPath="yes" Id=<%= id %> Source=<%= "!(bindpath.sources)\" + relativePartOutputPath %>/>
                           </Component>
                       </Fragment>
            End If
        End Function

        Private Shared Function IsVisualStudioLanguageServiceComponent(fileName As String) As Boolean
            Return fileName.StartsWith("Microsoft.VisualStudio.LanguageServices.")
        End Function

        Private Sub GenerateLocProject(fileName As String, devdivPath As String, locProjects As List(Of String))
            Dim lciFileName = fileName & ".lci"
            Dim lclFileName = fileName & ".lcl"
            Dim locProjFileName = fileName & ".locproj"

            Dim locProj = <?xml version="1.0" encoding="utf-8"?>
                          <Project DefaultTargets="Localize" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
                              <ItemGroup>
                                  <LocalizeFile Include=<%= "$(Sources)\" & devdivPath %>>
                                      <TranslationFile><%= "$(LocRepo)\{Lang}Roslyn\" & lclFileName %></TranslationFile>
                                      <LciCommentFile><%= "$(Sources)\Roslyn\LCI\" & lciFileName %></LciCommentFile>
                                      <%= If(CompilerFiles.Contains(fileName),
                                          <SimShip xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
                                              <Type>Simship</Type>
                                              <Languages>$(VS)</Languages>
                                          </SimShip>,
                                          Nothing) %>
                                      <ProjectFile>$(MSBuildThisFileFullPath)</ProjectFile>
                                  </LocalizeFile>
                              </ItemGroup>
                              <!--When running from a top-level project $(RunFromAll) = true-->
                              <Import Project="$(_NTBINDIR)\tools\devdiv\loc\Loctask\Localization.settings.targets" Condition=" '$(RunFromAll)' == '' "/>
                          </Project>

            If IsVisualStudioLanguageServiceComponent(fileName) Then
                ' Also, add a reference to the Shell binary as well
                locProj.<msbuild:Project>.<msbuild:ItemGroup>.Single().Add(<WPFDependency Include="$(Binaries)\Microsoft.VisualStudio.Shell.14.0.dll" xmlns="http://schemas.microsoft.com/developer/msbuild/2003"/>)
                locProj.<msbuild:Project>.<msbuild:ItemGroup>.Single().Add(<WPFDependency Include="$(Binaries)\bin\$(BinChip)\Microsoft.VisualStudio.Utilities.dll" xmlns="http://schemas.microsoft.com/developer/msbuild/2003"/>)
            End If

            locProjects.Add(locProjFileName)

            locProj.Save(GetAbsolutePathInOutputDirectory(Path.Combine("Roslyn", "LocProjects", locProjFileName)), SaveOptions.OmitDuplicateNamespaces)

            Dim lci = <?xml version="1.0" encoding="utf-8"?>
                      <LCX SchemaVersion="6.0" Name=<%= Path.Combine("f:\ddSetup\sources", devdivPath) %> PsrId="211" FileType="1" SrcCul="en-US" xmlns="http://schemas.microsoft.com/locstudio/2006/6/lcx">
                          <OwnedComments>
                              <Cmt Name="LcxAdmin"/>
                              <Cmt Name="Loc"/>
                          </OwnedComments>
                      </LCX>

            lci.Save(GetAbsolutePathInOutputDirectory(Path.Combine("Roslyn", "LCI", lciFileName)), SaveOptions.OmitDuplicateNamespaces)
        End Sub

        Private Function GetAssemblyResourcesDllName(fileName As String) As String
            ' The resource path always ends in a .dll, even if it's originally an .exe
            Return Path.GetFileNameWithoutExtension(fileName) + ".resources.dll"
        End Function

        Private Function IsLanguageServiceRegistrationFile(fileName As String) As Boolean
            Select Case Path.GetExtension(fileName)
                Case ".vsixmanifest", ".pkgdef", ".png", ".ico", ".vsdconfig"
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Private Shared Function IsExecutableCodeFileName(fileName As String) As Boolean
            Dim extension = Path.GetExtension(fileName)
            Return extension = ".exe" OrElse extension = ".dll"
        End Function

        Private Function GetAbsolutePathInOutputDirectory(relativePath As String) As String
            Dim absolutePath = Path.Combine(outputDirectory, relativePath)

            ' Ensure that the parent directories are all created
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath))

            Return absolutePath
        End Function

        ''' <summary>
        ''' Rewrites a .pkgdef file to load any packages from PrivateAssemblies instead of from the .vsix's folder. This allows
        ''' for better use of ngen'ed images when we are installed into VS.
        ''' </summary>
        Private Sub RewritePkgDef(fileToRewrite As String)
            ' Our VSIXes normally contain a number of CodeBase attributes in our .pkgdefs so Visual Studio knows where
            ' to load assemblies. These come in one of three forms:
            '
            ' 1) as a part of a binding redirection:
            '
            '     [$RootKey$\RuntimeConfiguration\dependentAssembly\bindingRedirection\{A907DD23-73A7-8934-9396-93F10C532071}]
            '     "name"="System.Reflection.Metadata"
            '     "publicKeyToken"="b03f5f7f11d50a3a"
            '     "culture"="neutral"
            '     "oldVersion"="1.0.0.0-1.0.99.0"
            '     "newVersion"="1.1.0.0"
            '     "codeBase"="$PackageFolder$\System.Reflection.Metadata.dll"
            '
            ' 2) as part of a codebase-only specification without a binding redirect:
            '
            '     [$RootKey$\RuntimeConfiguration\dependentAssembly\codeBase\{8C6E3F81-ED3F-306B-107F-60D6E74DA5B0}]
            '     "name"="Esent.Interop"
            '     "publicKeyToken"="31bf3856ad364e35"
            '     "culture"="neutral"
            '     "version"="1.9.2.0"
            '     "codeBase"="$PackageFolder$\Esent.Interop.dll"
            '
            ' 3) as part of a package definition:
            '
            '     [$RootKey$\Packages\{13c3bbb4-f18f-4111-9f54-a0fb010d9194}]
            '     @="CSharpPackage"
            '     "InprocServer32"="$WinDir$\SYSTEM32\MSCOREE.DLL"
            '     "Class"="Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService.CSharpPackage"
            '     "CodeBase"="$PackageFolder$\Microsoft.VisualStudio.LanguageServices.CSharp.dll"
            '
            ' Each of these use $PackageFolder$ as a way to specify the VSIX-relative path. When we convert our VSIXes
            ' to be installed as MSIs, we don't want the DLLs in the CommonExtensions next to our .pkgdefs. Instead
            ' we want them in PrivateAssemblies so they're in the loading path to enable proper ngen. Thus, these CodeBase
            ' attributes have to go. For #1, we can just delete the codeBase key, and leave the rest of the redirection
            ' in place. For #2, we can delete the entire section. For #3, it's a bit tricker; we have to convert it to
            ' an Assembly key which is just the name of the assembly without the path.

            Dim lines = File.ReadAllLines(fileToRewrite)
            Dim inBindingRedirect = False
            Dim inCodebase = False

            For i = 0 To lines.Count - 1

                Dim line = lines(i)

                If line.StartsWith("[") Then
                    inBindingRedirect = line.IndexOf("bindingRedirection", StringComparison.OrdinalIgnoreCase) >= 0
                    inCodebase = line.IndexOf("RuntimeConfiguration\dependentAssembly\codeBase", StringComparison.OrdinalIgnoreCase) >= 0
                End If

                Dim parts = line.Split({"="c}, count:=2)

                If inCodebase Then
                    ' Explicit codebase attributes must always be dropped
                    lines(i) = Nothing
                ElseIf String.Equals(parts(0), """CodeBase""", StringComparison.OrdinalIgnoreCase) Then
                    If inBindingRedirect Then
                        ' Drop CodeBase from all binding redirects -- they're only for VSIX installs
                        lines(i) = Nothing
                    ElseIf parts(1).StartsWith("""") AndAlso parts(1).EndsWith("""") Then
                        Dim valueWithoutQuotes = parts(1).Substring(1, parts(1).Length - 2)
                        Dim assemblyName = Path.GetFileNameWithoutExtension(valueWithoutQuotes)
                        Dim qualifiedName = assemblyName + ", Version=" + assemblyVersion + ", Culture=neutral, PublicKeyToken=" + publicKeyToken
                        lines(i) = """Assembly""=""" + qualifiedName + """"
                    End If
                ElseIf String.Equals(parts(0), """isPkgDefOverrideEnabled""", StringComparison.OrdinalIgnoreCase) Then
                    ' We always need to drop this, since this is only for experimental VSIXes
                    lines(i) = Nothing
                End If
            Next

            File.WriteAllLines(fileToRewrite, lines.Where(Function(l) l IsNot Nothing))
        End Sub
    End Class
End Module

Imports System.IO.Packaging
Imports System.IO
Imports System.Threading
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Reflection.PortableExecutable
Imports System.Reflection.Metadata
Imports System.Runtime.InteropServices

Public Class BuildDevDivInsertionFiles
    Private Const DevDivInsertionFilesDirName = "DevDivInsertionFiles"
    Private Const DevDivPackagesDirName = "DevDivPackages"
    Private Const DevDivVsixDirName = "DevDivVsix"
    Private Const ExternalApisDirName = "ExternalAPIs"
    Private Const PublicKeyToken = "31BF3856AD364E35"

    Private ReadOnly _binDirectory As String
    Private ReadOnly _outputDirectory As String
    Private ReadOnly _outputPackageDirectory As String
    Private ReadOnly _setupDirectory As String
    Private ReadOnly _nugetPackageRoot As String
    Private ReadOnly _assemblyVersion As String
    Private ReadOnly _pathMap As Dictionary(Of String, String)

    Private Sub New(args As String())
        _binDirectory = Path.GetFullPath(args(0))
        _setupDirectory = Path.GetFullPath(args(1))
        _nugetPackageRoot = Path.GetFullPath(args(2))
        _outputDirectory = Path.Combine(_binDirectory, DevDivInsertionFilesDirName)
        _outputPackageDirectory = Path.Combine(_binDirectory, DevDivPackagesDirName)
        _assemblyVersion = args(3)
        _pathMap = CreatePathMap()
    End Sub

    Public Shared Function Main(args As String()) As Integer
        If args.Length <> 4 Then
            Console.WriteLine("Expected arguments: <bin dir> <setup dir> <nuget root dir> <assembly version>")
            Console.WriteLine($"Actual argument count is {args.Length}")
            Return 1
        End If

        Try
            Call New BuildDevDivInsertionFiles(args).Execute()
            Return 0
        Catch ex As Exception
            Console.Error.WriteLine(ex.ToString())
            Return 1
        End Try
    End Function

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
        "SQLitePCLRaw.batteries_green.dll",
        "SQLitePCLRaw.batteries_v2.dll",
        "SQLitePCLRaw.core.dll",
        "SQLitePCLRaw.provider.e_sqlite3.dll",
        "e_sqlite3.dll",
        "Newtonsoft.Json.dll",
        "StreamJsonRpc.dll",
        "StreamJsonRpc.resources.dll",
        "codeAnalysisService.servicehub.service.json",
        "remoteHostService.servicehub.service.json",
        "serviceHubSnapshotService.servicehub.service.json",
        "Microsoft.Build.Conversion.Core.dll",
        "Microsoft.Build.dll",
        "Microsoft.Build.Engine.dll",
        "Microsoft.Build.Framework.dll",
        "Microsoft.Build.Tasks.Core.dll",
        "Microsoft.Build.Utilities.Core.dll",
        "Microsoft.VisualStudio.Threading.dll",
        "Microsoft.VisualStudio.Threading.resources.dll",
        "Microsoft.VisualStudio.Validation.dll",
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
        "VisualBasicInteractivePackageRegistration.pkgdef"
    }

    ' N.B. This list of facades must be kept in-sync with the &
    ' other facades used by the compiler. Facades are listed in
    ' the src/NuGet/Microsoft.Net.Compilers.nuspec file, the
    ' src/Setup/DevDivVsix/CompilersPackage/Microsoft.CodeAnalysis.Compilers.swr file,
    ' and src/Compilers/Extension/CompilerExtension.csproj file.
    '
    ' Note: Microsoft.DiaSymReader.Native.amd64.dll and Microsoft.DiaSymReader.Native.x86.dll
    ' are installed by msbuild setup, not Roslyn.
    Private ReadOnly CompilerFiles As String() = {
        "Microsoft.CodeAnalysis.dll",
        "Microsoft.CodeAnalysis.CSharp.dll",
        "Microsoft.CodeAnalysis.Scripting.dll",
        "Microsoft.CodeAnalysis.CSharp.Scripting.dll",
        "Microsoft.CodeAnalysis.VisualBasic.dll",
        "System.AppContext.dll",
        "System.Console.dll",
        "System.Diagnostics.FileVersionInfo.dll",
        "System.Diagnostics.Process.dll",
        "System.Diagnostics.StackTrace.dll",
        "System.IO.Compression.dll",
        "System.IO.FileSystem.dll",
        "System.IO.FileSystem.DriveInfo.dll",
        "System.IO.FileSystem.Primitives.dll",
        "System.IO.Pipes.dll",
        "System.Security.AccessControl.dll",
        "System.Security.Claims.dll",
        "System.Security.Cryptography.Algorithms.dll",
        "System.Security.Cryptography.Encoding.dll",
        "System.Security.Cryptography.Primitives.dll",
        "System.Security.Cryptography.X509Certificates.dll",
        "System.Security.Principal.Windows.dll",
        "System.Text.Encoding.CodePages.dll",
        "System.Threading.Thread.dll",
        "System.ValueTuple.dll",
        "System.Xml.ReaderWriter.dll",
        "System.Xml.XmlDocument.dll",
        "System.Xml.XPath.dll",
        "System.Xml.XPath.XDocument.dll",
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
        "Vsix\VisualStudioSetup\Roslyn.VisualStudio.Setup.vsix",
        "Vsix\ExpressionEvaluatorPackage\ExpressionEvaluatorPackage.vsix",
        "Vsix\VisualStudioInteractiveComponents\Roslyn.VisualStudio.InteractiveComponents.vsix",
        "Vsix\VisualStudioSetup.Next\Roslyn.VisualStudio.Setup.Next.vsix"
    }

    ' Files copied to Maddog machines running integration tests that are produced from our builds.
    Private ReadOnly IntegrationTestFiles As String() = {
        "xunit.*.dll",
        "*.UnitTests.dll.config",
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
        "Microsoft.VisualStudio.IntegrationTest.Setup.vsix",
        "Microsoft.VisualStudio.LanguageServices.CSharp.dll",
        "Microsoft.VisualStudio.LanguageServices.dll",
        "Microsoft.VisualStudio.LanguageServices.Implementation.dll",
        "Microsoft.VisualStudio.LanguageServices.VisualBasic.dll",
        "Microsoft.VisualStudio.Platform.VSEditor.Interop.dll",
        "Roslyn.Compilers.Test.Resources.dll",
        "Roslyn.Hosting.Diagnostics.dll",
        "Roslyn.Services.Test.Utilities.dll",
        "Roslyn.Test.PdbUtilities.dll",
        "Roslyn.Test.Utilities.dll"
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
        "*.UnitTests.dll.config",
        "Microsoft.*.UnitTests*.dll",
        "Roslyn.*.UnitTests*.dll",
        "xunit.*.dll",
        "PerfTests",
        "BasicUndo.dll",
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
        "Microsoft.CodeAnalysis.ExpressionEvaluator.FunctionResolver.dll",
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
        "Microsoft.DiaSymReader.Converter.Xml.dll",
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

    Private Sub DeleteDirContents(dir As String)
        If Directory.Exists(dir) Then
            ' Delete everything within it. We'll keep the top-level one around.
            For Each file In New DirectoryInfo(dir).GetFiles()
                file.Delete()
            Next

            For Each directory In New DirectoryInfo(dir).GetDirectories()
                directory.Delete(recursive:=True)
            Next
        End If
    End Sub

    Public Sub Execute()
        Retry(Sub()
                  DeleteDirContents(_outputDirectory)
                  DeleteDirContents(_outputPackageDirectory)
              End Sub)

        ' Build a dependency map
        Dim dependencies = BuildDependencyMap(_binDirectory)
        GenerateContractsListMsbuild(dependencies)
        GenerateAssemblyVersionList(dependencies)
        GeneratePortableFacadesSwrFile(dependencies)
        CopyDependencies(dependencies)

        ' List of files to add to VS.ExternalAPI.Roslyn.nuspec.
        ' Paths are relative to input directory.
        ' Files in DevDivInsertionFiles\ExternalAPIs don't need to be added, they are included in the nuspec using a pattern.
        ' May contain duplicates.
        Dim filesToInsert = New List(Of NugetFileInfo)

        ' And now copy over all our core compiler binaries and related files
        ' Build tools setup authoring depends on these files being inserted.
        For Each fileName In CompilerFiles
            Dim dependency As DependencyInfo = Nothing
            If Not dependencies.TryGetValue(fileName, dependency) Then
                AddXmlDocumentationFile(filesToInsert, fileName)
                filesToInsert.Add(New NugetFileInfo(GetMappedPath(fileName)))
            End If
        Next

        ' VS.Tools.Roslyn CoreXT package needs to contain all dependencies.
        Dim vsToolsetFiles = CompilerFiles.Concat({
            "System.Collections.Immutable.dll",
            "System.Reflection.Metadata.dll",
            "Microsoft.DiaSymReader.Native.amd64.dll",
            "Microsoft.DiaSymReader.Native.x86.dll"})

        GenerateVSToolsRoslynCoreXTNuspec(vsToolsetFiles)

        ' Copy over the files in the NetFX20 subdirectory (identical, except for references and Authenticode signing).
        ' These are for msvsmon, whose setup authoring is done by the debugger.
        For Each folder In Directory.EnumerateDirectories(Path.Combine(_binDirectory, "Dlls"), "*.NetFX20")
            For Each eePath In Directory.EnumerateFiles(folder, "*.ExpressionEvaluator.*.dll", SearchOption.TopDirectoryOnly)
                filesToInsert.Add(New NugetFileInfo(GetPathRelativeToBinaries(eePath), GetPathRelativeToBinaries(folder)))
            Next
        Next

        ProcessVsixFiles(filesToInsert, dependencies)

        ' Generate Roslyn.nuspec:
        GenerateRoslynNuSpec(filesToInsert)

        ' Generate lists of files that are needed to run unit and integration tests in Maddog:
        Dim insertedFiles = New HashSet(Of String)(filesToInsert.Select(Function(f) f.Path), StringComparer.OrdinalIgnoreCase)
        GenerateTestFileDependencyList(NameOf(UnitTestFiles), ExpandTestDependencies(UnitTestFiles), insertedFiles)
        GenerateTestFileDependencyList(NameOf(UnitTestFilesExtra), UnitTestFilesExtra, insertedFiles)
        GenerateTestFileDependencyList(NameOf(IntegrationTestFiles), ExpandTestDependencies(IntegrationTestFiles), insertedFiles)
        GenerateTestFileDependencyList(NameOf(IntegrationTestFilesExtra), IntegrationTestFilesExtra, insertedFiles)
    End Sub

    Private Function GetPathRelativeToBinaries(p As String) As String
        Debug.Assert(p.StartsWith(_binDirectory, StringComparison.OrdinalIgnoreCase))
        p = p.Substring(_binDirectory.Length)
        If Not String.IsNullOrEmpty(p) AndAlso p(0) = "\"c Then
            p = p.Substring(1)
        End If
        Return p
    End Function

    Private Shared Function GetExternalApiDirectory() As String
        Return Path.Combine(ExternalApisDirName, "Roslyn")
    End Function

    Private Shared Function GetExternalApiDirectory(dependency As DependencyInfo, contract As Boolean) As String
        Return If(dependency Is Nothing,
                GetExternalApiDirectory(),
                Path.Combine(ExternalApisDirName, dependency.PackageName, If(contract, dependency.ContractDir, dependency.ImplementationDir)))
    End Function

    Private Class NugetFileInfo
        Implements IEquatable(Of NugetFileInfo)

        Public Path As String
        Public Target As String

        Sub New(path As String, Optional target As String = "")
            If IO.Path.IsPathRooted(path) Then
                Throw New ArgumentException($"Parameter {NameOf(path)} cannot be absolute: {path}")
            End If

            If IO.Path.IsPathRooted(target) Then
                Throw New ArgumentException($"Parameter {NameOf(target)} cannot be absolute: {target}")
            End If

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

        Public Overrides Function ToString() As String
            Return Path
        End Function
    End Class

    Private Class DependencyInfo
        ' For example, "ref/net46"
        Public ContractDir As String

        ' For example, "lib/net46"
        Public ImplementationDir As String

        ' For example, "System.AppContext"
        Public PackageName As String

        ' For example, "4.1.0"
        Public PackageVersion As String

        Public IsNative As Boolean
        Public IsFacade As Boolean

        Sub New(contractDir As String, implementationDir As String, packageName As String, packageVersion As String, isNative As Boolean, isFacade As Boolean)
            Me.ContractDir = contractDir
            Me.ImplementationDir = implementationDir
            Me.PackageName = packageName
            Me.PackageVersion = packageVersion
            Me.IsNative = isNative
            Me.IsFacade = isFacade
        End Sub

        ' TODO: remove (https://github.com/dotnet/roslyn/issues/13204)
        ' Don't update CoreXT incompatible packages. They are inserted manually until CoreXT updates to NuGet 3.5 RTM.
        Public ReadOnly Property IsCoreXTCompatible As Boolean
            Get
                Select Case PackageName
                    Case "System.Security.Cryptography.Algorithms",
                          "System.Security.Cryptography.X509Certificates",
                          "System.Reflection.TypeExtensions",
                          "System.Net.Security",
                          "System.Diagnostics.Process",
                          "System.AppContext",
                          "System.IO.Compression"

                        Return False
                    Case Else
                        Return True
                End Select
            End Get
        End Property
    End Class

    Private Function BuildDependencyMap(inputDirectory As String) As Dictionary(Of String, DependencyInfo)
        Dim result = New Dictionary(Of String, DependencyInfo)
        Dim objDir = Path.Combine(Path.GetDirectoryName(_binDirectory.TrimEnd(Path.DirectorySeparatorChar)), "Obj")
        Dim files = New List(Of String)
        files.Add(Path.Combine(objDir, "DevDivPackagesRoslyn\project.assets.json"))
        files.Add(Path.Combine(objDir, "DevDivPackagesDebugger\project.assets.json"))

        For Each projectLockJson In files
            Dim items = JsonConvert.DeserializeObject(File.ReadAllText(projectLockJson))
            Const targetFx = ".NETFramework,Version=v4.6/win"

            Dim targetObj = DirectCast(DirectCast(DirectCast(items, JObject).Property("targets")?.Value, JObject).Property(targetFx)?.Value, JObject)
            If targetObj Is Nothing Then
                Throw New InvalidDataException($"Expected platform Not found in '{projectLockJson}': '{targetFx}'")
            End If

            For Each targetProperty In targetObj.Properties
                Dim packageNameAndVersion = targetProperty.Name.Split("/"c)
                Dim packageName = packageNameAndVersion(0)
                Dim packageVersion = packageNameAndVersion(1)
                Dim packageObj = DirectCast(targetProperty.Value, JObject)

                Dim contracts = DirectCast(packageObj.Property("compile")?.Value, JObject)
                Dim runtime = DirectCast(packageObj.Property("runtime")?.Value, JObject)
                Dim native = DirectCast(packageObj.Property("native")?.Value, JObject)
                Dim frameworkAssemblies = packageObj.Property("frameworkAssemblies")?.Value

                Dim implementations = If(runtime, native)
                If implementations Is Nothing Then
                    Continue For
                End If

                For Each assemblyProperty In implementations.Properties()
                    Dim fileName = Path.GetFileName(assemblyProperty.Name)
                    If fileName <> "_._" Then

                        Dim existingDependency As DependencyInfo = Nothing
                        If result.TryGetValue(fileName, existingDependency) Then

                            If existingDependency.PackageVersion <> packageVersion Then
                                Throw New InvalidOperationException($"Found multiple versions of package '{existingDependency.PackageName}': {existingDependency.PackageVersion} and {packageVersion}")
                            End If

                            Continue For
                        End If

                        Dim runtimeTarget = Path.GetDirectoryName(assemblyProperty.Name)

                        Dim compileDll = contracts?.Properties().Select(Function(p) p.Name).Where(Function(n) Path.GetFileName(n) = fileName).Single()
                        Dim compileTarget = If(compileDll IsNot Nothing, Path.GetDirectoryName(compileDll), Nothing)

                        result.Add(fileName, New DependencyInfo(compileTarget,
                                                                runtimeTarget,
                                                                packageName,
                                                                packageVersion,
                                                                isNative:=native IsNot Nothing,
                                                                isFacade:=frameworkAssemblies IsNot Nothing))
                    End If
                Next
            Next
        Next

        Return result
    End Function

    Private Sub GenerateContractsListMsbuild(dependencies As IReadOnlyDictionary(Of String, DependencyInfo))
        Using writer = New StreamWriter(GetAbsolutePathInOutputDirectory("ProductData\ContractAssemblies.props"))
            writer.WriteLine("<?xml version=""1.0"" encoding=""utf-8""?>")
            writer.WriteLine("<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">")
            writer.WriteLine("  <!-- Generated file, do not directly edit. Contact mlinfraswat@microsoft.com if you need to add a library that's not listed -->")
            writer.WriteLine("  <PropertyGroup>")

            For Each entry In GetContracts(dependencies)
                writer.WriteLine($"    <{entry.Key}>{entry.Value}</{entry.Key}>")
            Next

            writer.WriteLine("  </PropertyGroup>")
            writer.WriteLine("</Project>")
        End Using
    End Sub

    Private Iterator Function GetContracts(dependencies As IReadOnlyDictionary(Of String, DependencyInfo)) As IEnumerable(Of KeyValuePair(Of String, String))
        For Each entry In dependencies.OrderBy(Function(e) e.Key)
            Dim fileName = entry.Key
            Dim dependency = entry.Value
            If dependency.ContractDir IsNot Nothing Then
                Dim variableName = "FXContract_" + Path.GetFileNameWithoutExtension(fileName).Replace(".", "_")
                Dim dir = Path.Combine(dependency.PackageName, dependency.ContractDir)
                Yield New KeyValuePair(Of String, String)(variableName, Path.Combine(dir, fileName))
            End If
        Next
    End Function

    Private Iterator Function GetImplementations(dependencies As IReadOnlyDictionary(Of String, DependencyInfo)) As IEnumerable(Of KeyValuePair(Of String, String))
        For Each entry In dependencies.OrderBy(Function(e) e.Key)
            Dim fileName = entry.Key
            Dim dependency = entry.Value
            Dim variableName = "CoreFXLib_" + Path.GetFileNameWithoutExtension(fileName).Replace(".", "_")
            Dim dir = Path.Combine(dependency.PackageName, dependency.ImplementationDir)
            Yield New KeyValuePair(Of String, String)(variableName, Path.Combine(dir, fileName))
        Next
    End Function

    Private Sub GenerateAssemblyVersionList(dependencies As IReadOnlyDictionary(Of String, DependencyInfo))
        Using writer = New StreamWriter(GetAbsolutePathInOutputDirectory("DependentAssemblyVersions.csv"))
            For Each entry In dependencies.OrderBy(Function(e) e.Key)
                Dim fileName = entry.Key
                Dim dependency = entry.Value
                If Not dependency.IsNative Then

                    Dim version As Version
                    Dim dllPath = Path.Combine(_nugetPackageRoot, dependency.PackageName, dependency.PackageVersion, dependency.ImplementationDir, fileName)

                    Using peReader = New PEReader(File.OpenRead(dllPath))
                        version = peReader.GetMetadataReader().GetAssemblyDefinition().Version
                    End Using

                    writer.WriteLine($"{Path.GetFileNameWithoutExtension(fileName)},{version}")
                End If
            Next
        End Using
    End Sub

    Private Sub CopyDependencies(dependencies As IReadOnlyDictionary(Of String, DependencyInfo))
        For Each dependency In dependencies.Values
            ' TODO: remove (https://github.com/dotnet/roslyn/issues/13204)
            ' Don't update CoreXT incompatible packages. They are inserted manually until CoreXT updates to NuGet 3.5 RTM.
            If Not dependency.IsCoreXTCompatible Then
                Continue For
            End If

            Dim nupkg = $"{dependency.PackageName}.{dependency.PackageVersion}.nupkg"
            Dim srcPath = Path.Combine(_nugetPackageRoot, dependency.PackageName, dependency.PackageVersion, nupkg)
            Dim dstDir = Path.Combine(_outputPackageDirectory, If(dependency.IsNative, "NativeDependencies", "ManagedDependencies"))
            Dim dstPath = Path.Combine(dstDir, nupkg)

            Directory.CreateDirectory(dstDir)
            File.Copy(srcPath, dstPath, overwrite:=True)
        Next
    End Sub

    Private Sub GeneratePortableFacadesSwrFile(dependencies As Dictionary(Of String, DependencyInfo))
        Dim facades = dependencies.Where(Function(e) e.Value.IsFacade).OrderBy(Function(e) e.Key).ToArray()

        Dim swrPath = Path.Combine(_setupDirectory, DevDivVsixDirName, "PortableFacades", "PortableFacades.swr")
        Dim swrVersion As Version = Nothing
        Dim swrFiles As IEnumerable(Of String) = Nothing
        ParseSwrFile(swrPath, swrVersion, swrFiles)

        Dim expectedFiles = New List(Of String)
        For Each entry In facades
            Dim dependency = entry.Value
            Dim fileName = entry.Key
            Dim implPath = IO.Path.Combine(dependency.PackageName, dependency.PackageVersion, dependency.ImplementationDir, fileName)
            expectedFiles.Add($"    file source=""$(NuGetPackageRoot)\{implPath}"" vs.file.ngen=yes")
        Next

        If Not swrFiles.SequenceEqual(expectedFiles) Then
            Using writer = New StreamWriter(File.Open(swrPath, FileMode.Truncate, FileAccess.Write))
                writer.WriteLine("use vs")
                writer.WriteLine()
                writer.WriteLine($"package name=PortableFacades")
                writer.WriteLine($"        version={New Version(swrVersion.Major, swrVersion.Minor + 1, 0, 0)}")
                writer.WriteLine()
                writer.WriteLine("folder InstallDir:\Common7\IDE\PrivateAssemblies")

                For Each entry In expectedFiles
                    writer.WriteLine(entry)
                Next
            End Using

            Throw New Exception($"The content of file {swrPath} is not up-to-date. The file has been updated to reflect the changes in dependencies made in the repo " &
                                $"(in files {Path.Combine(_setupDirectory, DevDivPackagesDirName)}\**\project.json). Include this file change in your PR and rebuild.")
        End If
    End Sub

    Private Sub ParseSwrFile(path As String, <Out> ByRef version As Version, <Out> ByRef files As IEnumerable(Of String))
        Dim lines = File.ReadAllLines(path)

        version = Version.Parse(lines.Single(Function(line) line.TrimStart().StartsWith("version=")).Split("="c)(1))
        files = (From line In lines Where line.TrimStart().StartsWith("file")).ToArray()
    End Sub

    ''' <summary>
    ''' Generate a list of files which were not inserted and place them in the named file.
    ''' </summary>
    Private Sub GenerateTestFileDependencyList(outputFileName As String, fileSpecs As IEnumerable(Of String), insertedFiles As HashSet(Of String))
        File.WriteAllLines(
                Path.Combine(_outputDirectory, Path.ChangeExtension(outputFileName, ".txt")),
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
        Dim allGood = True
        For Each spec In fileSpecs
            If spec.Contains("*") Then
                For Each path In Directory.EnumerateFiles(_binDirectory, spec, SearchOption.TopDirectoryOnly)
                    Yield path.Substring(_binDirectory.Length)
                Next
            Else
                spec = GetPotentiallyMappedPath(spec)
                Dim inputItem = Path.Combine(_binDirectory, spec)

                If Directory.Exists(inputItem) Then
                    For Each path In Directory.EnumerateFiles(inputItem, "*.*", SearchOption.AllDirectories)
                        Yield path.Substring(_binDirectory.Length)
                    Next
                ElseIf File.Exists(inputItem) Then
                    Yield spec
                Else
                    Console.WriteLine($"File Or directory '{spec}' listed in test dependencies doesn't exist.", spec)
                    allGood = False
                End If
            End If
        Next

        If Not allGood Then
            Throw New Exception("Unable to expand test dependencies")
        End If
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

    ''' <summary>
    ''' Recently a number of our compontents have moved from the root of the output directory to sub-directories. The
    ''' map returned from this function maps file names to their relative path in the build output.
    '''
    ''' This is still pretty terrible though.  Instead of doing all this name matching we should have explicit paths 
    ''' and match on file contents.  That is a large change for this tool though.  As a temporary work around this 
    ''' map will be used instead.
    ''' </summary>
    Private Function CreatePathMap() As Dictionary(Of String, String)

        Dim map As New Dictionary(Of String, String)
        Dim add = Sub(filePath As String)
                      If Not File.Exists(Path.Combine(_binDirectory, filePath)) Then
                          Throw New Exception($"Mapped VSIX path does not exist: {filePath}")
                      End If
                      Dim name = Path.GetFileName(filePath)
                      map.Add(name, filePath)
                  End Sub

        Dim configPath = Path.Combine(_binDirectory, "..\..\build\config\SignToolData.json")
        Dim obj = JObject.Parse(File.ReadAllText(configPath))
        Dim array = CType(obj.Property("sign").Value, JArray)
        For Each element As JObject In array
            Dim values = CType(element.Property("values").Value, JArray)
            For Each item As String In values
                Dim parent = Path.GetDirectoryName(item)

                ' Don't add in the csc.exe or vbc.exe from the CoreCLR projects.
                If parent.EndsWith("Core", StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If

                If parent.EndsWith("NetFX20", StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If

                add(item)
            Next
        Next

        add("Exes\csc\csc.exe.config")
        add("Exes\csc\csc.rsp")
        add("Exes\vbc\vbc.exe.config")
        add("Exes\vbc\vbc.rsp")
        add("Exes\VBCSCompiler\VBCSCompiler.exe.config")
        add("Exes\InteractiveHost\InteractiveHost.exe.config")
        add("Exes\csi\csi.rsp")
        add("Vsix\Roslyn.Deployment.Full.Next\remoteSymbolSearchUpdateEngine.servicehub.service.json")
        add("Vsix\Roslyn.Deployment.Full.Next\snapshotService.servicehub.service.json")
        add("Vsix\VisualStudioInteractiveComponents\CSharpInteractive.rsp")
        add("Vsix\VisualStudioSetup\System.Composition.Convention.dll")
        add("Vsix\VisualStudioSetup\System.Composition.Hosting.dll")
        add("Vsix\VisualStudioSetup\System.Composition.TypedParts.dll")
        add("Vsix\VisualStudioSetup.Next\Microsoft.VisualStudio.CallHierarchy.Package.Definitions.dll")
        add("Dlls\BasicExpressionCompiler\Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.ExpressionCompiler.vsdconfig")
        add("Dlls\BasicResultProvider.Portable\Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.ResultProvider.vsdconfig")
        add("Dlls\CSharpExpressionCompiler\Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.ExpressionCompiler.vsdconfig")
        add("Dlls\CSharpResultProvider.Portable\Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.ResultProvider.vsdconfig")
        add("Dlls\FunctionResolver\Microsoft.CodeAnalysis.ExpressionEvaluator.FunctionResolver.vsdconfig")
        add("Dlls\MSBuildTask\Microsoft.CSharp.Core.targets")
        add("Dlls\MSBuildTask\Microsoft.VisualBasic.Core.targets")
        add("Dlls\CSharpCompilerTestUtilities\Roslyn.Compilers.CSharp.Test.Utilities.dll")
        add("Dlls\BasicCompilerTestUtilities\Roslyn.Compilers.VisualBasic.Test.Utilities.dll")
        add("Dlls\CompilerTestResources\\Roslyn.Compilers.Test.Resources.dll")
        add("Dlls\ExpressionCompilerTestUtilities\Roslyn.ExpressionEvaluator.ExpressionCompiler.Test.Utilities.dll")
        add("Dlls\ResultProviderTestUtilities\Roslyn.ExpressionEvaluator.ResultProvider.Test.Utilities.dll")
        add("Dlls\ServicesTestUtilities\Roslyn.Services.Test.Utilities.dll")
        add("Dlls\PdbUtilities\Roslyn.Test.PdbUtilities.dll")
        add("Dlls\TestUtilities.Desktop\Roslyn.Test.Utilities.Desktop.dll")
        add("Dlls\TestUtilities\net461\Roslyn.Test.Utilities.dll")
        add("UnitTests\EditorServicesTest\BasicUndo.dll")
        add("UnitTests\EditorServicesTest\Moq.dll")
        add("UnitTests\EditorServicesTest\Microsoft.CodeAnalysis.Test.Resources.Proprietary.dll")
        add("UnitTests\EditorServicesTest\Microsoft.DiaSymReader.PortablePdb.dll")
        add("UnitTests\EditorServicesTest\Microsoft.DiaSymReader.Converter.Xml.dll")
        add("UnitTests\EditorServicesTest\Microsoft.DiaSymReader.dll")
        add("UnitTests\EditorServicesTest\Microsoft.DiaSymReader.Native.amd64.dll")
        add("UnitTests\EditorServicesTest\Microsoft.DiaSymReader.Native.x86.dll")
        add("UnitTests\EditorServicesTest\Microsoft.VisualStudio.Platform.VSEditor.Interop.dll")
        add("Vsix\ExpressionEvaluatorPackage\Microsoft.VisualStudio.Debugger.Engine.dll")
        add("Vsix\VisualStudioIntegrationTestSetup\Microsoft.Diagnostics.Runtime.dll")
        add("Vsix\VisualStudioIntegrationTestSetup\Microsoft.VisualStudio.IntegrationTest.Setup.vsix")
        add("Exes\Toolset\System.AppContext.dll")
        add("Exes\Toolset\System.Console.dll")
        add("Exes\Toolset\System.Collections.Immutable.dll")
        add("Exes\Toolset\System.Diagnostics.FileVersionInfo.dll")
        add("Exes\Toolset\System.Diagnostics.Process.dll")
        add("Exes\Toolset\System.Diagnostics.StackTrace.dll")
        add("Exes\Toolset\System.IO.Compression.dll")
        add("Exes\Toolset\System.IO.FileSystem.dll")
        add("Exes\Toolset\System.IO.FileSystem.DriveInfo.dll")
        add("Exes\Toolset\System.IO.FileSystem.Primitives.dll")
        add("Exes\Toolset\System.IO.Pipes.dll")
        add("Exes\Toolset\System.Reflection.Metadata.dll")
        add("Exes\Toolset\System.Security.AccessControl.dll")
        add("Exes\Toolset\System.Security.Claims.dll")
        add("Exes\Toolset\System.Security.Cryptography.Algorithms.dll")
        add("Exes\Toolset\System.Security.Cryptography.Encoding.dll")
        add("Exes\Toolset\System.Security.Cryptography.Primitives.dll")
        add("Exes\Toolset\System.Security.Cryptography.X509Certificates.dll")
        add("Exes\Toolset\System.Security.Principal.Windows.dll")
        add("Exes\Toolset\System.Text.Encoding.CodePages.dll")
        add("Exes\Toolset\System.Threading.Thread.dll")
        add("Exes\Toolset\System.ValueTuple.dll")
        add("Exes\Toolset\System.Xml.ReaderWriter.dll")
        add("Exes\Toolset\System.Xml.XmlDocument.dll")
        add("Exes\Toolset\System.Xml.XPath.dll")
        add("Exes\Toolset\System.Xml.XPath.XDocument.dll")
        Return map
    End Function

    Private Function GetMappedPath(fileName As String) As String
        Dim mappedPath As String = Nothing
        If Not _pathMap.TryGetValue(fileName, mappedPath) Then
            Throw New Exception($"File name {fileName} does not have a mapped path")
        End If

        Return mappedPath
    End Function

    Private Function GetPotentiallyMappedPath(fileName As String) As String
        Dim mappedPath As String = Nothing
        If _pathMap.TryGetValue(fileName, mappedPath) Then
            Return mappedPath
        Else
            Return fileName
        End If
    End Function

    Private Sub ProcessVsixFiles(filesToInsert As List(Of NugetFileInfo), dependencies As Dictionary(Of String, DependencyInfo))
        Dim processedFiles = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Dim allGood = True

        ' We build our language service authoring by cracking our .vsixes and pulling out the bits that matter
        For Each vsixFileName In VsixesToInstall
            Dim vsixName As String = Path.GetFileNameWithoutExtension(vsixFileName)
            Using vsix = Package.Open(Path.Combine(_binDirectory, vsixFileName), FileMode.Open, FileAccess.Read, FileShare.Read)
                For Each vsixPart In vsix.GetParts()

                    ' This part might be metadata for the digital signature. In that case, skip it
                    If vsixPart.ContentType.StartsWith("application/vnd.openxmlformats-package.") Then
                        Continue For
                    End If

                    Dim partRelativePath = GetPartRelativePath(vsixPart)
                    Dim partFileName = Path.GetFileName(partRelativePath)

                    ' If this is something that we don't need to ship, skip it
                    If VsixContentsToSkip.Contains(partFileName) Then
                        Continue For
                    End If

                    If IsLanguageServiceRegistrationFile(partFileName) Then
                        Continue For
                    End If

                    ' Files generated by the VSIX v3 installer that don't need to be inserted.
                    If partFileName = "catalog.json" OrElse partFileName = "manifest.json" Then
                        Continue For
                    End If

                    If dependencies.ContainsKey(partFileName) Then
                        Continue For
                    End If

                    Dim relativeOutputFilePath = Path.Combine(GetExternalApiDirectory(), partFileName)

                    ' paths are relative to input directory:
                    If processedFiles.Add(relativeOutputFilePath) Then
                        ' In Razzle src\ArcProjects\debugger\ConcordSDK.targets references .vsdconfig files under LanguageServiceRegistration\ExpressionEvaluatorPackage
                        Dim target = If(Path.GetExtension(partFileName).Equals(".vsdconfig"), "LanguageServiceRegistration\ExpressionEvaluatorPackage", "")

                        Dim partPath = GetPotentiallyMappedPath(partFileName)

                        If Not File.Exists(Path.Combine(_binDirectory, partPath)) Then
                            Console.WriteLine($"File {partPath} does not exist at {_binDirectory}")
                            allGood = False
                        End If

                        filesToInsert.Add(New NugetFileInfo(partPath, target))
                        AddXmlDocumentationFile(filesToInsert, partPath)
                    End If
                Next
            End Using
        Next

        If Not allGood Then
            Throw New Exception("Error processing VSIX files")
        End If
    End Sub

    Private Function GetPartRelativePath(part As PackagePart) As String
        Dim name = part.Uri.OriginalString
        If name.Length > 0 AndAlso name(0) = "/"c Then
            name = name.Substring(1)
        End If

        Return name.Replace("/"c, "\"c)
    End Function

    ' XML doc file if exists:
    Private Sub AddXmlDocumentationFile(filesToInsert As List(Of NugetFileInfo), fileName As String)
        If IsExecutableCodeFileName(fileName) Then
            Dim xmlDocFile = Path.ChangeExtension(fileName, ".xml")
            If File.Exists(Path.Combine(_binDirectory, xmlDocFile)) Then
                ' paths are relative to input directory
                filesToInsert.Add(New NugetFileInfo(xmlDocFile))
            End If
        End If
    End Sub

    ''' <summary>
    ''' Takes a list of paths relative to <see cref="_outputDirectory"/> and generates a nuspec file that includes them.
    ''' </summary>
    Private Sub GenerateRoslynNuSpec(filesToInsert As List(Of NugetFileInfo))
        Const PackageName As String = "VS.ExternalAPIs.Roslyn"

        ' Do a quick sanity check for the files existing.  If they don't exist at this time then the tool output
        ' is going to be unusable
        Dim allGood = True
        For Each fileInfo In filesToInsert
            Dim filePath = Path.Combine(_binDirectory, fileInfo.Path)
            If Not File.Exists(filePath) Then
                allGood = False
                Console.WriteLine($"File {fileInfo.Path} does not exist at {_binDirectory}")
            End If
        Next

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
                          <%= filesToInsert.
                              OrderBy(Function(f) f.Path).
                              Distinct().
                              Select(Function(f) <file src=<%= f.Path %> target=<%= f.Target %> xmlns="http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"/>) %>
                      </files>
                  </package>

        xml.Save(GetAbsolutePathInOutputDirectory(PackageName & ".nuspec"), SaveOptions.OmitDuplicateNamespaces)
    End Sub


    Private Sub GenerateVSToolsRoslynCoreXTNuspec(filesToInsert As IEnumerable(Of String))
        Const PackageName As String = "VS.Tools.Roslyn"

        ' No duplicates are allowed
        filesToInsert.GroupBy(Function(x) x).All(Function(g) g.Count() = 1)

        Dim outputFolder = GetAbsolutePathInOutputDirectory(PackageName)

        Directory.CreateDirectory(outputFolder)

        ' Write an Init.cmd that sets DEVPATH to the toolset location. This overrides
        ' assembly loading during the VS build to always look in the Roslyn toolset
        ' first. This is necessary because there are various incompatible versions
        ' of Roslyn littered throughout the DEVPATH already and this one should always
        ' take precedence.
        Dim fileContents = "@echo off

set RoslynToolsRoot=%~dp0
set DEVPATH=%RoslynToolsRoot%;%DEVPATH%"

        File.WriteAllText(
            Path.Combine(outputFolder, "Init.cmd"),
            fileContents)

        ' Copy all dependent compiler files to the output directory
        ' It is most important to have isolated copies of the compiler
        ' exes (csc, vbc, vbcscompiler) since we are going to mark them
        ' 32-bit only to work around problems with the VS build.
        ' These binaries should never ship anywhere other than the VS toolset
        ' See https://github.com/dotnet/roslyn/issues/17864
        For Each fileName In filesToInsert
            Dim srcPath = Path.Combine(_binDirectory, GetMappedPath(fileName))
            Dim dstPath = Path.Combine(outputFolder, fileName)
            File.Copy(srcPath, dstPath)

            If Path.GetExtension(fileName) = ".exe" Then
                MarkFile32BitPref(dstPath)
            End If
        Next

        Dim xml = <?xml version="1.0" encoding="utf-8"?>
                  <package xmlns="http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd">
                      <metadata>
                          <id><%= PackageName %></id>
                          <summary>Roslyn compiler binaries used to build VS</summary>
                          <description>CoreXT package for Roslyn compiler toolset.</description>
                          <authors>Managed Language Compilers</authors>
                          <version>0.0</version>
                      </metadata>
                      <files>
                          <file src="Init.cmd"/>
                          <%= filesToInsert.
                              OrderBy(Function(f) f).
                              Select(Function(f) <file src=<%= f %> xmlns="http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"/>) %>
                      </files>
                  </package>

        xml.Save(Path.Combine(outputFolder, PackageName & ".nuspec"), SaveOptions.OmitDuplicateNamespaces)
    End Sub

    Private Sub MarkFile32BitPref(filePath As String)
        Const OffsetFromStartOfCorHeaderToFlags = 4 + ' byte count 
                                                  2 + ' Major version
                                                  2 + ' Minor version
                                                  8   ' Metadata directory

        Using stream As FileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read)
            Using reader As PEReader = New PEReader(stream)
                Dim newFlags As Int32 = reader.PEHeaders.CorHeader.Flags Or
                                        CorFlags.Prefers32Bit Or
                                        CorFlags.Requires32Bit ' CLR requires both req and pref flags to be set

                Using writer = New BinaryWriter(stream)
                    Dim mdReader = reader.GetMetadataReader()
                    stream.Position = reader.PEHeaders.CorHeaderStartOffset + OffsetFromStartOfCorHeaderToFlags

                    writer.Write(newFlags)
                    writer.Flush()
                End Using
            End Using
        End Using
    End Sub

    Private Function IsLanguageServiceRegistrationFile(fileName As String) As Boolean
        Select Case Path.GetExtension(fileName)
            Case ".vsixmanifest", ".pkgdef", ".png", ".ico"
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
        Dim absolutePath = Path.Combine(_outputDirectory, relativePath)

        ' Ensure that the parent directories are all created
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath))

        Return absolutePath
    End Function
End Class

Imports System.IO
Imports System.Threading
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Reflection.PortableExecutable
Imports System.Reflection.Metadata

Public Class BuildDevDivInsertionFiles
    Private Const DevDivInsertionFilesDirName = "DevDivInsertionFiles"
    Private Const DevDivPackagesDirName = "DevDivPackages"

    Private ReadOnly _binDirectory As String
    Private ReadOnly _outputDirectory As String
    Private ReadOnly _outputPackageDirectory As String
    Private ReadOnly _nugetPackageRoot As String

    Private Sub New(args As String())
        _binDirectory = Path.GetFullPath(args(0))
        _nugetPackageRoot = Path.GetFullPath(args(1))
        _outputDirectory = Path.Combine(_binDirectory, DevDivInsertionFilesDirName)
        _outputPackageDirectory = Path.Combine(_binDirectory, DevDivPackagesDirName)
    End Sub

    Public Shared Function Main(args As String()) As Integer
        If args.Length < 2 Then
            Console.WriteLine("Expected arguments: <bin dir> <nuget root dir>")
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
        CopyDependencies(dependencies)
    End Sub

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
    End Class

    Private Function BuildDependencyMap(inputDirectory As String) As Dictionary(Of String, DependencyInfo)
        Dim result = New Dictionary(Of String, DependencyInfo)
        Dim objDir = Path.Combine(Path.GetDirectoryName(_binDirectory.TrimEnd(Path.DirectorySeparatorChar)), "Obj")
        Dim files = New List(Of String)
        files.Add(Path.Combine(objDir, "Roslyn.Compilers.Extension\project.assets.json"))
        files.Add(Path.Combine(objDir, "Roslyn.VisualStudio.Setup.Dependencies\project.assets.json"))

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

                If packageObj.Property("type").Value.Value(Of String) = "project" Then
                    Continue For
                End If

                Dim contracts = DirectCast(packageObj.Property("compile")?.Value, JObject)
                Dim runtime = DirectCast(packageObj.Property("runtime")?.Value, JObject)
                Dim native = DirectCast(packageObj.Property("native")?.Value, JObject)
                Dim frameworkAssemblies = packageObj.Property("frameworkAssemblies")?.Value

                Dim implementations = If(runtime, native)
                If implementations Is Nothing Then
                    Continue For
                End If

                ' No need to insert Visual Studio packages back into the repository itself
                If packageName.StartsWith("Microsoft.VisualStudio.") OrElse
                   packageName = "EnvDTE" OrElse
                   packageName = "stdole" OrElse
                   packageName.StartsWith("Microsoft.Build") OrElse
                   packageName = "Microsoft.Composition" OrElse
                   packageName = "System.Net.Http" OrElse
                   packageName = "System.Diagnostics.DiagnosticSource" OrElse
                   packageName = "Newtonsoft.Json" Then
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

                        Dim compileDll = contracts?.Properties().Select(Function(p) p.Name).Where(Function(n) Path.GetFileName(n) = fileName).SingleOrDefault()
                        Dim compileTarget = If(compileDll IsNot Nothing, Path.GetDirectoryName(compileDll), Nothing)

                        result.Add(fileName, New DependencyInfo(compileTarget,
                                                                runtimeTarget,
                                                                packageName,
                                                                packageVersion,
                                                                isNative:=native IsNot Nothing,
                                                                isFacade:=frameworkAssemblies IsNot Nothing AndAlso packageName <> "Microsoft.Build" OrElse packageName = "System.IO.Pipes.AccessControl"))
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
            Dim nupkg = $"{dependency.PackageName}.{dependency.PackageVersion}.nupkg"
            Dim srcPath = Path.Combine(_nugetPackageRoot, dependency.PackageName, dependency.PackageVersion, nupkg)
            Dim dstDir = Path.Combine(_outputPackageDirectory, If(dependency.IsNative, "NativeDependencies", "ManagedDependencies"))
            Dim dstPath = Path.Combine(dstDir, nupkg)

            Directory.CreateDirectory(dstDir)
            File.Copy(srcPath, dstPath, overwrite:=True)
        Next
    End Sub

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

    Private Function GetAbsolutePathInOutputDirectory(relativePath As String) As String
        Dim absolutePath = Path.Combine(_outputDirectory, relativePath)

        ' Ensure that the parent directories are all created
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath))

        Return absolutePath
    End Function
End Class

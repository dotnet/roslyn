' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.MSBuild
Imports Roslyn.Utilities
Imports MSB = Microsoft.Build

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Class VisualBasicProjectFileLoader
        Inherits ProjectFileLoader

        Private _workspaceServices As HostWorkspaceServices

        Friend Sub New(workspaceServices As HostWorkspaceServices)
            Me._workspaceServices = workspaceServices
        End Sub

        Public Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Protected Overrides Function CreateProjectFile(loadedProject As MSB.Evaluation.Project) As ProjectFile
            Return New VisualBasicProjectFile(Me, loadedProject, Me._workspaceServices.GetService(Of IMetadataService), Me._workspaceServices.GetService(Of IAnalyzerService))
        End Function

        Friend Class VisualBasicProjectFile
            Inherits ProjectFile

            Private ReadOnly _metadataService As IMetadataService
            Private ReadOnly _analyzerService As IAnalyzerService

            Public Sub New(loader As VisualBasicProjectFileLoader, loadedProject As MSB.Evaluation.Project, metadataService As IMetadataService, analyzerService As IAnalyzerService)
                MyBase.New(loader, loadedProject)
                Me._metadataService = metadataService
                Me._analyzerService = analyzerService
            End Sub

            Public Overrides Function GetSourceCodeKind(documentFileName As String) As SourceCodeKind
                If documentFileName.EndsWith(".vbx", StringComparison.OrdinalIgnoreCase) Then
                    Return SourceCodeKind.Script
                End If
                Return SourceCodeKind.Regular
            End Function

            Public Overrides Function GetDocumentExtension(sourceCodeKind As SourceCodeKind) As String
                Select Case sourceCodeKind
                    Case SourceCodeKind.Script
                        Return ".vbx"
                    Case Else
                        Return ".vb"
                End Select
            End Function

            Public Overrides Async Function GetProjectFileInfoAsync(cancellationToken As CancellationToken) As Tasks.Task(Of ProjectFileInfo)
                Dim compilerInputs As New VisualBasicCompilerInputs(Me)

                Dim executedProject = Await Me.BuildAsync("Vbc", compilerInputs, cancellationToken).ConfigureAwait(False)

                If Not compilerInputs.Initialized Then
                    Me.InitializeFromModel(compilerInputs, executedProject)
                End If

                Return CreateProjectFileInfo(compilerInputs, executedProject)
            End Function

            Private Shadows Function CreateProjectFileInfo(compilerInputs As VisualBasicCompilerInputs, executedProject As MSB.Execution.ProjectInstance) As ProjectFileInfo

                Dim metadataReferences As IEnumerable(Of MetadataReference) = Nothing
                Dim analyzerReferences As IEnumerable(Of AnalyzerReference) = Nothing
                GetReferences(compilerInputs, executedProject, metadataReferences, analyzerReferences)

                Dim outputPath = Path.Combine(Me.GetOutputDirectory(), compilerInputs.OutputFileName)
                Dim assemblyName = Me.GetAssemblyName()

                Return New ProjectFileInfo(
                    outputPath,
                    assemblyName,
                    compilerInputs.CompilationOptions,
                    compilerInputs.ParseOptions.WithPreprocessorSymbols(AddPredefinedPreprocessorSymbols(
                        compilerInputs.CompilationOptions.OutputKind, compilerInputs.ParseOptions.PreprocessorSymbols)),
                    compilerInputs.CodePage,
                    Me.GetDocuments(compilerInputs.Sources, executedProject),
                    Me.GetDocuments(compilerInputs.AdditionalFiles, executedProject),
                    Me.GetProjectReferences(executedProject),
                    metadataReferences,
                    analyzerReferences)
            End Function

            Private Sub GetReferences(
                compilerInputs As VisualBasicCompilerInputs,
                executedProject As MSB.Execution.ProjectInstance,
                ByRef metadataReferences As IEnumerable(Of MetadataReference),
                ByRef analyzerReferences As IEnumerable(Of AnalyzerReference))

                ' use command line parser to compute references using common logic

                Dim args = New List(Of String)()

                If compilerInputs.LibPaths IsNot Nothing AndAlso compilerInputs.LibPaths.Count > 0 Then
                    args.Add("/libpath:""" + String.Join(";", compilerInputs.LibPaths) + """")
                End If

                ' metadata references
                For Each mr In compilerInputs.References
                    Dim filePath = GetDocumentFilePath(mr)
                    args.Add("/r:""" + filePath + """")
                Next

                ' analyzer references
                For Each ar In compilerInputs.AnalyzerReferences
                    Dim filePath = GetDocumentFilePath(ar)
                    args.Add("/a:""" + filePath + """")
                Next

                If compilerInputs.NoStandardLib Then
                    args.Add("/nostdlib")
                End If

                If Not String.IsNullOrEmpty(compilerInputs.VbRuntime) Then
                    If compilerInputs.VbRuntime = "Default" Then
                        args.Add("/vbruntime+")
                    ElseIf compilerInputs.VbRuntime = "Embed" Then
                        args.Add("/vbruntime*")
                    ElseIf compilerInputs.VbRuntime = "None" Then 'TODO: check on this
                        args.Add("/vbruntime-")
                    Else
                        args.Add("/vbruntime: " + compilerInputs.VbRuntime)
                    End If
                End If

                If Not String.IsNullOrEmpty(compilerInputs.SdkPath) Then
                    args.Add("/sdkpath:" + compilerInputs.SdkPath)
                End If

                Dim commandLineParser = VisualBasicCommandLineParser.Default
                Dim commandLineArgs = commandLineParser.Parse(args, executedProject.Directory, RuntimeEnvironment.GetRuntimeDirectory())
                Dim resolver = New MetadataFileReferenceResolver(commandLineArgs.ReferencePaths, commandLineArgs.BaseDirectory)
                metadataReferences = commandLineArgs.ResolveMetadataReferences(New AssemblyReferenceResolver(resolver, Me._metadataService.GetProvider()))

                Dim loader = _analyzerService.GetLoader()
                For Each path In commandLineArgs.AnalyzerReferences.Select(Function(r) r.FilePath)
                    loader.AddDependencyLocation(path)
                Next
                analyzerReferences = commandLineArgs.ResolveAnalyzerReferences(loader)

            End Sub

            Private Function GetDocuments(sources As IEnumerable(Of Build.Framework.ITaskItem), executedProject As MSB.Execution.ProjectInstance) As IEnumerable(Of DocumentFileInfo)
                If sources Is Nothing Then
                    Return ImmutableArray(Of DocumentFileInfo).Empty
                End If

                Dim projectDirectory = executedProject.Directory
                If Not projectDirectory.EndsWith(Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) Then
                    projectDirectory += Path.DirectorySeparatorChar
                End If

                Return sources.Where(Function(s) Not System.IO.Path.GetFileName(s.ItemSpec).StartsWith("TemporaryGeneratedFile_", StringComparison.Ordinal)) _
                         .Select(Function(s) New DocumentFileInfo(GetDocumentFilePath(s), GetDocumentLogicalPath(s, projectDirectory), IsDocumentLinked(s), IsDocumentGenerated(s))).ToImmutableArray()
            End Function

            Private Sub InitializeFromModel(compilerInputs As VisualBasicCompilerInputs, executedProject As MSB.Execution.ProjectInstance)
                compilerInputs.BeginInitialization()

                compilerInputs.SetBaseAddress(Me.ReadPropertyString(executedProject, "OutputType"), Me.ReadPropertyString(executedProject, "BaseAddress"))
                compilerInputs.SetCodePage(Me.ReadPropertyInt(executedProject, "CodePage"))
                compilerInputs.SetDebugType(Me.ReadPropertyBool(executedProject, "DebugSymbols"), Me.ReadPropertyString(executedProject, "DebugType"))
                compilerInputs.SetDefineConstants(Me.ReadPropertyString(executedProject, "FinalDefineConstants", "DefineConstants"))
                compilerInputs.SetDelaySign(Me.ReadPropertyBool(executedProject, "DelaySign"))
                compilerInputs.SetDisabledWarnings(Me.ReadPropertyString(executedProject, "NoWarn"))
                compilerInputs.SetDocumentationFile(Me.GetItemString(executedProject, "DocFileItem"))
                compilerInputs.SetErrorReport(Me.ReadPropertyString(executedProject, "ErrorReport"))
                compilerInputs.SetFileAlignment(Me.ReadPropertyInt(executedProject, "FileAlignment"))
                compilerInputs.SetGenerateDocumentation(Me.ReadPropertyBool(executedProject, "GenerateDocumentation"))
                compilerInputs.SetHighEntropyVA(Me.ReadPropertyBool(executedProject, "HighEntropyVA"))

                Dim _imports = Me.GetTaskItems(executedProject, "Import")
                If _imports IsNot Nothing Then
                    compilerInputs.SetImports(_imports.ToArray())
                End If

                Dim signAssembly = ReadPropertyBool(executedProject, "SignAssembly")
                If signAssembly Then
                    Dim keyFile = ReadPropertyString(executedProject, "KeyOriginatorFile", "AssemblyOriginatorKeyFile")
                    If Not String.IsNullOrEmpty(keyFile) Then
                        compilerInputs.SetKeyFile(keyFile)
                    End If

                    Dim keyContainer = ReadPropertyString(executedProject, "KeyContainerName")
                    If Not String.IsNullOrEmpty(keyContainer) Then
                        compilerInputs.SetKeyContainer(keyContainer)
                    End If
                End If

                compilerInputs.SetLanguageVersion(Me.ReadPropertyString(executedProject, "LangVersion"))
                compilerInputs.SetMainEntryPoint(Me.ReadPropertyString(executedProject, "StartupObject"))
                compilerInputs.SetModuleAssemblyName(Me.ReadPropertyString(executedProject, "ModuleEntryPoint"))
                compilerInputs.SetNoStandardLib(Me.ReadPropertyBool(executedProject, "NoCompilerStandardLib", "NoStdLib"))
                compilerInputs.SetNoWarnings(Me.ReadPropertyBool(executedProject, "_NoWarnings"))

                compilerInputs.SetOptimize(Me.ReadPropertyBool(executedProject, "Optimize"))
                compilerInputs.SetOptionCompare(Me.ReadPropertyString(executedProject, "OptionCompare"))
                compilerInputs.SetOptionExplicit(Me.ReadPropertyBool(executedProject, "OptionExplicit"))
                compilerInputs.SetOptionInfer(Me.ReadPropertyBool(executedProject, "OptionInfer"))

                Dim optionStrictText = Me.ReadPropertyString(executedProject, "OptionStrict")
                Dim optionStrictValue As OptionStrict
                If TryGetOptionStrict(optionStrictText, optionStrictValue) Then
                    compilerInputs.SetOptionStrict(optionStrictValue = OptionStrict.On)
                End If

                compilerInputs.SetOptionStrictType(Me.ReadPropertyString(executedProject, "OptionStrictType"))

                compilerInputs.SetOutputAssembly(Me.GetItemString(executedProject, "IntermediateAssembly"))

                If Me.ReadPropertyBool(executedProject, "Prefer32Bit") Then
                    compilerInputs.SetPlatformWith32BitPreference(Me.ReadPropertyString(executedProject, "PlatformTarget"))
                Else
                    compilerInputs.SetPlatform(Me.ReadPropertyString(executedProject, "PlatformTarget"))
                End If

                compilerInputs.SetRemoveIntegerChecks(Me.ReadPropertyBool(executedProject, "RemoveIntegerChecks"))

                compilerInputs.SetRootNamespace(Me.ReadPropertyString(executedProject, "RootNamespace"))
                compilerInputs.SetSdkPath(ReadPropertyString(executedProject, "FrameworkPathOverride"))
                compilerInputs.SetSubsystemVersion(Me.ReadPropertyString(executedProject, "SubsystemVersion"))
                compilerInputs.SetTargetCompactFramework(Me.ReadPropertyBool(executedProject, "TargetCompactFramework"))
                compilerInputs.SetTargetType(Me.ReadPropertyString(executedProject, "OutputType"))

                ' Decode the warning options from RuleSet file prior to reading explicit settings in the project file, so that project file settings prevail for duplicates.
                compilerInputs.SetRuleSet(Me.ReadPropertyString(executedProject, "RuleSet"))
                compilerInputs.SetTreatWarningsAsErrors(Me.ReadPropertyBool(executedProject, "SetTreatWarningsAsErrors"))
                compilerInputs.SetVBRuntime(ReadPropertyString(executedProject, "VbRuntime"))
                compilerInputs.SetWarningsAsErrors(Me.ReadPropertyString(executedProject, "WarningsAsErrors"))
                compilerInputs.SetWarningsNotAsErrors(Me.ReadPropertyString(executedProject, "WarningsNotAsErrors"))

                compilerInputs.SetReferences(Me.GetMetadataReferencesFromModel(executedProject).ToArray())
                compilerInputs.SetAnalyzers(Me.GetAnalyzerReferencesFromModel(executedProject).ToArray())
                compilerInputs.SetAdditionalFiles(Me.GetAdditionalFilesFromModel(executedProject).ToArray())
                compilerInputs.SetSources(Me.GetDocumentsFromModel(executedProject).ToArray())

                compilerInputs.EndInitialization()
            End Sub

            Private Shared Function TryGetOptionStrict(text As String, ByRef optionStrictType As OptionStrict) As Boolean
                If text = "On" Then
                    optionStrictType = OptionStrict.On
                    Return True
                ElseIf text = "Off" Then
                    optionStrictType = OptionStrict.Custom
                    Return True
                Else
                    Return False
                End If
            End Function

            Private Class VisualBasicCompilerInputs
                Implements MSB.Tasks.Hosting.IVbcHostObject5, MSB.Tasks.Hosting.IVbcHostObjectFreeThreaded
#If Not MSBUILD12 Then
                Implements MSB.Tasks.Hosting.IAnalyzerHostObject
#End If
                Private _projectFile As VisualBasicProjectFile
                Private _initialized As Boolean
                Private _parseOptions As VisualBasicParseOptions
                Private _compilationOptions As VisualBasicCompilationOptions
                Private _codePage As Integer
                Private _sources As IEnumerable(Of MSB.Framework.ITaskItem)
                Private _additionalFiles As IEnumerable(Of MSB.Framework.ITaskItem)
                Private _references As IEnumerable(Of MSB.Framework.ITaskItem)
                Private _analyzerReferences As IEnumerable(Of MSB.Framework.ITaskItem)
                Private _noStandardLib As Boolean
                Private _warnings As Dictionary(Of String, ReportDiagnostic)
                Private _sdkPath As String
                Private _targetCompactFramework As Boolean
                Private _vbRuntime As String
                Private _libPaths As IEnumerable(Of String)
                Private _outputFileName As String

                Public Sub New(projectFile As VisualBasicProjectFile)
                    Me._projectFile = projectFile
                    Me._parseOptions = VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Parse)
                    Dim projectDirectory = Path.GetDirectoryName(projectFile.FilePath)
                    Dim outputDirectory = projectFile.GetOutputDirectory()
                    Me._compilationOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication,
                        xmlReferenceResolver:=New XmlFileResolver(projectDirectory),
                        sourceReferenceResolver:=New SourceFileResolver(ImmutableArray(Of String).Empty, projectDirectory),
                        metadataReferenceResolver:=New AssemblyReferenceResolver(
                            New MetadataFileReferenceResolver(ImmutableArray(Of String).Empty, projectDirectory),
                            MetadataFileReferenceProvider.Default),
                        strongNameProvider:=New DesktopStrongNameProvider(ImmutableArray.Create(Of String)(projectDirectory, outputDirectory)),
                        assemblyIdentityComparer:=DesktopAssemblyIdentityComparer.Default)
                    Me._sources = SpecializedCollections.EmptyEnumerable(Of MSB.Framework.ITaskItem)()
                    Me._references = SpecializedCollections.EmptyEnumerable(Of MSB.Framework.ITaskItem)()
                    Me._analyzerReferences = SpecializedCollections.EmptyEnumerable(Of MSB.Framework.ITaskItem)()
                    Me._warnings = New Dictionary(Of String, ReportDiagnostic)()
                End Sub

                Public ReadOnly Property Initialized As Boolean
                    Get
                        Return Me._initialized
                    End Get
                End Property

                Public ReadOnly Property CompilationOptions As VisualBasicCompilationOptions
                    Get
                        Return Me._compilationOptions
                    End Get
                End Property

                Public ReadOnly Property ParseOptions As VisualBasicParseOptions
                    Get
                        Return Me._parseOptions
                    End Get
                End Property

                Public ReadOnly Property CodePage As Integer
                    Get
                        Return Me._codePage
                    End Get
                End Property

                Public ReadOnly Property References As IEnumerable(Of MSB.Framework.ITaskItem)
                    Get
                        Return Me._references
                    End Get
                End Property

                Public ReadOnly Property AnalyzerReferences As IEnumerable(Of MSB.Framework.ITaskItem)
                    Get
                        Return Me._analyzerReferences
                    End Get
                End Property

                Public ReadOnly Property Sources As IEnumerable(Of MSB.Framework.ITaskItem)
                    Get
                        Return Me._sources
                    End Get
                End Property

                Public ReadOnly Property AdditionalFiles As IEnumerable(Of MSB.Framework.ITaskItem)
                    Get
                        Return Me._additionalFiles
                    End Get
                End Property

                Public ReadOnly Property NoStandardLib As Boolean
                    Get
                        Return Me._noStandardLib
                    End Get
                End Property

                Public ReadOnly Property VbRuntime As String
                    Get
                        Return Me._vbRuntime
                    End Get
                End Property

                Public ReadOnly Property SdkPath As String
                    Get
                        Return Me._sdkPath
                    End Get
                End Property

                Public ReadOnly Property TargetCompactFramework As Boolean
                    Get
                        Return Me._targetCompactFramework
                    End Get
                End Property

                Public ReadOnly Property LibPaths As IEnumerable(Of String)
                    Get
                        Return Me._libPaths
                    End Get
                End Property

                Public ReadOnly Property OutputFileName As String
                    Get
                        Return Me._outputFileName
                    End Get
                End Property

                Public Sub BeginInitialization() Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.BeginInitialization
                End Sub

                Public Function Compile() As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.Compile
                    Return False
                End Function

                Public Sub EndInitialization() Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.EndInitialization
                    If Me._warnings.Count > 0 Then
                        Me._compilationOptions = Me._compilationOptions.WithSpecificDiagnosticOptions(Me._warnings)
                    End If

                    Me._initialized = True
                End Sub

                Public Function IsDesignTime() As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.IsDesignTime
                    Return True
                End Function

                Public Function IsUpToDate() As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.IsUpToDate
                    Return True
                End Function

                Public Function SetAdditionalLibPaths(additionalLibPaths() As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetAdditionalLibPaths
                    Me._libPaths = additionalLibPaths
                    Return True
                End Function

                Public Function SetAddModules(addModules() As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetAddModules
                    Return True
                End Function

                Public Function SetBaseAddress(targetType As String, baseAddress As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetBaseAddress
                    ' we don't capture emit options
                    Return True
                End Function

                Public Function SetCodePage(codePage As Integer) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetCodePage
                    Me._codePage = codePage
                    Return True
                End Function

                Public Function SetDebugType(emitDebugInformation As Boolean, debugType As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetDebugType
                    ' ignore, just check for expected values for backwards compat
                    Return String.Equals(debugType, "none", StringComparison.OrdinalIgnoreCase) OrElse
                           String.Equals(debugType, "pdbonly", StringComparison.OrdinalIgnoreCase) OrElse
                           String.Equals(debugType, "full", StringComparison.OrdinalIgnoreCase)
                End Function

                Public Function SetDefineConstants(defineConstants As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetDefineConstants
                    If Not String.IsNullOrEmpty(defineConstants) Then
                        Dim errors As IEnumerable(Of Diagnostic) = Nothing
                        Me._parseOptions = Me._parseOptions.WithPreprocessorSymbols(VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(defineConstants, errors))
                        Return True
                    End If
                    Return False
                End Function

                Public Function SetDelaySign(delaySign As Boolean) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetDelaySign
                    Me._compilationOptions = Me._compilationOptions.WithDelaySign(delaySign)
                    Return True
                End Function

                Public Function SetDisabledWarnings(disabledWarnings As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetDisabledWarnings
                    SetWarnings(disabledWarnings, ReportDiagnostic.Suppress)
                    Return True
                End Function

                Public Function SetDocumentationFile(documentationFile As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetDocumentationFile
                    If Not String.IsNullOrEmpty(documentationFile) Then
                        _parseOptions = _parseOptions.WithDocumentationMode(DocumentationMode.Diagnose)
                    Else
                        _parseOptions = _parseOptions.WithDocumentationMode(DocumentationMode.Parse)
                    End If

                    Return True
                End Function

                Public Function SetErrorReport(errorReport As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetErrorReport
                    ' ??
                    Return True
                End Function

                Public Function SetFileAlignment(fileAlignment As Integer) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetFileAlignment
                    ' we don't capture emit options
                    Return True
                End Function

                Public Function SetGenerateDocumentation(generateDocumentation As Boolean) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetGenerateDocumentation
                    Return True
                End Function

                Public Function SetImports(importsList() As Microsoft.Build.Framework.ITaskItem) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetImports
                    If importsList IsNot Nothing Then
                        Dim array = importsList.Select(Function(item) GlobalImport.Parse(item.ItemSpec.Trim()))
                        Me._compilationOptions = Me._compilationOptions.WithGlobalImports(array)
                    End If
                    Return True
                End Function

                Public Function SetKeyContainer(keyContainer As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetKeyContainer
                    If Not String.IsNullOrEmpty(keyContainer) Then
                        Me._compilationOptions = Me._compilationOptions.WithCryptoKeyContainer(keyContainer)
                        Return True
                    End If
                    Return False
                End Function

                Public Function SetKeyFile(keyFile As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetKeyFile
                    If Not String.IsNullOrEmpty(keyFile) Then
                        Me._compilationOptions = Me._compilationOptions.WithCryptoKeyFile(keyFile)
                        Return True
                    End If
                    Return False
                End Function

                Public Function SetLinkResources(linkResources() As Microsoft.Build.Framework.ITaskItem) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetLinkResources
                    ' ??
                    Return True
                End Function

                Public Function SetMainEntryPoint(mainEntryPoint As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetMainEntryPoint
                    If Not String.IsNullOrEmpty(mainEntryPoint) AndAlso mainEntryPoint <> "Sub Main" Then
                        Me._compilationOptions = Me._compilationOptions.WithMainTypeName(mainEntryPoint)
                    End If
                    Return True
                End Function

                Public Function SetNoConfig(noConfig As Boolean) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetNoConfig
                    Return True
                End Function

                Public Function SetNoStandardLib(noStandardLib As Boolean) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetNoStandardLib
                    Me._noStandardLib = noStandardLib
                    Return True
                End Function

                Public Function SetNoWarnings(noWarnings As Boolean) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetNoWarnings
                    Me._compilationOptions = Me._compilationOptions.WithGeneralDiagnosticOption(If(noWarnings, ReportDiagnostic.Suppress, ReportDiagnostic.Warn))
                    Return True
                End Function

                Public Function SetOptimize(optimize As Boolean) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetOptimize
                    Me._compilationOptions = Me._compilationOptions.WithOptimizationLevel(If(optimize, OptimizationLevel.Release, OptimizationLevel.Debug))
                    Return True
                End Function

                Public Function SetOptionCompare(optionCompare As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetOptionCompare
                    If Not String.IsNullOrEmpty(optionCompare) Then
                        Me._compilationOptions = Me._compilationOptions.WithOptionCompareText(optionCompare = "Text")
                        Return True
                    End If
                    Return False
                End Function

                Public Function SetOptionExplicit(optionExplicit As Boolean) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetOptionExplicit
                    Me._compilationOptions = Me._compilationOptions.WithOptionExplicit(optionExplicit)
                    Return True
                End Function

                Public Function SetOptionStrict(_optionStrict As Boolean) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetOptionStrict
                    Me._compilationOptions = Me._compilationOptions.WithOptionStrict(If(_optionStrict, OptionStrict.On, OptionStrict.Custom))
                    Return True
                End Function

                Public Function SetOptionStrictType(optionStrictType As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetOptionStrictType
                    If Not String.IsNullOrEmpty(optionStrictType) Then
                        Dim _optionStrict As OptionStrict = OptionStrict.Custom
                        If VisualBasicProjectFile.TryGetOptionStrict(optionStrictType, _optionStrict) Then
                            Me._compilationOptions = Me._compilationOptions.WithOptionStrict(_optionStrict)
                            Return True
                        End If
                    End If
                    Return False
                End Function

                Public Function SetOutputAssembly(outputAssembly As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetOutputAssembly
                    Me._outputFileName = Path.GetFileName(outputAssembly)
                    Return True
                End Function

                Public Function SetPlatform(_platform As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetPlatform
                    If Not String.IsNullOrEmpty(_platform) Then
                        Dim plat As Platform
                        If [Enum].TryParse(_platform, plat) Then
                            Me._compilationOptions = Me._compilationOptions.WithPlatform(plat)
                            Return True
                        End If
                    End If
                    Return False
                End Function

                Public Function SetPlatformWith32BitPreference(_platform As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject5.SetPlatformWith32BitPreference
                    If Not String.IsNullOrEmpty(_platform) Then
                        Dim plat As Platform
                        If [Enum].TryParse(_platform, True, plat) Then
                            Dim outputKind = Me._compilationOptions.OutputKind
                            If plat = Platform.AnyCpu AndAlso outputKind <> OutputKind.DynamicallyLinkedLibrary AndAlso outputKind <> OutputKind.NetModule AndAlso outputKind <> OutputKind.WindowsRuntimeMetadata Then
                                plat = Platform.AnyCpu32BitPreferred
                            End If
                            Me._compilationOptions = Me._compilationOptions.WithPlatform(plat)
                            Return True
                        End If
                    End If
                    Return False
                End Function

                Public Function SetReferences(references() As Microsoft.Build.Framework.ITaskItem) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetReferences
                    Me._references = If(references, SpecializedCollections.EmptyEnumerable(Of MSB.Framework.ITaskItem)())
                    Return True
                End Function

#If Not MSBUILD12 Then
                Public Function SetAnalyzers(analyzerReferences() As MSB.Framework.ITaskItem) As Boolean Implements MSB.Tasks.Hosting.IAnalyzerHostObject.SetAnalyzers
#Else
                Public Function SetAnalyzers(analyzerReferences() As MSB.Framework.ITaskItem) As Boolean
#End If
                    Me._analyzerReferences = If(analyzerReferences, SpecializedCollections.EmptyEnumerable(Of MSB.Framework.ITaskItem)())
                    Return True
                End Function

#If Not MSBUILD12 Then
                Public Function SetAdditionalFiles(additionalFiles() As MSB.Framework.ITaskItem) As Boolean Implements MSB.Tasks.Hosting.IAnalyzerHostObject.SetAdditionalFiles
#Else
                Public Function SetAdditionalFiles(additionalFiles() As MSB.Framework.ITaskItem) As Boolean
#End If
                    Me._additionalFiles = If(additionalFiles, SpecializedCollections.EmptyEnumerable(Of MSB.Framework.ITaskItem)())
                    Return True
                End Function

                Public Function SetRemoveIntegerChecks(removeIntegerChecks As Boolean) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetRemoveIntegerChecks
                    Me._compilationOptions = Me._compilationOptions.WithOverflowChecks(Not removeIntegerChecks)
                    Return True
                End Function

                Public Function SetResources(resources() As Microsoft.Build.Framework.ITaskItem) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetResources
                    Return True
                End Function

                Public Function SetResponseFiles(responseFiles() As Microsoft.Build.Framework.ITaskItem) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetResponseFiles
                    Return True
                End Function

                Public Function SetRootNamespace(rootNamespace As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetRootNamespace
                    If Not String.IsNullOrEmpty(rootNamespace) Then
                        Me._compilationOptions = Me._compilationOptions.WithRootNamespace(rootNamespace)
                        Return True
                    End If
                    Return False
                End Function

                Public Function SetSdkPath(sdkPath As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetSdkPath
                    Me._sdkPath = sdkPath
                    Return True
                End Function

                Public Function SetSources(sources() As Microsoft.Build.Framework.ITaskItem) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetSources
                    Me._sources = If(sources, SpecializedCollections.EmptyEnumerable(Of MSB.Framework.ITaskItem)())
                    Return True
                End Function

                Public Function SetTargetCompactFramework(targetCompactFramework As Boolean) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetTargetCompactFramework
                    Me._targetCompactFramework = targetCompactFramework
                    Return True
                End Function

                Public Function SetTargetType(targetType As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetTargetType
                    If Not String.IsNullOrEmpty(targetType) Then
                        Dim _outputKind As OutputKind
                        If VisualBasicProjectFile.TryGetOutputKind(targetType, _outputKind) Then
                            Me._compilationOptions = Me._compilationOptions.WithOutputKind(_outputKind)
                            If Me._compilationOptions.Platform = Platform.AnyCpu32BitPreferred AndAlso
                                (_outputKind = OutputKind.DynamicallyLinkedLibrary Or _outputKind = OutputKind.NetModule Or _outputKind = OutputKind.WindowsRuntimeMetadata) Then
                                Me._compilationOptions = Me._compilationOptions.WithPlatform(Platform.AnyCpu)
                            End If
                            Return True
                        End If
                    End If
                    Return False
                End Function

#If Not MSBUILD12 Then
                Public Function SetRuleSet(ruleSetFile As String) As Boolean Implements MSB.Tasks.Hosting.IAnalyzerHostObject.SetRuleSet
#Else
                Public Function SetRuleSet(ruleSetFile As String) As Boolean
#End If
                    If Not String.IsNullOrEmpty(ruleSetFile) Then
                        Dim fullPath = FileUtilities.ResolveRelativePath(ruleSetFile, Path.GetDirectoryName(Me._projectFile.FilePath))

                        Dim specificDiagnosticOptions As Dictionary(Of String, ReportDiagnostic) = Nothing
                        Dim generalDiagnosticOption = RuleSet.GetDiagnosticOptionsFromRulesetFile(fullPath, specificDiagnosticOptions)
                        Me._compilationOptions = Me._compilationOptions.WithGeneralDiagnosticOption(generalDiagnosticOption)
                        Me._warnings.AddRange(specificDiagnosticOptions)
                    End If

                    Return True
                End Function

                Public Function SetTreatWarningsAsErrors(treatWarningsAsErrors As Boolean) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetTreatWarningsAsErrors
                    Me._compilationOptions = Me._compilationOptions.WithGeneralDiagnosticOption(If(treatWarningsAsErrors, ReportDiagnostic.Error, ReportDiagnostic.Warn))
                    Return True
                End Function

                Public Function SetWarningsAsErrors(warningsAsErrors As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetWarningsAsErrors
                    SetWarnings(warningsAsErrors, ReportDiagnostic.Error)
                    Return True
                End Function

                Private Shared ReadOnly s_warningSeparators As Char() = {";"c, ","c}

                Private Sub SetWarnings(warnings As String, reportStyle As ReportDiagnostic)
                    If Not String.IsNullOrEmpty(warnings) Then
                        For Each warning In warnings.Split(s_warningSeparators, StringSplitOptions.None)
                            Dim warningId As Integer
                            If Int32.TryParse(warning, warningId) Then
                                Me._warnings("BC" + warningId.ToString("0000")) = reportStyle
                            Else
                                Me._warnings(warning) = reportStyle
                            End If
                        Next
                    End If
                End Sub

                Public Function SetWarningsNotAsErrors(warningsNotAsErrors As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetWarningsNotAsErrors
                    SetWarnings(warningsNotAsErrors, ReportDiagnostic.Warn)
                    Return True
                End Function

                Public Function SetWin32Icon(win32Icon As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetWin32Icon
                    Return True
                End Function

                Public Function SetWin32Resource(win32Resource As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject.SetWin32Resource
                    Return True
                End Function

                Public Function SetModuleAssemblyName(moduleAssemblyName As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject2.SetModuleAssemblyName
                    Return True
                End Function

                Public Function SetOptionInfer(optionInfer As Boolean) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject2.SetOptionInfer
                    Me._compilationOptions = Me._compilationOptions.WithOptionInfer(optionInfer)
                    Return True
                End Function

                Public Function SetWin32Manifest(win32Manifest As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject2.SetWin32Manifest
                    Return True
                End Function

                Public Function SetLanguageVersion(_languageVersion As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject3.SetLanguageVersion
                    If Not String.IsNullOrEmpty(_languageVersion) Then
                        Dim version As Integer
                        If Int32.TryParse(_languageVersion, version) Then
                            Dim lv As LanguageVersion = CType(version, LanguageVersion)
                            If [Enum].IsDefined(GetType(LanguageVersion), lv) Then
                                Me._parseOptions = Me._parseOptions.WithLanguageVersion(lv)
                                Return True
                            End If
                        End If
                    End If
                    Return False
                End Function

                Public Function SetVBRuntime(VBRuntime As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject4.SetVBRuntime
                    Me._vbRuntime = VBRuntime

                    If VBRuntime = "Embed" Then
                        Me._compilationOptions = Me._compilationOptions.WithEmbedVbCoreRuntime(True)
                    End If

                    Return True
                End Function

                Public Function CompileAsync(ByRef buildSucceededEvent As IntPtr, ByRef buildFailedEvent As IntPtr) As Integer Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject5.CompileAsync
                    Return 0
                End Function

                Public Function EndCompile(buildSuccess As Boolean) As Integer Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject5.EndCompile
                    Return 0
                End Function

                Public Function GetFreeThreadedHostObject() As Microsoft.Build.Tasks.Hosting.IVbcHostObjectFreeThreaded Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject5.GetFreeThreadedHostObject
                    Return Nothing
                End Function

                Public Function SetHighEntropyVA(highEntropyVA As Boolean) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject5.SetHighEntropyVA
                    ' we don't capture emit options
                    Return True
                End Function

                Public Function SetSubsystemVersion(subsystemVersion As String) As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObject5.SetSubsystemVersion
                    ' we don't capture emit options
                    Return True
                End Function

                Public Function Compile1() As Boolean Implements Microsoft.Build.Tasks.Hosting.IVbcHostObjectFreeThreaded.Compile
                    Return False
                End Function

            End Class

        End Class

    End Class

End Namespace

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks.Hosting;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.MSBuild;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.VisualBasic
{
    internal class VisualBasicProjectFileLoader : ProjectFileLoader
    {
        private readonly HostWorkspaceServices _workspaceServices;

        internal HostLanguageServices LanguageServices
        {
            get { return this._workspaceServices.GetLanguageServices(LanguageNames.VisualBasic); }
        }

        public override string Language
        {
            get { return LanguageNames.VisualBasic; }
        }

        internal VisualBasicProjectFileLoader(HostWorkspaceServices workspaceServices)
        {
            this._workspaceServices = workspaceServices;
        }

        protected override ProjectFile CreateProjectFile(MSB.Evaluation.Project loadedProject)
        {
            return new VisualBasicProjectFile(this, loadedProject, this._workspaceServices.GetService<IMetadataService>(), this._workspaceServices.GetService<IAnalyzerService>());
        }

        internal class VisualBasicProjectFile : ProjectFile
        {
            private readonly IMetadataService _metadataService;
            private readonly IAnalyzerService _analyzerService;
            private readonly IHostBuildDataFactory _hostBuildDataFactory;
            private readonly ICommandLineArgumentsFactoryService _commandLineArgumentsFactory;

            public VisualBasicProjectFile(VisualBasicProjectFileLoader loader, MSB.Evaluation.Project loadedProject, IMetadataService metadataService, IAnalyzerService analyzerService) : base(loader, loadedProject)
            {
                this._metadataService = metadataService;
                this._analyzerService = analyzerService;
                this._hostBuildDataFactory = loader.LanguageServices.GetService<IHostBuildDataFactory>();
                this._commandLineArgumentsFactory = loader.LanguageServices.GetService<ICommandLineArgumentsFactoryService>();
            }

            public override SourceCodeKind GetSourceCodeKind(string documentFileName)
            {
                SourceCodeKind result;
                if (documentFileName.EndsWith(".vbx", StringComparison.OrdinalIgnoreCase))
                {
                    result = SourceCodeKind.Script;
                }
                else
                {
                    result = SourceCodeKind.Regular;
                }
                return result;
            }

            public override string GetDocumentExtension(SourceCodeKind sourceCodeKind)
            {
                string result;
                if (sourceCodeKind != SourceCodeKind.Script)
                {
                    result = ".vb";
                }
                else
                {
                    result = ".vbx";
                }
                return result;
            }

            public override async Task<ProjectFileInfo> GetProjectFileInfoAsync(CancellationToken cancellationToken)
            {
                var compilerInputs = new VisualBasicCompilerInputs(this);
                var executedProject = await BuildAsync("Vbc", compilerInputs, cancellationToken).ConfigureAwait(false);

                if (!compilerInputs.Initialized)
                {
                    InitializeFromModel(compilerInputs, executedProject);
                }

                return CreateProjectFileInfo(compilerInputs, executedProject);
            }

            private ProjectFileInfo CreateProjectFileInfo(VisualBasicProjectFileLoader.VisualBasicProjectFile.VisualBasicCompilerInputs compilerInputs, ProjectInstance executedProject)
            {
                IEnumerable<MetadataReference> metadataReferences = null;
                IEnumerable<AnalyzerReference> analyzerReferences = null;
                this.GetReferences(compilerInputs, executedProject, ref metadataReferences, ref analyzerReferences);
                string outputPath = Path.Combine(this.GetOutputDirectory(), compilerInputs.OutputFileName);
                string assemblyName = this.GetAssemblyName();
                HostBuildData hostBuildData = this._hostBuildDataFactory.Create(compilerInputs.HostBuildOptions);
                return new ProjectFileInfo(outputPath, assemblyName, hostBuildData.CompilationOptions, hostBuildData.ParseOptions, compilerInputs.CodePage, this.GetDocuments(compilerInputs.Sources, executedProject), this.GetDocuments(compilerInputs.AdditionalFiles, executedProject), base.GetProjectReferences(executedProject), metadataReferences, analyzerReferences);
            }

            private void GetReferences(VisualBasicProjectFileLoader.VisualBasicProjectFile.VisualBasicCompilerInputs compilerInputs, ProjectInstance executedProject, ref IEnumerable<MetadataReference> metadataReferences, ref IEnumerable<AnalyzerReference> analyzerReferences)
            {
                // use command line parser to compute references using common logic
                List<string> list = new List<string>();
                if (compilerInputs.LibPaths != null && compilerInputs.LibPaths.Count<string>() > 0)
                {
                    list.Add("/libpath:\"" + string.Join(";", compilerInputs.LibPaths) + "\"");
                }

                // metadata references
                foreach (var current in compilerInputs.References)
                {
                    if (!IsProjectReferenceOutputAssembly(current))
                    {
                        string documentFilePath = base.GetDocumentFilePath(current);
                        list.Add("/r:\"" + documentFilePath + "\"");
                    }
                }

                // analyzer references
                foreach (var current in compilerInputs.AnalyzerReferences)
                {
                    string documentFilePath2 = base.GetDocumentFilePath(current);
                    list.Add("/a:\"" + documentFilePath2 + "\"");
                }

                if (compilerInputs.NoStandardLib)
                {
                    list.Add("/nostdlib");
                }

                if (!string.IsNullOrEmpty(compilerInputs.VbRuntime))
                {
                    if (compilerInputs.VbRuntime == "Default")
                    {
                        list.Add("/vbruntime+");
                    }
                    else if (compilerInputs.VbRuntime == "Embed")
                    {
                        list.Add("/vbruntime*");
                    }
                    else if (compilerInputs.VbRuntime == "None")
                    {
                        list.Add("/vbruntime-");
                    }
                    else
                    {
                        list.Add("/vbruntime: " + compilerInputs.VbRuntime);
                    }
                }

                if (!string.IsNullOrEmpty(compilerInputs.SdkPath))
                {
                    list.Add("/sdkpath:" + compilerInputs.SdkPath);
                }

                CommandLineArguments commandLineArguments = this._commandLineArgumentsFactory.CreateCommandLineArguments(list, executedProject.Directory, false, RuntimeEnvironment.GetRuntimeDirectory());
                MetadataFileReferenceResolver pathResolver = new MetadataFileReferenceResolver(commandLineArguments.ReferencePaths, commandLineArguments.BaseDirectory);
                metadataReferences = commandLineArguments.ResolveMetadataReferences(new AssemblyReferenceResolver(pathResolver, this._metadataService.GetProvider()));

                IAnalyzerAssemblyLoader loader = this._analyzerService.GetLoader();
                foreach (var path in commandLineArguments.AnalyzerReferences.Select((r) => r.FilePath))
                {
                    loader.AddDependencyLocation(path);
                }

                analyzerReferences = commandLineArguments.ResolveAnalyzerReferences(loader);
            }

            private IEnumerable<DocumentFileInfo> GetDocuments(IEnumerable<ITaskItem> sources, ProjectInstance executedProject)
            {
                IEnumerable<DocumentFileInfo> result;
                if (sources == null)
                {
                    result = ImmutableArray<DocumentFileInfo>.Empty;
                }

                var projectDirectory = executedProject.Directory;
                if (!projectDirectory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    projectDirectory += Path.DirectorySeparatorChar;
                }

                return sources
                    .Where(s => !System.IO.Path.GetFileName(s.ItemSpec).StartsWith("TemporaryGeneratedFile_", StringComparison.Ordinal))
                    .Select(s => new DocumentFileInfo(GetDocumentFilePath(s), GetDocumentLogicalPath(s, projectDirectory), IsDocumentLinked(s), IsDocumentGenerated(s))).ToImmutableArray();
            }

            private void InitializeFromModel(VisualBasicProjectFileLoader.VisualBasicProjectFile.VisualBasicCompilerInputs compilerInputs, ProjectInstance executedProject)
            {
                compilerInputs.BeginInitialization();
                compilerInputs.SetBaseAddress(base.ReadPropertyString(executedProject, "OutputType"), base.ReadPropertyString(executedProject, "BaseAddress"));
                compilerInputs.SetCodePage(base.ReadPropertyInt(executedProject, "CodePage"));
                compilerInputs.SetDebugType(base.ReadPropertyBool(executedProject, "DebugSymbols"), base.ReadPropertyString(executedProject, "DebugType"));
                compilerInputs.SetDefineConstants(base.ReadPropertyString(executedProject, "FinalDefineConstants", "DefineConstants"));
                compilerInputs.SetDelaySign(base.ReadPropertyBool(executedProject, "DelaySign"));
                compilerInputs.SetDisabledWarnings(base.ReadPropertyString(executedProject, "NoWarn"));
                compilerInputs.SetDocumentationFile(base.GetItemString(executedProject, "DocFileItem"));
                compilerInputs.SetErrorReport(base.ReadPropertyString(executedProject, "ErrorReport"));
                compilerInputs.SetFileAlignment(base.ReadPropertyInt(executedProject, "FileAlignment"));
                compilerInputs.SetGenerateDocumentation(base.ReadPropertyBool(executedProject, "GenerateDocumentation"));
                compilerInputs.SetHighEntropyVA(base.ReadPropertyBool(executedProject, "HighEntropyVA"));

                var _imports = this.GetTaskItems(executedProject, "Import");
                if (_imports != null)
                {
                    compilerInputs.SetImports(_imports.ToArray());
                }

                var signAssembly = ReadPropertyBool(executedProject, "SignAssembly");
                if (signAssembly)
                {
                    var keyFile = ReadPropertyString(executedProject, "KeyOriginatorFile", "AssemblyOriginatorKeyFile");
                    if (!string.IsNullOrEmpty(keyFile))
                    {
                        compilerInputs.SetKeyFile(keyFile);
                    }

                    var keyContainer = ReadPropertyString(executedProject, "KeyContainerName");
                    if (!string.IsNullOrEmpty(keyContainer))
                    {
                        compilerInputs.SetKeyContainer(keyContainer);
                    }
                }

                compilerInputs.SetLanguageVersion(base.ReadPropertyString(executedProject, "LangVersion"));
                compilerInputs.SetMainEntryPoint(base.ReadPropertyString(executedProject, "StartupObject"));
                compilerInputs.SetModuleAssemblyName(base.ReadPropertyString(executedProject, "ModuleEntryPoint"));
                compilerInputs.SetNoStandardLib(base.ReadPropertyBool(executedProject, "NoCompilerStandardLib", "NoStdLib"));
                compilerInputs.SetNoWarnings(base.ReadPropertyBool(executedProject, "_NoWarnings"));
                compilerInputs.SetOptimize(base.ReadPropertyBool(executedProject, "Optimize"));
                compilerInputs.SetOptionCompare(base.ReadPropertyString(executedProject, "OptionCompare"));
                compilerInputs.SetOptionExplicit(base.ReadPropertyBool(executedProject, "OptionExplicit"));
                compilerInputs.SetOptionInfer(base.ReadPropertyBool(executedProject, "OptionInfer"));
                compilerInputs.SetOptionStrictType(base.ReadPropertyString(executedProject, "OptionStrict"));
                compilerInputs.SetOutputAssembly(base.GetItemString(executedProject, "IntermediateAssembly"));

                if (base.ReadPropertyBool(executedProject, "Prefer32Bit"))
                {
                    compilerInputs.SetPlatformWith32BitPreference(base.ReadPropertyString(executedProject, "PlatformTarget"));
                }
                else
                {
                    compilerInputs.SetPlatform(base.ReadPropertyString(executedProject, "PlatformTarget"));
                }

                compilerInputs.SetRemoveIntegerChecks(base.ReadPropertyBool(executedProject, "RemoveIntegerChecks"));
                compilerInputs.SetRootNamespace(base.ReadPropertyString(executedProject, "RootNamespace"));
                compilerInputs.SetSdkPath(base.ReadPropertyString(executedProject, "FrameworkPathOverride"));
                compilerInputs.SetSubsystemVersion(base.ReadPropertyString(executedProject, "SubsystemVersion"));
                compilerInputs.SetTargetCompactFramework(base.ReadPropertyBool(executedProject, "TargetCompactFramework"));
                compilerInputs.SetTargetType(base.ReadPropertyString(executedProject, "OutputType"));

                // Decode the warning options from RuleSet file prior to reading explicit settings in the project file, so that project file settings prevail for duplicates.
                compilerInputs.SetRuleSet(base.ReadPropertyString(executedProject, "RuleSet"));

                compilerInputs.SetTreatWarningsAsErrors(base.ReadPropertyBool(executedProject, "SetTreatWarningsAsErrors"));
                compilerInputs.SetVBRuntime(base.ReadPropertyString(executedProject, "VbRuntime"));
                compilerInputs.SetWarningsAsErrors(base.ReadPropertyString(executedProject, "WarningsAsErrors"));
                compilerInputs.SetWarningsNotAsErrors(base.ReadPropertyString(executedProject, "WarningsNotAsErrors"));
                compilerInputs.SetReferences(this.GetMetadataReferencesFromModel(executedProject).ToArray<ITaskItem>());
                compilerInputs.SetAnalyzers(this.GetAnalyzerReferencesFromModel(executedProject).ToArray<ITaskItem>());
                compilerInputs.SetAdditionalFiles(this.GetAdditionalFilesFromModel(executedProject).ToArray<ITaskItem>());
                compilerInputs.SetSources(this.GetDocumentsFromModel(executedProject).ToArray<ITaskItem>());
                compilerInputs.EndInitialization();
            }

            private class VisualBasicCompilerInputs : 
                MSB.Tasks.Hosting.IVbcHostObject5, 
                MSB.Tasks.Hosting.IVbcHostObjectFreeThreaded
#if !MSBUILD12
                ,IAnalyzerHostObject 
#endif
            {
                private readonly VisualBasicProjectFile _projectFile;
                private bool _initialized;
                private HostBuildOptions _options;
                private int _codePage;
                private IEnumerable<MSB.Framework.ITaskItem> _sources;
                private IEnumerable<MSB.Framework.ITaskItem> _additionalFiles;
                private IEnumerable<MSB.Framework.ITaskItem> _references;
                private IEnumerable<MSB.Framework.ITaskItem> _analyzerReferences;
                private bool _noStandardLib;
                private readonly Dictionary<string, ReportDiagnostic> _warnings;
                private string _sdkPath;
                private bool _targetCompactFramework;
                private string _vbRuntime;
                private IEnumerable<string> _libPaths;

                private string _outputFileName;
                public VisualBasicCompilerInputs(VisualBasicProjectFile projectFile)
                {
                    this._projectFile = projectFile;
                    this._options = new HostBuildOptions();
                    this._sources = SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    this._references = SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    this._analyzerReferences = SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    this._warnings = new Dictionary<string, ReportDiagnostic>();

                    this._options.ProjectDirectory = Path.GetDirectoryName(projectFile.FilePath);
                    this._options.OutputDirectory = projectFile.GetOutputDirectory();
                }

                public bool Initialized
                {
                    get { return this._initialized; }
                }

                public HostBuildOptions HostBuildOptions
                {
                    get { return this._options; }
                }

                public int CodePage
                {
                    get { return this._codePage; }
                }

                public IEnumerable<MSB.Framework.ITaskItem> References
                {
                    get { return this._references; }
                }

                public IEnumerable<MSB.Framework.ITaskItem> AnalyzerReferences
                {
                    get { return this._analyzerReferences; }
                }

                public IEnumerable<MSB.Framework.ITaskItem> Sources
                {
                    get { return this._sources; }
                }

                public IEnumerable<MSB.Framework.ITaskItem> AdditionalFiles
                {
                    get { return this._additionalFiles; }
                }

                public bool NoStandardLib
                {
                    get { return this._noStandardLib; }
                }

                public string VbRuntime
                {
                    get { return this._vbRuntime; }
                }

                public string SdkPath
                {
                    get { return this._sdkPath; }
                }

                public bool TargetCompactFramework
                {
                    get { return this._targetCompactFramework; }
                }

                public IEnumerable<string> LibPaths
                {
                    get { return this._libPaths; }
                }

                public string OutputFileName
                {
                    get { return this._outputFileName; }
                }

                public void BeginInitialization()
                {
                }

                public bool Compile()
                {
                    return false;
                }

                public void EndInitialization()
                {
                    this._initialized = true;
                }

                public bool IsDesignTime()
                {
                    return true;
                }

                public bool IsUpToDate()
                {
                    return true;
                }

                public bool SetAdditionalLibPaths(string[] additionalLibPaths)
                {
                    this._libPaths = additionalLibPaths;
                    return true;
                }

                public bool SetAddModules(string[] addModules)
                {
                    return true;
                }

                public bool SetBaseAddress(string targetType, string baseAddress)
                {
                    // we don't capture emit options
                    return true;
                }

                public bool SetCodePage(int codePage)
                {
                    this._codePage = codePage;
                    return true;
                }

                public bool SetDebugType(bool emitDebugInformation, string debugType)
                {
                    // ignore, just check for expected values for backwards compat
                    return string.Equals(debugType, "none", StringComparison.OrdinalIgnoreCase) || string.Equals(debugType, "pdbonly", StringComparison.OrdinalIgnoreCase) || string.Equals(debugType, "full", StringComparison.OrdinalIgnoreCase);
                }

                public bool SetDefineConstants(string defineConstants)
                {
                    this._options.DefineConstants = defineConstants;
                    return true;
                }

                public bool SetDelaySign(bool delaySign)
                {
                    this._options.DelaySign = Tuple.Create(delaySign, false);
                    return true;
                }

                public bool SetDisabledWarnings(string disabledWarnings)
                {
                    SetWarnings(disabledWarnings, ReportDiagnostic.Suppress);
                    return true;
                }

                public bool SetDocumentationFile(string documentationFile)
                {
                    this._options.DocumentationFile = documentationFile;
                    return true;
                }

                public bool SetErrorReport(string errorReport)
                {
                    // ??
                    return true;
                }

                public bool SetFileAlignment(int fileAlignment)
                {
                    // we don't capture emit options
                    return true;
                }

                public bool SetGenerateDocumentation(bool generateDocumentation)
                {
                    return true;
                }

                public bool SetImports(Microsoft.Build.Framework.ITaskItem[] importsList)
                {
                    if (importsList != null)
                    {
                        _options.GlobalImports.AddRange(importsList.Select(item => item.ItemSpec.Trim()));
                    }
                    return true;
                }

                public bool SetKeyContainer(string keyContainer)
                {
                    this._options.KeyContainer = keyContainer;
                    return true;
                }

                public bool SetKeyFile(string keyFile)
                {
                    this._options.KeyFile = keyFile;
                    return true;
                }

                public bool SetLinkResources(Microsoft.Build.Framework.ITaskItem[] linkResources)
                {
                    // ??
                    return true;
                }

                public bool SetMainEntryPoint(string mainEntryPoint)
                {
                    this._options.MainEntryPoint = mainEntryPoint;
                    return true;
                }

                public bool SetNoConfig(bool noConfig)
                {
                    return true;
                }

                public bool SetNoStandardLib(bool noStandardLib)
                {
                    this._noStandardLib = noStandardLib;
                    return true;
                }

                public bool SetNoWarnings(bool noWarnings)
                {
                    this._options.NoWarnings = noWarnings;
                    return true;
                }

                public bool SetOptimize(bool optimize)
                {
                    this._options.Optimize = optimize;
                    return true;
                }

                public bool SetOptionCompare(string optionCompare)
                {
                    this._options.OptionCompare = optionCompare;
                    return true;
                }

                public bool SetOptionExplicit(bool optionExplicit)
                {
                    this._options.OptionExplicit = optionExplicit;
                    return true;
                }

                public bool SetOptionStrict(bool _optionStrict)
                {
                    this._options.OptionStrict = _optionStrict ? "On" : "Custom";
                    return true;
                }

                public bool SetOptionStrictType(string optionStrictType)
                {
                    if (!string.IsNullOrEmpty(optionStrictType))
                    {
                        this._options.OptionStrict = optionStrictType;
                    }
                    return true;
                }

                public bool SetOutputAssembly(string outputAssembly)
                {
                    this._outputFileName = Path.GetFileName(outputAssembly);
                    return true;
                }

                public bool SetPlatform(string _platform)
                {
                    this._options.Platform = _platform;
                    return true;
                }

                public bool SetPlatformWith32BitPreference(string _platform)
                {
                    this._options.PlatformWith32BitPreference = _platform;
                    return true;
                }

                public bool SetReferences(Microsoft.Build.Framework.ITaskItem[] references)
                {
                    this._references = references ?? SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    return true;
                }

                public bool SetAnalyzers(MSB.Framework.ITaskItem[] analyzerReferences)
                {
                    this._analyzerReferences = analyzerReferences ?? SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    return true;
                }

                public bool SetAdditionalFiles(MSB.Framework.ITaskItem[] additionalFiles)
                {
                    this._additionalFiles = additionalFiles ?? SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    return true;
                }

                public bool SetRemoveIntegerChecks(bool removeIntegerChecks)
                {
                    this._options.CheckForOverflowUnderflow = !removeIntegerChecks;
                    return true;
                }

                public bool SetResources(Microsoft.Build.Framework.ITaskItem[] resources)
                {
                    return true;
                }

                public bool SetResponseFiles(Microsoft.Build.Framework.ITaskItem[] responseFiles)
                {
                    return true;
                }

                public bool SetRootNamespace(string rootNamespace)
                {
                    this._options.RootNamespace = rootNamespace;
                    return true;
                }

                public bool SetSdkPath(string sdkPath)
                {
                    this._sdkPath = sdkPath;
                    return true;
                }

                public bool SetSources(Microsoft.Build.Framework.ITaskItem[] sources)
                {
                    this._sources = sources ?? SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    return true;
                }

                public bool SetTargetCompactFramework(bool targetCompactFramework)
                {
                    this._targetCompactFramework = targetCompactFramework;
                    return true;
                }

                public bool SetTargetType(string targetType)
                {
                    if (!string.IsNullOrEmpty(targetType))
                    {
                        OutputKind outputKind;
                        if (VisualBasicProjectFile.TryGetOutputKind(targetType, out outputKind))
                        {
                            this._options.OutputKind = outputKind;
                        }
                    }
                    return true;
                }

                public bool SetRuleSet(string ruleSetFile)
                {
                    this._options.RuleSetFile = ruleSetFile;
                    return true;
                }

                public bool SetTreatWarningsAsErrors(bool treatWarningsAsErrors)
                {
                    this._options.WarningsAsErrors = treatWarningsAsErrors;
                    return true;
                }

                public bool SetWarningsAsErrors(string warningsAsErrors)
                {
                    SetWarnings(warningsAsErrors, ReportDiagnostic.Error);
                    return true;
                }

                private static readonly char[] s_warningSeparators = { ';', ',' };

                private void SetWarnings(string warnings, ReportDiagnostic reportStyle)
                {
                    if (!string.IsNullOrEmpty(warnings))
                    {
                        foreach (var warning in warnings.Split(s_warningSeparators, StringSplitOptions.None))
                        {
                            int warningId = 0;
                            if (Int32.TryParse(warning, out warningId))
                            {
                                this._warnings["BC" + warningId.ToString("0000")] = reportStyle;
                            }
                            else
                            {
                                this._warnings[warning] = reportStyle;
                            }
                        }
                    }
                }

                public bool SetWarningsNotAsErrors(string warningsNotAsErrors)
                {
                    SetWarnings(warningsNotAsErrors, ReportDiagnostic.Warn);
                    return true;
                }

                public bool SetWin32Icon(string win32Icon)
                {
                    return true;
                }

                public bool SetWin32Resource(string win32Resource)
                {
                    return true;
                }

                public bool SetModuleAssemblyName(string moduleAssemblyName)
                {
                    return true;
                }

                public bool SetOptionInfer(bool optionInfer)
                {
                    this._options.OptionInfer = optionInfer;
                    return true;
                }

                public bool SetWin32Manifest(string win32Manifest)
                {
                    return true;
                }

                public bool SetLanguageVersion(string _languageVersion)
                {
                    this._options.LanguageVersion = _languageVersion;
                    return true;
                }

                public bool SetVBRuntime(string VBRuntime)
                {
                    this._vbRuntime = VBRuntime;
                    this._options.VBRuntime = VBRuntime;
                    return true;
                }

                public int CompileAsync(out IntPtr buildSucceededEvent, out IntPtr buildFailedEvent)
                {
                    buildSucceededEvent = IntPtr.Zero;
                    buildFailedEvent = IntPtr.Zero;
                    return 0;
                }

                public int EndCompile(bool buildSuccess)
                {
                    return 0;
                }

                public Microsoft.Build.Tasks.Hosting.IVbcHostObjectFreeThreaded GetFreeThreadedHostObject()
                {
                    return null;
                }

                public bool SetHighEntropyVA(bool highEntropyVA)
                {
                    // we don't capture emit options
                    return true;
                }

                public bool SetSubsystemVersion(string subsystemVersion)
                {
                    // we don't capture emit options
                    return true;
                }

                public bool Compile1()
                {
                    return false;
                }
                bool Microsoft.Build.Tasks.Hosting.IVbcHostObjectFreeThreaded.Compile()
                {
                    return Compile1();
                }
            }


        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.MSBuild;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class CSharpProjectFileLoader : ProjectFileLoader
    {
        private class CSharpProjectFile : ProjectFile
        {
            private readonly IMetadataService _metadataService;
            private readonly IAnalyzerService _analyzerService;
            private readonly IHostBuildDataFactory _msbuildHost;
            private readonly ICommandLineArgumentsFactoryService _commandLineArgumentsFactoryService;

            public CSharpProjectFile(CSharpProjectFileLoader loader, MSB.Evaluation.Project project, IMetadataService metadataService, IAnalyzerService analyzerService)
                : base(loader, project)
            {
                _metadataService = metadataService;
                _analyzerService = analyzerService;
                _msbuildHost = loader.MSBuildHost;
                _commandLineArgumentsFactoryService = loader.CommandLineArgumentsFactoryService;
            }

            public override SourceCodeKind GetSourceCodeKind(string documentFileName)
            {
                return documentFileName.EndsWith(".csx", StringComparison.OrdinalIgnoreCase)
                    ? SourceCodeKind.Script
                    : SourceCodeKind.Regular;
            }

            public override string GetDocumentExtension(SourceCodeKind sourceCodeKind)
            {
                switch (sourceCodeKind)
                {
                    case SourceCodeKind.Script:
                        return ".csx";
                    default:
                        return ".cs";
                }
            }

            public override async Task<ProjectFileInfo> GetProjectFileInfoAsync(CancellationToken cancellationToken)
            {
                var compilerInputs = new CSharpCompilerInputs(this);

                var executedProject = await this.BuildAsync("Csc", compilerInputs, cancellationToken).ConfigureAwait(false);

                if (!compilerInputs.Initialized)
                {
                    // if msbuild didn't reach the CSC task for some reason, attempt to initialize using the variables that were defined so far.
                    this.InitializeFromModel(compilerInputs, executedProject);
                }

                return CreateProjectFileInfo(compilerInputs, executedProject);
            }

            protected override ProjectFileReference CreateProjectFileReference(ProjectItemInstance reference)
            {
                var filePath = reference.EvaluatedInclude;
                var aliases = GetAliases(reference);

                return new ProjectFileReference(filePath, aliases);
            }

            private ProjectFileInfo CreateProjectFileInfo(CSharpCompilerInputs compilerInputs, MSB.Execution.ProjectInstance executedProject)
            {
                string projectDirectory = executedProject.Directory;
                string directorySeparator = Path.DirectorySeparatorChar.ToString();
                if (!projectDirectory.EndsWith(directorySeparator, StringComparison.OrdinalIgnoreCase))
                {
                    projectDirectory += directorySeparator;
                }

                var docs = compilerInputs.Sources
                       .Where(s => !Path.GetFileName(s.ItemSpec).StartsWith("TemporaryGeneratedFile_", StringComparison.Ordinal))
                       .Select(s => MakeDocumentFileInfo(projectDirectory, s))
                       .ToImmutableArray();

                var additionalDocs = compilerInputs.AdditionalFiles
                                     .Select(s => MakeDocumentFileInfo(projectDirectory, s))
                                     .ToImmutableArray();

                IEnumerable<MetadataReference> metadataRefs;
                IEnumerable<AnalyzerReference> analyzerRefs;
                this.GetReferences(compilerInputs, executedProject, out metadataRefs, out analyzerRefs);

                var outputPath = Path.Combine(this.GetOutputDirectory(), compilerInputs.OutputFileName);
                var assemblyName = this.GetAssemblyName();
                var msbuildData = _msbuildHost.Create(compilerInputs.Options);

                return new ProjectFileInfo(
                    outputPath,
                    assemblyName,
                    msbuildData.CompilationOptions,
                    msbuildData.ParseOptions,
                    compilerInputs.CodePage,
                    docs,
                    additionalDocs,
                    this.GetProjectReferences(executedProject),
                    metadataRefs,
                    analyzerRefs);
            }

            private DocumentFileInfo MakeDocumentFileInfo(string projectDirectory, MSB.Framework.ITaskItem item)
            {
                var filePath = GetDocumentFilePath(item);
                var logicalPath = GetDocumentLogicalPath(item, projectDirectory);
                var isLinked = IsDocumentLinked(item);
                var isGenerated = IsDocumentGenerated(item);
                return new DocumentFileInfo(filePath, logicalPath, isLinked, isGenerated);
            }

            private ImmutableArray<string> GetAliases(MSB.Framework.ITaskItem item)
            {
                var aliasesText = item.GetMetadata("Aliases");

                if (string.IsNullOrEmpty(aliasesText))
                {
                    return ImmutableArray<string>.Empty;
                }

                return ImmutableArray.CreateRange(aliasesText.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries));
            }

            private void GetReferences(
                CSharpCompilerInputs compilerInputs,
                MSB.Execution.ProjectInstance executedProject,
                out IEnumerable<MetadataReference> metadataReferences,
                out IEnumerable<AnalyzerReference> analyzerReferences)
            {
                // use command line parser to do reference translation same as command line compiler

                var args = new List<string>();

                if (compilerInputs.LibPaths != null && compilerInputs.LibPaths.Count > 0)
                {
                    args.Add("/lib:\"" + string.Join(";", compilerInputs.LibPaths) + "\"");
                }

                foreach (var mr in compilerInputs.References)
                {
                    if (!IsProjectReferenceOutputAssembly(mr))
                    {
                        var filePath = GetDocumentFilePath(mr);

                        var aliases = GetAliases(mr);
                        if (aliases.IsDefaultOrEmpty)
                        {
                            args.Add("/r:\"" + filePath + "\"");
                        }
                        else
                        {
                            foreach (var alias in aliases)
                            {
                                args.Add("/r:" + alias + "=\"" + filePath + "\"");
                            }
                        }
                    }
                }

                foreach (var ar in compilerInputs.AnalyzerReferences)
                {
                    var filePath = GetDocumentFilePath(ar);
                    args.Add("/a:\"" + filePath + "\"");
                }

                if (compilerInputs.NoStandardLib)
                {
                    args.Add("/nostdlib");
                }

                var commandLineArgs = _commandLineArgumentsFactoryService.CreateCommandLineArguments(args, executedProject.Directory, isInteractive: false, sdkDirectory: RuntimeEnvironment.GetRuntimeDirectory());

                var resolver = new MetadataFileReferenceResolver(commandLineArgs.ReferencePaths, commandLineArgs.BaseDirectory);
                metadataReferences = commandLineArgs.ResolveMetadataReferences(new AssemblyReferenceResolver(resolver, _metadataService.GetProvider()));

                var analyzerLoader = _analyzerService.GetLoader();
                foreach (var path in commandLineArgs.AnalyzerReferences.Select(r => r.FilePath))
                {
                    analyzerLoader.AddDependencyLocation(path);
                }
                analyzerReferences = commandLineArgs.ResolveAnalyzerReferences(analyzerLoader);
            }

            private void InitializeFromModel(CSharpCompilerInputs compilerInputs, MSB.Execution.ProjectInstance executedProject)
            {
                compilerInputs.BeginInitialization();

                compilerInputs.SetAllowUnsafeBlocks(this.ReadPropertyBool(executedProject, "AllowUnsafeBlocks"));
                compilerInputs.SetApplicationConfiguration(this.ReadPropertyString(executedProject, "AppConfigForCompiler"));
                compilerInputs.SetBaseAddress(this.ReadPropertyString(executedProject, "BaseAddress"));
                compilerInputs.SetCheckForOverflowUnderflow(this.ReadPropertyBool(executedProject, "CheckForOverflowUnderflow"));
                compilerInputs.SetCodePage(this.ReadPropertyInt(executedProject, "CodePage"));
                compilerInputs.SetDebugType(this.ReadPropertyString(executedProject, "DebugType"));
                compilerInputs.SetDefineConstants(this.ReadPropertyString(executedProject, "DefineConstants"));

                var delaySignProperty = this.GetProperty("DelaySign");
                compilerInputs.SetDelaySign(delaySignProperty != null && !string.IsNullOrEmpty(delaySignProperty.EvaluatedValue), this.ReadPropertyBool(executedProject, "DelaySign"));

                compilerInputs.SetDisabledWarnings(this.ReadPropertyString(executedProject, "NoWarn"));
                compilerInputs.SetDocumentationFile(this.GetItemString(executedProject, "DocFileItem"));
                compilerInputs.SetEmitDebugInformation(this.ReadPropertyBool(executedProject, "DebugSymbols"));
                compilerInputs.SetErrorReport(this.ReadPropertyString(executedProject, "ErrorReport"));
                compilerInputs.SetFileAlignment(this.ReadPropertyInt(executedProject, "FileAlignment"));
                compilerInputs.SetGenerateFullPaths(this.ReadPropertyBool(executedProject, "GenerateFullPaths"));
                compilerInputs.SetHighEntropyVA(this.ReadPropertyBool(executedProject, "HighEntropyVA"));

                bool signAssembly = this.ReadPropertyBool(executedProject, "SignAssembly");
                if (signAssembly)
                {
                    compilerInputs.SetKeyContainer(this.ReadPropertyString(executedProject, "KeyContainerName"));
                    compilerInputs.SetKeyFile(this.ReadPropertyString(executedProject, "KeyOriginatorFile", "AssemblyOriginatorKeyFile"));
                }

                compilerInputs.SetLangVersion(this.ReadPropertyString(executedProject, "LangVersion"));

                compilerInputs.SetMainEntryPoint(null, this.ReadPropertyString(executedProject, "StartupObject"));
                compilerInputs.SetModuleAssemblyName(this.ReadPropertyString(executedProject, "ModuleAssemblyName"));
                compilerInputs.SetNoStandardLib(this.ReadPropertyBool(executedProject, "NoCompilerStandardLib"));
                compilerInputs.SetOptimize(this.ReadPropertyBool(executedProject, "Optimize"));
                compilerInputs.SetOutputAssembly(this.GetItemString(executedProject, "IntermediateAssembly"));
                compilerInputs.SetPdbFile(this.ReadPropertyString(executedProject, "PdbFile"));

                if (this.ReadPropertyBool(executedProject, "Prefer32Bit"))
                {
                    compilerInputs.SetPlatformWith32BitPreference(this.ReadPropertyString(executedProject, "PlatformTarget"));
                }
                else
                {
                    compilerInputs.SetPlatform(this.ReadPropertyString(executedProject, "PlatformTarget"));
                }

                compilerInputs.SetSubsystemVersion(this.ReadPropertyString(executedProject, "SubsystemVersion"));
                compilerInputs.SetTargetType(this.ReadPropertyString(executedProject, "OutputType"));

                // Decode the warning options from RuleSet file prior to reading explicit settings in the project file, so that project file settings prevail for duplicates.
                compilerInputs.SetRuleSet(this.ReadPropertyString(executedProject, "RuleSet"));
                compilerInputs.SetTreatWarningsAsErrors(this.ReadPropertyBool(executedProject, "TreatWarningsAsErrors"));
                compilerInputs.SetWarningLevel(this.ReadPropertyInt(executedProject, "WarningLevel"));
                compilerInputs.SetWarningsAsErrors(this.ReadPropertyString(executedProject, "WarningsAsErrors"));
                compilerInputs.SetWarningsNotAsErrors(this.ReadPropertyString(executedProject, "WarningsNotAsErrors"));

                compilerInputs.SetReferences(this.GetMetadataReferencesFromModel(executedProject).ToArray());
                compilerInputs.SetAnalyzers(this.GetAnalyzerReferencesFromModel(executedProject).ToArray());
                compilerInputs.SetAdditionalFiles(this.GetAdditionalFilesFromModel(executedProject).ToArray());
                compilerInputs.SetSources(this.GetDocumentsFromModel(executedProject).ToArray());

                string errorMessage;
                int errorCode;
                compilerInputs.EndInitialization(out errorMessage, out errorCode);
            }

            private class CSharpCompilerInputs :
#if !MSBUILD12
                MSB.Tasks.Hosting.ICscHostObject4,
                MSB.Tasks.Hosting.IAnalyzerHostObject
#else
                MSB.Tasks.Hosting.ICscHostObject4
#endif
            {
                private readonly CSharpProjectFile _projectFile;

                internal bool Initialized { get; private set; }
                internal HostBuildOptions Options { get; private set; }
                internal int CodePage { get; private set; }
                internal IEnumerable<MSB.Framework.ITaskItem> Sources { get; private set; }
                internal IEnumerable<MSB.Framework.ITaskItem> References { get; private set; }
                internal IEnumerable<MSB.Framework.ITaskItem> AnalyzerReferences { get; private set; }
                internal IEnumerable<MSB.Framework.ITaskItem> AdditionalFiles { get; private set; }
                internal IReadOnlyList<string> LibPaths { get; private set; }
                internal bool NoStandardLib { get; private set; }
                internal string OutputFileName { get; private set; }

                internal CSharpCompilerInputs(CSharpProjectFile projectFile)
                {
                    _projectFile = projectFile;
                    this.Options = new HostBuildOptions();
                    this.Sources = SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    this.References = SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    this.AnalyzerReferences = SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    this.AdditionalFiles = SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    this.LibPaths = SpecializedCollections.EmptyReadOnlyList<string>();

                    this.Options.ProjectDirectory = Path.GetDirectoryName(projectFile.FilePath);
                    this.Options.OutputDirectory = projectFile.GetOutputDirectory();
                }

                public bool Compile()
                {
                    return false;
                }

                public void BeginInitialization()
                {
                }

                public bool EndInitialization(out string errorMessage, out int errorCode)
                {
                    this.Initialized = true;
                    errorMessage = string.Empty;
                    errorCode = 0;
                    return true;
                }

                public bool SetHighEntropyVA(bool highEntropyVA)
                {
                    // we don't capture emit options
                    return true;
                }

                public bool SetPlatformWith32BitPreference(string platformWith32BitPreference)
                {
                    this.Options.PlatformWith32BitPreference = platformWith32BitPreference;
                    return true;
                }

                public bool SetSubsystemVersion(string subsystemVersion)
                {
                    // we don't capture emit options
                    return true;
                }

                public bool SetApplicationConfiguration(string applicationConfiguration)
                {
                    this.Options.ApplicationConfiguration = applicationConfiguration;
                    return true;
                }

                public bool SetWin32Manifest(string win32Manifest)
                {
                    // Not used?
                    return true;
                }

                public bool IsDesignTime()
                {
                    return true;
                }

                public bool IsUpToDate()
                {
                    return true;
                }

                public bool SetAddModules(string[] addModules)
                {
                    // ???
                    return true;
                }

                public bool SetAdditionalLibPaths(string[] additionalLibPaths)
                {
                    this.LibPaths = additionalLibPaths;
                    return true;
                }

                public bool SetAllowUnsafeBlocks(bool allowUnsafeBlocks)
                {
                    this.Options.AllowUnsafeBlocks = allowUnsafeBlocks;
                    return true;
                }

                public bool SetBaseAddress(string baseAddress)
                {
                    // we don't capture emit options
                    return true;
                }

                public bool SetCheckForOverflowUnderflow(bool checkForOverflowUnderflow)
                {
                    this.Options.CheckForOverflowUnderflow = checkForOverflowUnderflow;
                    return true;
                }

                public bool SetCodePage(int codePage)
                {
                    this.CodePage = codePage;
                    return true;
                }

                public bool SetDebugType(string debugType)
                {
                    // ignore, just check for expected values for backwards compat
                    return string.Equals(debugType, "none", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(debugType, "pdbonly", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(debugType, "full", StringComparison.OrdinalIgnoreCase);
                }

                public bool SetDefineConstants(string defineConstants)
                {
                    this.Options.DefineConstants = defineConstants;
                    return true;
                }

                private static readonly char[] s_preprocessorSymbolSeparators = new char[] { ';', ',' };

                public bool SetDelaySign(bool delaySignExplicitlySet, bool delaySign)
                {
                    this.Options.DelaySign = Tuple.Create(delaySignExplicitlySet, delaySign);
                    return true;
                }

                public bool SetDisabledWarnings(string disabledWarnings)
                {
                    this.SetWarnings(disabledWarnings, ReportDiagnostic.Suppress);
                    return true;
                }

                private void SetWarnings(string warnings, ReportDiagnostic reportStyle)
                {
                    if (!string.IsNullOrEmpty(warnings))
                    {
                        foreach (var warning in warnings.Split(s_preprocessorSymbolSeparators, StringSplitOptions.None))
                        {
                            int warningId;
                            if (int.TryParse(warning, out warningId))
                            {
                                this.Options.Warnings["CS" + warningId.ToString("0000")] = reportStyle;
                            }
                            else
                            {
                                this.Options.Warnings[warning] = reportStyle;
                            }
                        }
                    }
                }

                public bool SetDocumentationFile(string documentationFile)
                {
                    this.Options.DocumentationFile = documentationFile;
                    return true;
                }

                public bool SetEmitDebugInformation(bool emitDebugInformation)
                {
                    // we don't capture emit options
                    return true;
                }

                public bool SetErrorReport(string errorReport)
                {
                    // ?? prompt?
                    return true;
                }

                public bool SetFileAlignment(int fileAlignment)
                {
                    // we don't capture emit options
                    return true;
                }

                public bool SetGenerateFullPaths(bool generateFullPaths)
                {
                    // ??
                    return true;
                }

                public bool SetKeyContainer(string keyContainer)
                {
                    this.Options.KeyContainer = keyContainer;
                    return true;
                }

                public bool SetKeyFile(string keyFile)
                {
                    this.Options.KeyFile = keyFile;
                    return true;
                }

                public bool SetLangVersion(string langVersion)
                {
                    this.Options.LanguageVersion = langVersion;
                    return true;
                }

                public bool SetLinkResources(MSB.Framework.ITaskItem[] linkResources)
                {
                    // ??
                    return true;
                }

                public bool SetMainEntryPoint(string targetType, string mainEntryPoint)
                {
                    this.Options.MainEntryPoint = mainEntryPoint;
                    return true;
                }

                public bool SetModuleAssemblyName(string moduleAssemblyName)
                {
                    this.Options.ModuleAssemblyName = moduleAssemblyName;
                    return true;
                }

                public bool SetNoConfig(bool noConfig)
                {
                    // ??
                    return true;
                }

                public bool SetNoStandardLib(bool noStandardLib)
                {
                    this.NoStandardLib = noStandardLib;
                    return true;
                }

                public bool SetOptimize(bool optimize)
                {
                    this.Options.Optimize = optimize;
                    return true;
                }

                public bool SetOutputAssembly(string outputAssembly)
                {
                    // ?? looks to be output file in obj directory not binaries\debug directory
                    this.OutputFileName = Path.GetFileName(outputAssembly);
                    return true;
                }

                public bool SetPdbFile(string pdbFile)
                {
                    // ??
                    return true;
                }

                public bool SetPlatform(string platform)
                {
                    this.Options.Platform = platform;
                    return true;
                }

                public bool SetReferences(MSB.Framework.ITaskItem[] references)
                {
                    this.References = references ?? SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    return true;
                }

                public bool SetAnalyzers(MSB.Framework.ITaskItem[] analyzerReferences)
                {
                    this.AnalyzerReferences = analyzerReferences ?? SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    return true;
                }

                public bool SetAdditionalFiles(ITaskItem[] additionalFiles)
                {
                    this.AdditionalFiles = additionalFiles ?? SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    return true;
                }

                public bool SetResources(MSB.Framework.ITaskItem[] resources)
                {
                    // ??
                    return true;
                }

                public bool SetResponseFiles(MSB.Framework.ITaskItem[] responseFiles)
                {
                    // ??
                    return true;
                }

                public bool SetSources(MSB.Framework.ITaskItem[] sources)
                {
                    this.Sources = sources ?? SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    return true;
                }

                public bool SetTargetType(string targetType)
                {
                    OutputKind kind;
                    if (ProjectFile.TryGetOutputKind(targetType, out kind))
                    {
                        this.Options.OutputKind = kind;
                    }

                    return true;
                }

                public bool SetRuleSet(string ruleSetFile)
                {
                    this.Options.RuleSetFile = ruleSetFile;
                    return true;
                }

                public bool SetTreatWarningsAsErrors(bool treatWarningsAsErrors)
                {
                    this.Options.WarningsAsErrors = treatWarningsAsErrors;
                    return true;
                }

                public bool SetWarningLevel(int warningLevel)
                {
                    this.Options.WarningLevel = warningLevel;
                    return true;
                }

                public bool SetWarningsAsErrors(string warningsAsErrors)
                {
                    this.SetWarnings(warningsAsErrors, ReportDiagnostic.Error);
                    return true;
                }

                public bool SetWarningsNotAsErrors(string warningsNotAsErrors)
                {
                    this.SetWarnings(warningsNotAsErrors, ReportDiagnostic.Default);
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
            }
        }
    }
}

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
using Microsoft.CodeAnalysis.CSharp.Utilities;
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

            public CSharpProjectFile(CSharpProjectFileLoader loader, MSB.Evaluation.Project project, IMetadataService metadataService, IAnalyzerService analyzerService)
                : base(loader, project)
            {
                _metadataService = metadataService;
                _analyzerService = analyzerService;
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

                return new ProjectFileInfo(
                    outputPath,
                    assemblyName,
                    compilerInputs.CompilationOptions,
                    compilerInputs.ParseOptions,
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

                foreach (var ar in compilerInputs.AnalyzerReferences)
                {
                    var filePath = GetDocumentFilePath(ar);
                    args.Add("/a:\"" + filePath + "\"");
                }

                if (compilerInputs.NoStandardLib)
                {
                    args.Add("/nostdlib");
                }

                var commandLineParser = CSharpCommandLineParser.Default;
                var commandLineArgs = commandLineParser.Parse(args, executedProject.Directory, RuntimeEnvironment.GetRuntimeDirectory());

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
                internal CSharpParseOptions ParseOptions { get; private set; }
                internal CSharpCompilationOptions CompilationOptions { get; private set; }
                internal int CodePage { get; private set; }
                internal IEnumerable<MSB.Framework.ITaskItem> Sources { get; private set; }
                internal IEnumerable<MSB.Framework.ITaskItem> References { get; private set; }
                internal IEnumerable<MSB.Framework.ITaskItem> AnalyzerReferences { get; private set; }
                internal IEnumerable<MSB.Framework.ITaskItem> AdditionalFiles { get; private set; }
                internal IReadOnlyList<string> LibPaths { get; private set; }
                internal bool NoStandardLib { get; private set; }
                internal Dictionary<string, ReportDiagnostic> Warnings { get; }
                internal string OutputFileName { get; private set; }

                private static readonly CSharpParseOptions s_defaultParseOptions = new CSharpParseOptions(languageVersion: LanguageVersion.CSharp6, documentationMode: DocumentationMode.Parse);

                internal CSharpCompilerInputs(CSharpProjectFile projectFile)
                {
                    _projectFile = projectFile;
                    var projectDirectory = Path.GetDirectoryName(projectFile.FilePath);
                    var outputDirectory = projectFile.GetOutputDirectory();

                    this.ParseOptions = s_defaultParseOptions;
                    this.CompilationOptions = new CSharpCompilationOptions(
                        OutputKind.ConsoleApplication,
                        xmlReferenceResolver: new XmlFileResolver(projectDirectory),
                        sourceReferenceResolver: new SourceFileResolver(ImmutableArray<string>.Empty, projectDirectory),
                        metadataReferenceResolver: new AssemblyReferenceResolver(
                            new MetadataFileReferenceResolver(ImmutableArray<string>.Empty, projectDirectory),
                            MetadataFileReferenceProvider.Default),
                        strongNameProvider: new DesktopStrongNameProvider(ImmutableArray.Create(projectDirectory, outputDirectory)),
                        assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);
                    this.Warnings = new Dictionary<string, ReportDiagnostic>();
                    this.Sources = SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    this.References = SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    this.AnalyzerReferences = SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    this.AdditionalFiles = SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    this.LibPaths = SpecializedCollections.EmptyReadOnlyList<string>();
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
                    if (this.Warnings.Count > 0)
                    {
                        this.CompilationOptions = this.CompilationOptions.WithSpecificDiagnosticOptions(this.Warnings);
                    }

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
                    if (!string.IsNullOrEmpty(platformWith32BitPreference))
                    {
                        Platform platform;
                        if (Enum.TryParse<Platform>(platformWith32BitPreference, true, out platform))
                        {
                            if (platform == Platform.AnyCpu &&
                                this.CompilationOptions.OutputKind != OutputKind.DynamicallyLinkedLibrary &&
                                this.CompilationOptions.OutputKind != OutputKind.NetModule &&
                                this.CompilationOptions.OutputKind != OutputKind.WindowsRuntimeMetadata)
                            {
                                platform = Platform.AnyCpu32BitPreferred;
                            }

                            this.CompilationOptions = this.CompilationOptions.WithPlatform(platform);
                            return true;
                        }
                    }

                    return false;
                }

                public bool SetSubsystemVersion(string subsystemVersion)
                {
                    // we don't capture emit options
                    return true;
                }

                public bool SetApplicationConfiguration(string applicationConfiguration)
                {
                    if (!string.IsNullOrEmpty(applicationConfiguration))
                    {
                        var appConfigPath = FileUtilities.ResolveRelativePath(applicationConfiguration, Path.GetDirectoryName(_projectFile.FilePath));
                        try
                        {
                            using (var appConfigStream = new FileStream(appConfigPath, FileMode.Open, FileAccess.Read))
                            {
                                this.CompilationOptions = this.CompilationOptions.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.LoadFromXml(appConfigStream));
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }

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
                    this.CompilationOptions = this.CompilationOptions.WithAllowUnsafe(allowUnsafeBlocks);
                    return true;
                }

                public bool SetBaseAddress(string baseAddress)
                {
                    // we don't capture emit options
                    return true;
                }

                public bool SetCheckForOverflowUnderflow(bool checkForOverflowUnderflow)
                {
                    this.CompilationOptions = this.CompilationOptions.WithOverflowChecks(checkForOverflowUnderflow);
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
                    if (!string.IsNullOrEmpty(defineConstants))
                    {
                        IEnumerable<Diagnostic> diagnostics;
                        this.ParseOptions = this.ParseOptions.WithPreprocessorSymbols(CSharpCommandLineParser.ParseConditionalCompilationSymbols(defineConstants, out diagnostics));
                        return true;
                    }

                    return false;
                }

                private static readonly char[] s_preprocessorSymbolSeparators = new char[] { ';', ',' };

                public bool SetDelaySign(bool delaySignExplicitlySet, bool delaySign)
                {
                    this.CompilationOptions = this.CompilationOptions.WithDelaySign(delaySignExplicitlySet ? delaySign : (bool?)null);
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
                                this.Warnings["CS" + warningId.ToString("0000")] = reportStyle;
                            }
                            else
                            {
                                this.Warnings[warning] = reportStyle;
                            }
                        }
                    }
                }

                public bool SetDocumentationFile(string documentationFile)
                {
                    this.ParseOptions = this.ParseOptions.WithDocumentationMode(!string.IsNullOrEmpty(documentationFile) ? DocumentationMode.Diagnose : DocumentationMode.Parse);

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
                    if (!string.IsNullOrEmpty(keyContainer))
                    {
                        this.CompilationOptions = this.CompilationOptions.WithCryptoKeyContainer(keyContainer);
                    }

                    return true;
                }

                public bool SetKeyFile(string keyFile)
                {
                    if (!string.IsNullOrEmpty(keyFile))
                    {
                        var fullPath = FileUtilities.ResolveRelativePath(keyFile, Path.GetDirectoryName(_projectFile.FilePath));
                        this.CompilationOptions = this.CompilationOptions.WithCryptoKeyFile(fullPath);
                    }

                    return true;
                }

                public bool SetLangVersion(string langVersion)
                {
                    var languageVersion = CompilationOptionsConversion.GetLanguageVersion(langVersion);
                    if (languageVersion.HasValue)
                    {
                        this.ParseOptions = this.ParseOptions.WithLanguageVersion(languageVersion.Value);
                    }

                    return true;
                }

                public bool SetLinkResources(MSB.Framework.ITaskItem[] linkResources)
                {
                    // ??
                    return true;
                }

                public bool SetMainEntryPoint(string targetType, string mainEntryPoint)
                {
                    // TODO: targetType is redundant? Already has SetTargetType()?
                    if (!string.IsNullOrEmpty(mainEntryPoint))
                    {
                        this.CompilationOptions = this.CompilationOptions.WithMainTypeName(mainEntryPoint);
                    }

                    return true;
                }

                public bool SetModuleAssemblyName(string moduleAssemblyName)
                {
                    if (!string.IsNullOrEmpty(moduleAssemblyName))
                    {
                        this.CompilationOptions = this.CompilationOptions.WithModuleName(moduleAssemblyName);
                    }

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
                    this.CompilationOptions = this.CompilationOptions.WithOptimizationLevel(optimize ? OptimizationLevel.Release : OptimizationLevel.Debug);
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
                    if (!string.IsNullOrEmpty(platform))
                    {
                        Platform plat;
                        if (Enum.TryParse<Platform>(platform, ignoreCase: true, result: out plat))
                        {
                            this.CompilationOptions = this.CompilationOptions.WithPlatform(plat);
                            return true;
                        }
                    }

                    return false;
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
                    if (!string.IsNullOrEmpty(targetType) && ProjectFile.TryGetOutputKind(targetType, out kind))
                    {
                        this.CompilationOptions = this.CompilationOptions.WithOutputKind(kind);
                        if (this.CompilationOptions.Platform == Platform.AnyCpu32BitPreferred &&
                            (kind == OutputKind.DynamicallyLinkedLibrary || kind == OutputKind.NetModule || kind == OutputKind.WindowsRuntimeMetadata))
                        {
                            this.CompilationOptions = this.CompilationOptions.WithPlatform(Platform.AnyCpu);
                        }

                        return true;
                    }

                    return false;
                }

                public bool SetRuleSet(string ruleSetFile)
                {
                    // Get options from the ruleset file, if any.
                    if (!string.IsNullOrEmpty(ruleSetFile))
                    {
                        var fullPath = FileUtilities.ResolveRelativePath(ruleSetFile, Path.GetDirectoryName(_projectFile.FilePath));

                        Dictionary<string, ReportDiagnostic> specificDiagnosticOptions;
                        var generalDiagnosticOption = RuleSet.GetDiagnosticOptionsFromRulesetFile(fullPath, out specificDiagnosticOptions);
                        this.CompilationOptions = this.CompilationOptions.WithGeneralDiagnosticOption(generalDiagnosticOption);
                        this.Warnings.AddRange(specificDiagnosticOptions);
                    }

                    return true;
                }

                public bool SetTreatWarningsAsErrors(bool treatWarningsAsErrors)
                {
                    this.CompilationOptions = this.CompilationOptions.WithGeneralDiagnosticOption(treatWarningsAsErrors ? ReportDiagnostic.Error : ReportDiagnostic.Default);
                    return true;
                }

                public bool SetWarningLevel(int warningLevel)
                {
                    this.CompilationOptions = this.CompilationOptions.WithWarningLevel(warningLevel);
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

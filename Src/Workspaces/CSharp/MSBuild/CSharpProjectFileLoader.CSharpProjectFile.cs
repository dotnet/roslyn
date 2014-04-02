// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class CSharpProjectFileLoader : ProjectFileLoader
    {
        private class CSharpProjectFile : ProjectFile
        {
            public CSharpProjectFile(CSharpProjectFileLoader loader, MSB.Evaluation.Project project)
                : base(loader, project)
            {
            }

            public override SourceCodeKind GetSourceCodeKind(string documentFileName)
            {
                return documentFileName.EndsWith(".csx", System.StringComparison.OrdinalIgnoreCase)
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

                var result = await this.BuildAsync("Csc", compilerInputs, cancellationToken).ConfigureAwait(false);
                var executedProject = result.Instance;

                if (!compilerInputs.Initialized)
                {
                    // if msbuild didn't reach the CSC task for some reason, attempt to initialize using the variables that were defined so far.
                    this.InitializeFromModel(compilerInputs, executedProject);
                }

                return CreateProjectFileInfo(compilerInputs, executedProject);
            }

            protected override IEnumerable<ProjectFileReference> GetProjectReferences(ProjectInstance executedProject)
            {
                return this.GetProjectReferencesCore(executedProject);
            }

            private IEnumerable<ProjectFileReference> GetProjectReferencesCore(ProjectInstance executedProject)
            {
                foreach (var projectReference in GetProjectReferenceItems(executedProject))
                {
                    Guid guid;
                    if (!Guid.TryParse(projectReference.GetMetadataValue("Project"), out guid))
                    {
                        continue;
                    }

                    var filePath = projectReference.EvaluatedInclude;
                    var aliases = GetAliases(projectReference);

                    yield return new ProjectFileReference(guid, filePath, aliases);
                }
            }

            private ProjectFileInfo CreateProjectFileInfo(CSharpCompilerInputs compilerInputs, MSB.Execution.ProjectInstance executedProject)
            {
                string projectDirectory = executedProject.Directory;
                if (!projectDirectory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    projectDirectory += Path.DirectorySeparatorChar;
                }

                var docs = compilerInputs.Sources
                       .Where(s => !Path.GetFileName(s.ItemSpec).StartsWith("TemporaryGeneratedFile_"))
                       .Select(s => MakeDocumentFileInfo(projectDirectory, s))
                       .ToImmutableList();

                var metadataRefs = compilerInputs.References.SelectMany(r => MakeMetadataInfo(r));

                var analyzerRefs = compilerInputs.AnalyzerReferences.Select(r => new AnalyzerFileReference(GetDocumentFilePath(r)));

                if (!compilerInputs.NoStandardLib)
                {
                    var mscorlibPath = typeof(object).Assembly.Location;
                    metadataRefs = metadataRefs.Concat(new[] { new MetadataInfo(mscorlibPath) });
                }

                return new ProjectFileInfo(
                    this.Guid,
                    this.GetTargetPath(),
                    this.GetAssemblyName(),
                    compilerInputs.CompilationOptions,
                    compilerInputs.ParseOptions,
                    docs,
                    this.GetProjectReferences(executedProject),
                    metadataRefs,
                    analyzerRefs,
                    appConfigPath: compilerInputs.AppConfigPath);
            }

            private DocumentFileInfo MakeDocumentFileInfo(string projectDirectory, MSB.Framework.ITaskItem item)
            {
                var filePath = GetDocumentFilePath(item);
                var logicalPath = GetDocumentLogicalPath(item, projectDirectory);
                var isLinked = IsDocumentLinked(item);
                var isGenerated = IsDocumentGenerated(item);
                return new DocumentFileInfo(filePath, logicalPath, isLinked, isGenerated);
            }

            private IEnumerable<MetadataInfo> MakeMetadataInfo(MSB.Framework.ITaskItem item)
            {
                var filePath = GetDocumentFilePath(item);

                var aliases = GetAliases(item);
                return new MetadataInfo[] { new MetadataInfo(filePath, new MetadataReferenceProperties(aliases: aliases)) };
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
                compilerInputs.SetSources(this.GetDocumentsFromModel(executedProject).ToArray());

                string errorMessage;
                int errorCode;
                compilerInputs.EndInitialization(out errorMessage, out errorCode);
            }

            private class CSharpCompilerInputs : MSB.Tasks.Hosting.ICscHostObject4
            {
                private readonly CSharpProjectFile projectFile;

                internal bool Initialized { get; private set; }
                internal CSharpParseOptions ParseOptions { get; private set; }
                internal CSharpCompilationOptions CompilationOptions { get; private set; }
                internal string AppConfigPath { get; private set; }
                internal IEnumerable<MSB.Framework.ITaskItem> Sources { get; private set; }
                internal IEnumerable<MSB.Framework.ITaskItem> References { get; private set; }
                internal IEnumerable<MSB.Framework.ITaskItem> AnalyzerReferences { get; private set; }
                internal bool NoStandardLib { get; private set; }
                internal Dictionary<string, ReportDiagnostic> Warnings { get; private set; }

                private static readonly CSharpParseOptions defaultParseOptions = new CSharpParseOptions(languageVersion: LanguageVersion.CSharp6, documentationMode: DocumentationMode.Parse);

                internal CSharpCompilerInputs(CSharpProjectFile projectFile)
                {
                    this.projectFile = projectFile;
                    var projectDirectory = Path.GetDirectoryName(projectFile.FilePath);
                    var outputDirectory = projectFile.GetTargetPath();
                    if (!string.IsNullOrEmpty(outputDirectory) && Path.IsPathRooted(outputDirectory))
                    {
                        outputDirectory = Path.GetDirectoryName(outputDirectory);
                    }
                    else
                    {
                        outputDirectory = projectDirectory;
                    }

                    this.ParseOptions = defaultParseOptions;
                    this.CompilationOptions = new CSharpCompilationOptions(
                        OutputKind.ConsoleApplication,
                        debugInformationKind: DebugInformationKind.None,
                        xmlReferenceResolver: new XmlFileResolver(projectDirectory),
                        sourceReferenceResolver: new SourceFileResolver(ImmutableArray<string>.Empty, projectDirectory),
                        metadataReferenceResolver: new MetadataFileReferenceResolver(ImmutableArray<string>.Empty, projectDirectory),
                        metadataReferenceProvider: MetadataFileReferenceProvider.Default,
                        strongNameProvider: new DesktopStrongNameProvider(ImmutableArray.Create(projectDirectory, outputDirectory)),
                        assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);
                    this.Warnings = new Dictionary<string, ReportDiagnostic>();
                    this.Sources = SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    this.References = SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                    this.AnalyzerReferences = SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
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
                    this.CompilationOptions = this.CompilationOptions.WithHighEntropyVirtualAddressSpace(highEntropyVA);
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
                    SubsystemVersion parsedVersion;

                    if (!string.IsNullOrEmpty(subsystemVersion))
                    {
                        if (SubsystemVersion.TryParse(subsystemVersion, out parsedVersion))
                        {
                            this.CompilationOptions = this.CompilationOptions.WithSubsystemVersion(parsedVersion);
                        }

                        return true;
                    }

                    return false;
                }

                public bool SetApplicationConfiguration(string applicationConfiguration)
                {
                    if (!string.IsNullOrEmpty(applicationConfiguration))
                    {
                        this.AppConfigPath = FileUtilities.ResolveRelativePath(applicationConfiguration, Path.GetDirectoryName(this.projectFile.FilePath));
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
                    // ???
                    return true;
                }

                public bool SetAllowUnsafeBlocks(bool allowUnsafeBlocks)
                {
                    this.CompilationOptions = this.CompilationOptions.WithAllowUnsafe(allowUnsafeBlocks);
                    return true;
                }

                public bool SetBaseAddress(string baseAddress)
                {
                    ulong addr;
                    if (ulong.TryParse(baseAddress, out addr))
                    {
                        this.CompilationOptions = this.CompilationOptions.WithBaseAddress(addr);
                        return true;
                    }

                    return false;
                }

                public bool SetCheckForOverflowUnderflow(bool checkForOverflowUnderflow)
                {
                    this.CompilationOptions = this.CompilationOptions.WithOverflowChecks(checkForOverflowUnderflow);
                    return true;
                }

                public bool SetCodePage(int codePage)
                {
                    // ??
                    return true;
                }

                public bool SetDebugType(string debugType)
                {
                    if (!string.IsNullOrEmpty(debugType))
                    {
                        DebugInformationKind kind;
                        if (Enum.TryParse<DebugInformationKind>(debugType, ignoreCase: true, result: out kind))
                        {
                            this.CompilationOptions = this.CompilationOptions.WithDebugInformationKind(kind);
                            return true;
                        }
                    }

                    return false;
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

                private static readonly char[] preprocessorSymbolSeparators = new char[] { ';', ',' };

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
                        foreach (var warning in warnings.Split(preprocessorSymbolSeparators, StringSplitOptions.None))
                        {
                            int warningId;
                            if (int.TryParse(warning, out warningId))
                            {
                                this.Warnings["CS" + warningId.ToString("0000")] = reportStyle;
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
                    this.CompilationOptions = this.CompilationOptions.WithFileAlignment(fileAlignment);
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
                        var fullPath = FileUtilities.ResolveRelativePath(keyFile, Path.GetDirectoryName(this.projectFile.FilePath));
                        this.CompilationOptions = this.CompilationOptions.WithCryptoKeyFile(fullPath);
                    }

                    return true;
                }

                public bool SetLangVersion(string langVersion)
                {
                    LanguageVersion? languageVersion = CompilationOptionsConversion.GetLanguageVersion(langVersion);
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
                    this.CompilationOptions = this.CompilationOptions.WithOptimizations(optimize);
                    return true;
                }

                public bool SetOutputAssembly(string outputAssembly)
                {
                    // ?? looks to be output file in obj directory not binaries\debug directory
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
                        var fullPath = FileUtilities.ResolveRelativePath(ruleSetFile, Path.GetDirectoryName(this.projectFile.FilePath));

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
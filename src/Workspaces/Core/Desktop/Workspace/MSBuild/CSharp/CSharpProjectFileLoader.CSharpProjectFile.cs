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
            public CSharpProjectFile(CSharpProjectFileLoader loader, MSB.Evaluation.Project project)
                : base(loader, project)
            {
            }

            public override SourceCodeKind GetSourceCodeKind(string documentFileName)
            {
                // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
                //return documentFileName.EndsWith(".csx", StringComparison.OrdinalIgnoreCase)
                //    ? SourceCodeKind.Script
                //    : SourceCodeKind.Regular;
                return SourceCodeKind.Regular;
            }

            public override string GetDocumentExtension(SourceCodeKind sourceCodeKind)
            {
                // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
                //return (sourceCodeKind != SourceCodeKind.Script) ? ".cs" : ".csx";
                return ".cs";
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

                var additionalDocs = compilerInputs.AdditionalSources
                        .Select(s => MakeDocumentFileInfo(projectDirectory, s))
                        .ToImmutableArray();

                var outputPath = Path.Combine(this.GetOutputDirectory(), compilerInputs.OutputFileName);
                var assemblyName = this.GetAssemblyName();

                return new ProjectFileInfo(
                    outputPath,
                    assemblyName,
                    compilerInputs.CommandLineArgs,
                    docs,
                    additionalDocs,
                    this.GetProjectReferences(executedProject));
            }

            private DocumentFileInfo MakeDocumentFileInfo(string projectDirectory, ITaskItem item)
            {
                var filePath = GetDocumentFilePath(item);
                var logicalPath = GetDocumentLogicalPath(item, projectDirectory);
                var isLinked = IsDocumentLinked(item);
                var isGenerated = IsDocumentGenerated(item);
                return new DocumentFileInfo(filePath, logicalPath, isLinked, isGenerated);
            }

            private ImmutableArray<string> GetAliases(ITaskItem item)
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
                compilerInputs.SetFeatures(this.ReadPropertyString(executedProject, "Features"));

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
                internal string ProjectDirectory { get; }
                internal string OutputDirectory { get; }
                internal string OutputFileName { get; private set; }
                internal List<string> CommandLineArgs { get; }
                internal IEnumerable<ITaskItem> Sources { get; private set; }
                internal IEnumerable<ITaskItem> AdditionalSources { get; private set; }

                private bool _emitDebugInfo;
                private string _debugType;

                private string _targetType;
                private string _platform;

                internal CSharpCompilerInputs(CSharpProjectFile projectFile)
                {
                    _projectFile = projectFile;
                    this.CommandLineArgs = new List<string>();
                    this.Sources = SpecializedCollections.EmptyEnumerable<ITaskItem>();
                    this.AdditionalSources = SpecializedCollections.EmptyEnumerable<ITaskItem>();
                    this.ProjectDirectory = Path.GetDirectoryName(projectFile.FilePath);
                    this.OutputDirectory = projectFile.GetOutputDirectory();
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

                    if (_emitDebugInfo)
                    {
                        if (string.Equals(_debugType, "none", StringComparison.OrdinalIgnoreCase))
                        {
                            // does this mean not debug???
                            this.CommandLineArgs.Add("/debug");
                        }
                        else if (string.Equals(_debugType, "pdbonly", StringComparison.OrdinalIgnoreCase))
                        {
                            this.CommandLineArgs.Add("/debug:pdbonly");
                        }
                        else if (string.Equals(_debugType, "full", StringComparison.OrdinalIgnoreCase))
                        {
                            this.CommandLineArgs.Add("/debug:full");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(_platform))
                    {
                        if (string.Equals("anycpu32bitpreferred", _platform, StringComparison.InvariantCultureIgnoreCase)
                            && (string.Equals("library", _targetType, StringComparison.InvariantCultureIgnoreCase)
                                || string.Equals("module", _targetType, StringComparison.InvariantCultureIgnoreCase)
                                || string.Equals("winmdobj", _targetType, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            _platform = "anycpu";
                        }

                        this.CommandLineArgs.Add("/platform:" + _platform);
                    }

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

                public bool SetHighEntropyVA(bool highEntropyVA)
                {
                    if (highEntropyVA)
                    {
                        this.CommandLineArgs.Add("/highentropyva");
                    }

                    return true;
                }

                public bool SetSubsystemVersion(string subsystemVersion)
                {
                    if (!string.IsNullOrWhiteSpace(subsystemVersion))
                    {
                        this.CommandLineArgs.Add("/subsystemversion:" + subsystemVersion);
                    }

                    return true;
                }

                public bool SetApplicationConfiguration(string applicationConfiguration)
                {
                    if (!string.IsNullOrWhiteSpace(applicationConfiguration))
                    {
                        this.CommandLineArgs.Add("/appconfig:" + applicationConfiguration);
                    }

                    return true;
                }

                public bool SetWin32Manifest(string win32Manifest)
                {
                    if (!string.IsNullOrWhiteSpace(win32Manifest))
                    {
                        this.CommandLineArgs.Add("/win32manifest:\"" + win32Manifest + "\"");
                    }

                    return true;
                }

                public bool SetAddModules(string[] addModules)
                {
                    if (addModules != null && addModules.Length > 0)
                    {
                        this.CommandLineArgs.Add("/addmodule:\"" + string.Join(";", addModules) + "\"");
                    }

                    return true;
                }

                public bool SetAdditionalLibPaths(string[] additionalLibPaths)
                {
                    if (additionalLibPaths != null && additionalLibPaths.Length > 0)
                    {
                        this.CommandLineArgs.Add("/lib:\"" + string.Join(";", additionalLibPaths) + "\"");
                    }
                    return true;
                }

                public bool SetAllowUnsafeBlocks(bool allowUnsafeBlocks)
                {
                    if (allowUnsafeBlocks)
                    {
                        this.CommandLineArgs.Add("/unsafe");
                    }

                    return true;
                }

                public bool SetBaseAddress(string baseAddress)
                {
                    if (!string.IsNullOrWhiteSpace(baseAddress))
                    {
                        this.CommandLineArgs.Add("/baseaddress:" + baseAddress);
                    }

                    return true;
                }

                public bool SetCheckForOverflowUnderflow(bool checkForOverflowUnderflow)
                {
                    if (checkForOverflowUnderflow)
                    {
                        this.CommandLineArgs.Add("/checked");
                    }

                    return true;
                }

                public bool SetCodePage(int codePage)
                {
                    if (codePage != 0)
                    {
                        this.CommandLineArgs.Add("/codepage:" + codePage);
                    }

                    return true;
                }

                public bool SetDebugType(string debugType)
                {
                    _debugType = debugType;
                    return true;
                }

                public bool SetDefineConstants(string defineConstants)
                {
                    if (!string.IsNullOrWhiteSpace(defineConstants))
                    {
                        this.CommandLineArgs.Add("/define:" + defineConstants);
                    }

                    return true;
                }

                private static readonly char[] s_preprocessorSymbolSeparators = new char[] { ';', ',' };

                public bool SetFeatures(string features)
                {
                    foreach (var feature in CompilerOptionParseUtilities.ParseFeatureFromMSBuild(features))
                    {
                        this.CommandLineArgs.Add($"/features:{feature}");
                    }

                    return true;
                }

                public bool SetDelaySign(bool delaySignExplicitlySet, bool delaySign)
                {
                    if (delaySignExplicitlySet)
                    {
                        this.CommandLineArgs.Add("/delaysign" + (delaySign ? "+" : "-"));
                    }

                    return true;
                }

                public bool SetDisabledWarnings(string disabledWarnings)
                {
                    if (!string.IsNullOrWhiteSpace(disabledWarnings))
                    {
                        this.CommandLineArgs.Add("/nowarn:" + disabledWarnings);
                    }

                    return true;
                }

                public bool SetDocumentationFile(string documentationFile)
                {
                    if (!string.IsNullOrWhiteSpace(documentationFile))
                    {
                        this.CommandLineArgs.Add("/doc:\"" + documentationFile + "\"");
                    }

                    return true;
                }

                public bool SetEmitDebugInformation(bool emitDebugInformation)
                {
                    _emitDebugInfo = emitDebugInformation;
                    return true;
                }

                public bool SetErrorReport(string errorReport)
                {
                    if (!string.IsNullOrWhiteSpace(errorReport))
                    {
                        this.CommandLineArgs.Add("/errorreport:" + errorReport.ToLower());
                    }

                    return true;
                }

                public bool SetFileAlignment(int fileAlignment)
                {
                    this.CommandLineArgs.Add("/filealign:" + fileAlignment);
                    return true;
                }

                public bool SetGenerateFullPaths(bool generateFullPaths)
                {
                    if (generateFullPaths)
                    {
                        this.CommandLineArgs.Add("/fullpaths");
                    }

                    return true;
                }

                public bool SetKeyContainer(string keyContainer)
                {
                    if (!string.IsNullOrWhiteSpace(keyContainer))
                    {
                        this.CommandLineArgs.Add("/keycontainer:\"" + keyContainer + "\"");
                    }

                    return true;
                }

                public bool SetKeyFile(string keyFile)
                {
                    if (!string.IsNullOrWhiteSpace(keyFile))
                    {
                        // keyFile = FileUtilities.ResolveRelativePath(keyFile, this.ProjectDirectory);
                        this.CommandLineArgs.Add("/keyfile:\"" + keyFile + "\"");
                    }

                    return true;
                }

                public bool SetLangVersion(string langVersion)
                {
                    if (!string.IsNullOrWhiteSpace(langVersion))
                    {
                        this.CommandLineArgs.Add("/langversion:" + langVersion);
                    }

                    return true;
                }

                public bool SetLinkResources(ITaskItem[] linkResources)
                {
                    if (linkResources != null && linkResources.Length > 0)
                    {
                        foreach (var lr in linkResources)
                        {
                            this.CommandLineArgs.Add("/linkresource:\"" + _projectFile.GetDocumentFilePath(lr) + "\"");
                        }
                    }

                    return true;
                }

                public bool SetMainEntryPoint(string targetType, string mainEntryPoint)
                {
                    if (!string.IsNullOrWhiteSpace(mainEntryPoint))
                    {
                        this.CommandLineArgs.Add("/main:\"" + mainEntryPoint + "\"");
                    }

                    return true;
                }

                public bool SetModuleAssemblyName(string moduleAssemblyName)
                {
                    if (!string.IsNullOrWhiteSpace(moduleAssemblyName))
                    {
                        this.CommandLineArgs.Add("/moduleassemblyname:\"" + moduleAssemblyName + "\"");
                    }

                    return true;
                }

                public bool SetNoConfig(bool noConfig)
                {
                    if (noConfig)
                    {
                        this.CommandLineArgs.Add("/noconfig");
                    }

                    return true;
                }

                public bool SetNoStandardLib(bool noStandardLib)
                {
                    if (noStandardLib)
                    {
                        this.CommandLineArgs.Add("/nostdlib");
                    }

                    return true;
                }

                public bool SetOptimize(bool optimize)
                {
                    if (optimize)
                    {
                        this.CommandLineArgs.Add("/optimize");
                    }

                    return true;
                }

                public bool SetOutputAssembly(string outputAssembly)
                {
                    // ?? looks to be output file in obj directory not binaries\debug directory
                    this.OutputFileName = Path.GetFileName(outputAssembly);
                    this.CommandLineArgs.Add("/out:\"" + outputAssembly + "\"");
                    return true;
                }

                public bool SetPdbFile(string pdbFile)
                {
                    if (!string.IsNullOrWhiteSpace(pdbFile))
                    {
                        this.CommandLineArgs.Add($"/pdb:\"{pdbFile}\"");
                    }

                    return true;
                }

                public bool SetPlatform(string platform)
                {
                    _platform = platform;
                    return true;
                }

                public bool SetPlatformWith32BitPreference(string platformWith32BitPreference)
                {
                    SetPlatform(platformWith32BitPreference);
                    return true;
                }

                public bool SetReferences(ITaskItem[] references)
                {
                    if (references != null)
                    {
                        foreach (var mr in references)
                        {
                            if (!_projectFile.IsProjectReferenceOutputAssembly(mr))
                            {
                                var filePath = _projectFile.GetDocumentFilePath(mr);

                                var aliases = _projectFile.GetAliases(mr);
                                if (aliases.IsDefaultOrEmpty)
                                {
                                    this.CommandLineArgs.Add("/reference:\"" + filePath + "\"");
                                }
                                else
                                {
                                    foreach (var alias in aliases)
                                    {
                                        this.CommandLineArgs.Add("/reference:" + alias + "=\"" + filePath + "\"");
                                    }
                                }
                            }
                        }
                    }

                    return true;
                }

                public bool SetAnalyzers(ITaskItem[] analyzerReferences)
                {
                    if (analyzerReferences != null)
                    {
                        foreach (var ar in analyzerReferences)
                        {
                            var filePath = _projectFile.GetDocumentFilePath(ar);
                            this.CommandLineArgs.Add("/analyzer:\"" + filePath + "\"");
                        }
                    }

                    return true;
                }

                public bool SetAdditionalFiles(ITaskItem[] additionalFiles)
                {
                    if (additionalFiles != null && additionalFiles.Length > 0)
                    {
                        this.AdditionalSources = additionalFiles;
                    }

                    return true;
                }

                public bool SetResources(ITaskItem[] resources)
                {
                    if (resources != null && resources.Length > 0)
                    {
                        foreach (var r in resources)
                        {
                            this.CommandLineArgs.Add("/resource:\"" + _projectFile.GetDocumentFilePath(r) + "\"");
                        }
                    }

                    return true;
                }

                public bool SetResponseFiles(ITaskItem[] responseFiles)
                {
                    if (responseFiles != null && responseFiles.Length > 0)
                    {
                        foreach (var rf in responseFiles)
                        {
                            this.CommandLineArgs.Add("@\"" + _projectFile.GetDocumentFilePath(rf) + "\"");
                        }
                    }

                    return true;
                }

                public bool SetSources(ITaskItem[] sources)
                {
                    if (sources != null && sources.Length > 0)
                    {
                        this.Sources = sources;
                    }

                    return true;
                }

                public bool SetTargetType(string targetType)
                {
                    if (!string.IsNullOrWhiteSpace(targetType))
                    {
                        _targetType = targetType.ToLower();
                        this.CommandLineArgs.Add("/target:" + _targetType);
                    }

                    return true;
                }

                public bool SetRuleSet(string ruleSetFile)
                {
                    if (!string.IsNullOrWhiteSpace(ruleSetFile))
                    {
                        this.CommandLineArgs.Add("/ruleset:\"" + ruleSetFile + "\"");
                    }

                    return true;
                }

                public bool SetTreatWarningsAsErrors(bool treatWarningsAsErrors)
                {
                    if (treatWarningsAsErrors)
                    {
                        this.CommandLineArgs.Add("/warningaserror");
                    }

                    return true;
                }

                public bool SetWarningLevel(int warningLevel)
                {
                    this.CommandLineArgs.Add("/warn:" + warningLevel);
                    return true;
                }

                public bool SetWarningsAsErrors(string warningsAsErrors)
                {
                    if (!string.IsNullOrWhiteSpace(warningsAsErrors))
                    {
                        this.CommandLineArgs.Add("/warnaserror+:" + warningsAsErrors);
                    }

                    return true;
                }

                public bool SetWarningsNotAsErrors(string warningsNotAsErrors)
                {
                    if (!string.IsNullOrWhiteSpace(warningsNotAsErrors))
                    {
                        this.CommandLineArgs.Add("/warnaserror-:" + warningsNotAsErrors);
                    }

                    return true;
                }

                public bool SetWin32Icon(string win32Icon)
                {
                    if (!string.IsNullOrWhiteSpace(win32Icon))
                    {
                        this.CommandLineArgs.Add("/win32icon:\"" + win32Icon + "\"");
                    }

                    return true;
                }

                public bool SetWin32Resource(string win32Resource)
                {
                    if (!string.IsNullOrWhiteSpace(win32Resource))
                    {
                        this.CommandLineArgs.Add("/win32res:\"" + win32Resource + "\"");
                    }

                    return true;
                }
            }
        }
    }
}

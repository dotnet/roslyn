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
        public override string Language
        {
            get { return LanguageNames.VisualBasic; }
        }

        internal VisualBasicProjectFileLoader()
        {
        }

        protected override ProjectFile CreateProjectFile(MSB.Evaluation.Project loadedProject)
        {
            return new VisualBasicProjectFile(this, loadedProject);
        }

        internal class VisualBasicProjectFile : ProjectFile
        {
            public VisualBasicProjectFile(VisualBasicProjectFileLoader loader, MSB.Evaluation.Project loadedProject) : base(loader, loadedProject)
            {
            }

            public override SourceCodeKind GetSourceCodeKind(string documentFileName)
            {
                // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
                //return documentFileName.EndsWith(".vbx", StringComparison.OrdinalIgnoreCase)
                //    ? SourceCodeKind.Script
                //    : SourceCodeKind.Regular;
                return SourceCodeKind.Regular;
            }

            public override string GetDocumentExtension(SourceCodeKind sourceCodeKind)
            {
                // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
                //return (sourceCodeKind != SourceCodeKind.Script) ? ".vb" : ".vbx";
                return ".vb";
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
                string outputPath = Path.Combine(this.GetOutputDirectory(), compilerInputs.OutputFileName);
                string assemblyName = this.GetAssemblyName();

                return new ProjectFileInfo(
                    outputPath, 
                    assemblyName, 
                    compilerInputs.CommandLineArgs,
                    this.GetDocuments(compilerInputs.Sources, executedProject),
                    this.GetDocuments(compilerInputs.AdditionalFiles, executedProject), 
                    base.GetProjectReferences(executedProject));
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
                compilerInputs.SetFeatures(base.ReadPropertyString(executedProject, "Features"));
                compilerInputs.SetDelaySign(base.ReadPropertyBool(executedProject, "DelaySign"));
                compilerInputs.SetDisabledWarnings(base.ReadPropertyString(executedProject, "NoWarn"));
                compilerInputs.SetDocumentationFile(base.GetItemString(executedProject, "DocFileItem"));
                compilerInputs.SetErrorReport(base.ReadPropertyString(executedProject, "ErrorReport"));
                compilerInputs.SetFileAlignment(base.ReadPropertyInt(executedProject, "FileAlignment"));
                compilerInputs.SetGenerateDocumentation(base.ReadPropertyBool(executedProject, "GenerateDocumentation"));
                compilerInputs.SetHighEntropyVA(base.ReadPropertyBool(executedProject, "HighEntropyVA"));
                compilerInputs.SetFeatures(base.ReadPropertyString(executedProject, "Features"));

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
                , IAnalyzerHostObject
#endif
            {
                private readonly VisualBasicProjectFile _projectFile;
                private bool _initialized;
                private string _projectDirectory;
                private string _outputDirectory;
                private List<string> _commandLineArgs;
                private IEnumerable<ITaskItem> _sources;
                private IEnumerable<ITaskItem> _additionalFiles;

                private string _outputFileName;
                private bool _emitDocComments;
                private string _docCommentFile;
                private string _targetType;
                private string _platform;

                public VisualBasicCompilerInputs(VisualBasicProjectFile projectFile)
                {
                    _projectFile = projectFile;
                    _commandLineArgs = new List<string>();
                    _sources = SpecializedCollections.EmptyEnumerable<ITaskItem>();
                    _additionalFiles = SpecializedCollections.EmptyEnumerable<ITaskItem>(); ;
                    _projectDirectory = Path.GetDirectoryName(projectFile.FilePath);
                    _outputDirectory = projectFile.GetOutputDirectory();
                }

                public bool Initialized
                {
                    get { return _initialized; }
                }

                public List<string> CommandLineArgs
                {
                    get { return _commandLineArgs; }
                }

                public IEnumerable<ITaskItem> Sources
                {
                    get { return _sources; }
                }

                public IEnumerable<ITaskItem> AdditionalFiles
                {
                    get { return _additionalFiles; }
                }

                public string OutputFileName
                {
                    get { return _outputFileName; }
                }

                public string OutputDirectory
                {
                    get { return _outputDirectory; }
                }

                public string ProjectDirectory
                {
                    get { return _projectDirectory; }
                }

                public void BeginInitialization()
                {
                }

                public bool Compile()
                {
                    return false;
                }

                public int CompileAsync(out IntPtr buildSucceededEvent, out IntPtr buildFailedEvent)
                {
                    buildSucceededEvent = IntPtr.Zero;
                    buildFailedEvent = IntPtr.Zero;
                    return 0;
                }

                public bool Compile1()
                {
                    return false;
                }

                bool IVbcHostObjectFreeThreaded.Compile()
                {
                    return Compile1();
                }

                public int EndCompile(bool buildSuccess)
                {
                    return 0;
                }

                public IVbcHostObjectFreeThreaded GetFreeThreadedHostObject()
                {
                    return null;
                }

                public void EndInitialization()
                {
                    _initialized = true;

                    if (_emitDocComments)
                    {
                        if (!string.IsNullOrWhiteSpace(_docCommentFile))
                        {
                            _commandLineArgs.Add("/doc:\"" + _docCommentFile + "\"");
                        }
                        else
                        {
                            _commandLineArgs.Add("/doc");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(_targetType))
                    {
                        this.CommandLineArgs.Add("/target:" + _targetType);
                    }

                    if (!string.IsNullOrWhiteSpace(_platform))
                    {
                        if (string.Equals("anycpu32bitpreferred", _platform, StringComparison.InvariantCultureIgnoreCase)
                            && (string.Equals("library", _targetType, StringComparison.InvariantCultureIgnoreCase)
                                || string.Equals("module", _targetType, StringComparison.InvariantCultureIgnoreCase)
                                || string.Equals("winmdobj", _targetType, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            _platform = "AnyCpu";
                        }

                        this.CommandLineArgs.Add("/platform:" + _platform);
                    }
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
                    if (additionalLibPaths != null && additionalLibPaths.Length > 0)
                    {
                        _commandLineArgs.Add("/libpath:\"" + string.Join(";", additionalLibPaths) + "\"");
                    }

                    return true;
                }

                public bool SetAddModules(string[] addModules)
                {
                    if (addModules != null && addModules.Length > 0)
                    {
                        _commandLineArgs.Add("/addmodules:\"" + string.Join(";", addModules) + "\"");
                    }

                    return true;
                }

                public bool SetBaseAddress(string targetType, string baseAddress)
                {
                    SetTargetType(targetType);

                    if (!string.IsNullOrWhiteSpace(baseAddress))
                    {
                        _commandLineArgs.Add("/baseaddress:" + baseAddress);
                    }

                    return true;
                }

                public bool SetCodePage(int codePage)
                {
                    if (codePage != 0)
                    {
                        _commandLineArgs.Add("/codepage:" + codePage);
                    }

                    return true;
                }

                public bool SetDebugType(bool emitDebugInformation, string debugType)
                {
                    if (emitDebugInformation)
                    {
                        if (string.Equals(debugType, "none", StringComparison.OrdinalIgnoreCase))
                        {
                            // ?? 
                            this.CommandLineArgs.Add("/debug");
                            return true;
                        }
                        else if (string.Equals(debugType, "pdbonly", StringComparison.OrdinalIgnoreCase))
                        {
                            this.CommandLineArgs.Add("/debug:pdbonly");
                            return true;
                        }
                        else if (string.Equals(debugType, "full", StringComparison.OrdinalIgnoreCase))
                        {
                            this.CommandLineArgs.Add("/debug:full");
                            return true;
                        }
                    }

                    return false;
                }

                public bool SetDefineConstants(string defineConstants)
                {
                    if (!string.IsNullOrWhiteSpace(defineConstants))
                    {
                        _commandLineArgs.Add("/define:" + defineConstants);
                    }

                    return true;
                }

                public bool SetFeatures(string features)
                {
                    foreach (var feature in CompilerOptionParseUtilities.ParseFeatureFromMSBuild(features))
                    {
                        this.CommandLineArgs.Add($"/features:{feature}");
                    }

                    return true;
                }

                public bool SetDelaySign(bool delaySign)
                {
                    if (delaySign)
                    {
                        _commandLineArgs.Add("/delaysign");
                    }

                    return true;
                }

                public bool SetDisabledWarnings(string disabledWarnings)
                {
                    if (!string.IsNullOrWhiteSpace(disabledWarnings))
                    {
                        _commandLineArgs.Add("/nowarn:" + disabledWarnings);
                    }

                    return true;
                }

                public bool SetDocumentationFile(string documentationFile)
                {
                    if (!string.IsNullOrWhiteSpace(documentationFile))
                    {
                        _emitDocComments = true;
                        _docCommentFile = documentationFile;
                    }

                    return true;
                }

                public bool SetErrorReport(string errorReport)
                {
                    if (!string.IsNullOrWhiteSpace(errorReport))
                    {
                        _commandLineArgs.Add("/errorreport:" + errorReport.ToLower());
                    }

                    return true;
                }

                public bool SetFileAlignment(int fileAlignment)
                {
                    _commandLineArgs.Add("/filealign:" + fileAlignment);
                    return true;
                }

                public bool SetGenerateDocumentation(bool generateDocumentation)
                {
                    if (generateDocumentation)
                    {
                        _emitDocComments = true;
                    }

                    return true;
                }

                public bool SetImports(ITaskItem[] importsList)
                {
                    if (importsList != null)
                    {
                        _commandLineArgs.Add("/imports:" + string.Join(",", importsList.Select(item => item.ItemSpec.Trim())));
                    }

                    return true;
                }

                public bool SetKeyContainer(string keyContainer)
                {
                    if (!string.IsNullOrWhiteSpace(keyContainer))
                    {
                        _commandLineArgs.Add("/keycontainer:\"" + keyContainer + "\"");
                    }

                    return true;
                }

                public bool SetKeyFile(string keyFile)
                {
                    if (!string.IsNullOrWhiteSpace(keyFile))
                    {
                        //keyFile  = FileUtilities.ResolveRelativePath(keyFile, this.ProjectDirectory);
                        _commandLineArgs.Add("/keyfile:\"" + keyFile + "\"");
                    }

                    return true;
                }

                public bool SetLinkResources(ITaskItem[] linkResources)
                {
                    if (linkResources != null && linkResources.Length > 0)
                    {
                        foreach (var lr in linkResources)
                        {
                            _commandLineArgs.Add("/linkresource:\"" + _projectFile.GetDocumentFilePath(lr) + "\"");
                        }
                    }

                    return true;
                }

                public bool SetMainEntryPoint(string mainEntryPoint)
                {
                    if (!string.IsNullOrWhiteSpace(mainEntryPoint))
                    {
                        _commandLineArgs.Add("/main:\"" + mainEntryPoint + "\"");
                    }

                    return true;
                }

                public bool SetNoConfig(bool noConfig)
                {
                    if (noConfig)
                    {
                        _commandLineArgs.Add("/noconfig");
                    }

                    return true;
                }

                public bool SetNoStandardLib(bool noStandardLib)
                {
                    if (noStandardLib)
                    {
                        _commandLineArgs.Add("/nostdlib");
                    }

                    return true;
                }

                public bool SetNoWarnings(bool noWarnings)
                {
                    if (noWarnings)
                    {
                        _commandLineArgs.Add("/nowarn");
                    }

                    return true;
                }

                public bool SetOptimize(bool optimize)
                {
                    if (optimize)
                    {
                        _commandLineArgs.Add("/optimize");
                    }

                    return true;
                }

                public bool SetOptionCompare(string optionCompare)
                {
                    if (string.Equals("binary", optionCompare, StringComparison.OrdinalIgnoreCase))
                    {
                        _commandLineArgs.Add("/optioncompare:binary");
                        return true;
                    }
                    else if (string.Equals("text", optionCompare, StringComparison.OrdinalIgnoreCase))
                    {
                        _commandLineArgs.Add("/optioncompare:text");
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                public bool SetOptionExplicit(bool optionExplicit)
                {
                    // default is on/true
                    if (!optionExplicit)
                    {
                        _commandLineArgs.Add("/optionexplicit-");
                    }

                    return true;
                }

                public bool SetOptionStrict(bool optionStrict)
                {
                    if (optionStrict)
                    {
                        _commandLineArgs.Add("/optionstrict");
                    }

                    return true;
                }

                public bool SetOptionStrictType(string optionStrictType)
                {
                    if (string.Equals("custom", optionStrictType, StringComparison.OrdinalIgnoreCase))
                    {
                        _commandLineArgs.Add("/optionstrict:custom");
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                public bool SetOutputAssembly(string outputAssembly)
                {
                    _outputFileName = Path.GetFileName(outputAssembly);
                    _commandLineArgs.Add("/out:\"" + outputAssembly + "\"");
                    return true;
                }

                public bool SetPlatform(string platform)
                {
                    _platform = platform;
                    return true;
                }

                public bool SetPlatformWith32BitPreference(string platform)
                {
                    SetPlatform(platform);
                    return true;
                }

                public bool SetReferences(ITaskItem[] references)
                {
                    if (references != null && references.Length > 0)
                    {
                        foreach (var current in references)
                        {
                            if (!_projectFile.IsProjectReferenceOutputAssembly(current))
                            {
                                _commandLineArgs.Add("/reference:\"" + _projectFile.GetDocumentFilePath(current) + "\"");
                            }
                        }
                    }

                    return true;
                }

                public bool SetAnalyzers(ITaskItem[] analyzerReferences)
                {
                    if (analyzerReferences != null && analyzerReferences.Length > 0)
                    {
                        foreach (var current in analyzerReferences)
                        {
                            _commandLineArgs.Add("/analyzer:\"" + _projectFile.GetDocumentFilePath(current) + "\"");
                        }
                    }

                    return true;
                }

                public bool SetAdditionalFiles(ITaskItem[] additionalFiles)
                {
                    if (additionalFiles != null)
                    {
                        _additionalFiles = additionalFiles;

                        foreach (var af in additionalFiles)
                        {
                            _commandLineArgs.Add("/additionalfile:\"" + _projectFile.GetDocumentFilePath(af) + "\"");
                        }
                    }
                        
                    return true;
                }

                public bool SetRemoveIntegerChecks(bool removeIntegerChecks)
                {
                    if (removeIntegerChecks)
                    {
                        _commandLineArgs.Add("/removeintchecks");
                    }

                    return true;
                }

                public bool SetResources(ITaskItem[] resources)
                {
                    if (resources != null && resources.Length > 0)
                    {
                        foreach (var r in resources)
                        {
                            _commandLineArgs.Add("/resource:\"" + _projectFile.GetDocumentFilePath(r) + "\"");
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
                            _commandLineArgs.Add("@\"" + _projectFile.GetDocumentFilePath(rf) + "\"");
                        }
                    }

                    return true;
                }

                public bool SetRootNamespace(string rootNamespace)
                {
                    if (!string.IsNullOrWhiteSpace(rootNamespace))
                    {
                        _commandLineArgs.Add("/rootnamespace:\"" + rootNamespace + "\"");
                    }

                    return true;
                }

                public bool SetSdkPath(string sdkPath)
                {
                    if (!string.IsNullOrWhiteSpace(sdkPath))
                    {
                        _commandLineArgs.Add("/sdkpath:\"" + sdkPath + "\"");
                    }

                    return true;
                }

                public bool SetSources(ITaskItem[] sources)
                {
                    if (sources != null)
                    {
                        _sources = sources;
                    }

                    return true;
                }

                public bool SetTargetCompactFramework(bool targetCompactFramework)
                {
                    if (targetCompactFramework)
                    {
                        _commandLineArgs.Add("/netcf");
                    }

                    return true;
                }

                public bool SetTargetType(string targetType)
                {
                    if (!string.IsNullOrWhiteSpace(targetType))
                    {
                        _targetType = targetType.ToLower();
                    }

                    return true;
                }

                public bool SetRuleSet(string ruleSetFile)
                {
                    if (!string.IsNullOrWhiteSpace(ruleSetFile))
                    {
                        _commandLineArgs.Add("/ruleset:\"" + ruleSetFile + "\"");
                    }

                    return true;
                }

                public bool SetTreatWarningsAsErrors(bool treatWarningsAsErrors)
                {
                    if (treatWarningsAsErrors)
                    {
                        _commandLineArgs.Add("/warnaserror");
                    }

                    return true;
                }

                public bool SetWarningsAsErrors(string warningsAsErrors)
                {
                    if (!string.IsNullOrWhiteSpace(warningsAsErrors))
                    {
                        _commandLineArgs.Add("/warnaserror+:" + warningsAsErrors);
                    }

                    return true;
                }

                public bool SetWarningsNotAsErrors(string warningsNotAsErrors)
                {
                    if (!string.IsNullOrWhiteSpace(warningsNotAsErrors))
                    {
                        _commandLineArgs.Add("/warnaserror-:" + warningsNotAsErrors);
                    }

                    return true;
                }

                public bool SetWin32Icon(string win32Icon)
                {
                    if (!string.IsNullOrWhiteSpace(win32Icon))
                    {
                        _commandLineArgs.Add("/win32icon:\"" + win32Icon + "\"");
                    }

                    return true;
                }

                public bool SetWin32Resource(string win32Resource)
                {
                    if (!string.IsNullOrWhiteSpace(win32Resource))
                    {
                        _commandLineArgs.Add("/win32resource:\"" + win32Resource + "\"");
                    }

                    return true;
                }

                public bool SetModuleAssemblyName(string moduleAssemblyName)
                {
                    if (!string.IsNullOrWhiteSpace(moduleAssemblyName))
                    {
                        _commandLineArgs.Add("/moduleassemblyname:\"" + moduleAssemblyName + "\"");
                    }

                    return true;
                }

                public bool SetOptionInfer(bool optionInfer)
                {
                    if (optionInfer)
                    {
                        _commandLineArgs.Add("/optioninfer");
                    }

                    return true;
                }

                public bool SetWin32Manifest(string win32Manifest)
                {
                    if (!string.IsNullOrWhiteSpace(win32Manifest))
                    {
                        _commandLineArgs.Add("/win32manifest:\"" + win32Manifest + "\"");
                    }

                    return true;
                }

                public bool SetLanguageVersion(string languageVersion)
                {
                    if (!string.IsNullOrWhiteSpace(languageVersion))
                    {
                        _commandLineArgs.Add("/languageversion:" + languageVersion);
                    }

                    return true;
                }

                public bool SetVBRuntime(string vbRuntime)
                {
                    if (!string.IsNullOrEmpty(vbRuntime))
                    {
                        if (string.Equals("Default", vbRuntime, StringComparison.OrdinalIgnoreCase))
                        {
                            _commandLineArgs.Add("/vbruntime+");
                        }
                        else if (string.Equals("Embed", vbRuntime, StringComparison.OrdinalIgnoreCase))
                        {
                            _commandLineArgs.Add("/vbruntime*");
                        }
                        else if (string.Equals("None", vbRuntime, StringComparison.OrdinalIgnoreCase))
                        {
                            _commandLineArgs.Add("/vbruntime-");
                        }
                        else
                        {
                            _commandLineArgs.Add("/vbruntime:\"" + vbRuntime + "\"");
                        }
                    }

                    return true;
                }

                public bool SetHighEntropyVA(bool highEntropyVA)
                {
                    if (highEntropyVA)
                    {
                        _commandLineArgs.Add("/highentropyva");
                    }

                    return true;
                }

                public bool SetSubsystemVersion(string subsystemVersion)
                {
                    if (!string.IsNullOrWhiteSpace(subsystemVersion))
                    {
                        _commandLineArgs.Add("/subsystemversion:" + subsystemVersion);
                    }

                    return true;
                }
            }
        }
    }
}

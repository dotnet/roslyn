using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.VisualBasic
{
    internal class VisualBasicProjectFile : ProjectFile
    {
        public VisualBasicProjectFile(VisualBasicProjectFileLoader loader, MSB.Evaluation.Project loadedProject, string errorMessage)
            : base(loader, loadedProject, errorMessage)
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
            var buildInfo = await BuildAsync("Vbc", compilerInputs, cancellationToken).ConfigureAwait(false);

            if (!compilerInputs.Initialized)
            {
                InitializeFromModel(compilerInputs, buildInfo.Project);
            }

            return CreateProjectFileInfo(compilerInputs, buildInfo);
        }

        private ProjectFileInfo CreateProjectFileInfo(VisualBasicCompilerInputs compilerInputs, BuildInfo buildInfo)
        {
            var outputPath = Path.Combine(this.GetOutputDirectory(), compilerInputs.OutputFileName);
            var assemblyName = this.GetAssemblyName();

            var project = buildInfo.Project;
            if (project == null)
            {
                return new ProjectFileInfo(
                    outputPath,
                    assemblyName,
                    commandLineArgs: SpecializedCollections.EmptyEnumerable<string>(),
                    documents: SpecializedCollections.EmptyEnumerable<DocumentFileInfo>(),
                    additionalDocuments: SpecializedCollections.EmptyEnumerable<DocumentFileInfo>(),
                    projectReferences: SpecializedCollections.EmptyEnumerable<ProjectFileReference>(),
                    errorMessage: buildInfo.ErrorMessage);
            }

            var projectDirectory = GetProjectDirectory(project);

            var docs = compilerInputs.Sources
                .Where(s => !IsTemporaryGeneratedFile(s.ItemSpec))
                .Select(s => MakeDocumentFileInfo(projectDirectory, s))
                .ToImmutableArray();

            var additionalDocs = compilerInputs.AdditionalFiles
                .Select(s => MakeDocumentFileInfo(projectDirectory, s))
                .ToImmutableArray();

            return new ProjectFileInfo(
                outputPath,
                assemblyName,
                compilerInputs.CommandLineArgs,
                docs,
                additionalDocs,
                this.GetProjectReferences(buildInfo.Project),
                buildInfo.ErrorMessage);
        }

        private void InitializeFromModel(VisualBasicCompilerInputs compilerInputs, MSB.Execution.ProjectInstance executedProject)
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
            compilerInputs.SetReferences(this.GetMetadataReferencesFromModel(executedProject).ToArray());
            compilerInputs.SetAnalyzers(this.GetAnalyzerReferencesFromModel(executedProject).ToArray());
            compilerInputs.SetAdditionalFiles(this.GetAdditionalFilesFromModel(executedProject).ToArray());
            compilerInputs.SetSources(this.GetDocumentsFromModel(executedProject).ToArray());
            compilerInputs.EndInitialization();
        }

        private class VisualBasicCompilerInputs :
            MSB.Tasks.Hosting.IVbcHostObject5,
            MSB.Tasks.Hosting.IVbcHostObjectFreeThreaded,
            MSB.Tasks.Hosting.IAnalyzerHostObject
        {
            private readonly VisualBasicProjectFile _projectFile;

            public bool Initialized { get; private set; }
            public string ProjectDirectory { get; }
            public string OutputDirectory { get; }
            public List<string> CommandLineArgs { get; }
            public IEnumerable<MSB.Framework.ITaskItem> Sources { get; private set; }
            public IEnumerable<MSB.Framework.ITaskItem> AdditionalFiles { get; private set; }

            private string _outputFileName;
            private bool _emitDocComments;
            private string _docCommentFile;
            private string _targetType;
            private string _platform;

            public VisualBasicCompilerInputs(VisualBasicProjectFile projectFile)
            {
                _projectFile = projectFile;
                this.CommandLineArgs = new List<string>();
                this.Sources = SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                this.AdditionalFiles = SpecializedCollections.EmptyEnumerable<MSB.Framework.ITaskItem>();
                this.ProjectDirectory = Path.GetDirectoryName(projectFile.FilePath);
                this.OutputDirectory = projectFile.GetOutputDirectory();
            }

            public string OutputFileName
            {
                get { return _outputFileName; }
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

            bool MSB.Tasks.Hosting.IVbcHostObjectFreeThreaded.Compile()
            {
                return Compile1();
            }

            public int EndCompile(bool buildSuccess)
            {
                return 0;
            }

            public MSB.Tasks.Hosting.IVbcHostObjectFreeThreaded GetFreeThreadedHostObject()
            {
                return null;
            }

            public void EndInitialization()
            {
                this.Initialized = true;

                if (_emitDocComments)
                {
                    if (!string.IsNullOrWhiteSpace(_docCommentFile))
                    {
                        this.CommandLineArgs.Add("/doc:\"" + _docCommentFile + "\"");
                    }
                    else
                    {
                        this.CommandLineArgs.Add("/doc");
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
                    this.CommandLineArgs.Add("/libpath:\"" + string.Join(";", additionalLibPaths) + "\"");
                }

                return true;
            }

            public bool SetAddModules(string[] addModules)
            {
                if (addModules != null && addModules.Length > 0)
                {
                    this.CommandLineArgs.Add("/addmodules:\"" + string.Join(";", addModules) + "\"");
                }

                return true;
            }

            public bool SetBaseAddress(string targetType, string baseAddress)
            {
                SetTargetType(targetType);

                if (!string.IsNullOrWhiteSpace(baseAddress))
                {
                    this.CommandLineArgs.Add("/baseaddress:" + baseAddress);
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
                    else if (string.Equals(debugType, "portable", StringComparison.OrdinalIgnoreCase))
                    {
                        this.CommandLineArgs.Add("/debug:portable");
                        return true;
                    }
                    else if (string.Equals(debugType, "embedded", StringComparison.OrdinalIgnoreCase))
                    {
                        this.CommandLineArgs.Add("/debug:embedded");
                        return true;
                    }
                }

                return false;
            }

            public bool SetDefineConstants(string defineConstants)
            {
                if (!string.IsNullOrWhiteSpace(defineConstants))
                {
                    this.CommandLineArgs.Add("/define:" + defineConstants);
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
                    this.CommandLineArgs.Add("/delaysign");
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
                    _emitDocComments = true;
                    _docCommentFile = documentationFile;
                }

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

            public bool SetGenerateDocumentation(bool generateDocumentation)
            {
                if (generateDocumentation)
                {
                    _emitDocComments = true;
                }

                return true;
            }

            public bool SetImports(MSB.Framework.ITaskItem[] importsList)
            {
                if (importsList != null)
                {
                    this.CommandLineArgs.Add("/imports:" + string.Join(",", importsList.Select(item => item.ItemSpec.Trim())));
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
                    this.CommandLineArgs.Add("/keyfile:\"" + keyFile + "\"");
                }

                return true;
            }

            public bool SetLinkResources(MSB.Framework.ITaskItem[] linkResources)
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

            public bool SetMainEntryPoint(string mainEntryPoint)
            {
                if (!string.IsNullOrWhiteSpace(mainEntryPoint))
                {
                    this.CommandLineArgs.Add("/main:\"" + mainEntryPoint + "\"");
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

            public bool SetNoWarnings(bool noWarnings)
            {
                if (noWarnings)
                {
                    this.CommandLineArgs.Add("/nowarn");
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

            public bool SetOptionCompare(string optionCompare)
            {
                if (string.Equals("binary", optionCompare, StringComparison.OrdinalIgnoreCase))
                {
                    this.CommandLineArgs.Add("/optioncompare:binary");
                    return true;
                }
                else if (string.Equals("text", optionCompare, StringComparison.OrdinalIgnoreCase))
                {
                    this.CommandLineArgs.Add("/optioncompare:text");
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
                    this.CommandLineArgs.Add("/optionexplicit-");
                }

                return true;
            }

            public bool SetOptionStrict(bool optionStrict)
            {
                if (optionStrict)
                {
                    this.CommandLineArgs.Add("/optionstrict");
                }

                return true;
            }

            public bool SetOptionStrictType(string optionStrictType)
            {
                if (string.Equals("custom", optionStrictType, StringComparison.OrdinalIgnoreCase))
                {
                    this.CommandLineArgs.Add("/optionstrict:custom");
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
                this.CommandLineArgs.Add("/out:\"" + outputAssembly + "\"");
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

            public bool SetReferences(MSB.Framework.ITaskItem[] references)
            {
                if (references != null && references.Length > 0)
                {
                    foreach (var current in references)
                    {
                        if (!IsProjectReferenceOutputAssembly(current))
                        {
                            this.CommandLineArgs.Add("/reference:\"" + _projectFile.GetDocumentFilePath(current) + "\"");
                        }
                    }
                }

                return true;
            }

            public bool SetAnalyzers(MSB.Framework.ITaskItem[] analyzerReferences)
            {
                if (analyzerReferences != null && analyzerReferences.Length > 0)
                {
                    foreach (var current in analyzerReferences)
                    {
                        this.CommandLineArgs.Add("/analyzer:\"" + _projectFile.GetDocumentFilePath(current) + "\"");
                    }
                }

                return true;
            }

            public bool SetAdditionalFiles(MSB.Framework.ITaskItem[] additionalFiles)
            {
                if (additionalFiles != null)
                {
                    this.AdditionalFiles = additionalFiles;

                    foreach (var af in additionalFiles)
                    {
                        this.CommandLineArgs.Add("/additionalfile:\"" + _projectFile.GetDocumentFilePath(af) + "\"");
                    }
                }

                return true;
            }

            public bool SetRemoveIntegerChecks(bool removeIntegerChecks)
            {
                if (removeIntegerChecks)
                {
                    this.CommandLineArgs.Add("/removeintchecks");
                }

                return true;
            }

            public bool SetResources(MSB.Framework.ITaskItem[] resources)
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

            public bool SetResponseFiles(MSB.Framework.ITaskItem[] responseFiles)
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

            public bool SetRootNamespace(string rootNamespace)
            {
                if (!string.IsNullOrWhiteSpace(rootNamespace))
                {
                    this.CommandLineArgs.Add("/rootnamespace:\"" + rootNamespace + "\"");
                }

                return true;
            }

            public bool SetSdkPath(string sdkPath)
            {
                if (!string.IsNullOrWhiteSpace(sdkPath))
                {
                    this.CommandLineArgs.Add("/sdkpath:\"" + sdkPath + "\"");
                }

                return true;
            }

            public bool SetSources(MSB.Framework.ITaskItem[] sources)
            {
                if (sources != null)
                {
                    this.Sources = sources;
                }

                return true;
            }

            public bool SetTargetCompactFramework(bool targetCompactFramework)
            {
                if (targetCompactFramework)
                {
                    this.CommandLineArgs.Add("/netcf");
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
                    this.CommandLineArgs.Add("/ruleset:\"" + ruleSetFile + "\"");
                }

                return true;
            }

            public bool SetTreatWarningsAsErrors(bool treatWarningsAsErrors)
            {
                if (treatWarningsAsErrors)
                {
                    this.CommandLineArgs.Add("/warnaserror");
                }

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
                    this.CommandLineArgs.Add("/win32resource:\"" + win32Resource + "\"");
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

            public bool SetOptionInfer(bool optionInfer)
            {
                if (optionInfer)
                {
                    this.CommandLineArgs.Add("/optioninfer");
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

            public bool SetLanguageVersion(string languageVersion)
            {
                if (!string.IsNullOrWhiteSpace(languageVersion))
                {
                    this.CommandLineArgs.Add("/langversion:" + languageVersion);
                }

                return true;
            }

            public bool SetVBRuntime(string vbRuntime)
            {
                if (!string.IsNullOrEmpty(vbRuntime))
                {
                    if (string.Equals("Default", vbRuntime, StringComparison.OrdinalIgnoreCase))
                    {
                        this.CommandLineArgs.Add("/vbruntime+");
                    }
                    else if (string.Equals("Embed", vbRuntime, StringComparison.OrdinalIgnoreCase))
                    {
                        this.CommandLineArgs.Add("/vbruntime*");
                    }
                    else if (string.Equals("None", vbRuntime, StringComparison.OrdinalIgnoreCase))
                    {
                        this.CommandLineArgs.Add("/vbruntime-");
                    }
                    else
                    {
                        this.CommandLineArgs.Add("/vbruntime:\"" + vbRuntime + "\"");
                    }
                }

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
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class CSharpProjectFile : ProjectFile
    {
        public CSharpProjectFile(CSharpProjectFileLoader loader, MSB.Evaluation.Project project, string errorMessage)
            : base(loader, project, errorMessage)
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
            var buildInfo = await this.BuildAsync(cancellationToken).ConfigureAwait(false);

            if (buildInfo.Project == null)
            {
                return new ProjectFileInfo(
                    outputFilePath: null,
                    commandLineArgs: SpecializedCollections.EmptyEnumerable<string>(),
                    documents: SpecializedCollections.EmptyEnumerable<DocumentFileInfo>(),
                    additionalDocuments: SpecializedCollections.EmptyEnumerable<DocumentFileInfo>(),
                    projectReferences: SpecializedCollections.EmptyEnumerable<ProjectFileReference>(),
                    errorMessage: buildInfo.ErrorMessage);
            }

            return CreateProjectFileInfo(buildInfo);
        }

        protected override IEnumerable<MSB.Framework.ITaskItem> GetCommandLineArgsFromModel(MSB.Execution.ProjectInstance executedProject)
        {
            return executedProject.GetItems("CscCommandLineArgs");
        }

        protected override ProjectFileReference CreateProjectFileReference(MSB.Execution.ProjectItemInstance reference)
        {
            var filePath = reference.EvaluatedInclude;
            var aliases = GetAliases(reference);

            return new ProjectFileReference(filePath, aliases);
        }

        private ProjectFileInfo CreateProjectFileInfo(BuildInfo buildInfo)
        {
            var project = buildInfo.Project;
            var projectDirectory = GetProjectDirectory(project);

            var commandLineArgs = this.GetCommandLineArgsFromModel(project)
                .Select(item => item.ItemSpec)
                .Where(item => item.StartsWith("/"))
                .ToImmutableArray();

            if (commandLineArgs.Length == 0)
            {
                // We didn't get any command-line arguments. Try to read them directly from the project.
                commandLineArgs = ReadCommandLineArguments(project);
            }

            commandLineArgs = FixPlatform(commandLineArgs);

            var outputFilePath = this.GetAbsolutePath(project.ReadPropertyString("TargetPath"));

            var docs = this.GetDocumentsFromModel(project)
                .Where(s => !IsTemporaryGeneratedFile(s.ItemSpec))
                .Select(s => MakeDocumentFileInfo(projectDirectory, s))
                .ToImmutableArray();

            var additionalDocs = this.GetAdditionalFilesFromModel(project)
                .Select(s => MakeDocumentFileInfo(projectDirectory, s))
                .ToImmutableArray();

            return new ProjectFileInfo(
                outputFilePath,
                commandLineArgs,
                docs,
                additionalDocs,
                this.GetProjectReferences(project),
                buildInfo.ErrorMessage);
        }

        private ImmutableArray<string> FixPlatform(ImmutableArray<string> commandLineArgs)
        {
            string platform = null, target = null;
            var platformIndex = -1;

            for (int i = 0; i < commandLineArgs.Length; i++)
            {
                var arg = commandLineArgs[i];

                if (platform != null && arg.StartsWith("/platform:", StringComparison.OrdinalIgnoreCase))
                {
                    platform = arg.Substring("/platform:".Length);
                    platformIndex = i;
                }
                else if (target != null && arg.StartsWith("/target:", StringComparison.OrdinalIgnoreCase))
                {
                    target = arg.Substring("/target:".Length);
                }

                if (platform != null && target != null)
                {
                    break;
                }
            }

            if (string.Equals("anycpu32bitpreferred", platform, StringComparison.OrdinalIgnoreCase)
                && (string.Equals("library", target, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("module", target, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("winmdobj", target, StringComparison.OrdinalIgnoreCase)))
            {
                return commandLineArgs.SetItem(platformIndex, "/platform:anycpu");
            }

            return commandLineArgs;
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

        private ImmutableArray<string> ReadCommandLineArguments(MSB.Execution.ProjectInstance project)
        {
            var builder = ImmutableArray.CreateBuilder<string>();

            var additionalFiles = this.GetAdditionalFilesFromModel(project);
            if (additionalFiles != null)
            {
                foreach (var additionalFile in additionalFiles)
                {
                    builder.Add("/additionalfile:\"" + this.GetDocumentFilePath(additionalFile) + "\"");
                }
            }

            var allowUnsafeBlocks = project.ReadPropertyBool("AllowUnsafeBlocks");
            if (allowUnsafeBlocks)
            {
                builder.Add("/unsafe");
            }

            var analyzers = this.GetAnalyzerReferencesFromModel(project);
            if (analyzers != null)
            {
                foreach (var analyzer in analyzers)
                {
                    builder.Add("/analyzer:\"" + this.GetDocumentFilePath(analyzer) + "\"");
                }
            }

            var applicationConfiguration = project.ReadPropertyString("AppConfigForCompiler");
            if (!string.IsNullOrWhiteSpace(applicationConfiguration))
            {
                builder.Add("/appconfig:" + applicationConfiguration);
            }

            var baseAddress = project.ReadPropertyString("BaseAddress");
            if (!string.IsNullOrWhiteSpace(baseAddress))
            {
                builder.Add("/baseaddress:" + baseAddress);
            }

            var checkForOverflowUnderflow = project.ReadPropertyBool("CheckForOverflowUnderflow");
            if (checkForOverflowUnderflow)
            {
                builder.Add("/checked");
            }

            var codePage = project.ReadPropertyInt("CodePage");
            if (codePage != 0)
            {
                builder.Add("/codepage:" + codePage);
            }

            var emitDebugInformation = project.ReadPropertyBool("DebugSymbols");
            if (emitDebugInformation)
            {
                var debugType = project.ReadPropertyString("DebugType");

                if (string.Equals(debugType, "none", StringComparison.OrdinalIgnoreCase))
                {
                    builder.Add("/debug");
                }
                else if (string.Equals(debugType, "pdbonly", StringComparison.OrdinalIgnoreCase))
                {
                    builder.Add("/debug:pdbonly");
                }
                else if (string.Equals(debugType, "full", StringComparison.OrdinalIgnoreCase))
                {
                    builder.Add("/debug:full");
                }
                else if (string.Equals(debugType, "portable", StringComparison.OrdinalIgnoreCase))
                {
                    builder.Add("/debug:portable");
                }
                else if (string.Equals(debugType, "embedded", StringComparison.OrdinalIgnoreCase))
                {
                    builder.Add("/debug:embedded");
                }
            }

            var defineConstants = project.ReadPropertyString("DefineConstants");
            if (!string.IsNullOrWhiteSpace(defineConstants))
            {
                builder.Add("/define:" + defineConstants);
            }

            var delaySignProperty = project.GetProperty("DelaySign");
            if (delaySignProperty != null && !string.IsNullOrWhiteSpace(delaySignProperty.EvaluatedValue))
            {
                var delaySign = project.ReadPropertyBool("DelaySign");
                if (delaySign)
                {
                    builder.Add("/delaysign+");
                }
                else
                {
                    builder.Add("/delaysign-");
                }
            }

            var errorReport = project.ReadPropertyString("ErrorReport");
            if (!string.IsNullOrWhiteSpace(errorReport))
            {
                builder.Add("/errorreport:" + errorReport.ToLower());
            }

            var features = project.ReadPropertyString("Features");
            if (!string.IsNullOrWhiteSpace(features))
            {
                foreach (var feature in CompilerOptionParseUtilities.ParseFeatureFromMSBuild(features))
                {
                    builder.Add("/features:" + feature);
                }
            }

            var fileAlignment = project.ReadPropertyString("FileAlignment");
            builder.Add("/filealign:" + fileAlignment);

            var documentationFile = this.GetItemString(project, "DocFileItem");
            if (!string.IsNullOrWhiteSpace(documentationFile))
            {
                builder.Add("/doc:\"" + documentationFile + "\"");
            }

            var generateFullPaths = project.ReadPropertyBool("GenerateFullPaths");
            if (generateFullPaths)
            {
                builder.Add("/fullpaths");
            }

            var highEntropyVA = project.ReadPropertyBool("HighEntropyVA");
            if (highEntropyVA)
            {
                builder.Add("/highentropyva");
            }

            var imports = this.GetTaskItems(project, "Import");
            if (imports != null)
            {
                var importsString = string.Join(",", imports.Select(item => item.ItemSpec.Trim()));
                builder.Add("/imports:" + importsString);
            }

            var languageVersion = project.ReadPropertyString("LangVersion");
            if (!string.IsNullOrWhiteSpace(languageVersion))
            {
                builder.Add("/langversion:" + languageVersion);
            }

            var mainEntryPoint = project.ReadPropertyString("StartupObject");
            if (!string.IsNullOrWhiteSpace(mainEntryPoint))
            {
                builder.Add("/main:\"" + mainEntryPoint + "\"");
            }

            var moduleAssemblyName = project.ReadPropertyString("ModuleAssemblyName");
            if (!string.IsNullOrWhiteSpace(moduleAssemblyName))
            {
                builder.Add("/moduleassemblyname:\"" + moduleAssemblyName + "\"");
            }

            var noStandardLib = project.ReadPropertyBool("NoCompilerStandardLib");
            if (noStandardLib)
            {
                builder.Add("/nostdlib");
            }

            var optimize = project.ReadPropertyBool("Optimize");
            if (optimize)
            {
                builder.Add("/optimize");
            }

            var outputAssembly = this.GetItemString(project, "IntermediateAssembly");
            if (!string.IsNullOrWhiteSpace(outputAssembly))
            {
                builder.Add("/out:\"" + outputAssembly + "\"");
            }

            var pdbFile = project.ReadPropertyString("PdbFile");
            if (!string.IsNullOrWhiteSpace(pdbFile))
            {
                builder.Add("/pdb:\"" + pdbFile + "\"");
            }

            var ruleSet = project.ReadPropertyString("ResolvedCodeAnalysisRuleSet");
            if (!string.IsNullOrWhiteSpace(ruleSet))
            {
                builder.Add("/ruleset:\"" + ruleSet + "\"");
            }

            var signAssembly = project.ReadPropertyBool("SignAssembly");
            if (signAssembly)
            {
                var keyFile = project.ReadPropertyString("KeyOriginatorFile");
                if (!string.IsNullOrWhiteSpace(keyFile))
                {
                    builder.Add("/keyfile:\"" + keyFile + "\"");
                }

                var keyContainer = project.ReadPropertyString("KeyContainerName");
                if (!string.IsNullOrWhiteSpace(keyContainer))
                {
                    builder.Add("/keycontainer:\"" + keyContainer + "\"");
                }
            }

            var references = this.GetMetadataReferencesFromModel(project);
            if (references != null)
            {
                foreach (var reference in references)
                {
                    if (!IsProjectReferenceOutputAssembly(reference))
                    {
                        var filePath = this.GetDocumentFilePath(reference);

                        var aliases = GetAliases(reference);
                        if (aliases.IsDefaultOrEmpty)
                        {
                            builder.Add("/reference:\"" + filePath + "\"");
                        }
                        else
                        {
                            foreach (var alias in aliases)
                            {
                                builder.Add("/reference:" + alias + "=\"" + filePath + "\"");
                            }
                        }
                    }
                }
            }

            var subsystemVersion = project.ReadPropertyString("SubsystemVersion");
            if (!string.IsNullOrWhiteSpace(subsystemVersion))
            {
                builder.Add("/subsystemversion:" + subsystemVersion);
            }

            var targetType = project.ReadPropertyString("OutputType");
            if (!string.IsNullOrWhiteSpace(targetType))
            {
                builder.Add("/target:" + targetType);
            }

            var platform = project.ReadPropertyString("PlatformTarget");
            var prefer32bit = project.ReadPropertyBool("Prefer32Bit");
            if (prefer32bit && (string.IsNullOrWhiteSpace(platform) || string.Equals("anycpu", platform, StringComparison.OrdinalIgnoreCase)))
            {
                platform = "anycpu32bitpreferred";
            }

            if (!string.IsNullOrWhiteSpace(platform))
            {
                builder.Add("/platform:" + platform);
            }

            var disabledWarnings = project.ReadPropertyString("NoWarn");
            if (!string.IsNullOrWhiteSpace(disabledWarnings))
            {
                builder.Add("/nowarn:" + disabledWarnings);
            }

            var treatWarningsAsErrors = project.ReadPropertyBool("TreatWarningsAsErrors");
            if (treatWarningsAsErrors)
            {
                builder.Add("/warnaserror");
            }

            var warningLevel = project.ReadPropertyInt("WarningLevel");
            builder.Add("/warn:" + warningLevel);

            var warningsAsErrors = project.ReadPropertyString("WarningsAsErrors");
            if (!string.IsNullOrWhiteSpace(warningsAsErrors))
            {
                builder.Add("/warnaserror+:" + warningsAsErrors);
            }

            var warningsNotAsErrors = project.ReadPropertyString("WarningsNotAsErrors");
            if (!string.IsNullOrWhiteSpace(warningsNotAsErrors))
            {
                builder.Add("/warnaserror-:" + warningsNotAsErrors);
            }

            return builder.ToImmutable();
        }
    }
}

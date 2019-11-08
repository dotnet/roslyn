// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    internal partial class CSharpProjectShim
    {
        private class OptionsProcessor : VisualStudioProjectOptionsProcessor
        {
            private readonly VisualStudioProject _visualStudioProject;

            private readonly object[] _options = new object[(int)CompilerOptions.LARGEST_OPTION_ID];
            private string _mainTypeName;
            private OutputKind _outputKind;

            public OptionsProcessor(VisualStudioProject visualStudioProject, HostWorkspaceServices workspaceServices)
                : base(visualStudioProject, workspaceServices)
            {
                _visualStudioProject = visualStudioProject;
            }

            public object this[CompilerOptions compilerOption]
            {
                get
                {
                    return _options[(int)compilerOption];
                }

                set
                {
                    if (object.Equals(_options[(int)compilerOption], value))
                    {
                        return;
                    }

                    _options[(int)compilerOption] = value;
                    UpdateProjectForNewHostValues();
                }
            }

            protected override CompilationOptions ComputeCompilationOptionsWithHostValues(CompilationOptions compilationOptions, IRuleSetFile ruleSetFileOpt)
            {
                IDictionary<string, ReportDiagnostic> ruleSetSpecificDiagnosticOptions = null;

                // Get options from the ruleset file, if any, first. That way project-specific
                // options can override them.
                ReportDiagnostic? ruleSetGeneralDiagnosticOption = null;

                // TODO: merge this core logic back down to the base of OptionsProcessor, since this should be the same for all languages. The CompilationOptions
                // would then already contain the right information, and could be updated accordingly by the language-specific logic.
                if (ruleSetFileOpt != null)
                {
                    ruleSetGeneralDiagnosticOption = ruleSetFileOpt.GetGeneralDiagnosticOption();
                    ruleSetSpecificDiagnosticOptions = new Dictionary<string, ReportDiagnostic>(ruleSetFileOpt.GetSpecificDiagnosticOptions());
                }
                else
                {
                    ruleSetSpecificDiagnosticOptions = new Dictionary<string, ReportDiagnostic>();
                }

                ReportDiagnostic generalDiagnosticOption;
                var warningsAreErrors = GetNullableBooleanOption(CompilerOptions.OPTID_WARNINGSAREERRORS);
                if (warningsAreErrors.HasValue)
                {
                    generalDiagnosticOption = warningsAreErrors.Value ? ReportDiagnostic.Error : ReportDiagnostic.Default;
                }
                else if (ruleSetGeneralDiagnosticOption.HasValue)
                {
                    generalDiagnosticOption = ruleSetGeneralDiagnosticOption.Value;
                }
                else
                {
                    generalDiagnosticOption = ReportDiagnostic.Default;
                }

                // Start with the rule set options
                var diagnosticOptions = new Dictionary<string, ReportDiagnostic>(ruleSetSpecificDiagnosticOptions);

                // Update the specific options based on the general settings
                if (warningsAreErrors is { HasValue: true, Value: true })
                {
                    foreach (var pair in ruleSetSpecificDiagnosticOptions)
                    {
                        if (pair.Value == ReportDiagnostic.Warn)
                        {
                            diagnosticOptions[pair.Key] = ReportDiagnostic.Error;
                        }
                    }
                }

                // Update the specific options based on the specific settings
                foreach (var diagnosticID in ParseWarningCodes(CompilerOptions.OPTID_WARNASERRORLIST))
                {
                    diagnosticOptions[diagnosticID] = ReportDiagnostic.Error;
                }

                foreach (var diagnosticID in ParseWarningCodes(CompilerOptions.OPTID_WARNNOTASERRORLIST))
                {
                    if (ruleSetSpecificDiagnosticOptions.TryGetValue(diagnosticID, out var ruleSetOption))
                    {
                        diagnosticOptions[diagnosticID] = ruleSetOption;
                    }
                    else
                    {
                        diagnosticOptions[diagnosticID] = ReportDiagnostic.Default;
                    }
                }

                foreach (var diagnosticID in ParseWarningCodes(CompilerOptions.OPTID_NOWARNLIST))
                {
                    diagnosticOptions[diagnosticID] = ReportDiagnostic.Suppress;
                }

                if (!Enum.TryParse(GetStringOption(CompilerOptions.OPTID_PLATFORM, ""), ignoreCase: true, result: out Platform platform))
                {
                    platform = Platform.AnyCpu;
                }

                if (!int.TryParse(GetStringOption(CompilerOptions.OPTID_WARNINGLEVEL, defaultValue: ""), out var warningLevel))
                {
                    warningLevel = 4;
                }

                // TODO: appConfigPath: GetFilePathOption(CompilerOptions.OPTID_FUSIONCONFIG), bug #869604

                return ((CSharpCompilationOptions)compilationOptions).WithAllowUnsafe(GetBooleanOption(CompilerOptions.OPTID_UNSAFE))
                    .WithOverflowChecks(GetBooleanOption(CompilerOptions.OPTID_CHECKED))
                    .WithCryptoKeyContainer(GetStringOption(CompilerOptions.OPTID_KEYNAME, defaultValue: null))
                    .WithCryptoKeyFile(GetFilePathRelativeOption(CompilerOptions.OPTID_KEYFILE))
                    .WithDelaySign(GetNullableBooleanOption(CompilerOptions.OPTID_DELAYSIGN))
                    .WithGeneralDiagnosticOption(generalDiagnosticOption)
                    .WithMainTypeName(_mainTypeName)
                    .WithModuleName(GetStringOption(CompilerOptions.OPTID_MODULEASSEMBLY, defaultValue: null))
                    .WithOptimizationLevel(GetBooleanOption(CompilerOptions.OPTID_OPTIMIZATIONS) ? OptimizationLevel.Release : OptimizationLevel.Debug)
                    .WithOutputKind(_outputKind)
                    .WithPlatform(platform)
                    .WithSpecificDiagnosticOptions(diagnosticOptions)
                    .WithWarningLevel(warningLevel);
            }

            private static string GetIdForErrorCode(int errorCode)
            {
                return "CS" + errorCode.ToString("0000");
            }

            private IEnumerable<string> ParseWarningCodes(CompilerOptions compilerOptions)
            {
                Contract.ThrowIfFalse(
                    compilerOptions == CompilerOptions.OPTID_NOWARNLIST ||
                    compilerOptions == CompilerOptions.OPTID_WARNASERRORLIST ||
                    compilerOptions == CompilerOptions.OPTID_WARNNOTASERRORLIST);

                foreach (var warning in GetStringOption(compilerOptions, defaultValue: "").Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var warningStringID = warning;
                    if (int.TryParse(warning, out var warningId))
                    {
                        warningStringID = GetIdForErrorCode(warningId);
                    }

                    yield return warningStringID;
                }
            }

            private bool? GetNullableBooleanOption(CompilerOptions optionID)
            {
                return (bool?)_options[(int)optionID];
            }

            private bool GetBooleanOption(CompilerOptions optionID)
            {
                return GetNullableBooleanOption(optionID).GetValueOrDefault(defaultValue: false);
            }

            private string GetFilePathRelativeOption(CompilerOptions optionID)
            {
                var path = GetStringOption(optionID, defaultValue: null);

                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                var directory = Path.GetDirectoryName(_visualStudioProject.FilePath);

                if (!string.IsNullOrEmpty(directory))
                {
                    return FileUtilities.ResolveRelativePath(path, directory);
                }

                return null;
            }

            private string GetStringOption(CompilerOptions optionID, string defaultValue)
            {
                var value = (string)_options[(int)optionID];

                if (string.IsNullOrEmpty(value))
                {
                    return defaultValue;
                }
                else
                {
                    return value;
                }
            }

            protected override ParseOptions ComputeParseOptionsWithHostValues(ParseOptions parseOptions)
            {
                var symbols = GetStringOption(CompilerOptions.OPTID_CCSYMBOLS, defaultValue: "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                // The base implementation of OptionsProcessor already tried this, but it didn't have the real documentation
                // path so we have to do it a second time
                var documentationMode = DocumentationMode.Parse;
                if (GetStringOption(CompilerOptions.OPTID_XML_DOCFILE, defaultValue: null) != null)
                {
                    documentationMode = DocumentationMode.Diagnose;
                }

                LanguageVersionFacts.TryParse(GetStringOption(CompilerOptions.OPTID_COMPATIBILITY, defaultValue: ""), out var languageVersion);

                return ((CSharpParseOptions)parseOptions).WithKind(SourceCodeKind.Regular)
                    .WithLanguageVersion(languageVersion)
                    .WithPreprocessorSymbols(symbols.AsImmutable())
                    .WithDocumentationMode(documentationMode);
            }

            public void SetOutputFileType(OutputFileType fileType)
            {
                var newOutputKind = fileType switch
                {
                    OutputFileType.Console => OutputKind.ConsoleApplication,
                    OutputFileType.Windows => OutputKind.WindowsApplication,
                    OutputFileType.Library => OutputKind.DynamicallyLinkedLibrary,
                    OutputFileType.Module => OutputKind.NetModule,
                    OutputFileType.AppContainer => OutputKind.WindowsRuntimeApplication,
                    OutputFileType.WinMDObj => OutputKind.WindowsRuntimeMetadata,
                    _ => throw new ArgumentException("fileType was not a valid OutputFileType", nameof(fileType)),
                };

                if (_outputKind != newOutputKind)
                {
                    _outputKind = newOutputKind;
                    UpdateProjectForNewHostValues();
                }
            }

            public void SetMainTypeName(string mainTypeName)
            {
                if (_mainTypeName != mainTypeName)
                {
                    _mainTypeName = mainTypeName;
                    UpdateProjectForNewHostValues();
                }
            }
        }
    }
}

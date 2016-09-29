// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    /// <summary>
    /// The representation of a project to both the project factory and workspace API.
    /// </summary>
    /// <remarks>
    /// Due to the number of interfaces this object must implement, all interface implementations
    /// are in a separate files. Methods that are shared across multiple interfaces (which are
    /// effectively methods that just QI from one interface to another), are implemented here.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    internal abstract partial class CSharpProjectShim : AbstractLegacyProject
    {
        /// <summary>
        /// This member is used to store a raw array of warning numbers, which is needed to properly implement
        /// ICSCompilerConfig.GetWarnNumbers. Read the implementation of that function for more details.
        /// </summary>
        private readonly IntPtr _warningNumberArrayPointer;

        private ICSharpProjectRoot _projectRoot;

        private OutputKind _outputKind = OutputKind.DynamicallyLinkedLibrary;
        private Platform _platform = Platform.AnyCpu;
        private string _mainTypeName;
        private object[] _options = new object[(int)CompilerOptions.LARGEST_OPTION_ID];

        public CSharpProjectShim(
            ICSharpProjectRoot projectRoot,
            VisualStudioProjectTracker projectTracker,
            Func<ProjectId, IVsReportExternalErrors> reportExternalErrorCreatorOpt,
            string projectSystemName,
            IVsHierarchy hierarchy,
            IServiceProvider serviceProvider,
            VisualStudioWorkspaceImpl visualStudioWorkspaceOpt,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSourceOpt,
            ICommandLineParserService commandLineParserServiceOpt)
            : base(projectTracker,
                   reportExternalErrorCreatorOpt,
                   projectSystemName,
                   hierarchy,
                   LanguageNames.CSharp,
                   serviceProvider,
                   visualStudioWorkspaceOpt,
                   hostDiagnosticUpdateSourceOpt,
                   commandLineParserServiceOpt)
        {
            _projectRoot = projectRoot;
            _warningNumberArrayPointer = Marshal.AllocHGlobal(0);

            // Ensure the default options are set up
            ResetAllOptions();
            UpdateOptions();

            projectTracker.AddProject(this);
        }

        public override void Disconnect()
        {
            _projectRoot = null;

            base.Disconnect();
        }

        private string GetIdForErrorCode(int errorCode)
        {
            return "CS" + errorCode.ToString("0000");
        }

        protected override CompilationOptions CreateCompilationOptions(CommandLineArguments commandLineArguments, ParseOptions newParseOptions)
        {
            // Get the base options from command line arguments + common workspace defaults.
            var options = (CSharpCompilationOptions)base.CreateCompilationOptions(commandLineArguments, newParseOptions);

            // Now override these with the options from our state.
            IDictionary<string, ReportDiagnostic> ruleSetSpecificDiagnosticOptions = null;

            // Get options from the ruleset file, if any, first. That way project-specific
            // options can override them.
            ReportDiagnostic? ruleSetGeneralDiagnosticOption = null;
            if (this.RuleSetFile != null)
            {
                ruleSetGeneralDiagnosticOption = this.RuleSetFile.GetGeneralDiagnosticOption();
                ruleSetSpecificDiagnosticOptions = new Dictionary<string, ReportDiagnostic>(this.RuleSetFile.GetSpecificDiagnosticOptions());
            }
            else
            {
                ruleSetSpecificDiagnosticOptions = new Dictionary<string, ReportDiagnostic>();
            }

            UpdateRuleSetError(this.RuleSetFile);

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
            IDictionary<string, ReportDiagnostic> diagnosticOptions = new Dictionary<string, ReportDiagnostic>(ruleSetSpecificDiagnosticOptions);

            // Update the specific options based on the general settings
            if (warningsAreErrors.HasValue && warningsAreErrors.Value == true)
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
                ReportDiagnostic ruleSetOption;
                if (ruleSetSpecificDiagnosticOptions.TryGetValue(diagnosticID, out ruleSetOption))
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

            Platform platform;

            if (!Enum.TryParse(GetStringOption(CompilerOptions.OPTID_PLATFORM, ""), ignoreCase: true, result: out platform))
            {
                platform = Platform.AnyCpu;
            }

            int warningLevel;

            if (!int.TryParse(GetStringOption(CompilerOptions.OPTID_WARNINGLEVEL, defaultValue: ""), out warningLevel))
            {
                warningLevel = 4;
            }

            // TODO: appConfigPath: GetFilePathOption(CompilerOptions.OPTID_FUSIONCONFIG), bug #869604

            return options.WithAllowUnsafe(GetBooleanOption(CompilerOptions.OPTID_UNSAFE))
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

        private IEnumerable<string> ParseWarningCodes(CompilerOptions compilerOptions)
        {
            Contract.ThrowIfFalse(compilerOptions == CompilerOptions.OPTID_NOWARNLIST || compilerOptions == CompilerOptions.OPTID_WARNASERRORLIST || compilerOptions == CompilerOptions.OPTID_WARNNOTASERRORLIST);
            foreach (var warning in GetStringOption(compilerOptions, defaultValue: "").Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int warningId;
                var warningStringID = warning;
                if (int.TryParse(warning, out warningId))
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

            var directory = this.ContainingDirectoryPathOpt;

            if (!string.IsNullOrEmpty(directory))
            {
                return FileUtilities.ResolveRelativePath(path, directory);
            }

            return null;
        }

        private string GetStringOption(CompilerOptions optionID, string defaultValue)
        {
            string value = (string)_options[(int)optionID];

            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            else
            {
                return value;
            }
        }

        protected override ParseOptions CreateParseOptions(CommandLineArguments commandLineArguments)
        {
            // Get the base parse options and override the defaults with the options from state.
            var options = (CSharpParseOptions)base.CreateParseOptions(commandLineArguments);
            var symbols = GetStringOption(CompilerOptions.OPTID_CCSYMBOLS, defaultValue: "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            DocumentationMode documentationMode = DocumentationMode.Parse;
            if (GetStringOption(CompilerOptions.OPTID_XML_DOCFILE, defaultValue: null) != null)
            {
                documentationMode = DocumentationMode.Diagnose;
            }

            var languageVersion = CompilationOptionsConversion.GetLanguageVersion(GetStringOption(CompilerOptions.OPTID_COMPATIBILITY, defaultValue: ""))
                                  ?? CSharpParseOptions.Default.LanguageVersion;

            return options.WithKind(SourceCodeKind.Regular)
                .WithLanguageVersion(languageVersion)
                .WithPreprocessorSymbols(symbols.AsImmutable())
                .WithDocumentationMode(documentationMode);
        }

        ~CSharpProjectShim()
        {
            // Free the unmanaged memory we allocated in the constructor
            Marshal.FreeHGlobal(_warningNumberArrayPointer);

            // Free any entry point strings.
            if (_startupClasses != null)
            {
                foreach (var @class in _startupClasses)
                {
                    Marshal.FreeHGlobal(@class);
                }
            }
        }

        internal bool CanCreateFileCodeModelThroughProject(string filePath)
        {
            return _projectRoot.CanCreateFileCodeModel(filePath);
        }

        internal object CreateFileCodeModelThroughProject(string filePath)
        {
            var iid = VSConstants.IID_IUnknown;
            return _projectRoot.CreateFileCodeModel(filePath, ref iid);
        }
    }
}

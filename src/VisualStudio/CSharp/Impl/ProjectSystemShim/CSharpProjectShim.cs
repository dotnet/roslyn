// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
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
    internal sealed partial class CSharpProjectShim : AbstractLegacyProject, ICodeModelInstanceFactory
    {
        /// <summary>
        /// This member is used to store a raw array of warning numbers, which is needed to properly implement
        /// ICSCompilerConfig.GetWarnNumbers. Read the implementation of that function for more details.
        /// </summary>
        private readonly IntPtr _warningNumberArrayPointer;

        private ICSharpProjectRoot _projectRoot;

        private OutputKind _outputKind = OutputKind.DynamicallyLinkedLibrary;
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

            ProjectCodeModel = new ProjectCodeModel(this.Id, this, visualStudioWorkspaceOpt, ServiceProvider);
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

        protected override bool CanUseTextBuffer(ITextBuffer textBuffer)
        {
            // In Web scenarios, the project system tells us about all files in the project, including ".aspx" and ".cshtml" files.
            // The impact of this is that we try to add a StandardTextDocument for the file, and parse it on disk, etc, which won't
            // end well.  We prevent that from happening by not allowing buffers that aren't of our content type to be used for
            // StandardTextDocuments.  In the web scenarios, we will instead end up creating a ContainedDocument that actually 
            // knows about the secondary buffer that contains valid code in our content type.
            return textBuffer.ContentType.IsOfType(ContentTypeNames.CSharpContentType);
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
                ruleSetGeneralDiagnosticOption = this.RuleSetFile.Target.GetGeneralDiagnosticOption();
                ruleSetSpecificDiagnosticOptions = new Dictionary<string, ReportDiagnostic>(this.RuleSetFile.Target.GetSpecificDiagnosticOptions());
            }
            else
            {
                ruleSetSpecificDiagnosticOptions = new Dictionary<string, ReportDiagnostic>();
            }

            UpdateRuleSetError(this.RuleSetFile?.Target);

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

            LanguageVersionFacts.TryParse(GetStringOption(CompilerOptions.OPTID_COMPATIBILITY, defaultValue: ""), out var languageVersion);

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

        EnvDTE.FileCodeModel ICodeModelInstanceFactory.TryCreateFileCodeModelThroughProjectSystem(string filePath)
        {
            if (_projectRoot.CanCreateFileCodeModel(filePath))
            {
                var iid = VSConstants.IID_IUnknown;
                return _projectRoot.CreateFileCodeModel(filePath, ref iid) as EnvDTE.FileCodeModel;
            }
            else
            {
                return null;
            }
        }
    }
}

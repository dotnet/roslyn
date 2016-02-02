// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
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
    internal abstract partial class CSharpProjectShim : AbstractEncProject
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
            MiscellaneousFilesWorkspace miscellaneousFilesWorkspaceOpt,
            VisualStudioWorkspaceImpl visualStudioWorkspaceOpt,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSourceOpt)
            : base(projectTracker,
                   reportExternalErrorCreatorOpt,
                   projectSystemName,
                   hierarchy,
                   LanguageNames.CSharp,
                   serviceProvider,
                   miscellaneousFilesWorkspaceOpt,
                   visualStudioWorkspaceOpt,
                   hostDiagnosticUpdateSourceOpt)
        {
            _projectRoot = projectRoot;
            _warningNumberArrayPointer = Marshal.AllocHGlobal(0);

            InitializeOptions();

            projectTracker.AddProject(this);
        }

        private void InitializeOptions()
        {
            // Ensure the default options are set up
            ResetAllOptions();

            this.SetOptions(this.CreateCompilationOptions(), this.CreateParseOptions());
        }

        protected override void UpdateAnalyzerRules()
        {
            base.UpdateAnalyzerRules();

            this.SetOptions(this.CreateCompilationOptions(), this.CreateParseOptions());
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

        protected CSharpCompilationOptions CreateCompilationOptions()
        {
            IDictionary<string, ReportDiagnostic> ruleSetSpecificDiagnosticOptions = null;

            // Get options from the ruleset file, if any, first. That way project-specific
            // options can override them.
            ReportDiagnostic? ruleSetGeneralDiagnosticOption = null;
            if (this.ruleSet != null)
            {
                ruleSetGeneralDiagnosticOption = this.ruleSet.GetGeneralDiagnosticOption();
                ruleSetSpecificDiagnosticOptions = new Dictionary<string, ReportDiagnostic>(this.ruleSet.GetSpecificDiagnosticOptions());
            }
            else
            {
                ruleSetSpecificDiagnosticOptions = new Dictionary<string, ReportDiagnostic>();
            }

            UpdateRuleSetError(ruleSet);

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

            string projectDirectory = this.ContainingDirectoryPathOpt;

            // TODO: #r support, should it include bin path?
            var referenceSearchPaths = ImmutableArray<string>.Empty;

            // TODO: #load support
            var sourceSearchPaths = ImmutableArray<string>.Empty;

            MetadataReferenceResolver referenceResolver;
            if (Workspace != null)
            {
                referenceResolver = new WorkspaceMetadataFileReferenceResolver(
                    Workspace.CurrentSolution.Services.MetadataService,
                    new RelativePathResolver(referenceSearchPaths, projectDirectory));
            }
            else
            {
                // can only happen in tests
                referenceResolver = null;
            }

            // TODO: appConfigPath: GetFilePathOption(CompilerOptions.OPTID_FUSIONCONFIG), bug #869604
            return new CSharpCompilationOptions(
                allowUnsafe: GetBooleanOption(CompilerOptions.OPTID_UNSAFE),
                checkOverflow: GetBooleanOption(CompilerOptions.OPTID_CHECKED),
                concurrentBuild: false,
                cryptoKeyContainer: GetStringOption(CompilerOptions.OPTID_KEYNAME, defaultValue: null),
                cryptoKeyFile: GetFilePathRelativeOption(CompilerOptions.OPTID_KEYFILE),
                delaySign: GetNullableBooleanOption(CompilerOptions.OPTID_DELAYSIGN),
                generalDiagnosticOption: generalDiagnosticOption,
                mainTypeName: _mainTypeName,
                moduleName: GetStringOption(CompilerOptions.OPTID_MODULEASSEMBLY, defaultValue: null),
                optimizationLevel: GetBooleanOption(CompilerOptions.OPTID_OPTIMIZATIONS) ? OptimizationLevel.Release : OptimizationLevel.Debug,
                outputKind: _outputKind,
                platform: platform,
                specificDiagnosticOptions: diagnosticOptions,
                warningLevel: warningLevel,
                xmlReferenceResolver: new XmlFileResolver(projectDirectory),
                sourceReferenceResolver: new SourceFileResolver(sourceSearchPaths, projectDirectory),
                metadataReferenceResolver: referenceResolver,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default,
                strongNameProvider: new DesktopStrongNameProvider(GetStrongNameKeyPaths()));
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

        protected CSharpParseOptions CreateParseOptions()
        {
            var symbols = GetStringOption(CompilerOptions.OPTID_CCSYMBOLS, defaultValue: "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            DocumentationMode documentationMode = DocumentationMode.Parse;
            if (GetStringOption(CompilerOptions.OPTID_XML_DOCFILE, defaultValue: null) != null)
            {
                documentationMode = DocumentationMode.Diagnose;
            }

            var languageVersion = CompilationOptionsConversion.GetLanguageVersion(GetStringOption(CompilerOptions.OPTID_COMPATIBILITY, defaultValue: ""))
                                  ?? CSharpParseOptions.Default.LanguageVersion;

            return new CSharpParseOptions(
                languageVersion: languageVersion,
                preprocessorSymbols: symbols.AsImmutable(),
                documentationMode: documentationMode);
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

        internal bool CanCreateFileCodeModelThroughProject(string fileName)
        {
            return _projectRoot.CanCreateFileCodeModel(fileName);
        }

        internal object CreateFileCodeModelThroughProject(string fileName)
        {
            var iid = VSConstants.IID_IUnknown;
            return _projectRoot.CreateFileCodeModel(fileName, ref iid);
        }
    }
}

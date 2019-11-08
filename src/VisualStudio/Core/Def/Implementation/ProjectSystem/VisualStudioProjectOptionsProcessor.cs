// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal class VisualStudioProjectOptionsProcessor : IDisposable
    {
        private readonly VisualStudioProject _project;
        private readonly HostWorkspaceServices _workspaceServices;
        private readonly ICommandLineParserService _commandLineParserService;

        /// <summary>
        /// Gate to guard all mutable fields in this class.
        /// The lock hierarchy means you are allowed to call out of this class and into <see cref="_project"/> while holding the lock.
        /// </summary>
        private readonly object _gate = new object();
        private string _commandLine = "";
        private CommandLineArguments _commandLineArgumentsForCommandLine;
        private string _explicitRuleSetFilePath;
        private IReferenceCountedDisposable<ICacheEntry<string, IRuleSetFile>> _ruleSetFile = null;
        private readonly IOptionService _optionService;

        public VisualStudioProjectOptionsProcessor(VisualStudioProject project, HostWorkspaceServices workspaceServices)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _workspaceServices = workspaceServices;
            _commandLineParserService = workspaceServices.GetLanguageServices(project.Language).GetRequiredService<ICommandLineParserService>();

            // Set up _commandLineArgumentsForCommandLine to a default. No lock taken since we're in the constructor so nothing can race.
            ReparseCommandLine_NoLock();

            _optionService = workspaceServices.GetRequiredService<IOptionService>();

            // For C#, we need to listen to the options for NRT analysis 
            // that can change in VS through tools > options
            if (_project.Language == LanguageNames.CSharp)
            {
                _optionService.OptionChanged += OptionService_OptionChanged;
            }
        }

        public string CommandLine
        {
            get
            {
                return _commandLine;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                lock (_gate)
                {
                    if (_commandLine == value)
                    {
                        return;
                    }

                    _commandLine = value;

                    ReparseCommandLine_NoLock();
                    UpdateProjectOptions_NoLock();
                }
            }
        }

        public string ExplicitRuleSetFilePath
        {
            get => _explicitRuleSetFilePath;

            set
            {
                lock (_gate)
                {
                    if (_explicitRuleSetFilePath == value)
                    {
                        return;
                    }

                    _explicitRuleSetFilePath = value;

                    UpdateProjectOptions_NoLock();
                }
            }
        }

        /// <summary>
        /// Returns the active path to the rule set file that is being used by this project, or null if there isn't a rule set file.
        /// </summary>
        public string EffectiveRuleSetFilePath
        {
            get
            {
                // We take a lock when reading this because we might be in the middle of processing a file update on another
                // thread.
                lock (_gate)
                {
                    return _ruleSetFile?.Target.Value.FilePath;
                }
            }
        }

        private void OptionService_OptionChanged(object sender, OptionChangedEventArgs e)
        {
            if (e is
            {
                Option: { Name: FeatureOnOffOptions.UseNullableReferenceTypeAnalysis.Name, Feature: FeatureOnOffOptions.UseNullableReferenceTypeAnalysis.Feature }
            }
)
            {
                UpdateProjectForNewHostValues();
            }
        }

        private void DisposeOfRuleSetFile_NoLock()
        {
            if (_ruleSetFile != null)
            {
                _ruleSetFile.Target.Value.UpdatedOnDisk -= RuleSetFile_UpdatedOnDisk;
                _ruleSetFile.Dispose();
                _ruleSetFile = null;
            }
        }

        private void ReparseCommandLine_NoLock()
        {
            var arguments = CommandLineParser.SplitCommandLineIntoArguments(_commandLine, removeHashComments: false);
            _commandLineArgumentsForCommandLine = _commandLineParserService.Parse(arguments, Path.GetDirectoryName(_project.FilePath), isInteractive: false, sdkDirectory: null);
        }

        private void UpdateProjectOptions_NoLock()
        {
            var effectiveRuleSetPath = ExplicitRuleSetFilePath ?? _commandLineArgumentsForCommandLine.RuleSetPath;

            if (_ruleSetFile?.Target.Value.FilePath != effectiveRuleSetPath)
            {
                // We're changing in some way. Be careful: this might mean the path is switching to or from null, so either side so far
                // could be changed.
                DisposeOfRuleSetFile_NoLock();

                if (effectiveRuleSetPath != null)
                {
                    _ruleSetFile = _workspaceServices.GetRequiredService<VisualStudioRuleSetManager>().GetOrCreateRuleSet(effectiveRuleSetPath);
                    _ruleSetFile.Target.Value.UpdatedOnDisk += RuleSetFile_UpdatedOnDisk;
                }
            }

            // TODO: #r support, should it include bin path?
            var referenceSearchPaths = ImmutableArray<string>.Empty;

            // TODO: #load support
            var sourceSearchPaths = ImmutableArray<string>.Empty;

            var referenceResolver = new WorkspaceMetadataFileReferenceResolver(
                    _workspaceServices.GetRequiredService<IMetadataService>(),
                    new RelativePathResolver(referenceSearchPaths, _commandLineArgumentsForCommandLine.BaseDirectory));

            var compilationOptions = _commandLineArgumentsForCommandLine.CompilationOptions
                .WithConcurrentBuild(concurrent: false)
                .WithMetadataReferenceResolver(referenceResolver)
                .WithXmlReferenceResolver(new XmlFileResolver(_commandLineArgumentsForCommandLine.BaseDirectory))
                .WithSourceReferenceResolver(new SourceFileResolver(sourceSearchPaths, _commandLineArgumentsForCommandLine.BaseDirectory))
                .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                .WithStrongNameProvider(new DesktopStrongNameProvider(_commandLineArgumentsForCommandLine.KeyFileSearchPaths.WhereNotNull().ToImmutableArray()));

            // Override the default documentation mode.
            var documentationMode = _commandLineArgumentsForCommandLine.DocumentationPath != null ? DocumentationMode.Diagnose : DocumentationMode.Parse;
            var parseOptions = _commandLineArgumentsForCommandLine.ParseOptions
                .WithDocumentationMode(documentationMode);

            parseOptions = ComputeOptionsServiceParseOptions(parseOptions);

            // We've computed what the base values should be; we now give an opportunity for any host-specific settings to be computed
            // before we apply them
            compilationOptions = ComputeCompilationOptionsWithHostValues(compilationOptions, this._ruleSetFile?.Target.Value);
            parseOptions = ComputeParseOptionsWithHostValues(parseOptions);

            // For managed projects, AssemblyName has to be non-null, but the command line we get might be a partial command line
            // and not contain the existing value. Only update if we have one.
            _project.AssemblyName = _commandLineArgumentsForCommandLine.CompilationName ?? _project.AssemblyName;
            _project.CompilationOptions = compilationOptions;

            var fullIntermediateOutputPath = _commandLineArgumentsForCommandLine.OutputDirectory != null && _commandLineArgumentsForCommandLine.OutputFileName != null
                                                    ? Path.Combine(_commandLineArgumentsForCommandLine.OutputDirectory, _commandLineArgumentsForCommandLine.OutputFileName)
                                                    : _commandLineArgumentsForCommandLine.OutputFileName;

            _project.IntermediateOutputFilePath = fullIntermediateOutputPath ?? _project.IntermediateOutputFilePath;
            _project.ParseOptions = parseOptions;
        }

        private ParseOptions ComputeOptionsServiceParseOptions(ParseOptions parseOptions)
        {
            if (_project.Language == LanguageNames.CSharp)
            {
                var useNullableReferenceAnalysisOption = _optionService.GetOption(FeatureOnOffOptions.UseNullableReferenceTypeAnalysis);

                if (useNullableReferenceAnalysisOption == -1)
                {
                    parseOptions = parseOptions.WithFeatures(new[] { KeyValuePairUtil.Create(CompilerFeatureFlags.RunNullableAnalysis, "false") });
                }
                else if (useNullableReferenceAnalysisOption == 1)
                {
                    parseOptions = parseOptions.WithFeatures(new[] { KeyValuePairUtil.Create(CompilerFeatureFlags.RunNullableAnalysis, "true") });
                }
            }

            return parseOptions;
        }

        private void RuleSetFile_UpdatedOnDisk(object sender, EventArgs e)
        {
            lock (_gate)
            {
                // This event might have gotten fired "late" if the file change was already in flight. We can see if this is still our current file;
                // it won't be if this is disposed or was already changed to a different file. We hard-cast sender to an IRuleSetFile because if it's
                // something else that means our comparison below is definitely broken.
                if (_ruleSetFile?.Target.Value != (IRuleSetFile)sender)
                {
                    return;
                }

                // The IRuleSetFile held by _ruleSetFile is now out of date. We'll dispose our old one first so as to let go of any old cached values.
                // Then, we must reparse: in the case where the command line we have from the project system includes a /ruleset, the computation of the
                // effective values was potentially done by the act of parsing the command line. Even though the command line didn't change textually,
                // the effective result did. Then we call UpdateProjectOptions_NoLock to reapply any values; that will also re-acquire the new ruleset
                // includes in the IDE so we can be watching for changes again.
                DisposeOfRuleSetFile_NoLock();
                ReparseCommandLine_NoLock();
                UpdateProjectOptions_NoLock();
            }
        }

        /// <summary>
        /// Overridden by derived classes to provide a hook to modify a <see cref="CompilationOptions"/> with any host-provided values that didn't come from
        /// the command line string.
        /// </summary>
        protected virtual CompilationOptions ComputeCompilationOptionsWithHostValues(CompilationOptions compilationOptions, IRuleSetFile ruleSetFileOpt)
        {
            return compilationOptions;
        }

        /// <summary>
        /// Override by derived classes to provide a hook to modify a <see cref="ParseOptions"/> with any host-provided values that didn't come from 
        /// the command line string.
        /// </summary>
        protected virtual ParseOptions ComputeParseOptionsWithHostValues(ParseOptions parseOptions)
        {
            return parseOptions;
        }

        /// <summary>
        /// Called by a derived class to notify that we need to update the settings in the project system for something that will be provided
        /// by either <see cref="ComputeCompilationOptionsWithHostValues(CompilationOptions, IRuleSetFile)"/> or <see cref="ComputeParseOptionsWithHostValues(ParseOptions)"/>.
        /// </summary>
        protected void UpdateProjectForNewHostValues()
        {
            lock (_gate)
            {
                UpdateProjectOptions_NoLock();
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                DisposeOfRuleSetFile_NoLock();
                _optionService.OptionChanged -= OptionService_OptionChanged;
            }
        }
    }
}

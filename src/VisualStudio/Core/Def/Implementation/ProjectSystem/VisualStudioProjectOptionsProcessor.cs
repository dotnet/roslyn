using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
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
        private object _gate = new object();
        private string _commandLine = "";
        private CommandLineArguments _commandLineArgumentsForCommandLine;
        private string _explicitRuleSetFilePath;
        private IReferenceCountedDisposable<IRuleSetFile> _ruleSetFile = null;

        public VisualStudioProjectOptionsProcessor(VisualStudioProject project, HostWorkspaceServices workspaceServices)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _workspaceServices = workspaceServices;
            _commandLineParserService = workspaceServices.GetLanguageServices(project.Language).GetRequiredService<ICommandLineParserService>();

            // Set up _commandLineArgumentsForCommandLine to a default. No lock taken since we're in the constructor so nothing can race.
            ReparseCommandLine_NoLock();
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

        public HostWorkspaceServices WorkspaceServices => _workspaceServices;

        private void ReparseCommandLine_NoLock()
        {
            var arguments = CommandLineParser.SplitCommandLineIntoArguments(_commandLine, removeHashComments: false);
            _commandLineArgumentsForCommandLine = _commandLineParserService.Parse(arguments, Path.GetDirectoryName(_project.FilePath), isInteractive: false, sdkDirectory: null);
        }

        private void UpdateProjectOptions_NoLock()
        {
            var effectiveRuleSetPath = ExplicitRuleSetFilePath ?? _commandLineArgumentsForCommandLine.RuleSetPath;

            if (_ruleSetFile?.Target.FilePath != effectiveRuleSetPath)
            {
                // We're changing in some way. Be careful: this might mean the path is switching to or from null, so either side so far
                // could be changed.
                _ruleSetFile?.Dispose();
                _ruleSetFile = null;

                if (effectiveRuleSetPath != null)
                {
                    _ruleSetFile = _workspaceServices.GetRequiredService<VisualStudioRuleSetManager>().GetOrCreateRuleSet(effectiveRuleSetPath);
                }
            }

            // TODO: #r support, should it include bin path?
            var referenceSearchPaths = ImmutableArray<string>.Empty;

            // TODO: #load support
            var sourceSearchPaths = ImmutableArray<string>.Empty;

            var referenceResolver = new WorkspaceMetadataFileReferenceResolver(
                    WorkspaceServices.GetRequiredService<IMetadataService>(),
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

            // We've computed what the base values should be; we now give an opportunity for any host-specific settings to be computed
            // before we apply them
            compilationOptions = ComputeCompilationOptionsWithHostValues(compilationOptions, this._ruleSetFile?.Target);
            parseOptions = ComputeParseOptionsWithHostValues(parseOptions);

            // For managed projects, AssemblyName has to be non-null, but the command line we get might be a partial command line
            // and not contain the existing value. Only update if we have one.
            _project.AssemblyName = _commandLineArgumentsForCommandLine.CompilationName ?? _project.AssemblyName;
            _project.CompilationOptions = compilationOptions;

            string fullIntermediateOutputPath = _commandLineArgumentsForCommandLine.OutputDirectory != null && _commandLineArgumentsForCommandLine.OutputFileName != null
                                                    ? Path.Combine(_commandLineArgumentsForCommandLine.OutputDirectory, _commandLineArgumentsForCommandLine.OutputFileName)
                                                    : _commandLineArgumentsForCommandLine.OutputFileName;

            _project.IntermediateOutputFilePath = fullIntermediateOutputPath ?? _project.IntermediateOutputFilePath;
            _project.ParseOptions = parseOptions;
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
            _ruleSetFile?.Dispose();
            _ruleSetFile = null;
        }
    }
}

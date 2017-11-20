// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal abstract partial class AbstractProject
    {
        #region Options
        /// <summary>Can be null if there is no <see cref="CommandLineParserService" /> available.</summary>
        private ImmutableArray<string> _lastSplitCommandLine;
        private CommandLineArguments _lastParsedCommandLineArguments;
        private CompilationOptions _currentCompilationOptions;
        private ParseOptions _currentParseOptions;

        // internal for testing purposes.
        internal CompilationOptions CurrentCompilationOptions => _currentCompilationOptions;
        internal ParseOptions CurrentParseOptions => _currentParseOptions;

        private void SetOptionsCore(CompilationOptions newCompilationOptions)
        {
            lock (_gate)
            {
                _currentCompilationOptions = newCompilationOptions;
            }
        }

        private void SetOptionsCore(CompilationOptions newCompilationOptions, ParseOptions newParseOptions)
        {
            lock (_gate)
            {
                _currentCompilationOptions = newCompilationOptions;
                _currentParseOptions = newParseOptions;
            }
        }

        private void SetArgumentsCore(ImmutableArray<string> splitCommandLine, CommandLineArguments commandLineArguments)
        {
            lock (_gate)
            {
                _lastSplitCommandLine = splitCommandLine;
                _lastParsedCommandLineArguments = commandLineArguments;
            }
        }
        #endregion

        /// <summary>
        /// Creates and sets new options using the last parsed command line arguments.  In the case that the last
        /// parsed options are <see langword="null"/> then this call is a NOP.
        /// </summary>
        protected void UpdateOptions()
        {
            AssertIsForeground();

            var lastParsedCommandLineArguments = _lastParsedCommandLineArguments;
            if (lastParsedCommandLineArguments != null)
            {
                // do nothing if the last parsed arguments aren't present, which is the case for languages that don't
                // export an ICommandLineParserService like F#
                var newParseOptions = CreateParseOptions(lastParsedCommandLineArguments);
                var newCompilationOptions = CreateCompilationOptions(lastParsedCommandLineArguments, newParseOptions);
                if (newCompilationOptions == CurrentCompilationOptions && newParseOptions == CurrentParseOptions)
                {
                    return;
                }

                SetOptions(newCompilationOptions, newParseOptions);
            }
        }

        /// <summary>
        /// Parses the given command line and sets new command line arguments.
        /// Subsequently, creates and sets new options using the last parsed command line arguments.
        /// </summary>
        protected CommandLineArguments SetArgumentsAndUpdateOptions(string commandLine)
        {
            AssertIsForeground();

            var splitArguments = CommandLineParser.SplitCommandLineIntoArguments(commandLine, removeHashComments: false).ToImmutableArray();
            return SetArgumentsAndUpdateOptions(splitArguments);
        }

        private CommandLineArguments SetArgumentsAndUpdateOptions(ImmutableArray<string> splitCommandLine)
        {
            AssertIsForeground();

            var parsedCommandLineArguments = CommandLineParserService?.Parse(splitCommandLine, this.ContainingDirectoryPathOpt, isInteractive: false, sdkDirectory: System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());
            SetArgumentsAndUpdateOptions(splitCommandLine, parsedCommandLineArguments);
            return parsedCommandLineArguments;
        }

        protected void SetArgumentsAndUpdateOptions(ImmutableArray<string> splitCommandLine, CommandLineArguments commandLineArguments)
        {
            AssertIsForeground();

            SetArgumentsCore(splitCommandLine, commandLineArguments);
            UpdateOptions();
            SetRuleSetFileCore(commandLineArguments?.RuleSetPath);
        }

        /// <summary>
        /// Resets the last parsed command line and updates options with the same command line.
        /// </summary>
        /// <remarks>
        /// Use this method when options can go stale due to side effects, even though the command line is identical.
        /// For example, changes to contents of a ruleset file needs to force update the options for the same command line.
        /// </remarks>
        protected void ReparseOptionsBecauseRulesetChanged()
        {
            // Clear last parsed command line.
            var savedSplitCommandLine = _lastSplitCommandLine;
            SetArgumentsCore(splitCommandLine: ImmutableArray<string>.Empty, commandLineArguments: null);

            // Now set arguments and update options with the saved command line.
            SetArgumentsAndUpdateOptions(savedSplitCommandLine);
        }

        /// <summary>
        /// Sets the given compilation and parse options.
        /// </summary>
        protected void SetOptions(CompilationOptions newCompilationOptions, ParseOptions newParseOptions)
        {
            AssertIsForeground();

            this.UpdateRuleSetError(this.RuleSetFile);

            // Set options.
            this.SetOptionsCore(newCompilationOptions, newParseOptions);

            if (_pushingChangesToWorkspaceHosts)
            {
                this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnOptionsChanged(Id, newCompilationOptions, newParseOptions));
            }
        }

        /// <summary>
        /// Creates new compilation options from parsed command line arguments, with additional workspace specific options appended.
        /// It is expected that derived types which need to add more specific options will fetch the base options and override those options.
        /// </summary>
        protected virtual CompilationOptions CreateCompilationOptions(CommandLineArguments commandLineArguments, ParseOptions newParseOptions)
        {
            Contract.ThrowIfNull(commandLineArguments);

            // Get options from command line arguments.
            var options = commandLineArguments.CompilationOptions;

            // Now set the default workspace options (these are not set by the command line parser).
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

            // Explicitly disable concurrent build.
            options = options.WithConcurrentBuild(concurrent: false);

            // Set default resolvers.
            options = options.WithMetadataReferenceResolver(referenceResolver)
                .WithXmlReferenceResolver(new XmlFileResolver(projectDirectory))
                .WithSourceReferenceResolver(new SourceFileResolver(sourceSearchPaths, projectDirectory))
                .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                .WithStrongNameProvider(new DesktopStrongNameProvider(GetStrongNameKeyPaths()));

            return options;
        }

        /// <summary>
        /// Creates new parse options from parsed command line arguments (with overridden default DocumentationMode).
        /// It is expected that derived types which need to add more specific options will fetch the base options and override those options.
        /// </summary>
        protected virtual ParseOptions CreateParseOptions(CommandLineArguments commandLineArguments)
        {
            Contract.ThrowIfNull(commandLineArguments);

            // Override the default documentation mode.
            var documentationMode = commandLineArguments.DocumentationPath != null ? DocumentationMode.Diagnose : DocumentationMode.Parse;
            return commandLineArguments.ParseOptions.WithDocumentationMode(documentationMode);
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal abstract partial class AbstractProject
    {
        private string _lastParsedCompilerOptions;
        private CommandLineArguments _lastParsedCommandLineArguments;

        /// <summary>
        /// Creates and sets new options using the last parsed command line arguments.
        /// </summary>
        protected void UpdateOptions()
        {
            Contract.ThrowIfNull(_lastParsedCommandLineArguments);

            var newParseOptions = CreateParseOptions(_lastParsedCommandLineArguments);
            var newCompilationOptions = CreateCompilationOptions(_lastParsedCommandLineArguments, newParseOptions);
            if (newCompilationOptions == CurrentCompilationOptions && newParseOptions == CurrentParseOptions)
            {
                return;
            }

            SetOptions(newCompilationOptions, newParseOptions);
        }

        /// <summary>
        /// If the command line has changed from the last parsed command line, then it parses it and sets new command line arguments.
        /// Subsequently, regardless of whether command line changed, it creates and sets new options using the last parsed command line arguments.
        /// </summary>
        protected CommandLineArguments SetArgumentsAndUpdateOptions(string commandlineForOptions)
        {
            SetArguments(commandlineForOptions);
            UpdateOptions();
            return _lastParsedCommandLineArguments;
        }

        /// <summary>
        /// Resets the last parsed command line and updates options with the same command line.
        /// </summary>
        /// <remarks>
        /// Use this method when options can go stale due to side effects, even though the command line is identical.
        /// For example, changes to contents of a ruleset file needs to force update the options for the same command line.
        /// </remarks>
        protected CommandLineArguments ResetArgumentsAndUpdateOptions()
        {
            // Clear last parsed command line.
            var savedLastParsedCompilerOptions = _lastParsedCompilerOptions;
            _lastParsedCompilerOptions = null;
            _lastParsedCommandLineArguments = null;

            // Now set arguments and update options with the saved command line.
            return SetArgumentsAndUpdateOptions(savedLastParsedCompilerOptions);
        }

        /// <summary>
        /// If the command line has changed from the last parsed command line, then it parses it and sets new command line arguments.
        /// </summary>
        /// <param name="commandlineForOptions"></param>
        /// <returns></returns>
        protected CommandLineArguments SetArguments(string commandlineForOptions)
        {
            if (!string.Equals(_lastParsedCompilerOptions, commandlineForOptions, StringComparison.OrdinalIgnoreCase))
            {
                // Command line options have changed, so update options with new parsed CommandLineArguments.
                var splitArguments = CommandLineParser.SplitCommandLineIntoArguments(commandlineForOptions, removeHashComments: false);
                _lastParsedCommandLineArguments = CommandLineParserService?.Parse(splitArguments, this.ContainingDirectoryPathOpt, isInteractive: false, sdkDirectory: null);
                _lastParsedCompilerOptions = commandlineForOptions;
            }

            return _lastParsedCommandLineArguments;
        }

        /// <summary>
        /// Sets the given compilation and parse options.
        /// </summary>
        protected void SetOptions(CompilationOptions newCompilationOptions, ParseOptions newParseOptions)
        {
            this.UpdateRuleSetError(this.RuleSetFile);

            // Set options.
            CurrentCompilationOptions = newCompilationOptions;
            CurrentParseOptions = newParseOptions;

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

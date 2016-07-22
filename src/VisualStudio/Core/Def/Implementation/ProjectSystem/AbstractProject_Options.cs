// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal abstract partial class AbstractProject
    {
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
        /// Sets the given command line arguments to be the last parsed command line arguments.
        /// </summary>
        protected void SetArguments(CommandLineArguments commandLineArguments)
        {
            _lastParsedCommandLineArguments = commandLineArguments;
        }

        /// <summary>
        /// Sets the given command line arguments to be the last parsed command line arguments and
        /// creates and sets new options using these command line arguments.
        /// </summary>
        protected void SetArgumentsAndUpdateOptions(CommandLineArguments commandLineArguments)
        {
            SetArguments(commandLineArguments);
            UpdateOptions();            
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

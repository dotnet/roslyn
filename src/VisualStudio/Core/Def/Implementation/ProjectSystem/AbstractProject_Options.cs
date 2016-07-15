// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal abstract partial class AbstractProject
    {
        private string _lastParsedCompilerOptions;
        private CommandLineArguments _lastParsedCommandLineArguments;

        /// <summary>
        /// Returns the parsed command line arguments (parsed by <see cref="ParseCommandLineArguments"/>) that were set by the project
        /// system's call to <see cref="ICompilerOptionsHostObject.SetCompilerOptions(string, out bool)"/>.
        /// </summary>
        protected CommandLineArguments GetParsedCommandLineArguments()
        {
            if (_lastParsedCommandLineArguments == null)
            {
                // We don't have any yet, so let's parse nothing
                _lastParsedCompilerOptions = string.Empty;
                _lastParsedCommandLineArguments = ParseCommandLineArguments(SpecializedCollections.EmptyEnumerable<string>());
            }

            return _lastParsedCommandLineArguments;
        }

        protected abstract CommandLineArguments ParseCommandLineArguments(IEnumerable<string> arguments);

        protected void SetCommandLineArguments(string commandLine)
        {
            if (string.Equals(_lastParsedCompilerOptions, commandLine, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var splitArguments = CommandLineParser.SplitCommandLineIntoArguments(commandLine, removeHashComments: false);
            var commandLineArguments = ParseCommandLineArguments(splitArguments);
            _lastParsedCompilerOptions = commandLine;

            SetCommandLineArguments(commandLineArguments);
        }

        protected void SetCommandLineArguments(CommandLineArguments commandLineArguments)
        {
            _lastParsedCommandLineArguments = commandLineArguments;
            UpdateOptions();
        }

        protected void UpdateOptions()
        {
            var parseOptions = GetParseOptions();
            var compilationOptions = GetCompilationOptions(parseOptions);
            if (compilationOptions == CurrentCompilationOptions && parseOptions == CurrentParseOptions)
            {
                return;
            }

            this.UpdateRuleSetError(this.ruleSet);
            this.SetOptions(compilationOptions, parseOptions);
            this.PostSetOptions();
        }

        /// <summary>
        /// Override this method to execute anything after the options have been set.
        /// </summary>
        protected virtual void PostSetOptions()
        {
        }

        /// <summary>
        /// Gets the compilation options from parsed command line arguments, with additional workspace specific options appended.
        /// It is expected that derived types which need to add more specific options will fetch the base options and override those options.
        /// </summary>
        protected virtual CompilationOptions GetCompilationOptions(ParseOptions newParseOptions)
        {
            // Get options from command line arguments.
            var options = GetParsedCommandLineArguments().CompilationOptions;

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
        /// Gets the parse options from parsed command line arguments (with overridden default DocumentationMode).
        /// It is expected that derived types which need to add more specific options will fetch the base options and override those options.
        /// </summary>
        protected virtual ParseOptions GetParseOptions()
        {
            var parsedArguments = GetParsedCommandLineArguments();
            
            // Override the default documentation mode.
            var documentationMode = parsedArguments.DocumentationPath != null ? DocumentationMode.Diagnose : DocumentationMode.Parse;
            return parsedArguments.ParseOptions.WithDocumentationMode(documentationMode);
        }
    }
}

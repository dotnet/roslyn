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
        protected CommandLineArguments ParsedCommandLineArguments { get; private set; }

        protected void SetCommandLineArguments(CommandLineArguments commandLineArguments)
        {
            ParsedCommandLineArguments = commandLineArguments;
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

            this.UpdateRuleSetError(this.RuleSetFile);
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
            Contract.ThrowIfNull(ParsedCommandLineArguments);

            // Get options from command line arguments.
            var options = ParsedCommandLineArguments.CompilationOptions;

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
            Contract.ThrowIfNull(ParsedCommandLineArguments);

            // Override the default documentation mode.
            var documentationMode = ParsedCommandLineArguments.DocumentationPath != null ? DocumentationMode.Diagnose : DocumentationMode.Parse;
            return ParsedCommandLineArguments.ParseOptions.WithDocumentationMode(documentationMode);
        }
    }
}

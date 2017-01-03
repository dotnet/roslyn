// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal interface IDeferredProjectWorkspaceService : IWorkspaceService
    {
        /// <summary>
        /// Returns a mapping of project file path to information about that project.
        /// </summary>
        Task<IReadOnlyDictionary<string, DeferredProjectInformation>> GetDeferredProjectInfoForConfigurationAsync(
            string solutionConfiguration,
            CancellationToken cancellationToken);
    }

    internal struct DeferredProjectInformation
    {
        public DeferredProjectInformation(
            string targetPath,
            ImmutableArray<string> commandLineArgs,
            ImmutableArray<string> referencedProjectFilePaths)
        {
            TargetPath = targetPath;
            CommandLineArguments = commandLineArgs;
            ReferencedProjectFilePaths = referencedProjectFilePaths;
        }

        /// <summary>
        /// The full path to the binary this project would create if built *by msbuild*.
        /// May be different than the /out argument in <see cref="CommandLineArguments"/>.
        /// </summary>
        public string TargetPath { get; }

        /// <summary>
        /// The set of command line arguments that can be used to build this project.
        /// </summary>

        public ImmutableArray<string> CommandLineArguments { get; }

        /// <summary>
        /// The paths to referenced projects.
        /// </summary>
        public ImmutableArray<string> ReferencedProjectFilePaths { get; }

    }
}

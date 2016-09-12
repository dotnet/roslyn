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
        bool IsDeferredProjectLoadEnabled { get; }
        Task<IReadOnlyDictionary<string, DeferredProjectInformation>> GetCommandLineArgumentsAndProjectReferencesForProjectAsync(
            string solutionConfiguration, CancellationToken cancellationToken);
    }

    internal struct DeferredProjectInformation
    {
        public DeferredProjectInformation(
            string targetPath,
            ImmutableArray<string> commandLineArgs,
            ImmutableArray<string> projectReferences)
        {
            TargetPath = targetPath;
            CommandLineArguments = commandLineArgs;
            ProjectReferences = projectReferences;
        }

        public string TargetPath { get; }
        public ImmutableArray<string> CommandLineArguments { get; }
        public ImmutableArray<string> ProjectReferences { get; }

    }
}

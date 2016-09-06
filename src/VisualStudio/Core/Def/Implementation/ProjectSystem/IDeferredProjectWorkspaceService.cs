using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal interface IDeferredProjectWorkspaceService : IWorkspaceService
    {
        bool IsDeferredProjectLoadEnabled { get; }
        Task<ValueTuple<ImmutableArray<string>, ImmutableArray<string>>> GetCommandLineArgumentsAndProjectReferencesForProjectAsync(string projectFilePath);
    }
}

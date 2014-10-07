using System.Collections.Generic;
using Roslyn.Services;

namespace Roslyn.Services.Host
{
    /// <summary>
    /// Used by HostWorkspace to access information about a solution. This data is read once and is
    /// expected to be the state of the solution when it is first loaded or declared.
    /// </summary>
    public interface ISolutionInfo
    {
        /// <summary>
        /// The unique Id of the solution.
        /// </summary>
        SolutionId Id { get; }

        /// <summary>
        /// A list of projects initially associated with the solution.
        /// </summary>
        IEnumerable<IProjectInfo> Projects { get; }

        /// <summary>
        /// The version of the solution.
        /// </summary>
        VersionStamp Version { get; }

        /// <summary>
        /// The path to the solution file, or null if there is no solution file.
        /// </summary>
        string FilePath { get; }
    }
}
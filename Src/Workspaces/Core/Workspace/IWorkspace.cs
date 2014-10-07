using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services.WorkspaceServices;

namespace Roslyn.Services
{
    /// <summary>
    /// A workspace provides access to a active set of source code projects and documents and their
    /// associated syntax trees, compilations and semantic models. A workspace has a current solution
    /// that is an immutable snapshot of the projects and documents. This property may change over time 
    /// as the workspace is updated either from live interactions in the environment or via call to the
    /// workspace's ApplyChanges method.
    /// </summary>
    public interface IWorkspace
    {
        /// <summary>
        /// The kind of the workspace.
        /// </summary>
        string Kind { get; }

        /// <summary>
        /// The current solution. 
        /// 
        /// The solution is an immutable model of the current set of projects and source documents.
        /// It provides access to source text, syntax trees and semantics.
        /// 
        /// This property may change as the workspace reacts to changes in the environment or 
        /// after ApplyChanges is called.
        /// </summary>
        Solution CurrentSolution { get; }

        /// <summary>
        /// An event raised whenever the current solution is changed.
        /// </summary>
        event EventHandler<WorkspaceChangeEventArgs> WorkspaceChanged;

        /// <summary>
        /// An event raised whenever the workspace or part of its solution model
        /// fails to access a file or other external resource.
        /// </summary>
        event EventHandler<WorkspaceDiagnosticEventArgs> WorkspaceFailed;

        /// <summary>
        /// Determines if the specific kind of change is supported by the ApplyChanges method.
        /// </summary>
        bool CanApplyChange(ApplyChangesKind changeKind);

        /// <summary>
        /// Apply changes made to a solution back to the workspace.
        /// 
        /// The specified solution must be one that originated from this workspace. If it is not, or the workspace
        /// has been updated since the solution was obtained from the workspace, then this method returns false.
        /// </summary>
        bool TryApplyChanges(Solution solutionWithChanges);
    }
}
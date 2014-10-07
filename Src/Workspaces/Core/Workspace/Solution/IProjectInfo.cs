using System;
using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services
{
    /// <summary>
    /// Used by HostWorkspace to access information about a project. This data is read once and is
    /// expected to be the state of the project when it is first loaded or declared.
    /// </summary>
    public interface IProjectInfo
    {
        /// <summary>
        /// The unique Id of the project.
        /// </summary>
        ProjectId Id { get; }

        /// <summary>
        /// The version of the project.
        /// </summary>
        VersionStamp Version { get; }

        /// <summary>
        /// The name of the project. This may differ from the project's filename.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The name of the assembly that this project will create, without file extension.
        /// </summary>,
        string AssemblyName { get; }

        /// <summary>
        /// The language of the project.
        /// </summary>
        string Language { get; }

        /// <summary>
        /// The path to the project file or null if there is no project file.
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// The initial compilation options for the project.
        /// </summary>
        CommonCompilationOptions CompilationOptions { get; }

        /// <summary>
        /// The initial parse options for the source code documents in this project.
        /// </summary>
        CommonParseOptions ParseOptions { get; }

        /// <summary>
        /// The list of source documents initially associated with the project.
        /// </summary>
        IEnumerable<IDocumentInfo> Documents { get; }

        /// <summary>
        /// The project references initially defined for the project.
        /// </summary>
        IEnumerable<ProjectReference> ProjectReferences { get; }

        /// <summary>
        /// The metadata references initially defined for the project.
        /// </summary>
        IEnumerable<MetadataReference> MetadataReferences { get; }

        /// <summary>
        /// Returns the file resolver for the project.
        /// </summary>
        FileResolver FileResolver { get; }

        /// <summary>
        /// Returns true if this is a submission project for interactive sessions.
        /// </summary>
        bool IsSubmission { get; }

        /// <summary>
        /// Type of the host object.
        /// </summary>
        Type HostObjectType { get; }
    }
}

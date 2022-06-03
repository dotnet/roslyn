// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// Various operations that occur during a project load.
    /// </summary>
    public enum ProjectLoadOperation
    {
        /// <summary>
        /// Represents the MSBuild evaluation of a project. This occurs before <see cref="Build"/>
        /// to evaluate the project file before any tasks are executed.
        /// </summary>
        Evaluate,
        /// <summary>
        /// Represents the MSBuild design-time build of a project. This build does not produce any
        /// any compiled binaries, but computes the information need to compile the project, such
        /// as compiler flags, source files and references.
        /// </summary>
        Build,
        /// <summary>
        /// Represents a resolution step that occurs after the MSBuild design-time build. This step
        /// performs final logic to resolve metadata and project references and produces the information
        /// needed to populate a <see cref="Workspace"/>.
        /// </summary>
        Resolve
    }
}

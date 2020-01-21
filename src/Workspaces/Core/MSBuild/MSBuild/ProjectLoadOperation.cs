// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// Various operations that occur during a project load.
    /// </summary>
    public enum ProjectLoadOperation
    {
        /// <summary>
        /// Represents the MSBuild evaluation of a project. This occurs before <see cref="Build"/>
        /// to evalute the project file before any tasks are executed.
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

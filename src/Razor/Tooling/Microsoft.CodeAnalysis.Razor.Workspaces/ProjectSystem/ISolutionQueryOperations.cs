// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal interface ISolutionQueryOperations
{
    /// <summary>
    /// Returns all Razor project snapshots.
    /// </summary>
    IEnumerable<IProjectSnapshot> GetProjects();

    /// <summary>
    ///  Returns all Razor valid project snapshots that contain the given document file path.
    /// </summary>
    /// <param name="documentFilePath">A file path to a Razor document.</param>
    /// <remarks>
    ///  In multi-targeting scenarios, this will return a project for each target that the
    ///  contains the document.
    /// </remarks>
    ImmutableArray<IProjectSnapshot> GetProjectsContainingDocument(string documentFilePath);
}

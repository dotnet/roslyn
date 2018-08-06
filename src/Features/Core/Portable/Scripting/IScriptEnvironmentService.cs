// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// Provides information on the current script environment.
    /// </summary>
    internal interface IScriptEnvironmentService : IWorkspaceService
    {
        /// <summary>
        /// Full path of a directory to be used to resolve relative paths specified in #r and #load directives
        /// that are used in script that itself doesn't have a path (e.g. interactive submission).
        /// </summary>
        string BaseDirectory { get; }

        /// <summary>
        /// Search paths used to find metadata references (#r directive).
        /// </summary>
        ImmutableArray<string> MetadataReferenceSearchPaths { get; }

        /// <summary>
        /// Search paths uses to find source references (#load directive).
        /// </summary>
        ImmutableArray<string> SourceReferenceSearchPaths { get; }
    }
}

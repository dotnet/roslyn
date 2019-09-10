// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Scripting;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// Environment corresponding to csi running a script with default command line arguments.
    /// </summary>
    [ExportWorkspaceService(typeof(IScriptEnvironmentService), WorkspaceKind.MiscellaneousFiles), Shared]
    internal sealed class MiscellaneousFilesScriptEnvironmentService : IScriptEnvironmentService
    {
        private static readonly ImmutableArray<string> s_metadataReferenceSearchPaths = ImmutableArray.Create(RuntimeEnvironment.GetRuntimeDirectory());

        [ImportingConstructor]
        public MiscellaneousFilesScriptEnvironmentService()
        {
        }

        public ImmutableArray<string> MetadataReferenceSearchPaths => s_metadataReferenceSearchPaths;
        public ImmutableArray<string> SourceReferenceSearchPaths => ImmutableArray<string>.Empty;
        public string BaseDirectory => null;
    }
}

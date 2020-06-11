// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MiscellaneousFilesScriptEnvironmentService()
        {
        }

        public ImmutableArray<string> MetadataReferenceSearchPaths => s_metadataReferenceSearchPaths;
        public ImmutableArray<string> SourceReferenceSearchPaths => ImmutableArray<string>.Empty;
        public string BaseDirectory => null;
    }
}

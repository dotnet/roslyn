// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal class MetadataAsSourceWorkspace : Workspace
    {
        public readonly MetadataAsSourceFileService FileService;

        public MetadataAsSourceWorkspace(MetadataAsSourceFileService fileService, HostServices hostServices)
            : base(hostServices, WorkspaceKind.MetadataAsSource)
        {
            this.FileService = fileService;
        }
    }
}

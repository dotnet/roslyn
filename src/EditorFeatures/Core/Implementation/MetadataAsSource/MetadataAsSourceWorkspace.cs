// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor.Implementation.MetadataAsSource
{
    internal class MetadataAsSourceWorkspace : Workspace
    {
        public readonly MetadataAsSourceFileService FileService;

        public MetadataAsSourceWorkspace(MetadataAsSourceFileService fileService, HostServices hostServices)
            : base(hostServices, "MetadataAsSource")
        {
            this.FileService = fileService;
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    ///  Known workspace kinds
    /// </summary>
    public static class WorkspaceKind
    {
        public const string Host = "Host";
        public const string Debugger = "Debugger";
        public const string Interactive = "Interactive";
        public const string MetadataAsSource = "MetadataAsSource";
        public const string MiscellaneousFiles = "MiscellaneousFiles";
        public const string Preview = "Preview";
    }
}

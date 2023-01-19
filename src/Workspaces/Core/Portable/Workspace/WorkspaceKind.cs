// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    ///  Known workspace kinds
    /// </summary>
    public static class WorkspaceKind
    {
        public const string Host = nameof(Host);
        public const string Debugger = nameof(Debugger);
        public const string Interactive = nameof(Interactive);
        public const string MetadataAsSource = nameof(MetadataAsSource);
        public const string MiscellaneousFiles = nameof(MiscellaneousFiles);
        public const string Preview = nameof(Preview);
        public const string MSBuild = "MSBuildWorkspace"; // This string is specifically used to avoid a breaking change.

        /// <summary>
        /// <see cref="AdhocWorkspace"/> kind.
        /// </summary>
        internal const string Custom = "Custom";

        internal const string CloudEnvironmentClientWorkspace = nameof(CloudEnvironmentClientWorkspace);
        internal const string RemoteWorkspace = nameof(RemoteWorkspace);
    }
}

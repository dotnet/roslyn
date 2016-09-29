// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// The layer of an exported service.  
    /// 
    /// If there are multiple definitions of a service, the <see cref="ServiceLayer"/> is used to determine which is used.
    /// </summary>
    public static class ServiceLayer
    {
        /// <summary>
        /// Service layer that overrides <see cref="Editor"/>, <see cref="Desktop"/> and <see cref="Default"/>.
        /// </summary>
        public const string Host = nameof(Host);

        /// <summary>
        /// Service layer that overrides <see cref="Desktop" /> and <see cref="Default"/>.
        /// </summary>
        public const string Editor = nameof(Editor);

        /// <summary>
        /// Service layer that overrides <see cref="Default"/>.
        /// </summary>
        public const string Desktop = nameof(Desktop);

        /// <summary>
        /// The base service layer.
        /// </summary>
        public const string Default = nameof(Default);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Emit
{
    public enum SemanticEditKind
    {
        /// <summary>
        /// No change.
        /// </summary>
        None = 0,

        /// <summary>
        /// Symbol is updated.
        /// </summary>
        Update = 1,

        /// <summary>
        /// Symbol is inserted.
        /// </summary>
        Insert = 2,

        /// <summary>
        /// Symbol is deleted.
        /// </summary>
        Delete = 3,

        /// <summary>
        /// Existing symbol is replaced by its new version.
        /// </summary>
        Replace = 4
    }

    public enum ResourceEditKind
    {
        Update,
        Insert,
        Delete,
    }
}

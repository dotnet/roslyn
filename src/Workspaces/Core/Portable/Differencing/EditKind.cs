// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Differencing
{
    public enum EditKind
    {
        /// <summary>
        /// No change.
        /// </summary>
        None = 0,

        /// <summary>
        /// Node value was updated.
        /// </summary>
        Update = 1,

        /// <summary>
        /// Node was inserted.
        /// </summary>
        Insert = 2,

        /// <summary>
        /// Node was deleted.
        /// </summary>
        Delete = 3,

        /// <summary>
        /// Node changed parent.
        /// </summary>
        Move = 4,

        /// <summary>
        /// Node changed position within its parent. The parent nodes of the old node and the new node are matching.
        /// </summary>
        Reorder = 5,
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Differencing;

namespace Microsoft.CodeAnalysis.SyntaxDifferencing
{
    public enum SyntaxEditKind
    {
        /// <summary>
        /// No change.
        /// </summary>
        None = EditKind.None,

        /// <summary>
        /// Node value was updated.
        /// </summary>
        Update = EditKind.Update,

        /// <summary>
        /// Node was inserted.
        /// </summary>
        Insert = EditKind.Insert,

        /// <summary>
        /// Node was deleted.
        /// </summary>
        Delete = EditKind.Delete,

        /// <summary>
        /// Node changed parent.
        /// </summary>
        Move = EditKind.Move,

        /// <summary>
        /// Node changed position within its parent. The parent nodes of the old node and the new node are matching.
        /// </summary>
        Reorder = EditKind.Reorder,
    }
}
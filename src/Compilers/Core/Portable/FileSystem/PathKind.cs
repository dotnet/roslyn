// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.Utilities
{
    internal enum PathKind
    {
        /// <summary>
        /// Null or empty.
        /// </summary>
        Empty,

        /// <summary>
        /// "file"
        /// </summary>
        Relative,

        /// <summary>
        /// ".\file"
        /// </summary>
        RelativeToCurrentDirectory,

        /// <summary>
        /// "..\file"
        /// </summary>
        RelativeToCurrentParent,

        /// <summary>
        /// "\dir\file"
        /// </summary>
        RelativeToCurrentRoot,

        /// <summary>
        /// "C:dir\file"
        /// </summary>
        RelativeToDriveDirectory,

        /// <summary>
        /// "C:\file" or "\\machine" (UNC).
        /// </summary>
        Absolute,
    }
}

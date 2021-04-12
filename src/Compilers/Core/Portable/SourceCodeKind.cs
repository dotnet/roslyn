// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the C# or VB source code kind.
    /// </summary>
    public enum SourceCodeKind
    {
        /// <summary>
        /// No scripting. Used for .cs/.vb file parsing.
        /// </summary>
        Regular = 0,

        /// <summary>
        /// Allows top-level statements, declarations, and optional trailing expression. 
        /// Used for parsing .csx/.vbx and interactive submissions.
        /// </summary>
        Script = 1,

        /// <summary>
        /// The same as <see cref="Script"/>.
        /// </summary>
        [Obsolete("Use Script instead", error: false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        Interactive = 2,
    }
}

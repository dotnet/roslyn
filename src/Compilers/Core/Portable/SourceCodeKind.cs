// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

    internal static partial class SourceCodeKindExtensions
    {
        internal static bool IsValid(this SourceCodeKind value)
        {
            return value >= SourceCodeKind.Regular && value <= SourceCodeKind.Script;
        }
    }
}

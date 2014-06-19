// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// Allows top-level statements and declarations. Used for .csx/.vbx file parsing.
        /// </summary>
        Script = 1,

        /// <summary>
        /// Allows top-level expressions and optional semicolon.
        /// </summary>
        Interactive = 2,
    }

    internal static partial class SourceCodeKindExtensions
    {
        internal static bool IsValid(this SourceCodeKind value)
        {
            return value >= SourceCodeKind.Regular && value <= SourceCodeKind.Interactive;
        }
    }
}
// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Denotes the kind of reference parameter.
    /// </summary>
    public enum RefKind : byte
    {
        /// <summary>
        /// Indicates a "value" parameter.
        /// </summary>
        None,

        /// <summary>
        /// Indicates a "ref" parameter.
        /// </summary>
        Ref,

        /// <summary>
        /// Indicates an "out" parameter.
        /// </summary>
        Out
    }

    internal static class RefKindExtensions
    {
        internal static string ToDisplayString(this RefKind kind)
        {
            switch (kind)
            {
                case RefKind.Out: return "out";
                case RefKind.Ref: return "ref";
                default: return null;
            }
        }

        internal static string ToPrefix(this RefKind kind)
        {
            switch (kind)
            {
                case RefKind.Out: return "out ";
                case RefKind.Ref: return "ref ";
                default: return string.Empty;
            }
        }
    }
}
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

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
        None = 0,

        /// <summary>
        /// Indicates a "ref" parameter.
        /// </summary>
        Ref = 1,

        /// <summary>
        /// Indicates an "out" parameter.
        /// </summary>
        Out = 2,

        /// <summary>
        /// Indicates a "ref readonly" parameter.
        /// </summary>
        RefReadOnly = 3,
    }

    internal static class RefKindExtensions
    {
        internal static string ToParameterDisplayString(this RefKind kind)
        {
            switch (kind)
            {
                case RefKind.Out: return "out";
                case RefKind.Ref: return "ref";
                case RefKind.RefReadOnly: return "ref readonly";
                default: throw new ArgumentException($"Invalid RefKind for parameters: {kind}");
            }
        }

        internal static string ToArgumentDisplayString(this RefKind kind)
        {
            switch (kind)
            {
                case RefKind.Out: return "out";
                case RefKind.Ref: return "ref";
                default: throw new ArgumentException($"Invalid RefKind for arguments: {kind}");
            }
        }

        internal static string ToPrefix(this RefKind kind)
        {
            switch (kind)
            {
                case RefKind.Out: return "out ";
                case RefKind.Ref: return "ref ";
                case RefKind.RefReadOnly: return "ref readonly ";
                default: return string.Empty;
            }
        }
    }
}

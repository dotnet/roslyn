// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    internal static partial class SourceCodeKindExtensions
    {
        internal static SourceCodeKind MapSpecifiedToEffectiveKind(this SourceCodeKind kind)
        {
            switch (kind)
            {
                case SourceCodeKind.Script:
#pragma warning disable CS0618 // SourceCodeKind.Interactive is obsolete
                case SourceCodeKind.Interactive:
#pragma warning restore CS0618 // SourceCodeKind.Interactive is obsolete
                    return SourceCodeKind.Script;

                case SourceCodeKind.Regular:
                default:
                    return SourceCodeKind.Regular;
            }
        }

        internal static bool IsValid(this SourceCodeKind value)
        {
            return value >= SourceCodeKind.Regular && value <= SourceCodeKind.Script;
        }
    }
}

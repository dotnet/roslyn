// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Extension methods for the <see cref="BinderFlags"/> type.
    /// </summary>
    internal static class BinderFlagsExtensions
    {
        public static bool Includes(this BinderFlags self, BinderFlags other)
        {
            return (self & other) == other;
        }

        public static bool IncludesAny(this BinderFlags self, BinderFlags other)
        {
            return (self & other) != 0;
        }
    }
}

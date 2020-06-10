// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SolutionExtensions
    {
        public static void WriteTo(this IObjectWritable @object, ObjectWriter writer)
            => @object.WriteTo(writer);
    }
}

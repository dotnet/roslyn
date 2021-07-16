// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    // TODO: move to ObjectReader (see https://github.com/dotnet/roslyn/issues/45837)
    internal static class ObjectReaderExtensions
    {
        public static T[] ReadArray<T>(this ObjectReader reader)
            => (T[])reader.ReadValue();
    }
}

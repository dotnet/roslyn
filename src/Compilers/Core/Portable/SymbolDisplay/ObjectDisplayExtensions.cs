// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    internal static class ObjectDisplayExtensions
    {
        /// <summary>
        /// Determines if a flag is set on the <see cref="ObjectDisplayOptions"/> enum.
        /// </summary>
        /// <param name="options">The value to check.</param>
        /// <param name="flag">An enum field that specifies the flag.</param>
        /// <returns>Whether the <paramref name="flag"/> is set on the <paramref name="options"/>.</returns>
        internal static bool IncludesOption(this ObjectDisplayOptions options, ObjectDisplayOptions flag)
        {
            return (options & flag) == flag;
        }
    }
}

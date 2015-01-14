// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class SyntaxKindExtensions
    {
        /// <summary>
        /// Determine if the given <see cref="SyntaxKind"/> array contains the given kind.
        /// </summary>
        /// <param name="kinds">Array to search</param>
        /// <param name="kind">Sought value</param>
        /// <returns>True if <paramref name = "kinds"/> contains the value<paramref name= "kind"/>.</returns>
        /// <remarks>PERF: Not using Array.IndexOf here because it results in a call to IndexOf on the
        /// default EqualityComparer for SyntaxKind.The default comparer for SyntaxKind is the
        /// ObjectEqualityComparer which results in boxing allocations.</remarks>
        public static bool Contains(this SyntaxKind[] kinds, SyntaxKind kind)
        {
            foreach (var k in kinds)
            {
                if (k == kind)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

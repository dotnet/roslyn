// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A class used to provide XML documentation to the compiler for members from metadata. A
    /// custom implementation of this class should be returned from a DocumentationResolver to provide XML
    /// documentation comments from custom caches or locations.
    /// </summary>
    public abstract partial class DocumentationProvider
    {
        public static DocumentationProvider Default { get; } = new NullDocumentationProvider();

        protected DocumentationProvider()
        {
        }

        /// <summary>
        /// Fetches a documentation comment for the given member ID.
        /// </summary>
        /// <param name="documentationMemberID">The documentation member ID of the item to fetch.</param>
        /// <param name="preferredCulture">The preferred culture to receive a comment in. Null if
        /// there is no preference. This is a preference only, and providers may choose to provide
        /// results from another culture if the preferred culture was unavailable.</param>
        /// <param name="cancellationToken">A cancellation token for the search.</param>
        /// <returns>A DocumentationComment.</returns>
        protected internal abstract string? GetDocumentationForSymbol(
            string documentationMemberID,
            CultureInfo preferredCulture,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// DocumentationProviders are compared when determining whether an AssemblySymbol can be reused.
        /// Hence, if multiple instances can represent the same documentation, it is imperative that
        /// Equals (and GetHashCode) be overridden to capture this fact.  Otherwise, it is possible to end
        /// up with multiple AssemblySymbols for the same assembly, which plays havoc with the type hierarchy.
        /// </summary>
        public abstract override bool Equals(object? obj);

        /// <summary>
        /// DocumentationProviders are compared when determining whether an AssemblySymbol can be reused.
        /// Hence, if multiple instances can represent the same documentation, it is imperative that
        /// GetHashCode (and Equals) be overridden to capture this fact.  Otherwise, it is possible to end
        /// up with multiple AssemblySymbols for the same assembly, which plays havoc with the type hierarchy.
        /// </summary>
        public abstract override int GetHashCode();
    }
}

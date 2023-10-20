// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MapCode
{
    /// <summary>
    /// A service to identify, map, and integrate code snippets obtained from the AI model into a target document. 
    /// A Mapper's primary responsibility is twofold:
    /// 1. Replace existing code. The mapper should determine if the code provided by the model already exists in the target and needs to be replaced.
    /// 2. Insert new code. The mapper should recognize new code and identify the correct location for its insertion in the target document.
    /// </summary>
    internal interface IMapCodeService : ILanguageService
    {
        /// <summary>
        /// Map code snippets into the target document.
        /// </summary>
        /// <param name="document">Target document for mapping</param>
        /// <param name="contents">Code snippet that we we are attempting to map into the target document</param>
        /// <param name="focusLocations">Prioritized Locations to be used when applying heuristics. For example, cursor location, related classes (in other documents), viewport, etc. Earlier items should be considered higher priority</param>
        /// <param name="formatMappedCode">Indicate whether the mapped code should be formatted</param>
        /// <returns>A document with code snippets mapped into; or null if mapping failed</returns>
        Task<Document?> MapCodeAsync(
            Document document,
            ImmutableArray<string> contents,
            ImmutableArray<(Document, TextSpan)> focusLocations,
            bool formatMappedCode,
            CancellationToken cancellationToken);
    }
}

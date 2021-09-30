// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal interface IWorkspaceVenusSpanMappingService : IWorkspaceService
    {
        /// <summary>
        /// Given the original location of the diagnostic and the mapped line info based on line directives in source,
        /// apply any necessary adjustments to these diagnostic spans and returns the effective source span for the diagnostic.
        /// For example, for Venus, we might change the mapped location to be the location in the primary buffer.
        /// Additionally, if the secondary buffer location is outside visible user code, then the original location is also adjusted to be within visible user code.
        /// </summary>
        void GetAdjustedDiagnosticSpan(DocumentId documentId, Location location, out TextSpan span, out FileLinePositionSpan originalLineInfo, out FileLinePositionSpan mappedLineInfo);
    }
}

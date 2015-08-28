// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ReassignedVariable
{
    /// <summary>
    /// Service which can analyze a span of a document and identify all locations of parameters or locals that are ever
    /// reassigned.  Note that the locations provided are not the reassignment points.  Rather if a local or parameter
    /// is ever reassigned, these are all the locations of those locals or parameters within that span.
    /// </summary>
    internal interface IReassignedVariableService : ILanguageService
    {
        Task<ImmutableArray<TextSpan>> GetLocationsAsync(Document document, ImmutableArray<TextSpan> textSpans, CancellationToken cancellationToken);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Indentation
{
    internal interface IInferredIndentationService : IWorkspaceService
    {
        Task<DocumentOptionSet> GetDocumentOptionsWithInferredIndentationAsync(Document document, bool explicitFormat, CancellationToken cancellationToken);
    }
}

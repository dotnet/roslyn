// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.GenerateImplementation;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot
{
    internal interface IExternalCSharpCopilotGenerateImplementationService
    {
        Task<ImplementationDetailsWrapper> ImplementNotImplementedExceptionAsync(Document document, SyntaxNode throwNode, CancellationToken cancellationToken);
    }
}

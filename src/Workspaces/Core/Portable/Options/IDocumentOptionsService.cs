// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Allows other languages that call into Roslyn to specify their own language's
    /// document options instead of using the C#/VB document options.
    /// </summary>
    internal interface IDocumentOptionsService : IDocumentService
    {
        Task<IDocumentOptions?> GetOptionsForDocumentAsync(Document document, CancellationToken cancellationToken);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddMissingImports
{
    internal interface IAddMissingImportsFeatureService : ILanguageService
    {
        Task<bool> HasMissingImportsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);

        Task<Project> AddMissingImportsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);
    }
}

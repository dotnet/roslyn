// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IAsyncGoToDefinitionService : ILanguageService
    {
        Task<INavigableLocation?> FindDefinitionLocationAsync(Document document, int position, CancellationToken cancellationToken);
    }
}

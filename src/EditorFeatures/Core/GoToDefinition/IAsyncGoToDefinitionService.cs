// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IAsyncGoToDefinitionService : ILanguageService
    {
        /// <summary>
        /// If the supplied <paramref name="position"/> is on a code construct with a navigable location, then this
        /// returns that <see cref="INavigableLocation"/>.  The <see cref="TextSpan"/> returned in the span of the
        /// symbol in the code that references that navigable location.  e.g. the full identifier token that the
        /// position is within.
        /// </summary>
        Task<(INavigableLocation? location, TextSpan symbolSpan)> FindDefinitionLocationAsync(
            Document document, int position, bool includeType, CancellationToken cancellationToken);
    }
}

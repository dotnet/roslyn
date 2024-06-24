// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor.FindUsages
{
    internal class FSharpFindUsagesContext : IFSharpFindUsagesContext
    {
        private readonly IFindUsagesContext _context;
        private readonly CancellationToken _cancellationToken;

        public FSharpFindUsagesContext(IFindUsagesContext context, CancellationToken cancellationToken)
        {
            _context = context;
            _cancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken => _cancellationToken;

        public Task OnDefinitionFoundAsync(FSharp.FindUsages.FSharpDefinitionItem definition)
        {
            return _context.OnDefinitionFoundAsync(definition.RoslynDefinitionItem, _cancellationToken).AsTask();
        }

        public Task OnReferenceFoundAsync(FSharp.FindUsages.FSharpSourceReferenceItem reference)
        {
            return _context.OnReferencesFoundAsync(IAsyncEnumerableExtensions.SingletonAsync(reference.RoslynSourceReferenceItem), _cancellationToken).AsTask();
        }

        public Task ReportMessageAsync(string message)
        {
            return _context.ReportNoResultsAsync(message, _cancellationToken).AsTask();
        }

        public Task ReportProgressAsync(int current, int maximum)
        {
            return Task.CompletedTask;
        }

        public Task SetSearchTitleAsync(string title)
        {
            return _context.SetSearchTitleAsync(title, _cancellationToken).AsTask();
        }
    }
}

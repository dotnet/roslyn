// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.CodeAnalysis.AutomaticCompletion
{
    internal abstract class AbstractBraceCompletionServiceFactory : ForegroundThreadAffinitizedObject, IBraceCompletionServiceFactory
    {
        private readonly ImmutableArray<IBraceCompletionService> _braceCompletionServices;

        protected AbstractBraceCompletionServiceFactory(
            IEnumerable<IBraceCompletionService> braceCompletionServices,
            IThreadingContext threadingContext)
            : base(threadingContext)
        {
            _braceCompletionServices = braceCompletionServices.ToImmutableArray();
        }

        public async Task<IBraceCompletionService?> TryGetServiceAsync(Document document, int openingPosition, char openingBrace, CancellationToken cancellationToken)
        {
            foreach (var service in _braceCompletionServices)
            {
                if (await service.CanProvideBraceCompletionAsync(openingBrace, openingPosition, document, cancellationToken).ConfigureAwait(false))
                {
                    return service;
                }
            }

            return null;
        }
    }
}

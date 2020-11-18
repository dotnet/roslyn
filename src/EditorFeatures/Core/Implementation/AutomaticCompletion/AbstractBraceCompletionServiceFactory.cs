// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion
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

        public IBraceCompletionService? TryGetService(Document document, int openingPosition, char openingBrace, CancellationToken cancellationToken)
        {
            this.AssertIsForeground();
            return _braceCompletionServices.SingleOrDefault(service => IsServiceValidForBraceCompletion(service, document, openingPosition, openingBrace, cancellationToken));
        }

        private static bool IsServiceValidForBraceCompletion(IBraceCompletionService service, Document document, int openingPosition, char openingBrace, CancellationToken cancellationToken)
        {
            return service.IsValidForBraceCompletionAsync(openingBrace, openingPosition, document, cancellationToken).WaitAndGetResult(cancellationToken);
        }
    }
}

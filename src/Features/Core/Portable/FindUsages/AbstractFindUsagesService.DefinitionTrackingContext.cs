// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal abstract partial class AbstractFindUsagesService
    {
        /// <summary>
        /// Forwards <see cref="IFindUsagesContext"/> notifications to an underlying <see cref="IFindUsagesContext"/>
        /// while also keeping track of the <see cref="DefinitionItem"/> definitions reported.
        /// 
        /// These can then be used by <see cref="GetThirdPartyDefinitionsAsync"/> to report the
        /// definitions found to third parties in case they want to add any additional definitions
        /// to the results we present.
        /// </summary>
        private sealed class DefinitionTrackingContext : IFindUsagesContext
        {
            private readonly IFindUsagesContext _underlyingContext;
            private readonly object _gate = new();
            private readonly List<DefinitionItem> _definitions = new();

            public DefinitionTrackingContext(IFindUsagesContext underlyingContext)
                => _underlyingContext = underlyingContext;

            public ValueTask<FindUsagesOptions> GetOptionsAsync(string language, CancellationToken cancellationToken)
                => _underlyingContext.GetOptionsAsync(language, cancellationToken);

            public IStreamingProgressTracker ProgressTracker
                => _underlyingContext.ProgressTracker;

            public ValueTask ReportMessageAsync(string message, CancellationToken cancellationToken)
                => _underlyingContext.ReportMessageAsync(message, cancellationToken);

            public ValueTask ReportInformationalMessageAsync(string message, CancellationToken cancellationToken)
                => _underlyingContext.ReportInformationalMessageAsync(message, cancellationToken);

            public ValueTask SetSearchTitleAsync(string title, CancellationToken cancellationToken)
                => _underlyingContext.SetSearchTitleAsync(title, cancellationToken);

            public ValueTask OnReferenceFoundAsync(SourceReferenceItem reference, CancellationToken cancellationToken)
                => _underlyingContext.OnReferenceFoundAsync(reference, cancellationToken);

            public ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken)
            {
                lock (_gate)
                {
                    _definitions.Add(definition);
                }

                return _underlyingContext.OnDefinitionFoundAsync(definition, cancellationToken);
            }

            public ImmutableArray<DefinitionItem> GetDefinitions()
            {
                lock (_gate)
                {
                    return _definitions.ToImmutableArray();
                }
            }
        }
    }
}

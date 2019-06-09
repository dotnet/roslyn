// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal abstract partial class AbstractFindUsagesService
    {
        /// <summary>
        /// Forwards <see cref="IFindUsagesContext"/> notifications to an underlying <see cref="IFindUsagesContext"/>
        /// while also keeping track of the <see cref="DefinitionItem"/> definitions reported.
        /// 
        /// These can then be used by <see cref="GetThirdPartyDefinitions"/> to report the
        /// definitions found to third parties in case they want to add any additional definitions
        /// to the results we present.
        /// </summary>
        private class DefinitionTrackingContext : IFindUsagesContext
        {
            private readonly IFindUsagesContext _underlyingContext;
            private readonly object _gate = new object();
            private readonly List<DefinitionItem> _definitions = new List<DefinitionItem>();

            public DefinitionTrackingContext(IFindUsagesContext underlyingContext)
            {
                _underlyingContext = underlyingContext;
            }

            public CancellationToken CancellationToken
                => _underlyingContext.CancellationToken;

            public Task ReportMessageAsync(string message)
                => _underlyingContext.ReportMessageAsync(message);

            public Task SetSearchTitleAsync(string title)
                => _underlyingContext.SetSearchTitleAsync(title);

            public Task OnReferenceFoundAsync(SourceReferenceItem reference)
                => _underlyingContext.OnReferenceFoundAsync(reference);

            public Task ReportProgressAsync(int current, int maximum)
                => _underlyingContext.ReportProgressAsync(current, maximum);

            public Task OnDefinitionFoundAsync(DefinitionItem definition)
            {
                lock (_gate)
                {
                    _definitions.Add(definition);
                }

                return _underlyingContext.OnDefinitionFoundAsync(definition);
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

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy.Finders
{
    internal class CallToOverrideFinder : AbstractCallFinder
    {
        public CallToOverrideFinder(ISymbol symbol, ProjectId projectId, IAsynchronousOperationListener asyncListener, CallHierarchyProvider provider)
            : base(symbol, projectId, asyncListener, provider)
        {
        }

        public override string DisplayName => EditorFeaturesResources.Calls_To_Overrides;

        protected override async Task<IEnumerable<SymbolCallerInfo>> GetCallersAsync(ISymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            var overrides = await SymbolFinder.FindOverridesAsync(symbol, project.Solution, cancellationToken: cancellationToken).ConfigureAwait(false);
            var callsToOverrides = new List<SymbolCallerInfo>();

            foreach (var @override in overrides)
            {
                var calls = await SymbolFinder.FindCallersAsync(@override, project.Solution, documents, cancellationToken).ConfigureAwait(false);

                foreach (var call in calls)
                {
                    if (call.IsDirect)
                    {
                        callsToOverrides.Add(call);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            return callsToOverrides;
        }
    }
}

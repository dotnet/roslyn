// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.CallHierarchy;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy.Finders
{
    internal class OverridingMemberFinder : AbstractCallFinder
    {
        public OverridingMemberFinder(ISymbol symbol, ProjectId projectId, IAsynchronousOperationListener asyncListener, CallHierarchyProvider provider)
            : base(symbol, projectId, asyncListener, provider)
        {
        }

        public override string DisplayName => EditorFeaturesResources.Overrides_;

        public override string SearchCategory => CallHierarchyPredefinedSearchCategoryNames.Overrides;

        protected override Task<IEnumerable<SymbolCallerInfo>> GetCallersAsync(ISymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override async Task SearchWorkerAsync(ISymbol symbol, Project project, ICallHierarchySearchCallback callback, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            var overrides = await SymbolFinder.FindOverridesAsync(symbol, project.Solution, cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (var @override in overrides)
            {
                var sourceLocations = @override.DeclaringSyntaxReferences.Select(d => project.Solution.GetDocument(d.SyntaxTree)).WhereNotNull();
                var bestLocation = sourceLocations.FirstOrDefault(d => documents == null || documents.Contains(d));
                if (bestLocation != null)
                {
                    var item = await Provider.CreateItemAsync(@override, bestLocation.Project, SpecializedCollections.EmptyEnumerable<Location>(), cancellationToken).ConfigureAwait(false);
                    callback.AddResult(item);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }
    }
}

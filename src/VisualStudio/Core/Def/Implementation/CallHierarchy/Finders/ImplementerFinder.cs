﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal class ImplementerFinder : AbstractCallFinder
    {
        public ImplementerFinder(ISymbol symbol, ProjectId projectId, IAsynchronousOperationListener asyncListener, CallHierarchyProvider provider)
            : base(symbol, projectId, asyncListener, provider)
        {
        }

        public override string DisplayName
        {
            get
            {
                return string.Format(EditorFeaturesResources.Implements_0, SymbolName);
            }
        }

        protected override Task<IEnumerable<SymbolCallerInfo>> GetCallers(ISymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override async Task SearchWorkerAsync(ISymbol symbol, Project project, ICallHierarchySearchCallback callback, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            var implementations = await SymbolFinder.FindImplementationsAsync(symbol, project.Solution, cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (var implementation in implementations)
            {
                var sourceLocations = implementation.DeclaringSyntaxReferences.Select(d => project.Solution.GetDocument(d.SyntaxTree)).WhereNotNull();
                var bestLocation = sourceLocations.FirstOrDefault(d => documents == null || documents.Contains(d));
                if (bestLocation != null)
                {
                    var item = await Provider.CreateItem(implementation, bestLocation.Project, SpecializedCollections.EmptyEnumerable<Location>(), cancellationToken).ConfigureAwait(false);
                    callback.AddResult(item);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }
    }
}

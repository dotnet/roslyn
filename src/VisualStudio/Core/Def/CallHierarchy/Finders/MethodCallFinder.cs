// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.CallHierarchy;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy.Finders
{
    internal class MethodCallFinder : AbstractCallFinder
    {
        public MethodCallFinder(ISymbol symbol, ProjectId projectId, IAsynchronousOperationListener asyncListener, CallHierarchyProvider provider)
            : base(symbol, projectId, asyncListener, provider)
        {
        }

        public override string DisplayName
        {
            get
            {
                return string.Format(EditorFeaturesResources.Calls_To_0, SymbolName);
            }
        }

        public override string SearchCategory => CallHierarchyPredefinedSearchCategoryNames.Callers;

        protected override async Task<IEnumerable<SymbolCallerInfo>> GetCallersAsync(ISymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            var callers = await SymbolFinder.FindCallersAsync(symbol, project.Solution, documents, cancellationToken).ConfigureAwait(false);
            return callers.Where(c => c.IsDirect);
        }
    }
}

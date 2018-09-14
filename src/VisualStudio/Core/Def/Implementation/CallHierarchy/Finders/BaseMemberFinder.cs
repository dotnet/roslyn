// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy.Finders
{
    internal class BaseMemberFinder : AbstractCallFinder
    {
        private readonly string _text;

        public BaseMemberFinder(ISymbol symbol, ProjectId projectId, IAsynchronousOperationListener asyncListener, CallHierarchyProvider provider)
            : base(symbol, projectId, asyncListener, provider)
        {
            _text = string.Format(EditorFeaturesResources.Calls_To_Base_Member_0, symbol.ToDisplayString());
        }

        public override string DisplayName => _text;

        protected override async Task<IEnumerable<SymbolCallerInfo>> GetCallersAsync(ISymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            var calls = await SymbolFinder.FindCallersAsync(symbol, project.Solution, documents, cancellationToken).ConfigureAwait(false);
            return calls.Where(c => c.IsDirect);
        }
    }
}

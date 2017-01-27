// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal class InterfaceImplementationCallFinder : AbstractCallFinder
    {
        private readonly string _text;

        public InterfaceImplementationCallFinder(ISymbol symbol, Project project, IAsynchronousOperationListener asyncListener, CallHierarchyProvider provider)
            : base(symbol, project, asyncListener, provider)
        {
            _text = string.Format(EditorFeaturesResources.Calls_To_Interface_Implementation_0, symbol.ToDisplayString());
        }

        public override string DisplayName
        {
            get
            {
                return _text;
            }
        }

        public override string SearchCategory
        {
            get
            {
                return CallHierarchyPredefinedSearchCategoryNames.InterfaceImplementations;
            }
        }

        protected override async Task<IEnumerable<SymbolCallerInfo>> GetCallers(ISymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            var calls = await SymbolFinder.FindCallersAsync(symbol, project.Solution, documents, cancellationToken).ConfigureAwait(false);
            return calls.Where(c => c.IsDirect);
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindReferences
{
    internal partial class DefinitionLocation
    {
        /// <summary>
        /// Implementation of a <see cref="DefinitionLocation"/> that sits on top of an
        /// <see cref="ISymbol"/>.  In order to not keep anything alive too long, we only
        /// hold onto IDs and Keys.  When the user tries to navigate to an item we will 
        /// attempt to find the symbol again in the current solution snapshot and 
        /// navigate to it there.
        /// </summary>
        private sealed class SymbolDefinitionLocation : DefinitionLocation
        {
            private readonly Workspace _workspace;
            private readonly SymbolKey _symbolKey;
            private readonly AssemblyIdentity _symbolAssemblyIdentity;
            private readonly ImmutableArray<TaggedText> _originationParts;

            public SymbolDefinitionLocation(Solution solution, ISymbol definition)
            {
                _workspace = solution.Workspace;
                _symbolKey = definition.GetSymbolKey();
                _symbolAssemblyIdentity = definition.ContainingAssembly?.Identity;
                _originationParts = GetOriginationParts(definition);
            }

            public override ImmutableArray<TaggedText> OriginationParts => _originationParts;

            public override bool CanNavigateTo()
            {
                return TryNavigateTo((symbol, project, service) => true);
            }

            public override bool TryNavigateTo()
            {
                return TryNavigateTo((symbol, project, service) =>
                    service.TryNavigateToSymbol(symbol, project));
            }

            private bool TryNavigateTo(Func<ISymbol, Project, ISymbolNavigationService, bool> action)
            {
                var projectAndSymbol = ResolveSymbolInCurrentSolution();
                if (projectAndSymbol == null)
                {
                    return false;
                }

                var project = projectAndSymbol.Value.Item1;
                var symbol = projectAndSymbol.Value.Item2;
                if (symbol == null || project == null)
                {
                    return false;
                }

                if (symbol.Kind == SymbolKind.Namespace)
                {
                    return false;
                }

                var navigationService = _workspace.Services.GetService<ISymbolNavigationService>();
                return action(symbol, project, navigationService);
            }

            private ValueTuple<Project, ISymbol>? ResolveSymbolInCurrentSolution()
            {
                var project = _workspace.CurrentSolution
                    .ProjectsWithReferenceToAssembly(_symbolAssemblyIdentity)
                    .FirstOrDefault();

                if (project == null)
                {
                    return null;
                }

                var compilation = project.GetCompilationAsync(CancellationToken.None)
                                         .WaitAndGetResult(CancellationToken.None);
                return ValueTuple.Create(project, _symbolKey.Resolve(compilation).Symbol);
            }
        }
    }
}
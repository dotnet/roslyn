// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy.Finders;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.CallHierarchy;
using Microsoft.VisualStudio.Language.Intellisense;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy
{
    [Export(typeof(CallHierarchyProvider))]
    internal partial class CallHierarchyProvider
    {
        private readonly IAsynchronousOperationListener _asyncListener;
        public IGlyphService GlyphService { get; }

        [ImportingConstructor]
        public CallHierarchyProvider(
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners,
            IGlyphService glyphService)
        {
            _asyncListener = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.CallHierarchy);
            this.GlyphService = glyphService;
        }

        public async Task<ICallHierarchyMemberItem> CreateItem(ISymbol symbol,
            Project project, IEnumerable<Location> callsites, CancellationToken cancellationToken)
        {
            if (symbol.Kind == SymbolKind.Method ||
                symbol.Kind == SymbolKind.Property ||
                symbol.Kind == SymbolKind.Event ||
                symbol.Kind == SymbolKind.Field)
            {
                symbol = GetTargetSymbol(symbol);

                var finders = await CreateFinders(symbol, project, cancellationToken).ConfigureAwait(false);

                ICallHierarchyMemberItem item = new CallHierarchyItem(symbol,
                    project.Id,
                    finders,
                    () => symbol.GetGlyph().GetImageSource(GlyphService),
                    this,
                    callsites,
                    project.Solution.Workspace);

                return item;
            }

            return null;
        }

        private ISymbol GetTargetSymbol(ISymbol symbol)
        {
            if (symbol is IMethodSymbol)
            {
                var methodSymbol = (IMethodSymbol)symbol;
                methodSymbol = methodSymbol.ReducedFrom != null ? methodSymbol.ReducedFrom : methodSymbol;
                methodSymbol = methodSymbol.ConstructedFrom != null ? methodSymbol.ConstructedFrom : methodSymbol;
                return methodSymbol;
            }

            return symbol;
        }

        public FieldInitializerItem CreateInitializerItem(IEnumerable<CallHierarchyDetail> details)
        {
            return new FieldInitializerItem(EditorFeaturesResources.Initializers,
                                            "__" + EditorFeaturesResources.Initializers,
                                            Glyph.FieldPublic.GetImageSource(GlyphService),
                                            details);
        }

        public async Task<IEnumerable<AbstractCallFinder>> CreateFinders(ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            if (symbol.Kind == SymbolKind.Property ||
                    symbol.Kind == SymbolKind.Event ||
                    symbol.Kind == SymbolKind.Method)
            {
                var finders = new List<AbstractCallFinder>();

                finders.Add(new MethodCallFinder(symbol, project, _asyncListener, this));

                if (symbol.IsVirtual || symbol.IsAbstract)
                {
                    finders.Add(new OverridingMemberFinder(symbol, project, _asyncListener, this));
                }

                var @overrides = await SymbolFinder.FindOverridesAsync(symbol, project.Solution, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (overrides.Any())
                {
                    finders.Add(new CallToOverrideFinder(symbol, project, _asyncListener, this));
                }

                if (symbol.OverriddenMember() != null)
                {
                    finders.Add(new BaseMemberFinder(symbol.OverriddenMember(), project, _asyncListener, this));
                }

                var implementedInterfaceMembers = await SymbolFinder.FindImplementedInterfaceMembersAsync(symbol, project.Solution, cancellationToken: cancellationToken).ConfigureAwait(false);
                foreach (var implementedInterfaceMember in implementedInterfaceMembers)
                {
                    finders.Add(new InterfaceImplementationCallFinder(implementedInterfaceMember, project, _asyncListener, this));
                }

                if (symbol.IsImplementableMember())
                {
                    finders.Add(new ImplementerFinder(symbol, project, _asyncListener, this));
                }

                return finders;
            }

            if (symbol.Kind == SymbolKind.Field)
            {
                return SpecializedCollections.SingletonEnumerable(new FieldReferenceFinder(symbol, project, _asyncListener, this));
            }

            return null;
        }

        public void NavigateTo(SymbolKey id, Project project, CancellationToken cancellationToken)
        {
            var compilation = project.GetCompilationAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var resolution = id.Resolve(compilation, cancellationToken: cancellationToken);
            var workspace = project.Solution.Workspace;
            var options = workspace.Options.WithChangedOption(NavigationOptions.PreferProvisionalTab, true);
            var symbolNavigationService = workspace.Services.GetService<ISymbolNavigationService>();

            symbolNavigationService.TryNavigateToSymbol(resolution.Symbol, project, options, cancellationToken);
        }
    }
}

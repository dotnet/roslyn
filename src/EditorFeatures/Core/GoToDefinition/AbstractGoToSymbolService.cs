// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.GoToDefinition
{
    // Ctrl+Click (GoToSymbol)
    internal abstract class AbstractGoToSymbolService : ForegroundThreadAffinitizedObject, IGoToSymbolService
    {
        protected AbstractGoToSymbolService(IThreadingContext threadingContext, bool assertIsForeground = false)
            : base(threadingContext, assertIsForeground)
        {
        }

        public async Task GetSymbolsAsync(GoToSymbolContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;
            var service = document.GetLanguageService<IGoToDefinitionSymbolService>();

            // [includeType: false]
            // Enable Ctrl+Click on tokens with aliased, referenced or declared symbol.
            // If the token has none of those but does have a type (mostly literals), we're not interested
            var (symbol, span) = await service.GetSymbolAndBoundSpanAsync(document, position, includeType: false, cancellationToken).ConfigureAwait(false);

            if (symbol == null)
            {
                return;
            }

            // We want ctrl-click GTD to be as close to regular GTD as possible.
            // This means we have to query for "third party navigation", from
            // XAML, etc. That call has to be done on the UI thread.
            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var definitions = GoToDefinitionHelpers.GetDefinitions(symbol, document.Project, thirdPartyNavigationAllowed: true, cancellationToken)
                .WhereAsArray(d => d.CanNavigateTo(document.Project.Solution.Workspace));

            await TaskScheduler.Default;

            foreach (var definition in definitions)
            {
                context.AddItem(WellKnownSymbolTypes.Definition, definition);
            }

            context.Span = span;
        }
    }
}

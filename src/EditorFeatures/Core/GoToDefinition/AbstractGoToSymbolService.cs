// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.GoToDefinition
{
    internal abstract class AbstractGoToSymbolService : ForegroundThreadAffinitizedObject, IGoToSymbolService
    {
        public async Task GetSymbolsAsync(GoToSymbolContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var service = document.GetLanguageService<IGoToDefinitionSymbolService>();
            var (symbol, span) = await service.GetSymbolAndBoundSpanAsync(document, position, cancellationToken).ConfigureAwait(false);

            if (symbol == null)
            {
                return;
            }

            // We want ctrl-click GTD to be as close to regular GTD as possible.
            // This means we have to query for "third party navigation", from
            // XAML, etc. That call has to be done on the UI thread.
            var definitions = await Task.Factory.StartNew(() =>
                GoToDefinitionHelpers.GetDefinitions(symbol, document.Project, thirdPartyNavigationAllowed: true, cancellationToken: cancellationToken)
                    .WhereAsArray(d => d.CanNavigateTo(document.Project.Solution.Workspace)),
                        cancellationToken,
                        TaskCreationOptions.None,
                        ForegroundTaskScheduler).ConfigureAwait(false);

            foreach (var definition in definitions)
            {
                context.AddItem(WellKnownSymbolTypes.Definition, definition);
            }

            context.Span = span;
        }
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor
{
    [Shared]
    [ExportLanguageService(typeof(INavigationBarItemService), LanguageNames.FSharp)]
    internal class FSharpNavigationBarItemService : INavigationBarItemService
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IFSharpNavigationBarItemService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpNavigationBarItemService(
            IThreadingContext threadingContext,
            IFSharpNavigationBarItemService service)
        {
            _threadingContext = threadingContext;
            _service = service;
        }

        public async Task<ImmutableArray<NavigationBarItem>> GetItemsAsync(Document document, ITextVersion textVersion, CancellationToken cancellationToken)
        {
            var items = await _service.GetItemsAsync(document, cancellationToken).ConfigureAwait(false);
            return items == null
                ? ImmutableArray<NavigationBarItem>.Empty
                : ConvertItems(items, textVersion);
        }

        private static ImmutableArray<NavigationBarItem> ConvertItems(IList<FSharpNavigationBarItem> items, ITextVersion textVersion)
            => (items ?? SpecializedCollections.EmptyList<FSharpNavigationBarItem>()).Where(x => x.Spans.Any()).SelectAsArray(x => ConvertToNavigationBarItem(x, textVersion));

        public async Task<bool> TryNavigateToItemAsync(
            Document document, NavigationBarItem item, ITextView view, ITextVersion textVersion, CancellationToken cancellationToken)
        {
            // The logic here was ported from FSharp's implementation. The main reason was to avoid shimming INotificationService.
            var navigationSpan = item.TryGetNavigationSpan(textVersion);
            if (navigationSpan != null)
            {
                var span = navigationSpan.Value;
                var workspace = document.Project.Solution.Workspace;
                var navigationService = workspace.Services.GetRequiredService<IFSharpDocumentNavigationService>();

                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                if (navigationService.CanNavigateToPosition(workspace, document.Id, span.Start, virtualSpace: 0, cancellationToken))
                {
                    navigationService.TryNavigateToPosition(workspace, document.Id, span.Start, virtualSpace: 0, options: null, cancellationToken);
                }
                else
                {
                    var notificationService = workspace.Services.GetRequiredService<INotificationService>();
                    notificationService.SendNotification(EditorFeaturesResources.The_definition_of_the_object_is_hidden, severity: NotificationSeverity.Error);
                }
            }

            return true;
        }

        public bool ShowItemGrayedIfNear(NavigationBarItem item)
        {
            return false;
        }

        private static NavigationBarItem ConvertToNavigationBarItem(FSharpNavigationBarItem item, ITextVersion textVersion)
        {
            var spans = item.Spans.ToImmutableArrayOrEmpty();
            return new SimpleNavigationBarItem(
                textVersion,
                item.Text,
                FSharpGlyphHelpers.ConvertTo(item.Glyph),
                spans,
                spans.First(),
                ConvertItems(item.ChildItems, textVersion),
                item.Indent,
                item.Bolded,
                item.Grayed);
        }
    }
}

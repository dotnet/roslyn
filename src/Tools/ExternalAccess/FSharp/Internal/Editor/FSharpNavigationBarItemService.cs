// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor
{
    [Shared]
    [ExportLanguageService(typeof(INavigationBarItemServiceRenameOnceTypeScriptMovesToExternalAccess), LanguageNames.FSharp)]
    internal class FSharpNavigationBarItemService : INavigationBarItemServiceRenameOnceTypeScriptMovesToExternalAccess
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

        public async Task<IList<NavigationBarItem>?> GetItemsAsync(Document document, CancellationToken cancellationToken)
        {
            var items = await _service.GetItemsAsync(document, cancellationToken).ConfigureAwait(false);
            return items?.Select(x => ConvertToNavigationBarItem(x)).ToList();
        }

        public async Task<bool> TryNavigateToItemAsync(Document document, NavigationBarItem item, ITextView view, CancellationToken cancellationToken)
        {
            // The logic here was ported from FSharp's implementation. The main reason was to avoid shimming INotificationService.
            if (!item.Spans.IsEmpty)
            {
                var span = item.Spans.First();
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

        private static NavigationBarItem ConvertToNavigationBarItem(FSharpNavigationBarItem item)
        {
            var childItems = item.ChildItems ?? SpecializedCollections.EmptyList<FSharpNavigationBarItem>();

            return new InternalNavigationBarItem(
                item.Text,
                FSharpGlyphHelpers.ConvertTo(item.Glyph),
                item.Spans.ToImmutableArrayOrEmpty(),
                childItems.SelectAsArray(x => ConvertToNavigationBarItem(x)),
                item.Indent,
                item.Bolded,
                item.Grayed);
        }

        private class InternalNavigationBarItem : NavigationBarItem
        {
            public InternalNavigationBarItem(string text, Glyph glyph, ImmutableArray<TextSpan> spans, ImmutableArray<NavigationBarItem> childItems, int indent, bool bolded, bool grayed)
                : base(text, glyph, spans, childItems, indent, bolded, grayed)
            {
            }
        }
    }
}

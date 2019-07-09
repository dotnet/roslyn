// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Composition;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation;
using Microsoft.CodeAnalysis.Notification;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor
{
    [Shared]
    [ExportLanguageService(typeof(INavigationBarItemService), LanguageNames.FSharp)]
    internal class FSharpNavigationBarItemService : INavigationBarItemService
    {
        private readonly IFSharpNavigationBarItemService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpNavigationBarItemService(IFSharpNavigationBarItemService service)
        {
            _service = service;
        }

        public async Task<IList<NavigationBarItem>> GetItemsAsync(Document document, CancellationToken cancellationToken)
        {
            var items = await _service.GetItemsAsync(document, cancellationToken).ConfigureAwait(false);
            return items?.Select(x => ConvertToNavigationBarItem(x)).ToList();
        }

        public void NavigateToItem(Document document, NavigationBarItem item, ITextView view, CancellationToken cancellationToken)
        {
            // The logic here was ported from FSharp's implementation. The main reason was to avoid shimming INotificationService.
            if (item.Spans.Count > 0)
            {
                var span = item.Spans.First();
                var workspace = document.Project.Solution.Workspace;
                var navigationService = workspace.Services.GetService<IFSharpDocumentNavigationService>();

                if (navigationService.CanNavigateToPosition(workspace, document.Id, span.Start))
                {
                    navigationService.TryNavigateToPosition(workspace, document.Id, span.Start);
                }
                else
                {
                    var notificationService = workspace.Services.GetService<INotificationService>();
                    notificationService.SendNotification(EditorFeaturesResources.The_definition_of_the_object_is_hidden, severity: NotificationSeverity.Error);
                }
            }
        }

        public bool ShowItemGrayedIfNear(NavigationBarItem item)
        {
            return false;
        }

        private static NavigationBarItem ConvertToNavigationBarItem(FSharpNavigationBarItem item)
        {
            return
                new InternalNavigationBarItem(
                    item.Text,
                    FSharpGlyphHelpers.ConvertTo(item.Glyph),
                    item.Spans,
                    item.ChildItems?.Select(x => ConvertToNavigationBarItem(x)).ToList(),
                    item.Indent,
                    item.Bolded,
                    item.Grayed);
        }

        private class InternalNavigationBarItem : NavigationBarItem
        {
            public InternalNavigationBarItem(
                string text,
                Glyph glyph,
                IList<TextSpan> spans,
                IList<NavigationBarItem> childItems,
                int indent,
                bool bolded,
                bool grayed) : base(text, glyph, spans, childItems, indent, bolded, grayed)
            {
            }
        }
    }
}

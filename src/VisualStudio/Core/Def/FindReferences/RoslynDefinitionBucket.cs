// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal partial class StreamingFindUsagesPresenter
    {
        private class RoslynDefinitionBucket : DefinitionBucket, ISupportsNavigation
        {
            private readonly StreamingFindUsagesPresenter _presenter;

            public readonly DefinitionItem DefinitionItem;
            private readonly IThreadingContext _threadingContext;

            /// <summary>
            /// Due to linked files, we may have results for several locations that are all effectively
            /// the same file/span.  So we represent this as one entry with several project flavors.  If
            /// we get more than one flavor, we'll show that the user in the UI.
            /// </summary>
            private readonly Dictionary<(string? filePath, TextSpan span), DocumentSpanEntry> _locationToEntry = new();

            public RoslynDefinitionBucket(
                string name,
                bool expandedByDefault,
                StreamingFindUsagesPresenter presenter,
                AbstractTableDataSourceFindUsagesContext context,
                DefinitionItem definitionItem,
                IThreadingContext threadingContext)
                : base(name,
                       sourceTypeIdentifier: context.SourceTypeIdentifier,
                       identifier: context.Identifier,
                       expandedByDefault: expandedByDefault)
            {
                _presenter = presenter;
                DefinitionItem = definitionItem;
                _threadingContext = threadingContext;
            }

            public static RoslynDefinitionBucket Create(
                StreamingFindUsagesPresenter presenter,
                AbstractTableDataSourceFindUsagesContext context,
                DefinitionItem definitionItem,
                bool expandedByDefault,
                IThreadingContext threadingContext)
            {
                var isPrimary = definitionItem.Properties.ContainsKey(DefinitionItem.Primary);

                // Sort the primary item above everything else.
                var name = $"{(isPrimary ? 0 : 1)} {definitionItem.DisplayParts.JoinText()} {definitionItem.GetHashCode()}";

                return new RoslynDefinitionBucket(
                    name, expandedByDefault, presenter, context, definitionItem, threadingContext);
            }

            public bool CanNavigateTo()
                => true;

            public async Task NavigateToAsync(NavigationOptions options, CancellationToken cancellationToken)
            {
                var location = await DefinitionItem.GetNavigableLocationAsync(
                    _presenter._workspace, cancellationToken).ConfigureAwait(false);
                await location.TryNavigateToAsync(_threadingContext, options, cancellationToken).ConfigureAwait(false);
            }

            public override bool TryGetValue(string key, out object? content)
            {
                content = GetValue(key);
                return content != null;
            }

            /// <summary>
            /// The editor is presenting 'Text' while telling the screen reader to use the 'Name' field.
            /// Workaround this bug by overriding the string content to provide the proper data for the screen reader.
            /// https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1020534/
            /// </summary>
            public override bool TryCreateStringContent(out string? content)
            {
                if (TryGetValue(StandardTableKeyNames.Text, out var contentValue) && contentValue is string textContent)
                {
                    content = textContent;
                    return true;
                }

                content = null;
                return false;
            }

            private object? GetValue(string key)
            {
                switch (key)
                {
                    case StandardTableKeyNames.Text:
                    case StandardTableKeyNames.FullText:
                        return DefinitionItem.DisplayParts.JoinText();

                    case StandardTableKeyNames2.TextInlines:
                        var inlines = new List<Inline> { new Run(" ") };
                        inlines.AddRange(DefinitionItem.DisplayParts.ToInlines(_presenter.ClassificationFormatMap, _presenter.TypeMap));
                        foreach (var inline in inlines)
                        {
                            inline.SetValue(TextElement.FontWeightProperty, FontWeights.Bold);
                        }

                        return inlines;

                    case StandardTableKeyNames2.DefinitionIcon:
                        return DefinitionItem.Tags.GetFirstGlyph().GetImageMoniker();
                }

                return null;
            }

            public DocumentSpanEntry GetOrAddEntry(string? filePath, TextSpan sourceSpan, DocumentSpanEntry entry)
            {
                var key = (filePath, sourceSpan);
                lock (_locationToEntry)
                {
                    return _locationToEntry.GetOrAdd(key, entry);
                }
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings;

internal sealed partial class SettingsEditorPane
{
    internal sealed class SearchFilter : IEntryFilter
    {
        private readonly IEnumerable<IVsSearchToken> _searchTokens;
        private readonly IReadOnlyList<ITableColumnDefinition>? _visibleColumns;

        public SearchFilter(IVsSearchQuery searchQuery, IWpfTableControl control)
        {
            _searchTokens = SearchUtilities.ExtractSearchTokens(searchQuery);
            _searchTokens ??= [];

            var newVisibleColumns = new List<ITableColumnDefinition>();
            foreach (var c in control.ColumnStates)
            {
                if (c.IsVisible || ((c as ColumnState2)?.GroupingPriority > 0))
                {
                    var definition = control.ColumnDefinitionManager.GetColumnDefinition(c.Name);
                    if (definition != null)
                    {
                        newVisibleColumns.Add(definition);
                    }
                }
            }

            _visibleColumns = newVisibleColumns;
        }

        public bool Match(ITableEntryHandle entry)
        {
            if (_visibleColumns is null)
            {
                return false;
            }

            // An entry is considered matching a search query if all tokens in the search query are matching at least one of entry's columns.
            // Reserve one more column for details content
            var cachedColumnValues = new string[_visibleColumns.Count + 1];

            foreach (var searchToken in _searchTokens)
            {
                // No support for filters yet
                if (searchToken is IVsSearchFilterToken)
                {
                    continue;
                }

                if (!AtLeastOneColumnOrDetailsContentMatches(entry, searchToken, cachedColumnValues))
                {
                    return false;
                }
            }

            return true;
        }

        private bool AtLeastOneColumnOrDetailsContentMatches(ITableEntryHandle entry, IVsSearchToken searchToken, string[] cachedColumnValues)
        {
            // Check details content for any matches
            if (cachedColumnValues[0] == null)
            {
                cachedColumnValues[0] = GetDetailsContentAsString(entry);
            }

            var detailsContent = cachedColumnValues[0];
            if (detailsContent != null && Match(detailsContent, searchToken))
            {
                // Found match in details content
                return true;
            }

            if (_visibleColumns is null)
            {
                return false;
            }

            // Check each column for any matches
            for (var i = 0; i < _visibleColumns.Count; i++)
            {
                if (cachedColumnValues[i + 1] == null)
                {
                    cachedColumnValues[i + 1] = GetColumnValueAsString(entry, _visibleColumns[i]);
                }

                var columnValue = cachedColumnValues[i + 1];

                if (columnValue != null && Match(columnValue, searchToken))
                {
                    // Found match in this column
                    return true;
                }
            }

            // No match found in this entry
            return false;
        }

        private static string GetColumnValueAsString(ITableEntryHandle entry, ITableColumnDefinition column)
            => (entry.TryCreateStringContent(column, truncatedText: false, singleColumnView: false, content: out var columnValue) && columnValue is not null)
                ? columnValue
                : string.Empty;

        private static string GetDetailsContentAsString(ITableEntryHandle entry)
        {
            string? detailsString = null;

            if (entry.CanShowDetails)
            {
                if (entry is IWpfTableEntry wpfEntry)
                {
                    _ = wpfEntry.TryCreateDetailsStringContent(out detailsString);
                }
            }

            return detailsString ?? string.Empty;
        }

        private static bool Match(string columnValue, IVsSearchToken searchToken)
            => (columnValue is not null) && (columnValue.IndexOf(searchToken.ParsedTokenText, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}

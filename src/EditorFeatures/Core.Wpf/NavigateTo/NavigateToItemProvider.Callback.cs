// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.Text.PatternMatching;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal partial class NavigateToItemProvider
    {
        private class NavigateToItemProviderCallback : INavigateToSearchCallback
        {
            private readonly INavigateToItemDisplayFactory _displayFactory;
            private readonly INavigateToCallback _callback;

            public NavigateToItemProviderCallback(INavigateToItemDisplayFactory displayFactory, INavigateToCallback callback)
            {
                _displayFactory = displayFactory;
                _callback = callback;
            }

            public void Done(bool isFullyLoaded)
            {
                if (!isFullyLoaded && _callback is INavigateToCallback2 callback2)
                {
                    callback2.Done(IncompleteReason.SolutionLoading);
                }
                else
                {
                    _callback.Done();
                }
            }

            public Task AddItemAsync(Project project, INavigateToSearchResult result, CancellationToken cancellationToken)
            {
                ReportMatchResult(project, result);
                return Task.CompletedTask;
            }

            public void ReportProgress(int current, int maximum)
            {
                _callback.ReportProgress(current, maximum);
            }

            private void ReportMatchResult(Project project, INavigateToSearchResult result)
            {
                var matchedSpans = result.NameMatchSpans.SelectAsArray(t => t.ToSpan());

                var patternMatch = new PatternMatch(GetPatternMatchKind(result.MatchKind),
                    punctuationStripped: true, result.IsCaseSensitive, matchedSpans);

                var navigateToItem = new NavigateToItem(
                    result.Name,
                    GetKind(result.Kind),
                    GetNavigateToLanguage(project.Language),
                    result.SecondarySort,
                    result,
                    patternMatch,
                    _displayFactory);
                _callback.AddItem(navigateToItem);
            }

            private static string GetKind(string kind)
                => kind switch
                {
                    CodeAnalysis.NavigateTo.NavigateToItemKind.Class
                        => VisualStudio.Language.NavigateTo.Interfaces.NavigateToItemKind.Class,
                    CodeAnalysis.NavigateTo.NavigateToItemKind.Constant
                        => VisualStudio.Language.NavigateTo.Interfaces.NavigateToItemKind.Constant,
                    CodeAnalysis.NavigateTo.NavigateToItemKind.Delegate
                        => VisualStudio.Language.NavigateTo.Interfaces.NavigateToItemKind.Delegate,
                    CodeAnalysis.NavigateTo.NavigateToItemKind.Enum
                        => VisualStudio.Language.NavigateTo.Interfaces.NavigateToItemKind.Enum,
                    CodeAnalysis.NavigateTo.NavigateToItemKind.EnumItem
                        => VisualStudio.Language.NavigateTo.Interfaces.NavigateToItemKind.EnumItem,
                    CodeAnalysis.NavigateTo.NavigateToItemKind.Event
                        => VisualStudio.Language.NavigateTo.Interfaces.NavigateToItemKind.Event,
                    CodeAnalysis.NavigateTo.NavigateToItemKind.Field
                        => VisualStudio.Language.NavigateTo.Interfaces.NavigateToItemKind.Field,
                    CodeAnalysis.NavigateTo.NavigateToItemKind.File
                        => VisualStudio.Language.NavigateTo.Interfaces.NavigateToItemKind.File,
                    CodeAnalysis.NavigateTo.NavigateToItemKind.Interface
                        => VisualStudio.Language.NavigateTo.Interfaces.NavigateToItemKind.Interface,
                    CodeAnalysis.NavigateTo.NavigateToItemKind.Line
                        => VisualStudio.Language.NavigateTo.Interfaces.NavigateToItemKind.Line,
                    CodeAnalysis.NavigateTo.NavigateToItemKind.Method
                        => VisualStudio.Language.NavigateTo.Interfaces.NavigateToItemKind.Method,
                    CodeAnalysis.NavigateTo.NavigateToItemKind.Module
                        => VisualStudio.Language.NavigateTo.Interfaces.NavigateToItemKind.Module,
                    CodeAnalysis.NavigateTo.NavigateToItemKind.OtherSymbol
                        => VisualStudio.Language.NavigateTo.Interfaces.NavigateToItemKind.OtherSymbol,
                    CodeAnalysis.NavigateTo.NavigateToItemKind.Property
                        => VisualStudio.Language.NavigateTo.Interfaces.NavigateToItemKind.Property,
                    // VisualStudio.Language.NavigateTo.Interfaces.NavigateToItemKind doesn't have a record, fall back to class.
                    // This should be updated whenever NavigateToItemKind has a record.
                    CodeAnalysis.NavigateTo.NavigateToItemKind.Record
                        => VisualStudio.Language.NavigateTo.Interfaces.NavigateToItemKind.Class,
                    CodeAnalysis.NavigateTo.NavigateToItemKind.Structure
                        => VisualStudio.Language.NavigateTo.Interfaces.NavigateToItemKind.Structure,
                    _ => throw ExceptionUtilities.UnexpectedValue(kind)
                };

            private static PatternMatchKind GetPatternMatchKind(NavigateToMatchKind matchKind)
                => matchKind switch
                {
                    NavigateToMatchKind.Exact => PatternMatchKind.Exact,
                    NavigateToMatchKind.Prefix => PatternMatchKind.Prefix,
                    NavigateToMatchKind.Substring => PatternMatchKind.Substring,
                    NavigateToMatchKind.Regular => PatternMatchKind.Fuzzy,
                    NavigateToMatchKind.None => PatternMatchKind.Fuzzy,
                    NavigateToMatchKind.CamelCaseExact => PatternMatchKind.CamelCaseExact,
                    NavigateToMatchKind.CamelCasePrefix => PatternMatchKind.CamelCasePrefix,
                    NavigateToMatchKind.CamelCaseNonContiguousPrefix => PatternMatchKind.CamelCaseNonContiguousPrefix,
                    NavigateToMatchKind.CamelCaseSubstring => PatternMatchKind.CamelCaseSubstring,
                    NavigateToMatchKind.CamelCaseNonContiguousSubstring => PatternMatchKind.CamelCaseNonContiguousSubstring,
                    NavigateToMatchKind.Fuzzy => PatternMatchKind.Fuzzy,
                    _ => throw ExceptionUtilities.UnexpectedValue(matchKind),
                };

            /// <summary>
            /// Returns the name for the language used by the old Navigate To providers.
            /// </summary>
            /// <remarks> It turns out this string is used for sorting and for some SQM data, so it's best
            /// to keep it unchanged.</remarks>
            private static string GetNavigateToLanguage(string languageName)
                => languageName switch
                {
                    LanguageNames.CSharp => "csharp",
                    LanguageNames.VisualBasic => "vb",
                    _ => languageName,
                };
        }
    }
}

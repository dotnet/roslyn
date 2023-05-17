// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using RoslynCompletionList = Microsoft.CodeAnalysis.Completion.CompletionList;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    /// <summary>
    /// Contains data need to be tracked over an entire IAsyncCompletionSession completion
    /// session for various operations.
    /// </summary>
    internal sealed class CompletionSessionData
    {
        private const string RoslynCompletionSessionData = nameof(RoslynCompletionSessionData);
        public bool TargetTypeFilterSelected { get; set; }
        public bool HasSuggestionItemOptions { get; set; }

        public SnapshotPoint? ExpandedItemTriggerLocation { get; set; }
        public TextSpan? CompletionListSpan { get; set; }
        public CompletionList<CompletionItem>? CombinedSortedList { get; set; }
        public Task<(CompletionContext, RoslynCompletionList)>? ExpandedItemsTask { get; set; }
        public bool IsExclusive { get; set; }
        public bool NonBlockingCompletionEnabled { get; }

        private CompletionSessionData(IAsyncCompletionSession session)
        {
            // Editor has to separate options control the behavior of block waiting computation of completion items.
            // When set to true, `NonBlockingCompletionOptionId` takes precedence over `ResponsiveCompletionOptionId`
            // and is equivalent to `ResponsiveCompletionOptionId` to true and `ResponsiveCompletionThresholdOptionId` to 0.
            var nonBlockingCompletionEnabled = session.TextView.Options.GetOptionValue(DefaultOptions.NonBlockingCompletionOptionId);
            var responsiveCompletionEnabled = session.TextView.Options.GetOptionValue(DefaultOptions.ResponsiveCompletionOptionId);
            NonBlockingCompletionEnabled = nonBlockingCompletionEnabled || responsiveCompletionEnabled;
        }

        public static CompletionSessionData GetOrCreateSessionData(IAsyncCompletionSession session)
            => session.Properties.GetOrCreateSingletonProperty(RoslynCompletionSessionData, () => new CompletionSessionData(session));
    }
}

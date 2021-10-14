// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal partial class SuggestedActionsSourceProvider
    {
        private partial class SyncSuggestedActionsSource : SuggestedActionsSource
        {
            public SyncSuggestedActionsSource(
                IThreadingContext threadingContext,
                IGlobalOptionService globalOptions,
                SuggestedActionsSourceProvider owner,
                ITextView textView,
                ITextBuffer textBuffer,
                ISuggestedActionCategoryRegistryService suggestedActionCategoryRegistry)
                : base(threadingContext, globalOptions, owner, textView, textBuffer, suggestedActionCategoryRegistry)
            {
            }
        }
    }
}

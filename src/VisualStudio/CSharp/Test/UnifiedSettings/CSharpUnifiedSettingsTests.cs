// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings;

namespace Roslyn.VisualStudio.CSharp.UnitTests.UnifiedSettings
{
    public class CSharpUnifiedSettingsTests : UnifiedSettingsTests
    {
        internal override ImmutableArray<IOption2> OnboardedOptions => ImmutableArray.Create<IOption2>(
            CompletionOptionsStorage.TriggerOnTypingLetters,
            CompletionOptionsStorage.TriggerOnDeletion,
            CompletionOptionsStorage.TriggerInArgumentLists,
            CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems,
            CompletionViewOptionsStorage.ShowCompletionItemFilters,
            CompletionOptionsStorage.SnippetsBehavior
        );

        internal override ImmutableDictionary<IOption2, object> OptionsToDefaultValue => ImmutableDictionary<IOption2, object>.Empty.
            Add(CompletionOptionsStorage.SnippetsBehavior, SnippetsRule.AlwaysInclude).
            Add(CompletionOptionsStorage.TriggerOnDeletion, false);

        internal override ImmutableDictionary<IOption2, ImmutableArray<object>> EnumOptionsToValues => ImmutableDictionary<IOption2, ImmutableArray<object>>.Empty.
                Add(CompletionOptionsStorage.SnippetsBehavior, ImmutableArray.Create<object>(
                    SnippetsRule.NeverInclude, SnippetsRule.AlwaysInclude, SnippetsRule.IncludeAfterTypingIdentifierQuestionTab));
    }
}

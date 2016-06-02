// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Completion
{
    internal static class CompletionOptions
    {
        internal const string FeatureName = "Completion";

        public static readonly PerLanguageOption<bool> HideAdvancedMembers = new PerLanguageOption<bool>(FeatureName, "HideAdvancedMembers", defaultValue: false);
        public static readonly PerLanguageOption<bool> IncludeKeywords = new PerLanguageOption<bool>(FeatureName, "IncludeKeywords", defaultValue: true);
        public static readonly PerLanguageOption<bool> TriggerOnTyping = new PerLanguageOption<bool>(FeatureName, "TriggerOnTyping", defaultValue: true);
        public static readonly PerLanguageOption<bool> TriggerOnTypingLetters = new PerLanguageOption<bool>(FeatureName, "TriggerOnTypingLetters", defaultValue: true);

        // Dev15 options
        public static readonly PerLanguageOption<bool> ShowCompletionItemFilters = new PerLanguageOption<bool>(FeatureName, nameof(ShowCompletionItemFilters), defaultValue: false);
        public static readonly PerLanguageOption<bool> HighlightMatchingPortionsOfCompletionListItems = new PerLanguageOption<bool>(FeatureName, nameof(HighlightMatchingPortionsOfCompletionListItems), defaultValue: false);

        public static IEnumerable<PerLanguageOption<bool>> GetDev15CompletionOptions()
        {
            yield return ShowCompletionItemFilters;
            yield return HighlightMatchingPortionsOfCompletionListItems;
        }
    }

    internal static class CompletionControllerOptions
    {
        internal const string ControllerFeatureName = "CompletionController";

        public static readonly Option<bool> AlwaysShowBuilder = new Option<bool>(ControllerFeatureName, "AlwaysShowBuilder", defaultValue: false);
        public static readonly Option<bool> FilterOutOfScopeLocals = new Option<bool>(ControllerFeatureName, "FilterOutOfScopeLocals", defaultValue: true);
        public static readonly Option<bool> ShowXmlDocCommentCompletion = new Option<bool>(ControllerFeatureName, "ShowXmlDocCommentCompletion", defaultValue: true);
    }
}
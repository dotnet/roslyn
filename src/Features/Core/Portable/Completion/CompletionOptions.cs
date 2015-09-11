// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Completion
{
    internal static class CompletionOptions
    {
        public const string FeatureName = "Completion";
        public const string ControllerFeatureName = "CompletionController";

        public static readonly PerLanguageOption<bool> HideAdvancedMembers = new PerLanguageOption<bool>(FeatureName, "HideAdvancedMembers", defaultValue: false);
        public static readonly PerLanguageOption<bool> IncludeKeywords = new PerLanguageOption<bool>(FeatureName, "IncludeKeywords", defaultValue: true);
        public static readonly PerLanguageOption<bool> TriggerOnTyping = new PerLanguageOption<bool>(FeatureName, "TriggerOnTyping", defaultValue: true);
        public static readonly PerLanguageOption<bool> TriggerOnTypingLetters = new PerLanguageOption<bool>(FeatureName, "TriggerOnTypingLetters", defaultValue: true);

        public static readonly Option<bool> AlwaysShowBuilder = new Option<bool>(ControllerFeatureName, "AlwaysShowBuilder", defaultValue: false);
        public static readonly Option<bool> FilterOutOfScopeLocals = new Option<bool>(ControllerFeatureName, "FilterOutOfScopeLocals", defaultValue: true);
        public static readonly Option<bool> ShowXmlDocCommentCompletion = new Option<bool>(ControllerFeatureName, "ShowXmlDocCommentCompletion", defaultValue: true);
    }
}

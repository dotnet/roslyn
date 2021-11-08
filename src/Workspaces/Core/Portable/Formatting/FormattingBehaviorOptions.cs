// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// Solution-wide formatting options.
    /// </summary>
    internal sealed class FormattingBehaviorOptions
    {
        [ExportOptionProvider, Shared]
        internal sealed class Provider : IOptionProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Provider()
            {
            }

            public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
                SmartIndent,
                PreferredWrappingColumn,
                AllowDisjointSpanMerging,
                AutoFormattingOnReturn,
                AutoFormattingOnTyping,
                AutoFormattingOnSemicolon,
                FormatOnPaste);
        }

        private const string FeatureName = "FormattingOptions";

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static PerLanguageOption2<FormattingOptions.IndentStyle> SmartIndent { get; } =
            new(FeatureName, FormattingOptionGroups.IndentationAndSpacing, nameof(SmartIndent), defaultValue: FormattingOptions.IndentStyle.Smart);

        /// <summary>
        /// Default value of 120 was picked based on the amount of code in a github.com diff at 1080p.
        /// That resolution is the most common value as per the last DevDiv survey as well as the latest
        /// Steam hardware survey.  This also seems to a reasonable length default in that shorter
        /// lengths can often feel too cramped for .NET languages, which are often starting with a
        /// default indentation of at least 16 (for namespace, class, member, plus the final construct
        /// indentation).
        /// 
        /// TODO: Currently the option has no storage and always has its default value. See https://github.com/dotnet/roslyn/pull/30422#issuecomment-436118696.
        /// </summary>
        internal static Option2<int> PreferredWrappingColumn { get; } =
            new(FeatureName, FormattingOptionGroups.NewLine, nameof(PreferredWrappingColumn), defaultValue: 120);

        /// <summary>
        /// TODO: Currently the option has no storage and always has its default value. 
        /// </summary>
        internal static Option2<bool> AllowDisjointSpanMerging { get; } =
            new(FeatureName, OptionGroup.Default, nameof(AllowDisjointSpanMerging), defaultValue: false);

        internal static readonly PerLanguageOption2<bool> AutoFormattingOnReturn =
            new(FeatureName, OptionGroup.Default, nameof(AutoFormattingOnReturn), defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Return"));

        public static readonly PerLanguageOption2<bool> AutoFormattingOnTyping =
            new(FeatureName, OptionGroup.Default, nameof(AutoFormattingOnTyping), defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Typing"));

        public static readonly PerLanguageOption2<bool> AutoFormattingOnSemicolon =
            new(FeatureName, OptionGroup.Default, nameof(AutoFormattingOnSemicolon), defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Semicolon"));

        public static readonly PerLanguageOption2<bool> FormatOnPaste =
            new(FeatureName, OptionGroup.Default, nameof(FormatOnPaste), defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.FormatOnPaste"));
    }
}

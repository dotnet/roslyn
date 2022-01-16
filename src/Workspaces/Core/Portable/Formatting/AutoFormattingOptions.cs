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
    internal readonly record struct AutoFormattingOptions(
        FormattingOptions.IndentStyle IndentStyle,
        bool FormatOnReturn,
        bool FormatOnTyping,
        bool FormatOnSemicolon,
        bool FormatOnCloseBrace)
    {
        public static AutoFormattingOptions From(Project project)
            => From(project.Solution.Options, project.Language);

        public static AutoFormattingOptions From(OptionSet options, string language)
            => new(
                IndentStyle: options.GetOption(Metadata.SmartIndent, language),
                FormatOnReturn: options.GetOption(Metadata.AutoFormattingOnReturn, language),
                FormatOnTyping: options.GetOption(Metadata.AutoFormattingOnTyping, language),
                FormatOnSemicolon: options.GetOption(Metadata.AutoFormattingOnSemicolon, language),
                FormatOnCloseBrace: options.GetOption(Metadata.AutoFormattingOnCloseBrace, language));

        [ExportSolutionOptionProvider, Shared]
        internal sealed class Metadata : IOptionProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Metadata()
            {
            }

            public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
                SmartIndent,
                AutoFormattingOnReturn,
                AutoFormattingOnTyping,
                AutoFormattingOnSemicolon,
                AutoFormattingOnCloseBrace);

            private const string FeatureName = "FormattingOptions";

            // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
            public static PerLanguageOption2<FormattingOptions.IndentStyle> SmartIndent { get; } =
                new(FeatureName, FormattingOptionGroups.IndentationAndSpacing, nameof(SmartIndent), defaultValue: FormattingOptions.IndentStyle.Smart);

            internal static readonly PerLanguageOption2<bool> AutoFormattingOnReturn =
                new(FeatureName, OptionGroup.Default, nameof(AutoFormattingOnReturn), defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Return"));

            public static readonly PerLanguageOption2<bool> AutoFormattingOnTyping =
                new(FeatureName, OptionGroup.Default, nameof(AutoFormattingOnTyping), defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Typing"));

            public static readonly PerLanguageOption2<bool> AutoFormattingOnSemicolon =
                new(FeatureName, OptionGroup.Default, nameof(AutoFormattingOnSemicolon), defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Semicolon"));

            public static readonly PerLanguageOption2<bool> AutoFormattingOnCloseBrace = new(
                "BraceCompletionOptions", nameof(AutoFormattingOnCloseBrace), defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Close Brace"));
        }
    }
}

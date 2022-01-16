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
        FormattingOptions.IndentStyle SmartIndent,
        bool AutoFormattingOnReturn,
        bool AutoFormattingOnTyping,
        bool AutoFormattingOnSemicolon,
        bool AutoFormattingOnCloseBrace,
        bool FormatOnPaste)
    {
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
                AutoFormattingOnCloseBrace,
                FormatOnPaste);

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

            public static readonly PerLanguageOption2<bool> FormatOnPaste =
                new(FeatureName, OptionGroup.Default, nameof(FormatOnPaste), defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.FormatOnPaste"));
        }
    }
}

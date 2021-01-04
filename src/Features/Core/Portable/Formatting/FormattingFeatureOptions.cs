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
    internal static class FormattingFeatureOptions
    {
        public static readonly PerLanguageOption2<bool> AutoFormattingOnTyping = new(
            nameof(FormattingFeatureOptions), nameof(AutoFormattingOnTyping), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Typing"));

        public static readonly PerLanguageOption2<bool> AutoFormattingOnSemicolon = new(
            nameof(FormattingFeatureOptions), nameof(AutoFormattingOnSemicolon), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Semicolon"));
    }

    [ExportOptionProvider, Shared]
    internal sealed class FormattingCommentOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FormattingCommentOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            FormattingFeatureOptions.AutoFormattingOnSemicolon,
            FormattingFeatureOptions.AutoFormattingOnTyping);
    }
}

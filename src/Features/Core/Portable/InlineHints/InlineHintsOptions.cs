// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal static class InlineHintsOptions
    {
        public static readonly Option2<bool> DisplayAllHintsWhilePressingCtrlAlt =
            new(nameof(InlineHintsOptions),
                nameof(DisplayAllHintsWhilePressingCtrlAlt),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.Specific.DisplayAllHintsWhilePressingCtrlAlt"));

        /// <summary>
        /// Non-persisted option used to switch to displaying everything while the user is holding ctrl-alt.
        /// </summary>
        public static readonly Option2<bool> DisplayAllOverride =
            new(nameof(DisplayAllOverride),
                nameof(EnabledForParameters),
                defaultValue: false);

        public static readonly PerLanguageOption2<bool> EnabledForParameters =
            new(nameof(InlineHintsOptions),
                nameof(EnabledForParameters),
                defaultValue: false,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints"));

        public static readonly PerLanguageOption2<bool> ForLiteralParameters =
            new(nameof(InlineHintsOptions),
                nameof(ForLiteralParameters),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForLiteralParameters"));

        public static readonly PerLanguageOption2<bool> ForObjectCreationParameters =
            new(nameof(InlineHintsOptions),
                nameof(ForObjectCreationParameters),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForObjectCreationParameters"));

        public static readonly PerLanguageOption2<bool> ForOtherParameters =
            new(nameof(InlineHintsOptions),
                nameof(ForOtherParameters),
                defaultValue: false,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForOtherParameters"));

        public static readonly PerLanguageOption2<bool> SuppressForParametersThatDifferOnlyBySuffix =
            new(nameof(InlineHintsOptions),
                nameof(SuppressForParametersThatDifferOnlyBySuffix),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.SuppressForParametersThatDifferOnlyBySuffix"));

        public static readonly PerLanguageOption2<bool> SuppressForParametersThatMatchMethodIntent =
            new(nameof(InlineHintsOptions),
                nameof(SuppressForParametersThatMatchMethodIntent),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.SuppressForParametersThatMatchMethodIntent"));
    }

    [ExportOptionProvider, Shared]
    internal sealed class InlineHintsOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineHintsOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            InlineHintsOptions.DisplayAllHintsWhilePressingCtrlAlt,
            InlineHintsOptions.EnabledForParameters,
            InlineHintsOptions.ForLiteralParameters,
            InlineHintsOptions.ForObjectCreationParameters,
            InlineHintsOptions.ForOtherParameters);
    }
}

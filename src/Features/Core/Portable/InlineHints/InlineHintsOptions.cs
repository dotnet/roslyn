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
        public static readonly Option2<bool> DisplayAllHintsWhilePressingAltF1 =
            new(nameof(InlineHintsOptions),
                nameof(DisplayAllHintsWhilePressingAltF1),
                defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.Specific.DisplayAllHintsWhilePressingAltF1"));

        public static readonly PerLanguageOption2<bool> ColorHints =
            new(nameof(InlineHintsOptions),
                nameof(ColorHints),
                defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ColorHints"));

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
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints"));

        public static readonly PerLanguageOption2<bool> ForLiteralParameters =
            new(nameof(InlineHintsOptions),
                nameof(ForLiteralParameters),
                defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForLiteralParameters"));

        public static readonly PerLanguageOption2<bool> ForObjectCreationParameters =
            new(nameof(InlineHintsOptions),
                nameof(ForObjectCreationParameters),
                defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForObjectCreationParameters"));

        public static readonly PerLanguageOption2<bool> ForOtherParameters =
            new(nameof(InlineHintsOptions),
                nameof(ForOtherParameters),
                defaultValue: false,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForOtherParameters"));

        public static readonly PerLanguageOption2<bool> ForIndexerParameters =
            new(nameof(InlineHintsOptions),
                nameof(ForIndexerParameters),
                defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForArrayIndexers"));

        public static readonly PerLanguageOption2<bool> SuppressForParametersThatDifferOnlyBySuffix =
            new(nameof(InlineHintsOptions),
                nameof(SuppressForParametersThatDifferOnlyBySuffix),
                defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.SuppressForParametersThatDifferOnlyBySuffix"));

        public static readonly PerLanguageOption2<bool> SuppressForParametersThatMatchMethodIntent =
            new(nameof(InlineHintsOptions),
                nameof(SuppressForParametersThatMatchMethodIntent),
                defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.SuppressForParametersThatMatchMethodIntent"));

        public static readonly PerLanguageOption2<bool> SuppressForParametersThatMatchArgumentName =
            new(nameof(InlineHintsOptions),
                nameof(SuppressForParametersThatMatchArgumentName),
                defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.SuppressForParametersThatMatchArgumentName"));

        public static readonly PerLanguageOption2<bool> EnabledForTypes =
            new(nameof(InlineHintsOptions),
                nameof(EnabledForTypes),
                defaultValue: false,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineTypeHints"));

        public static readonly PerLanguageOption2<bool> ForImplicitVariableTypes =
            new(nameof(InlineHintsOptions),
                nameof(ForImplicitVariableTypes),
                defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineTypeHints.ForImplicitVariableTypes"));

        public static readonly PerLanguageOption2<bool> ForLambdaParameterTypes =
            new(nameof(InlineHintsOptions),
                nameof(ForLambdaParameterTypes),
                defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineTypeHints.ForLambdaParameterTypes"));

        public static readonly PerLanguageOption2<bool> ForImplicitObjectCreation =
            new(nameof(InlineHintsOptions),
                nameof(ForImplicitObjectCreation),
                defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineTypeHints.ForImplicitObjectCreation"));
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
            InlineHintsOptions.DisplayAllHintsWhilePressingAltF1,
            InlineHintsOptions.ColorHints,
            InlineHintsOptions.EnabledForParameters,
            InlineHintsOptions.ForLiteralParameters,
            InlineHintsOptions.ForIndexerParameters,
            InlineHintsOptions.ForObjectCreationParameters,
            InlineHintsOptions.ForOtherParameters,
            InlineHintsOptions.EnabledForTypes,
            InlineHintsOptions.ForImplicitVariableTypes,
            InlineHintsOptions.ForLambdaParameterTypes,
            InlineHintsOptions.ForImplicitObjectCreation);
    }
}

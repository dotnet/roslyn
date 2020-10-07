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

        public static readonly PerLanguageOption2<bool> HideForParametersThatDifferOnlyBySuffix =
            new(nameof(InlineHintsOptions),
                nameof(HideForParametersThatDifferOnlyBySuffix),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.HideForParametersThatDifferOnlyBySuffix"));

        public static readonly PerLanguageOption2<bool> HideForParametersThatMatchMethodIntent =
            new(nameof(InlineHintsOptions),
                nameof(HideForParametersThatMatchMethodIntent),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.HideForParametersThatMatchMethodIntent"));
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
            InlineHintsOptions.EnabledForParameters,
            InlineHintsOptions.ForLiteralParameters,
            InlineHintsOptions.ForObjectCreationParameters,
            InlineHintsOptions.ForOtherParameters);
    }
}

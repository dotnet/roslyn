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
    internal readonly record struct InlineParameterHintsOptions(
        bool EnabledForParameters,
        bool ForLiteralParameters,
        bool ForIndexerParameters,
        bool ForObjectCreationParameters,
        bool ForOtherParameters,
        bool SuppressForParametersThatDifferOnlyBySuffix,
        bool SuppressForParametersThatMatchMethodIntent,
        bool SuppressForParametersThatMatchArgumentName)
    {
        public static InlineParameterHintsOptions From(Project project)
            => From(project.Solution.Options, project.Language);

        public static InlineParameterHintsOptions From(OptionSet options, string language)
            => new(
                EnabledForParameters: options.GetOption(Metadata.EnabledForParameters, language),
                ForLiteralParameters: options.GetOption(Metadata.ForLiteralParameters, language),
                ForIndexerParameters: options.GetOption(Metadata.ForIndexerParameters, language),
                ForObjectCreationParameters: options.GetOption(Metadata.ForObjectCreationParameters, language),
                ForOtherParameters: options.GetOption(Metadata.ForOtherParameters, language),
                SuppressForParametersThatDifferOnlyBySuffix: options.GetOption(Metadata.SuppressForParametersThatDifferOnlyBySuffix, language),
                SuppressForParametersThatMatchMethodIntent: options.GetOption(Metadata.SuppressForParametersThatMatchMethodIntent, language),
                SuppressForParametersThatMatchArgumentName: options.GetOption(Metadata.SuppressForParametersThatMatchArgumentName, language));

        [ExportSolutionOptionProvider, Shared]
        internal sealed class Metadata : IOptionProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Metadata()
            {
            }

            public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
                EnabledForParameters,
                ForLiteralParameters,
                ForIndexerParameters,
                ForObjectCreationParameters,
                ForOtherParameters,
                SuppressForParametersThatDifferOnlyBySuffix,
                SuppressForParametersThatMatchMethodIntent,
                SuppressForParametersThatMatchArgumentName);

            private const string FeatureName = "InlineHintsOptions";

            public static readonly PerLanguageOption2<bool> EnabledForParameters =
                new(FeatureName,
                    nameof(EnabledForParameters),
                    defaultValue: false,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints"));

            public static readonly PerLanguageOption2<bool> ForLiteralParameters =
                new(FeatureName,
                    nameof(ForLiteralParameters),
                    defaultValue: true,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForLiteralParameters"));

            public static readonly PerLanguageOption2<bool> ForObjectCreationParameters =
                new(FeatureName,
                    nameof(ForObjectCreationParameters),
                    defaultValue: true,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForObjectCreationParameters"));

            public static readonly PerLanguageOption2<bool> ForOtherParameters =
                new(FeatureName,
                    nameof(ForOtherParameters),
                    defaultValue: false,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForOtherParameters"));

            public static readonly PerLanguageOption2<bool> ForIndexerParameters =
                new(FeatureName,
                    nameof(ForIndexerParameters),
                    defaultValue: true,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForArrayIndexers"));

            public static readonly PerLanguageOption2<bool> SuppressForParametersThatDifferOnlyBySuffix =
                new(FeatureName,
                    nameof(SuppressForParametersThatDifferOnlyBySuffix),
                    defaultValue: true,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.SuppressForParametersThatDifferOnlyBySuffix"));

            public static readonly PerLanguageOption2<bool> SuppressForParametersThatMatchMethodIntent =
                new(FeatureName,
                    nameof(SuppressForParametersThatMatchMethodIntent),
                    defaultValue: true,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.SuppressForParametersThatMatchMethodIntent"));

            public static readonly PerLanguageOption2<bool> SuppressForParametersThatMatchArgumentName =
                new(FeatureName,
                    nameof(SuppressForParametersThatMatchArgumentName),
                    defaultValue: true,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.SuppressForParametersThatMatchArgumentName"));
        }
    }
}

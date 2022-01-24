// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Json
{
    [DataContract]
    internal readonly record struct JsonOptions(
        [property: DataMember(Order = 0)] bool ColorizeJsonPatterns,
        [property: DataMember(Order = 1)] bool ReportInvalidJsonPatterns,
        [property: DataMember(Order = 2)] bool HighlightRelatedJsonComponentsUnderCursor,
        [property: DataMember(Order = 3)] bool DetectAndOfferEditorFeaturesForProbableJsonStrings)
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
                ColorizeJsonPatterns,
                ReportInvalidJsonPatterns,
                HighlightRelatedJsonComponentsUnderCursor,
                DetectAndOfferEditorFeaturesForProbableJsonStrings);

            public static PerLanguageOption<bool> ColorizeJsonPatterns =
                new PerLanguageOption<bool>(
                    nameof(JsonOptions),
                    nameof(ColorizeJsonPatterns),
                    defaultValue: true,
                    storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ColorizeJsonPatterns"));

            public static PerLanguageOption<bool> ReportInvalidJsonPatterns =
                new PerLanguageOption<bool>(
                    nameof(JsonOptions),
                    nameof(ReportInvalidJsonPatterns),
                    defaultValue: true,
                    storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ReportInvalidJsonPatterns"));

            public static PerLanguageOption<bool> HighlightRelatedJsonComponentsUnderCursor =
                new PerLanguageOption<bool>(
                    nameof(JsonOptions),
                    nameof(HighlightRelatedJsonComponentsUnderCursor),
                    defaultValue: true,
                    storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.HighlightRelatedJsonComponentsUnderCursor"));

            public static PerLanguageOption<bool> DetectAndOfferEditorFeaturesForProbableJsonStrings =
                new PerLanguageOption<bool>(
                    nameof(JsonOptions),
                    nameof(DetectAndOfferEditorFeaturesForProbableJsonStrings),
                    defaultValue: true,
                    storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.DetectAndOfferEditorFeaturesForProbableJsonStrings"));
        }

        public static readonly JsonOptions Default
          = new(
              ColorizeJsonPatterns: Metadata.ColorizeJsonPatterns.DefaultValue,
              ReportInvalidJsonPatterns: Metadata.ReportInvalidJsonPatterns.DefaultValue,
              HighlightRelatedJsonComponentsUnderCursor: Metadata.HighlightRelatedJsonComponentsUnderCursor.DefaultValue,
              DetectAndOfferEditorFeaturesForProbableJsonStrings: Metadata.DetectAndOfferEditorFeaturesForProbableJsonStrings.DefaultValue);

        public static JsonOptions From(Project project)
            => From(project.Solution.Options, project.Language);

        public static JsonOptions From(OptionSet options, string language)
            => new(
                ColorizeJsonPatterns: options.GetOption(Metadata.ColorizeJsonPatterns, language),
                ReportInvalidJsonPatterns: options.GetOption(Metadata.ReportInvalidJsonPatterns, language),
                HighlightRelatedJsonComponentsUnderCursor: options.GetOption(Metadata.HighlightRelatedJsonComponentsUnderCursor, language),
                DetectAndOfferEditorFeaturesForProbableJsonStrings: options.GetOption(Metadata.DetectAndOfferEditorFeaturesForProbableJsonStrings, language));
    }
}

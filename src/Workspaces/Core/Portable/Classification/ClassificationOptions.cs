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

namespace Microsoft.CodeAnalysis.Classification
{
    [DataContract]
    internal readonly record struct ClassificationOptions(
        [property: DataMember(Order = 0)] bool ClassifyReassignedVariables,
        [property: DataMember(Order = 1)] bool ColorizeRegexPatterns,
        [property: DataMember(Order = 2)] bool ColorizeJsonPatterns)
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
                ClassifyReassignedVariables,
                ColorizeRegexPatterns,
                ColorizeJsonPatterns);

            private const string FeatureName = "ClassificationOptions";

            public static PerLanguageOption2<bool> ClassifyReassignedVariables =
               new(FeatureName, "ClassifyReassignedVariables", defaultValue: false,
                   storageLocation: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.ClassificationOptions.ClassifyReassignedVariables"));

            public static PerLanguageOption2<bool> ColorizeRegexPatterns =
                new("RegularExpressionsOptions", "ColorizeRegexPatterns", defaultValue: true,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ColorizeRegexPatterns"));

            public static PerLanguageOption2<bool> ColorizeJsonPatterns =
                new("JsonFeatureOptions", "ColorizeJsonPatterns", defaultValue: true,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ColorizeJsonPatterns"));
        }

        public static readonly ClassificationOptions Default
          = new(
              ClassifyReassignedVariables: Metadata.ClassifyReassignedVariables.DefaultValue,
              ColorizeRegexPatterns: Metadata.ColorizeRegexPatterns.DefaultValue,
              ColorizeJsonPatterns: Metadata.ColorizeJsonPatterns.DefaultValue);

        public static ClassificationOptions From(Project project)
            => From(project.Solution.Options, project.Language);

        public static ClassificationOptions From(OptionSet options, string language)
            => new(
                ClassifyReassignedVariables: options.GetOption(Metadata.ClassifyReassignedVariables, language),
                ColorizeRegexPatterns: options.GetOption(Metadata.ColorizeRegexPatterns, language),
                ColorizeJsonPatterns: options.GetOption(Metadata.ColorizeJsonPatterns, language));
    }
}

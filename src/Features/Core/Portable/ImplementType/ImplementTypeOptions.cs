// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.ImplementType
{
    internal readonly record struct ImplementTypeOptions(
        ImplementTypeInsertionBehavior InsertionBehavior,
        ImplementTypePropertyGenerationBehavior PropertyGenerationBehavior)
    {
        public static ImplementTypeOptions From(Project project)
            => From(project.Solution.Options, project.Language);

        public static ImplementTypeOptions From(OptionSet options, string language)
          => new(
              InsertionBehavior: options.GetOption(Metadata.InsertionBehavior, language),
              PropertyGenerationBehavior: options.GetOption(Metadata.PropertyGenerationBehavior, language));

        [ExportSolutionOptionProvider, Shared]
        internal sealed class Metadata : IOptionProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Metadata()
            {
            }

            public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
                InsertionBehavior,
                PropertyGenerationBehavior);

            private const string FeatureName = "ImplementTypeOptions";

            public static readonly PerLanguageOption2<ImplementTypeInsertionBehavior> InsertionBehavior =
                new(FeatureName,
                    "InsertionBehavior",
                    defaultValue: ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.ImplementTypeOptions.InsertionBehavior"));

            public static readonly PerLanguageOption2<ImplementTypePropertyGenerationBehavior> PropertyGenerationBehavior =
                new(FeatureName,
                    "PropertyGenerationBehavior",
                    defaultValue: ImplementTypePropertyGenerationBehavior.PreferThrowingProperties,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.ImplementTypeOptions.PropertyGenerationBehavior"));
        }
    }
}

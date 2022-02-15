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
    internal readonly record struct InlineTypeHintsOptions(
        bool EnabledForTypes,
        bool ForImplicitVariableTypes,
        bool ForLambdaParameterTypes,
        bool ForImplicitObjectCreation)
    {
        public static InlineTypeHintsOptions From(Project project)
            => From(project.Solution.Options, project.Language);

        public static InlineTypeHintsOptions From(OptionSet options, string language)
          => new(
                EnabledForTypes: options.GetOption(Metadata.EnabledForTypes, language),
                ForImplicitVariableTypes: options.GetOption(Metadata.ForImplicitVariableTypes, language),
                ForLambdaParameterTypes: options.GetOption(Metadata.ForLambdaParameterTypes, language),
                ForImplicitObjectCreation: options.GetOption(Metadata.ForImplicitObjectCreation, language));

        [ExportSolutionOptionProvider, Shared]
        internal sealed class Metadata : IOptionProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Metadata()
            {
            }

            public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
                EnabledForTypes,
                ForImplicitVariableTypes,
                ForLambdaParameterTypes,
                ForImplicitObjectCreation);

            private const string FeatureName = "InlineHintsOptions";

            public static readonly PerLanguageOption2<bool> EnabledForTypes =
                new(FeatureName,
                    nameof(EnabledForTypes),
                    defaultValue: false,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineTypeHints"));

            public static readonly PerLanguageOption2<bool> ForImplicitVariableTypes =
                new(FeatureName,
                    nameof(ForImplicitVariableTypes),
                    defaultValue: true,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineTypeHints.ForImplicitVariableTypes"));

            public static readonly PerLanguageOption2<bool> ForLambdaParameterTypes =
                new(FeatureName,
                    nameof(ForLambdaParameterTypes),
                    defaultValue: true,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineTypeHints.ForLambdaParameterTypes"));

            public static readonly PerLanguageOption2<bool> ForImplicitObjectCreation =
                new(FeatureName,
                    nameof(ForImplicitObjectCreation),
                    defaultValue: true,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineTypeHints.ForImplicitObjectCreation"));
        }
    }
}

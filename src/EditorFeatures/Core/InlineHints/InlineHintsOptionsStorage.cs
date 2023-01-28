// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal static class InlineHintsOptionsStorage
    {
        public static InlineHintsOptions GetInlineHintsOptions(this IGlobalOptionService globalOptions, string language)
            => new()
            {
                ParameterOptions = globalOptions.GetInlineParameterHintsOptions(language),
                TypeOptions = globalOptions.GetInlineTypeHintsOptions(language),
                DisplayOptions = globalOptions.GetSymbolDescriptionOptions(language),
            };

        public static InlineParameterHintsOptions GetInlineParameterHintsOptions(this IGlobalOptionService globalOptions, string language)
            => new()
            {
                EnabledForParameters = globalOptions.GetOption(EnabledForParameters, language),
                ForLiteralParameters = globalOptions.GetOption(ForLiteralParameters, language),
                ForIndexerParameters = globalOptions.GetOption(ForIndexerParameters, language),
                ForObjectCreationParameters = globalOptions.GetOption(ForObjectCreationParameters, language),
                ForOtherParameters = globalOptions.GetOption(ForOtherParameters, language),
                SuppressForParametersThatDifferOnlyBySuffix = globalOptions.GetOption(SuppressForParametersThatDifferOnlyBySuffix, language),
                SuppressForParametersThatMatchMethodIntent = globalOptions.GetOption(SuppressForParametersThatMatchMethodIntent, language),
                SuppressForParametersThatMatchArgumentName = globalOptions.GetOption(SuppressForParametersThatMatchArgumentName, language),
            };

        public static InlineTypeHintsOptions GetInlineTypeHintsOptions(this IGlobalOptionService globalOptions, string language)
          => new()
          {
              EnabledForTypes = globalOptions.GetOption(EnabledForTypes, language),
              ForImplicitVariableTypes = globalOptions.GetOption(ForImplicitVariableTypes, language),
              ForLambdaParameterTypes = globalOptions.GetOption(ForLambdaParameterTypes, language),
              ForImplicitObjectCreation = globalOptions.GetOption(ForImplicitObjectCreation, language),
          };

        //  Parameter hints

        public static readonly PerLanguageOption2<bool> EnabledForParameters =
            new("InlineHintsOptions_EnabledForParameters",
                InlineParameterHintsOptions.Default.EnabledForParameters);

        public static readonly PerLanguageOption2<bool> ForLiteralParameters =
            new("InlineHintsOptions_ForLiteralParameters",
                InlineParameterHintsOptions.Default.ForLiteralParameters);

        public static readonly PerLanguageOption2<bool> ForIndexerParameters =
            new("InlineHintsOptions_ForIndexerParameters",
                InlineParameterHintsOptions.Default.ForIndexerParameters);

        public static readonly PerLanguageOption2<bool> ForObjectCreationParameters =
            new("InlineHintsOptions_ForObjectCreationParameters",
                InlineParameterHintsOptions.Default.ForObjectCreationParameters);

        public static readonly PerLanguageOption2<bool> ForOtherParameters =
            new("InlineHintsOptions_ForOtherParameters",
                InlineParameterHintsOptions.Default.ForOtherParameters);

        public static readonly PerLanguageOption2<bool> SuppressForParametersThatDifferOnlyBySuffix =
            new("InlineHintsOptions_SuppressForParametersThatDifferOnlyBySuffix",
                InlineParameterHintsOptions.Default.SuppressForParametersThatDifferOnlyBySuffix);

        public static readonly PerLanguageOption2<bool> SuppressForParametersThatMatchMethodIntent =
            new("InlineHintsOptions_SuppressForParametersThatMatchMethodIntent",
                InlineParameterHintsOptions.Default.SuppressForParametersThatMatchMethodIntent);

        public static readonly PerLanguageOption2<bool> SuppressForParametersThatMatchArgumentName =
            new("InlineHintsOptions_SuppressForParametersThatMatchArgumentName",
                InlineParameterHintsOptions.Default.SuppressForParametersThatMatchArgumentName);

        // Type Hints

        public static readonly PerLanguageOption2<bool> EnabledForTypes =
            new("InlineHintsOptions_EnabledForTypes",
                defaultValue: InlineTypeHintsOptions.Default.EnabledForTypes);

        public static readonly PerLanguageOption2<bool> ForImplicitVariableTypes =
            new("InlineHintsOptions_ForImplicitVariableTypes",
                defaultValue: InlineTypeHintsOptions.Default.ForImplicitVariableTypes);

        public static readonly PerLanguageOption2<bool> ForLambdaParameterTypes =
            new("InlineHintsOptions_ForLambdaParameterTypes",
                defaultValue: InlineTypeHintsOptions.Default.ForLambdaParameterTypes);

        public static readonly PerLanguageOption2<bool> ForImplicitObjectCreation =
            new("InlineHintsOptions_ForImplicitObjectCreation",
                defaultValue: InlineTypeHintsOptions.Default.ForImplicitObjectCreation);
    }
}

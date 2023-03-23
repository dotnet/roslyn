﻿// Licensed to the .NET Foundation under one or more agreements.
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

        private static readonly OptionGroup s_inlineHintOptionGroup = new(name: "inline_hint", description: "");

        //  Parameter hints

        public static readonly PerLanguageOption2<bool> EnabledForParameters =
            new("dotnet_enable_inline_hints_for_parameters",
                InlineParameterHintsOptions.Default.EnabledForParameters,
                group: s_inlineHintOptionGroup);

        public static readonly PerLanguageOption2<bool> ForLiteralParameters =
            new("dotnet_enable_inline_hints_for_literal_parameters",
                InlineParameterHintsOptions.Default.ForLiteralParameters,
                group: s_inlineHintOptionGroup);

        public static readonly PerLanguageOption2<bool> ForIndexerParameters =
            new("dotnet_enable_inline_hints_for_indexer_parameters",
                InlineParameterHintsOptions.Default.ForIndexerParameters,
                group: s_inlineHintOptionGroup);

        public static readonly PerLanguageOption2<bool> ForObjectCreationParameters =
            new("dotnet_enable_inline_hints_for_object_creation_parameters",
                InlineParameterHintsOptions.Default.ForObjectCreationParameters,
                group: s_inlineHintOptionGroup);

        public static readonly PerLanguageOption2<bool> ForOtherParameters =
            new("dotnet_enable_inline_hints_for_other_parameters",
                InlineParameterHintsOptions.Default.ForOtherParameters,
                group: s_inlineHintOptionGroup);

        public static readonly PerLanguageOption2<bool> SuppressForParametersThatDifferOnlyBySuffix =
            new("dotnet_suppress_inline_hints_for_parameters_that_differ_only_by_suffix",
                InlineParameterHintsOptions.Default.SuppressForParametersThatDifferOnlyBySuffix,
                group: s_inlineHintOptionGroup);

        public static readonly PerLanguageOption2<bool> SuppressForParametersThatMatchMethodIntent =
            new("dotnet_suppress_inline_hints_for_parameters_that_match_method_intent",
                InlineParameterHintsOptions.Default.SuppressForParametersThatMatchMethodIntent,
                group: s_inlineHintOptionGroup);

        public static readonly PerLanguageOption2<bool> SuppressForParametersThatMatchArgumentName =
            new("dotnet_suppress_inline_hints_for_parameters_that_match_argument_name",
                InlineParameterHintsOptions.Default.SuppressForParametersThatMatchArgumentName,
                group: s_inlineHintOptionGroup);

        // Type Hints

        public static readonly PerLanguageOption2<bool> EnabledForTypes =
            new("csharp_enable_inline_hints_for_types",
                defaultValue: InlineTypeHintsOptions.Default.EnabledForTypes,
                group: s_inlineHintOptionGroup);

        public static readonly PerLanguageOption2<bool> ForImplicitVariableTypes =
            new("csharp_enable_inline_hints_for_implicit_variable_types",
                defaultValue: InlineTypeHintsOptions.Default.ForImplicitVariableTypes,
                group: s_inlineHintOptionGroup);

        public static readonly PerLanguageOption2<bool> ForLambdaParameterTypes =
            new("csharp_enable_inline_hints_for_lambda_parameter_types",
                defaultValue: InlineTypeHintsOptions.Default.ForLambdaParameterTypes,
                group: s_inlineHintOptionGroup);

        public static readonly PerLanguageOption2<bool> ForImplicitObjectCreation =
            new("csharp_enable_inline_hints_for_implicit_object_creation",
                defaultValue: InlineTypeHintsOptions.Default.ForImplicitObjectCreation,
                group: s_inlineHintOptionGroup);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.InlineHints;

internal readonly record struct OmniSharpInlineHintsOptions(
    OmniSharpInlineParameterHintsOptions ParameterOptions,
    OmniSharpInlineTypeHintsOptions TypeOptions)
{
    internal InlineHintsOptions ToInlineHintsOptions()
        => new()
        {
            ParameterOptions = ParameterOptions.ToInlineParameterHintsOptions(),
            TypeOptions = TypeOptions.ToInlineTypeHintsOptions(),
        };
}
internal readonly record struct OmniSharpInlineParameterHintsOptions(
    bool EnabledForParameters,
    bool ForLiteralParameters,
    bool ForIndexerParameters,
    bool ForObjectCreationParameters,
    bool ForOtherParameters,
    bool SuppressForParametersThatDifferOnlyBySuffix,
    bool SuppressForParametersThatMatchMethodIntent,
    bool SuppressForParametersThatMatchArgumentName)
{
    internal InlineParameterHintsOptions ToInlineParameterHintsOptions()
        => new()
        {
            EnabledForParameters = EnabledForParameters,
            ForLiteralParameters = ForLiteralParameters,
            ForIndexerParameters = ForIndexerParameters,
            ForObjectCreationParameters = ForObjectCreationParameters,
            ForOtherParameters = ForOtherParameters,
            SuppressForParametersThatDifferOnlyBySuffix = SuppressForParametersThatDifferOnlyBySuffix,
            SuppressForParametersThatMatchMethodIntent = SuppressForParametersThatMatchMethodIntent,
            SuppressForParametersThatMatchArgumentName = SuppressForParametersThatMatchArgumentName,
        };
}

internal readonly record struct OmniSharpInlineTypeHintsOptions(
    bool EnabledForTypes,
    bool ForImplicitVariableTypes,
    bool ForLambdaParameterTypes,
    bool ForImplicitObjectCreation)
{
    internal InlineTypeHintsOptions ToInlineTypeHintsOptions()
        => new()
        {
            EnabledForTypes = EnabledForTypes,
            ForImplicitVariableTypes = ForImplicitVariableTypes,
            ForLambdaParameterTypes = ForLambdaParameterTypes,
            ForImplicitObjectCreation = ForImplicitObjectCreation,
        };
}

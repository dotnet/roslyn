// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.InlineHints;

internal readonly record struct OmniSharpInlineHintsOptions(
    OmniSharpInlineParameterHintsOptions ParameterOptions,
    OmniSharpInlineTypeHintsOptions TypeOptions)
{
    internal InlineHintsOptions ToInlineHintsOptions()
        => new(ParameterOptions.ToInlineParameterHintsOptions(),
               TypeOptions.ToInlineTypeHintsOptions(),
               SymbolDescriptionOptions.Default);
}


internal readonly record struct OmniSharpInlineParameterHintsOptions(
    bool EnabledForParameters = false,
    bool ForLiteralParameters = true,
    bool ForIndexerParameters = true,
    bool ForObjectCreationParameters = true,
    bool ForOtherParameters = false,
    bool SuppressForParametersThatDifferOnlyBySuffix = true,
    bool SuppressForParametersThatMatchMethodIntent = true,
    bool SuppressForParametersThatMatchArgumentName = true)
{
    internal InlineParameterHintsOptions ToInlineParameterHintsOptions()
        => new(EnabledForParameters, ForLiteralParameters, ForIndexerParameters, ForObjectCreationParameters, ForOtherParameters, SuppressForParametersThatDifferOnlyBySuffix, SuppressForParametersThatMatchMethodIntent, SuppressForParametersThatMatchArgumentName);
}

internal readonly record struct OmniSharpInlineTypeHintsOptions(
    bool EnabledForTypes = false,
    bool ForImplicitVariableTypes = true,
    bool ForLambdaParameterTypes = true,
    bool ForImplicitObjectCreation = true)
{
    internal InlineTypeHintsOptions ToInlineTypeHintsOptions()
        => new(EnabledForTypes, ForImplicitVariableTypes, ForLambdaParameterTypes, ForImplicitObjectCreation);
}

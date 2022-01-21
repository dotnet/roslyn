// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal readonly record struct InlineHintsOptions(
        InlineParameterHintsOptions ParameterOptions,
        InlineTypeHintsOptions TypeOptions,
        SymbolDescriptionOptions DisplayOptions)
    {
        public static InlineHintsOptions From(Project project)
            => From(project.Solution.Options, project.Language);

        public static InlineHintsOptions From(OptionSet options, string language)
            => new(
                ParameterOptions: InlineParameterHintsOptions.From(options, language),
                TypeOptions: InlineTypeHintsOptions.From(options, language),
                DisplayOptions: SymbolDescriptionOptions.From(options, language));
    }
}

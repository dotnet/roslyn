// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.ImplementType
{
    internal static class OmniSharpImplementTypeOptions
    {
        public static OmniSharpImplementTypeInsertionBehavior GetInsertionBehavior(OptionSet options, string language)
            => (OmniSharpImplementTypeInsertionBehavior)options.GetOption(ImplementTypeOptions.InsertionBehavior, language);

        public static OptionSet SetInsertionBehavior(OptionSet options, string language, OmniSharpImplementTypeInsertionBehavior value)
            => options.WithChangedOption(ImplementTypeOptions.InsertionBehavior, language, (ImplementTypeInsertionBehavior)value);

        public static OmniSharpImplementTypePropertyGenerationBehavior GetPropertyGenerationBehavior(OptionSet options, string language)
            => (OmniSharpImplementTypePropertyGenerationBehavior)options.GetOption(ImplementTypeOptions.PropertyGenerationBehavior, language);

        public static OptionSet SetPropertyGenerationBehavior(OptionSet options, string language, OmniSharpImplementTypePropertyGenerationBehavior value)
            => options.WithChangedOption(ImplementTypeOptions.PropertyGenerationBehavior, language, (ImplementTypePropertyGenerationBehavior)value);
    }

    internal enum OmniSharpImplementTypeInsertionBehavior
    {
        WithOtherMembersOfTheSameKind = ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind,
        AtTheEnd = ImplementTypeInsertionBehavior.AtTheEnd,
    }

    internal enum OmniSharpImplementTypePropertyGenerationBehavior
    {
        PreferThrowingProperties = ImplementTypePropertyGenerationBehavior.PreferThrowingProperties,
        PreferAutoProperties = ImplementTypePropertyGenerationBehavior.PreferAutoProperties,
    }
}

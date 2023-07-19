// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ImplementType
{
    internal static class ImplementTypeOptionsStorage
    {
        public static ImplementTypeOptions GetImplementTypeOptions(this IGlobalOptionService globalOptions, string language)
          => new()
          {
              InsertionBehavior = globalOptions.GetOption(InsertionBehavior, language),
              PropertyGenerationBehavior = globalOptions.GetOption(PropertyGenerationBehavior, language)
          };

        public static ImplementTypeGenerationOptions GetImplementTypeGenerationOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
          => new(globalOptions.GetImplementTypeOptions(languageServices.Language),
                 globalOptions.CreateProvider());

        private static readonly OptionGroup s_implementTypeGroup = new(name: "implement_type", description: "");

        public static readonly PerLanguageOption2<ImplementTypeInsertionBehavior> InsertionBehavior =
            new("dotnet_insertion_behavior",
                defaultValue: ImplementTypeOptions.Default.InsertionBehavior, group: s_implementTypeGroup, serializer: EditorConfigValueSerializer.CreateSerializerForEnum<ImplementTypeInsertionBehavior>());

        public static readonly PerLanguageOption2<ImplementTypePropertyGenerationBehavior> PropertyGenerationBehavior =
            new("dotnet_property_generation_behavior",
                defaultValue: ImplementTypeOptions.Default.PropertyGenerationBehavior, group: s_implementTypeGroup, serializer: EditorConfigValueSerializer.CreateSerializerForEnum<ImplementTypePropertyGenerationBehavior>());
    }
}

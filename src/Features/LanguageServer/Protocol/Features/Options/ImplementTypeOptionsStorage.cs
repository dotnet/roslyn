// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeGeneration;
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

        public static readonly PerLanguageOption2<ImplementTypeInsertionBehavior> InsertionBehavior =
            new("ImplementTypeOptions_InsertionBehavior",
                defaultValue: ImplementTypeOptions.Default.InsertionBehavior, serializer: EditorConfigValueSerializer.CreateSerializerForEnum<ImplementTypeInsertionBehavior>());

        public static readonly PerLanguageOption2<ImplementTypePropertyGenerationBehavior> PropertyGenerationBehavior =
            new("ImplementTypeOptions_PropertyGenerationBehavior",
                defaultValue: ImplementTypeOptions.Default.PropertyGenerationBehavior, serializer: EditorConfigValueSerializer.CreateSerializerForEnum<ImplementTypePropertyGenerationBehavior>());
    }
}

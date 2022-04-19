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
          => new(
              InsertionBehavior: globalOptions.GetOption(InsertionBehavior, language),
              PropertyGenerationBehavior: globalOptions.GetOption(PropertyGenerationBehavior, language));

        public static ImplementTypeGenerationOptions GetImplementTypeGenerationOptions(this IGlobalOptionService globalOptions, HostLanguageServices languageServices)
          => new(globalOptions.GetImplementTypeOptions(languageServices.Language),
                 globalOptions.CreateProvider());

        private const string FeatureName = "ImplementTypeOptions";

        public static readonly PerLanguageOption2<ImplementTypeInsertionBehavior> InsertionBehavior =
            new(FeatureName,
                "InsertionBehavior",
                defaultValue: ImplementTypeOptions.Default.InsertionBehavior,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.ImplementTypeOptions.InsertionBehavior"));

        public static readonly PerLanguageOption2<ImplementTypePropertyGenerationBehavior> PropertyGenerationBehavior =
            new(FeatureName,
                "PropertyGenerationBehavior",
                defaultValue: ImplementTypeOptions.Default.PropertyGenerationBehavior,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.ImplementTypeOptions.PropertyGenerationBehavior"));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel
{
    internal class ExpectedSetting(string path, IOption2 option, UnifiedSettingBase setting)
    {
        public string ExpectedUnifiedSettingsPath => path;
        public IOption2 Option => option;
        public UnifiedSettingBase ExpectedUnifiedSetting => setting;

        public static ExpectedSetting Create<T>(
            string ExpectedUnifiedSettingsPath,
            IOption2 roslynOption,
            string title,
            int order,
            T defaultValue,
            (IOption2 featureFlagOption, object value) featureFlagAndExperimentValue = default,
            (IOption2 enableWhenOption, object whenValue) enableWhenOptionAndValue = default,
            string? languageName = null)
        {
            var migration = new Migration
            {
                Pass = new Pass()
                {
                    Input = Input(roslynOption, languageName)
                }
            };

            var alternativeDefault = featureFlagAndExperimentValue is not default
                ? new AlternativeDefault<T>(featureFlagAndExperimentValue.featureFlagOption, featureFlagAndExperimentValue.value)
                : null;

            var enableWhen = enableWhenOptionAndValue is not default
                ? $"config:{enableWhen}"




        }
    }
}

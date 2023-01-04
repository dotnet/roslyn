// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.Options;

public sealed class GlobalOptionsTest : AbstractIntegrationTest
{
    public GlobalOptionsTest()
    {
    }

    [IdeFact]
    public async Task ValidateAllOptions()
    {
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);

        var optionsInfo = OptionsTestInfo.CollectOptions(Path.GetDirectoryName(typeof(GlobalOptionsTest).Assembly.Location!));
        var allLanguages = new[] { LanguageNames.CSharp, LanguageNames.VisualBasic };
        var noLanguages = new[] { (string?)null };

        foreach (var (option, _, _, _) in optionsInfo.GlobalOptions.Values)
        {
            foreach (var language in option.IsPerLanguage ? allLanguages : noLanguages)
            {
                var key = new OptionKey2(option, language);
                var currentValue = globalOptions.GetOption<object?>(key);

                // do not attempt to update feature flags
                if (option.StorageLocations.Any(s => s is FeatureFlagStorageLocation))
                {
                    Assert.True(currentValue is bool);
                    continue;
                }

                var differentValue = OptionsTestHelpers.GetDifferentValue(option.Type, currentValue);
                globalOptions.SetGlobalOption(key, differentValue);

                object? updatedValue;

                try
                {
                    updatedValue = globalOptions.GetOption<object?>(key);
                }
                finally
                {
                    globalOptions.SetGlobalOption(key, currentValue);
                }

                Assert.Equal(differentValue, updatedValue);
            }
        }
    }
}

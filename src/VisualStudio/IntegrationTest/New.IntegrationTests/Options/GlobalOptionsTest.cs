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
using Microsoft.CodeAnalysis.TaskList;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.Settings;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
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
        var globalOptions = (GlobalOptionService)await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        var provider = await TestServices.Shell.GetComponentModelServiceAsync<VisualStudioOptionPersisterProvider>(HangMitigatingCancellationToken);
        var vsSettingsPersister = (VisualStudioOptionPersister)await provider.GetOrCreatePersisterAsync(HangMitigatingCancellationToken);

        var optionsInfo = OptionsTestInfo.CollectOptions(Path.GetDirectoryName(typeof(GlobalOptionsTest).Assembly.Location!));
        var allLanguages = new[] { LanguageNames.CSharp, LanguageNames.VisualBasic };
        var noLanguages = new[] { (string?)null };

        foreach (var (configName, optionInfo) in optionsInfo)
        {
            var option = optionInfo.Option;

            // skip public options:
            if (option is IPublicOption)
            {
                continue;
            }

            if (!VisualStudioOptionStorage.Storages.TryGetValue(configName, out var storage))
            {
                continue;
            }

            // TODO: issue https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1585884
            if (option == TaskListOptionsStorage.Descriptors)
            {
                continue;
            }

            foreach (var language in option.IsPerLanguage ? allLanguages : noLanguages)
            {
                var key = new OptionKey2(option, language);

                // validate that reading the option directly from the persister without falling back to default value works:
                AssertEx.AreEqual(true, vsSettingsPersister.TryFetch(key, out var currentValue),
                    message: $"Option '{option.Definition.ConfigName}' failed to load from VS settings.");

                // do not attempt to update feature flags
                if (storage is VisualStudioOptionStorage.FeatureFlagStorage)
                {
                    Assert.True(currentValue is bool);
                    continue;
                }

                var differentValue = OptionsTestHelpers.GetDifferentValue(option.Type, currentValue);

                await vsSettingsPersister.PersistAsync(storage, key, differentValue);

                // make sure we fetch the value from the storage:
                globalOptions.ClearCachedValues();

                object? updatedValue;
                try
                {
                    updatedValue = globalOptions.GetOption<object?>(key);
                }
                finally
                {
                    await vsSettingsPersister.PersistAsync(storage, key, currentValue);
                }

                AssertEx.AreEqual(differentValue, updatedValue, message: $"Option '{option.Definition.ConfigName}' failed to persist to VS settings.");
            }
        }
    }
}

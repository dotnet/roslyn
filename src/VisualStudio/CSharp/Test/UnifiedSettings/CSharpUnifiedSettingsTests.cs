// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Options;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.UnifiedSettings
{
    public class CSharpUnifiedSettingsTests
    {
        private readonly static ImmutableArray<IOption2> s_onboardedOptions = ImmutableArray.Create<IOption2>(CompletionOptionsStorage.TriggerOnTypingLetters);

        [Fact]
        public async Task CSharpUnifiedSettingsTest()
        {
            var registrationFileStream = typeof(CSharpUnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.CSharp.UnitTests.csharpSettings.registration.json");
            using var reader = new StreamReader(registrationFileStream);
            var registrationFile = await reader.ReadToEndAsync().ConfigureAwait(false);

            foreach (var option in s_onboardedOptions)
            {
                var optionName = option.Definition.ConfigName;
                if (VisualStudioOptionStorage.UnifiedSettingsStorages.TryGetValue(optionName, out var unifiedSettingsStorage))
                {

                }
                else
                {
                    // Can't find the option in the storage dictionary
                    throw ExceptionUtilities.UnexpectedValue(optionName);
                }
            }
        }
    }
}

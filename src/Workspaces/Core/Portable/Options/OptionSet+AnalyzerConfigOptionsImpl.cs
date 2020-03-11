// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Options
{
    public abstract partial class OptionSet
    {
        private sealed class AnalyzerConfigOptionsImpl : AnalyzerConfigOptions
        {
            private readonly OptionSet _optionSet;
            private readonly IOptionService _optionService;
            private readonly string? _language;

            public AnalyzerConfigOptionsImpl(OptionSet optionSet, IOptionService optionService, string? language)
            {
                _optionSet = optionSet;
                _optionService = optionService;
                _language = language;
            }

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            {
                if (!_optionService.TryMapEditorConfigKeyToOption(key, _language, out var storageLocation, out var optionKey))
                {
                    // There are couple of reasons this assert might fire:
                    //  1. Attempting to access an option which does not have an IEditorConfigStorageLocation.
                    //  2. Attempting to access an option which is not exposed from any option provider, i.e. IOptionProvider.Options.
                    Debug.Fail("Failed to find an .editorconfig entry for the requested key.");
                    value = null;
                    return false;
                }

                var typedValue = _optionSet.GetOption(optionKey);
                value = storageLocation.GetEditorConfigStringValue(typedValue, _optionSet);
                return true;
            }
        }
    }
}

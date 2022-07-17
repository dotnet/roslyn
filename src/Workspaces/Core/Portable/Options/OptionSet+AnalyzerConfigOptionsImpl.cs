// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Options
{
    public abstract partial class OptionSet
    {
        private sealed class AnalyzerConfigOptionsImpl : AnalyzerConfigOptions
        {
            private readonly OptionSet _optionSet;
            private readonly IEditorConfigOptionMappingService _optionMappingService;
            private readonly string? _language;

            public AnalyzerConfigOptionsImpl(OptionSet optionSet, IEditorConfigOptionMappingService optionMappingService, string? language)
            {
                _optionSet = optionSet;
                _optionMappingService = optionMappingService;
                _language = language;
            }

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            {
                if (!_optionMappingService.TryMapEditorConfigKeyToOption(key, _language, out var storageLocation, out var optionKey))
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

            // no way to enumerate OptionSet
            public override IEnumerable<string> Keys
                => throw new NotImplementedException();
        }
    }
}

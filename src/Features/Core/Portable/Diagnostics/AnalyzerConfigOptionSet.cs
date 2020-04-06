// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// This class proxies requests for option values first to the <see cref="AnalyzerConfigOptions" /> then to a backup <see cref="OptionSet" /> if provided.
    /// </summary>
    internal sealed partial class AnalyzerConfigOptionSet : OptionSet
    {
        private readonly AnalyzerConfigOptions _analyzerConfigOptions;
        private readonly OptionSet? _optionSet;

        public AnalyzerConfigOptionSet(AnalyzerConfigOptions analyzerConfigOptions, OptionSet? optionSet)
        {
            _analyzerConfigOptions = analyzerConfigOptions;
            _optionSet = optionSet;
        }

        private protected override object GetOptionCore(OptionKey optionKey)
        {
            // First try to find the option from the .editorconfig options parsed by the compiler.
            if (_analyzerConfigOptions.TryGetEditorConfigOption<object>(optionKey.Option, out var value))
            {
                return value;
            }

            // Fallback to looking for option from the document's optionset if unsuccessful.
            return _optionSet?.GetOption(optionKey) ?? optionKey.Option.DefaultValue!;
        }

        public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object? value)
            => throw new NotImplementedException();

        private protected override AnalyzerConfigOptions CreateAnalyzerConfigOptions(IOptionService optionService, string? language)
        {
            if (_optionSet is null)
            {
                return _analyzerConfigOptions;
            }

            return new AnalyzerConfigOptionsImpl(_analyzerConfigOptions, _optionSet.AsAnalyzerConfigOptions(optionService, language));
        }

        internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet)
            => throw new NotImplementedException();
    }
}

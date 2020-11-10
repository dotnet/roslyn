
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.Structure
{
    internal sealed class BlockStructureOptionProvider
    {
        private readonly OptionSet _options;

        public BlockStructureOptionProvider(OptionSet options, bool isMetadataAsSource)
        {
            _options = options;
            IsMetadataAsSource = isMetadataAsSource;
        }

        public bool IsMetadataAsSource { get; }

        public T GetOption<T>(PerLanguageOption2<T> option, string language)
        {
#if CODE_STYLE
            _options.TryGetEditorConfigOptionOrDefault<T>(option, out var value);
            return value;
#else
            return _options.GetOption(option, language);
#endif
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

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
            => _options.GetOption(option, language);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Options;

public abstract partial class OptionSet
{
    private sealed class EmptyOptionSet : OptionSet
    {
        private protected override object? GetOptionCore(OptionKey optionKey)
            => optionKey.Option.DefaultValue;

        public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object? value)
            => throw new NotSupportedException();

        internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet)
            => Array.Empty<OptionKey>();
    }
}

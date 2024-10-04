// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal class EditAndContinueTestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _options;

        public EditAndContinueTestAnalyzerConfigOptions(IEnumerable<(string key, string value)> options)
            => _options = options.ToDictionary(e => e.key, e => e.value);

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            => _options.TryGetValue(key, out value);
    }
}

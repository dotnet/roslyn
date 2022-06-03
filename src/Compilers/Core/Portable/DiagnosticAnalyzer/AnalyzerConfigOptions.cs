// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public abstract class AnalyzerConfigOptions
    {
        /// <summary>
        /// Comparer that should be used for all analyzer config keys. This is a case-insensitive comparison based
        /// on Unicode case sensitivity rules for identifiers.
        /// </summary>
        public static StringComparer KeyComparer { get; } = AnalyzerConfig.Section.PropertiesKeyComparer;

        /// <summary>
        /// Get an analyzer config value for the given key, using the <see cref="KeyComparer"/>.
        /// </summary>
        public abstract bool TryGetValue(string key, [NotNullWhen(true)] out string? value);

        /// <summary>
        /// Enumerates unique keys of all available options in no specific order.
        /// </summary>
        /// <exception cref="NotImplementedException">Not implemented by the derived type.</exception>
        public virtual IEnumerable<string> Keys
            => throw new NotImplementedException();
    }
}

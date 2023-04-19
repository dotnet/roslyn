// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Specifies that the option is stored in feature flag storage.
    /// </summary>
    internal sealed class FeatureFlagStorageLocation : OptionStorageLocation2
    {
        public string Name { get; }

        public FeatureFlagStorageLocation(string name)
        {
            // feature flag name must be qualified by a component name, e.g. "Roslyn.", "Xaml.", "Lsp.", etc.
            Contract.ThrowIfFalse(name.IndexOf('.') > 0);
            Name = name;
        }
    }
}

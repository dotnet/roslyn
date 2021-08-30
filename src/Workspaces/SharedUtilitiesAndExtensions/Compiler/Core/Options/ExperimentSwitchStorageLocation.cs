// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Specifies that the option is stored in a storage that provides switches for experiments.
    /// </summary>
    internal sealed class ExperimentSwitchStorageLocation : OptionStorageLocation2
    {
        public string Name { get; }

        public ExperimentSwitchStorageLocation(string name)
        {
            Contract.ThrowIfTrue(name.IndexOf('.') >= 0);
            Name = name;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a global key that was unset due to conflicts
    /// </summary>
    public readonly struct AnalyzerUnsetKey
    {
        /// <summary>
        /// The name of the key that was unset
        /// </summary>
        public string? KeyName { get; }

        /// <summary>
        /// The section this key came from 
        /// </summary>
        /// <remarks>
        /// Will be 'Global Section' when <see cref="IsGlobalSection"/> is <c>true</c>.
        /// </remarks>
        public string? SectionName { get; }

        /// <summary>
        /// <c>true</c> if the section this key came from was the global section
        /// </summary>
        public bool IsGlobalSection { get; }

        /// <summary>
        /// The list of paths of the global configs that each set this key
        /// </summary>
        public ImmutableArray<string> ConfigPaths { get; }

        internal AnalyzerUnsetKey(string keyName, string sectionName, bool isGlobalSection, ImmutableArray<string> paths)
        {
            KeyName = keyName;
            SectionName = sectionName;
            IsGlobalSection = isGlobalSection;
            ConfigPaths = paths;
        }
    }
}

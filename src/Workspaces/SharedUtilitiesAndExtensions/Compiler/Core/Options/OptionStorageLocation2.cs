// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// The base type of all types that specify where options are stored.
    /// </summary>
    internal abstract class OptionStorageLocation2
#if !CODE_STYLE
        : OptionStorageLocation
#endif
    {
    }

    internal static partial class Extensions
    {
        public static string GetOptionConfigName(this ImmutableArray<OptionStorageLocation2> locations, string? feature, string? name)
            => GetOptionConfigName(locations.OfType<IEditorConfigStorageLocation2>(), feature, name);

#if !CODE_STYLE
        public static string GetOptionConfigName(this ImmutableArray<OptionStorageLocation> locations, string? feature, string? name)
            => GetOptionConfigName(locations.OfType<IEditorConfigStorageLocation2>(), feature, name);
#endif

        private static string GetOptionConfigName(IEnumerable<IEditorConfigStorageLocation2> locations, string? feature, string? name)
        {
            // If this option is an editorconfig option we use the editorconfig name specified in the storage location.
            // Otherwise, the option is a global option. If it is a global option with a new unique name then feature is unspecified and we use the name as is.
            // Otherwise, the option is a global option with an old name and we join feature and name to form a unique config name for now. We expect these options get eventually renamed.
            // TODO: https://github.com/dotnet/roslyn/issues/65787
            var configName = locations.SingleOrDefault()?.KeyName ?? (feature is null ? name : feature + "_" + name);
            Contract.ThrowIfNull(configName);
            return configName;
        }
    }
}

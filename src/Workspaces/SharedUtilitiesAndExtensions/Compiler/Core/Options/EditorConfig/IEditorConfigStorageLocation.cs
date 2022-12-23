// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Roslyn.Utilities;

#if !CODE_STYLE
using System.Linq;
#endif

namespace Microsoft.CodeAnalysis.Options
{
    internal interface IEditorConfigStorageLocation
    {
        bool TryParseValue(string value, out object? result);

        /// <summary>
        /// The name of the editorconfig key for the option.
        /// </summary>
        string KeyName { get; }

        /// <summary>
        /// Gets the editorconfig string representation for the specified <paramref name="value"/>. 
        /// </summary>
        string GetEditorConfigStringValue(object? value);

#if !CODE_STYLE
        /// <summary>
        /// Gets the editorconfig string representation for the option value stored in <paramref name="optionSet"/>.
        /// May combine values of multiple options stored in the set.
        /// </summary>
        string GetEditorConfigStringValue(OptionKey optionKey, OptionSet optionSet);
#endif
    }

    internal static partial class Extensions
    {
#if !CODE_STYLE
        public static string GetOptionConfigName(this ImmutableArray<OptionStorageLocation> locations, string? feature, string? name)
            => (locations.FirstOrDefault() as IEditorConfigStorageLocation).GetOptionConfigName(feature, name);
#endif
        public static string GetOptionConfigName(this IEditorConfigStorageLocation? location, string? feature, string? name)
        {
            // If this option is an editorconfig option we use the editorconfig name specified in the storage location.
            // Otherwise, the option is a global option. If it is a global option with a new unique name then feature is unspecified and we use the name as is.
            // Otherwise, the option is a global option with an old name and we join feature and name to form a unique config name for now. We expect these options get eventually renamed.
            // TODO: https://github.com/dotnet/roslyn/issues/65787
            var configName = location?.KeyName ?? (feature is null ? name : feature + "_" + name);
            Contract.ThrowIfNull(configName);
            return configName;
        }
    }
}

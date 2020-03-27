// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    internal sealed class NamingStylePreferenceEditorConfigStorageLocation : OptionStorageLocation2, IEditorConfigStorageLocation
    {
        public bool TryGetOption(IReadOnlyDictionary<string, string?> rawOptions, Type type, out object result)
        {
            var tuple = ParseDictionary(rawOptions, type);
            result = tuple.result;
            return tuple.succeeded;
        }

        private static (object result, bool succeeded) ParseDictionary(
            IReadOnlyDictionary<string, string?> allRawConventions, Type type)
        {
            if (type == typeof(NamingStylePreferences))
            {
                var editorconfigNamingStylePreferences = EditorConfigNamingStyleParser.GetNamingStylesFromDictionary(allRawConventions);

                if (!editorconfigNamingStylePreferences.NamingRules.Any() &&
                    !editorconfigNamingStylePreferences.NamingStyles.Any() &&
                    !editorconfigNamingStylePreferences.SymbolSpecifications.Any())
                {
                    // We were not able to parse any rules from editorconfig, tell the caller that the parse failed
                    return (result: editorconfigNamingStylePreferences, succeeded: false);
                }

                // no existing naming styles were passed so just return the set of styles that were parsed from editorconfig
                return (result: editorconfigNamingStylePreferences, succeeded: true);
            }

            throw ExceptionUtilities.UnexpectedValue(type);
        }
    }
}

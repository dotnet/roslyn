﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    internal sealed class NamingStylePreferenceEditorConfigStorageLocation : OptionStorageLocation, IEditorConfigStorageLocation
    {
        public bool TryGetOption(object underlyingOption, IReadOnlyDictionary<string, object> allRawConventions, Type type, out object result)
        {
            var tuple = ParseDictionary(underlyingOption, allRawConventions, type);
            result = tuple.result;
            return tuple.succeeded;
        }

        private static (object result, bool succeeded) ParseDictionary(
            object underlyingOption, IReadOnlyDictionary<string, object> allRawConventions, Type type)
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

                if (underlyingOption is NamingStylePreferences workspaceNamingStylePreferences)
                {
                    // We parsed naming styles from editorconfig, append them to our existing styles
                    var combinedNamingStylePreferences = workspaceNamingStylePreferences.PrependNamingStylePreferences(editorconfigNamingStylePreferences);
                    return (result: combinedNamingStylePreferences, succeeded: true);
                }

                // no existing naming styles were passed so just return the set of styles that were parsed from editorconfig
                return (result: editorconfigNamingStylePreferences, succeeded: true);
            }
            else
            {
                return Contract.FailWithReturn<(object, bool)>(
                    $"{nameof(NamingStylePreferenceEditorConfigStorageLocation)} can only be called with {nameof(PerLanguageOption<NamingStylePreferences>)}<{nameof(NamingStylePreferences)}>.");
            }
        }
    }
}

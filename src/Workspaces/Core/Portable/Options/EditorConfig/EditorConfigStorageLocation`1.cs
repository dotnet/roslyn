// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Specifies that an option should be read from an .editorconfig file.
    /// </summary>
    internal sealed class EditorConfigStorageLocation<T> : OptionStorageLocation, IEditorConfigStorageLocation
    {
        public string KeyName { get; }

        private readonly Func<string, Type, Optional<T>> _parseValue;

        private readonly Func<object, IReadOnlyDictionary<string, object>, Type, (object result, bool succeeded)> _tryParseDictionary;

        private static Func<object, IReadOnlyDictionary<string, object>, Type, (object result, bool succeeded)> _cachedTryParseDictionary = (underlyingOption, dictionary, type) =>
        {
            if (type == typeof(NamingStylePreferences))
            {
                var editorconfigNamingStylePreferences = EditorConfigNamingStyleParser.GetNamingStylesFromDictionary(dictionary);

                if (!editorconfigNamingStylePreferences.NamingRules.Any() &&
                    !editorconfigNamingStylePreferences.NamingStyles.Any() &&
                    !editorconfigNamingStylePreferences.SymbolSpecifications.Any())
                {
                    // We were not able to parse any rules from editorconfig, tell the caller that the parse failed
                    return (result: editorconfigNamingStylePreferences, succeeded: false);
                }

                var workspaceNamingStylePreferences = underlyingOption as NamingStylePreferences;
                if (workspaceNamingStylePreferences != null)
                {
                    // We parsed naming styles from editorconfig, append them to our existing styles
                    var combinedNamingStylePreferences = workspaceNamingStylePreferences.PrependNamingStylePreferences(editorconfigNamingStylePreferences);
                    return (result: (object)combinedNamingStylePreferences, succeeded: true);
                }

                // no existing naming styles were passed so just return the set of styles that were parsed from editorconfig
                return (result: editorconfigNamingStylePreferences, succeeded: true);
            }
            else
            {
                throw new NotSupportedException(WorkspacesResources.Option_0_has_an_unsupported_type_to_use_with_1_You_should_specify_a_parsing_function);
            }
        };

        public bool TryGetOption(object underlyingOption, IReadOnlyDictionary<string, object> allRawConventions, Type type, out object result)
        {
            if (_parseValue != null && KeyName != null)
            {
                if (allRawConventions.TryGetValue(KeyName, out object value))
                {
                    var optionalValue = _parseValue(value.ToString(), type);
                    if (optionalValue.HasValue)
                    {
                        result = optionalValue.Value;
                    }
                    else
                    {
                        result = null;
                    }

                    return result != null;
                }
            }
            else if (_tryParseDictionary != null)
            {
                var tuple = _tryParseDictionary(underlyingOption, allRawConventions, type);
                result = tuple.result;
                return tuple.succeeded;
            }

            result = null;
            return false;
        }

        public EditorConfigStorageLocation(string keyName, Func<string, Optional<T>> parseValue)
        {
            KeyName = keyName;

            // If we're explicitly given a parsing function we can throw away the type when parsing
            _parseValue = (s, type) => parseValue(s);
        }

        public EditorConfigStorageLocation()
        {
            Contract.ThrowIfFalse(typeof(T) == typeof(NamingStylePreferences),
                $"Using {nameof(EditorConfigStorageLocation)}() with arguments is only supported for {nameof(NamingStylePreferences)}");

            // If the user didn't pass a keyName assume we need to parse the entire dictionary
            _tryParseDictionary = _cachedTryParseDictionary;
        }

        public EditorConfigStorageLocation(Func<object, IReadOnlyDictionary<string, object>, (object result, bool succeeded)> tryParseDictionary)
        {
            // If we're explicitly given a parsing function we can throw away the type when parsing
            _tryParseDictionary = (underlyingOption, dictionary, type) => tryParseDictionary(underlyingOption, dictionary);
        }
    }
}
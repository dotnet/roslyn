// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    internal static class CompilerOptionParseUtilities
    {
        /// <summary>
        /// Parse the value provided to an MSBuild Feature option into a list of entries.  This will 
        /// leave name=value in their raw form.
        /// </summary>
        public static IList<string> ParseFeatureFromMSBuild(string features)
        {
            if (string.IsNullOrEmpty(features))
            {
                return SpecializedCollections.EmptyList<string>();
            }

            var all = features.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<string>(capacity: all.Length);
            foreach (var feature in all)
            {
                list.Add(feature);
            }

            return list;
        }

        public static ImmutableDictionary<string, string> ParseFeatures(List<string> values)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var commaFeatures in values)
            {
                foreach (var feature in commaFeatures.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    ParseFeatureCore(builder, feature);
                }
            }

            return builder.ToImmutable();
        }

        public static ImmutableDictionary<string, string> ParseFeatures(string features)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var feature in ParseFeatureFromMSBuild(features))
            {
                ParseFeatureCore(builder, feature);
            }
            return builder.ToImmutable();
        }

        private static void ParseFeatureCore(ImmutableDictionary<string, string>.Builder builder, string feature)
        {
            int equals = feature.IndexOf('=');
            if (equals > 0)
            {
                string name = feature.Substring(0, equals);
                string value = feature.Substring(equals + 1);
                builder[name] = value;
            }
            else
            {
                builder[feature] = "true";
            }
        }
    }
}

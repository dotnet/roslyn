// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal static class CompilerOptionParseUtilities
    {
        /// <summary>
        /// Parse the value provided to an MSBuild Feature option into a list of entries.  This will 
        /// leave name=value in their raw form.
        /// </summary>
        public static IList<string> ParseFeatureFromMSBuild(string? features)
        {
            if (RoslynString.IsNullOrEmpty(features))
            {
                return new List<string>(capacity: 0);
            }

            return features.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static void ParseFeatures(IDictionary<string, string> builder, List<string> values)
        {
            foreach (var commaFeatures in values)
            {
                foreach (var feature in commaFeatures.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    ParseFeatureCore(builder, feature);
                }
            }
        }

        private static void ParseFeatureCore(IDictionary<string, string> builder, string feature)
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

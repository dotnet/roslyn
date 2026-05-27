// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal static class CompilerOptionParseUtilities
    {
        internal const string CachePathEnvironmentVariable = "ROSLYN_CACHE_PATH";
        internal const string UseGlobalCacheFeatureFlag = "use-global-cache";

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

        internal static void PrependFeatureFlagFromEnvironment(List<string> arguments, Action<string>? log = null)
        {
            if (Environment.GetEnvironmentVariable(CachePathEnvironmentVariable) is not { Length: > 0 } cachePath)
            {
                return;
            }

            if (cachePath[0] != '"')
            {
                cachePath = $"\"{cachePath}\"";
            }

            log?.Invoke($"Normalizing {CachePathEnvironmentVariable} to /features:{UseGlobalCacheFeatureFlag}={cachePath}");
            arguments.Insert(0, $"/features:{UseGlobalCacheFeatureFlag}={cachePath}");
        }

        private static void ParseFeatureCore(IDictionary<string, string> builder, string feature)
        {
            int equals = feature.IndexOf('=');
            if (equals > 0)
            {
                string name = feature.Substring(0, equals);
                string value = feature.Substring(equals + 1).Trim('"');
                builder[name] = value;
            }
            else
            {
                builder[feature] = "true";
            }
        }
    }
}

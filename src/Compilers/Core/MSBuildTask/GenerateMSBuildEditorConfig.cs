// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// Transforms a set of MSBuild Properties and Metadata into a global analyzer config.
    /// </summary>
    /// <remarks>
    /// This task takes a set of items passed in via <see cref="MetadataItems"/> and <see cref="PropertyItems"/> and transforms
    /// them into a global analyzer config. 
    /// 
    /// <see cref="PropertyItems"/> is expected to be a list of items whose <see cref="ITaskItem.ItemSpec"/> is the property name
    /// and have a metadata value called <c>Value</c> that contains the evaluated value of the property. Each of the ]
    /// <see cref="PropertyItems"/> will be transformed into an <c>build_property.<em>ItemSpec</em> = <em>Value</em></c> entry in the
    /// global section of the generated config file.
    /// 
    /// <see cref="MetadataItems"/> is expected to be a list of items whose <see cref="ITaskItem.ItemSpec"/> represents a file in the 
    /// compilation source tree. It should have two metadata values: <c>ItemType</c> is the name of the MSBuild item that originally 
    /// included the file (e.g. <c>Compile</c>, <c>AdditionalFile</c> etc.); <c>MetadataName</c> is expected to contain the name of
    /// another piece of metadata that should be retrieved and used as the output value in the file. It is expected that a given 
    /// file can have multiple entries in the <see cref="MetadataItems" /> differing by its <c>ItemType</c>.
    /// 
    /// Each of the <see cref="MetadataItems"/> will be transformed into a new section in the generated config file. The section
    /// header will be the full path of the item (generated via its<see cref="ITaskItem.ItemSpec"/>), and each section will have a 
    /// set of <c>build_metadata.<em>ItemType</em>.<em>MetadataName</em> = <em>RetrievedMetadataValue</em></c>, one per <c>ItemType</c>
    /// 
    /// The Microsoft.Managed.Core.targets calls this task with the collected results of the <c>AnalyzerProperty</c> and 
    /// <c>AnalyzerItemMetadata</c> item groups. 
    /// </remarks>
    public sealed class GenerateMSBuildEditorConfig : Task
    {
        /// <remarks>
        /// Although this task does its own writing to disk, this
        /// output parameter is here for testing purposes.
        /// </remarks>
        [Output]
        public string ConfigFileContents { get; set; }

        [Required]
        public ITaskItem[] MetadataItems { get; set; }

        [Required]
        public ITaskItem[] PropertyItems { get; set; }

        public ITaskItem FileName { get; set; }

        /// <summary>
        /// The path map used by the compiler (the value of the <c>/pathmap</c> option), in the same
        /// <c>from=to,from2=to2</c> format. It is applied to the generated config only where
        /// <see cref="MapSectionHeaderPaths"/> or <see cref="MapPropertyValues"/> opts in, rewriting
        /// absolute paths to their deterministic (mapped) form so the config is independent of the
        /// directory the build ran in. Paths that do not begin with a mapped root are left unchanged.
        /// </summary>
        public string PathMap { get; set; }

        /// <summary>
        /// When <see langword="true"/>, the file paths used as section headers are rewritten through
        /// <see cref="PathMap"/>. The compiler tries both the real and mapped path when resolving a
        /// file's options, so mapped headers continue to match. Opt-in and off by default.
        /// </summary>
        public bool MapSectionHeaderPaths { get; set; }

        /// <summary>
        /// When <see langword="true"/>, any emitted <c>build_property</c> value that begins with a
        /// mapped root (e.g. <c>ProjectDir</c>) is rewritten through <see cref="PathMap"/>. Opt-in
        /// and off by default because a source generator that reads such a value and opens or embeds
        /// it would receive a non-openable mapped path.
        /// </summary>
        public bool MapPropertyValues { get; set; }

        public GenerateMSBuildEditorConfig()
        {
            ConfigFileContents = string.Empty;
            MetadataItems = Array.Empty<ITaskItem>();
            PropertyItems = Array.Empty<ITaskItem>();
            FileName = new TaskItem();
            PathMap = string.Empty;
            MapSectionHeaderPaths = false;
            MapPropertyValues = false;
        }

        public override bool Execute()
        {
            StringBuilder builder = new StringBuilder();

            // Only parse the path map if some part of the config opts in to mapping.
            var pathMap = (MapSectionHeaderPaths || MapPropertyValues) ? ParsePathMap(PathMap) : s_emptyPathMap;

            // we always generate global configs
            builder.AppendLine("is_global = true");

            // collect the properties into a global section
            foreach (var prop in PropertyItems)
            {
                // Path-valued properties (e.g. ProjectDir) are absolute and would otherwise make the
                // config location-dependent. NormalizePathPrefix is prefix-anchored, so only a value
                // that starts with a mapped root is rewritten; other values are left as-is.
                var value = prop.GetMetadata("Value");
                if (MapPropertyValues)
                {
                    value = NormalizePathPrefix(value, pathMap);
                }

                builder.Append("build_property.")
                       .Append(prop.ItemSpec)
                       .Append(" = ")
                       .AppendLine(value);
            }

            // group the metadata items by their full path, optionally rewriting each path through
            // the compiler's path map so the section headers match the paths the compiler computes.
            var groupedItems = MetadataItems.GroupBy(i =>
            {
                var fullPath = i.GetMetadata("FullPath");
                if (MapSectionHeaderPaths)
                {
                    fullPath = NormalizePathPrefix(fullPath, pathMap);
                }

                return NormalizeWithForwardSlash(fullPath);
            });

            foreach (var group in groupedItems)
            {
                // write the section for this item
                builder.AppendLine()
                       .Append('[');
                EncodeString(builder, group.Key);
                builder.AppendLine("]");

                foreach (var item in group)
                {
                    string itemType = item.GetMetadata("ItemType");
                    string metadataName = item.GetMetadata("MetadataName");
                    if (!string.IsNullOrWhiteSpace(itemType) && !string.IsNullOrWhiteSpace(metadataName))
                    {
                        builder.Append("build_metadata.")
                               .Append(itemType)
                               .Append('.')
                               .Append(metadataName)
                               .Append(" = ")
                               .AppendLine(item.GetMetadata(metadataName));
                    }
                }
            }

            ConfigFileContents = builder.ToString();
            return string.IsNullOrEmpty(FileName.ItemSpec) ? true : WriteMSBuildEditorConfig();
        }

        internal bool WriteMSBuildEditorConfig()
        {
            try
            {
                var targetFileName = FileName.ItemSpec;
                if (File.Exists(targetFileName))
                {
                    string existingContents = File.ReadAllText(targetFileName);
                    if (existingContents.Equals(ConfigFileContents))
                    {
                        return true;
                    }
                }
                var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
                File.WriteAllText(targetFileName, ConfigFileContents, encoding);
                return true;
            }
            catch (IOException ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }
        }

        /// <remarks>
        /// Filenames with special characters like '#' and'{' get written
        /// into the section names in the resulting .editorconfig file. Later,
        /// when the file is parsed in configuration options these special
        /// characters are interpretted as invalid values and ignored by the
        /// processor. We encode the special characters in these strings
        /// before writing them here.
        /// </remarks>

        private static void EncodeString(StringBuilder builder, string value)
        {
            foreach (var c in value)
            {
                if (c is '*' or '?' or '{' or ',' or ';' or '}' or '[' or ']' or '#' or '!')
                {
                    builder.Append('\\');
                }
                builder.Append(c);
            }
        }

        /// <remarks>
        /// Equivalent to Roslyn.Utilities.PathUtilities.NormalizeWithForwardSlash
        /// Both methods should be kept in sync.
        /// </remarks>
        private static string NormalizeWithForwardSlash(string p)
            => PlatformInformation.IsUnix ? p : p.Replace('\\', '/');

        private static readonly List<KeyValuePair<string, string>> s_emptyPathMap = new List<KeyValuePair<string, string>>();

        /// <remarks>
        /// Parses the <see cref="PathMap"/> string into a list of prefix mappings, ordered the
        /// same way the compiler orders them (longest key first, so the most specific prefix
        /// wins). Kept in sync with Microsoft.CodeAnalysis.CommandLineParser.ParsePathMap and
        /// SortPathMap. Malformed entries are ignored here; the compiler reports diagnostics for
        /// the same <c>/pathmap</c> value.
        /// </remarks>
        private static List<KeyValuePair<string, string>> ParsePathMap(string pathMap)
        {
            var result = new List<KeyValuePair<string, string>>();
            if (string.IsNullOrEmpty(pathMap))
            {
                return result;
            }

            foreach (var kEqualsV in SplitWithDoubledSeparatorEscaping(pathMap, ','))
            {
                if (kEqualsV.Length == 0)
                {
                    continue;
                }

                var kv = SplitWithDoubledSeparatorEscaping(kEqualsV, '=');
                if (kv.Length != 2)
                {
                    continue;
                }

                var from = kv[0];
                var to = kv[1];
                if (from.Length == 0 || to.Length == 0)
                {
                    continue;
                }

                result.Add(new KeyValuePair<string, string>(EnsureTrailingSeparator(from), EnsureTrailingSeparator(to)));
            }

            result.Sort((x, y) => -x.Key.Length.CompareTo(y.Key.Length));
            return result;
        }

        /// <remarks>
        /// Kept in sync with Microsoft.CodeAnalysis.CommandLineParser.SplitWithDoubledSeparatorEscaping.
        /// </remarks>
        private static string[] SplitWithDoubledSeparatorEscaping(string str, char separator)
        {
            if (str.Length == 0)
            {
                return Array.Empty<string>();
            }

            var result = new List<string>();
            var part = new StringBuilder();

            int i = 0;
            while (i < str.Length)
            {
                char c = str[i++];
                if (c == separator)
                {
                    if (i < str.Length && str[i] == separator)
                    {
                        i++;
                    }
                    else
                    {
                        result.Add(part.ToString());
                        part.Clear();
                        continue;
                    }
                }

                part.Append(c);
            }

            result.Add(part.ToString());
            return result.ToArray();
        }

        /// <remarks>
        /// Kept in sync with Roslyn.Utilities.PathUtilities.EnsureTrailingSeparator.
        /// </remarks>
        private static string EnsureTrailingSeparator(string s)
        {
            if (s.Length == 0 || s[s.Length - 1] == '/' || s[s.Length - 1] == '\\')
            {
                return s;
            }

            // Use the existing slashes in the path, if they're consistent
            bool hasSlash = s.IndexOf('/') >= 0;
            bool hasBackslash = s.IndexOf('\\') >= 0;
            if (hasSlash && !hasBackslash)
            {
                return s + '/';
            }
            else if (!hasSlash && hasBackslash)
            {
                return s + '\\';
            }
            else
            {
                // If there are no slashes or they are inconsistent, use the current platform's slash.
                return s + Path.DirectorySeparatorChar;
            }
        }

        /// <remarks>
        /// Kept in sync with Roslyn.Utilities.PathUtilities.NormalizePathPrefix.
        /// </remarks>
        private static string NormalizePathPrefix(string filePath, List<KeyValuePair<string, string>> pathMap)
        {
            if (pathMap.Count == 0)
            {
                return filePath;
            }

            // find the first key in the path map that matches a prefix of the path.
            // Note that we expect the client to use consistent capitalization; we use ordinal (case-sensitive) comparisons.
            foreach (var kv in pathMap)
            {
                var oldPrefix = kv.Key;
                if (!(oldPrefix?.Length > 0)) continue;

                // oldPrefix always ends with a path separator, so there's no need to check if it was a partial match
                // e.g. for the map /goo=/bar and filename /goooo
                if (filePath.StartsWith(oldPrefix, StringComparison.Ordinal))
                {
                    var replacementPrefix = kv.Value;

                    // Replace that prefix.
                    var replacement = replacementPrefix + filePath.Substring(oldPrefix.Length);

                    // Normalize the path separators if used uniformly in the replacement
                    bool hasSlash = replacementPrefix.IndexOf('/') >= 0;
                    bool hasBackslash = replacementPrefix.IndexOf('\\') >= 0;
                    return
                        (hasSlash && !hasBackslash) ? replacement.Replace('\\', '/') :
                        (hasBackslash && !hasSlash) ? replacement.Replace('/', '\\') :
                        replacement;
                }
            }

            return filePath;
        }
    }
}

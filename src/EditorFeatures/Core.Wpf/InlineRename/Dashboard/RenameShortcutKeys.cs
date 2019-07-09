// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal class RenameShortcutKey
    {
        public static string RenameOverloads { get; }
        public static string SearchInStrings { get; }
        public static string SearchInComments { get; }
        public static string PreviewChanges { get; }
        public static string Apply { get; }
        public static string RenameFile { get; }

        static RenameShortcutKey()
        {
            RenameOverloads = ExtractAccessKey(EditorFeaturesResources.Include_overload_s, "O");
            SearchInStrings = ExtractAccessKey(EditorFeaturesResources.Include_strings, "S");
            SearchInComments = ExtractAccessKey(EditorFeaturesResources.Include_comments, "C");
            PreviewChanges = ExtractAccessKey(EditorFeaturesResources.Preview_changes1, "P");
            Apply = ExtractAccessKey(EditorFeaturesResources.Apply1, "A");
            RenameFile = ExtractAccessKey(EditorFeaturesResources.Rename_file, "F");
        }

        /// <summary>
        /// Given a localized label, searches for _ and extracts the accelerator key. If none found,
        /// returns defaultValue.
        /// </summary>
        private static string ExtractAccessKey(string localizedLabel, string defaultValue)
        {
            var underscoreIndex = localizedLabel.IndexOf('_');

            if (underscoreIndex >= 0 && underscoreIndex < localizedLabel.Length - 1)
            {
                return new string(new char[] { char.ToUpperInvariant(localizedLabel[underscoreIndex + 1]) });
            }

            Debug.Fail("Could not locate accelerator for " + localizedLabel + " for the rename dashboard");
            return defaultValue;
        }
    }
}

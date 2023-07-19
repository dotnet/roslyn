// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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

        public GenerateMSBuildEditorConfig()
        {
            ConfigFileContents = string.Empty;
            MetadataItems = Array.Empty<ITaskItem>();
            PropertyItems = Array.Empty<ITaskItem>();
            FileName = new TaskItem();
        }

        public override bool Execute()
        {
            StringBuilder builder = new StringBuilder();

            // we always generate global configs
            builder.AppendLine("is_global = true");

            // collect the properties into a global section
            foreach (var prop in PropertyItems)
            {
                builder.Append("build_property.")
                       .Append(prop.ItemSpec)
                       .Append(" = ")
                       .AppendLine(prop.GetMetadata("Value"));
            }

            // group the metadata items by their full path
            var groupedItems = MetadataItems.GroupBy(i => NormalizeWithForwardSlash(i.GetMetadata("FullPath")));

            foreach (var group in groupedItems)
            {
                // write the section for this item
                builder.AppendLine()
                       .Append("[");
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
                               .Append(".")
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
                    builder.Append("\\");
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
    }
}

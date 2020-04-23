// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    public class GenerateMSBuildAnalyzerConfig : Task
    {
        [Output]
        public string ConfigFileContents { get; set; }

        [Required]
        public ITaskItem[] MetadataItems { get; set; }

        [Required]
        public ITaskItem[] PropertyItems { get; set; }

        public GenerateMSBuildAnalyzerConfig()
        {
            ConfigFileContents = string.Empty;
            MetadataItems = new ITaskItem[0];
            PropertyItems = new ITaskItem[0];
        }

        public override bool Execute()
        {
            StringBuilder builder = new StringBuilder();

            // we always generate global configs
            builder.AppendLine("is_global = true");

            // collect the properties into a global section
            foreach (var prop in PropertyItems)
            {
                builder.Append("msbuild_property.")
                       .Append(prop.ItemSpec)
                       .Append(" = ")
                       .AppendLine(prop.GetMetadata("Value")); //TODO: do we need to encode/escape this?
            }

            // group the metadata items by their itemspec
            var groupedItems = MetadataItems.GroupBy(i => i.ItemSpec);

            foreach (var group in groupedItems)
            {
                // write the section for this item
                builder.AppendLine()
                       .Append("[")
                       .Append(group.Key)
                       .AppendLine("]");

                foreach (var item in group)
                {
                    string metadataName = item.GetMetadata("MetadataName");
                    builder.Append("msbuild_item.")
                           .Append(item.GetMetadata("ItemType"))
                           .Append(".")
                           .Append(metadataName)
                           .Append(" = ")
                           .AppendLine(item.GetMetadata(metadataName));
                }
            }

            ConfigFileContents = builder.ToString();
            return true;
        }
    }
}

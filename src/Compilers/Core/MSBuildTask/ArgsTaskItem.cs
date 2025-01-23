// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    internal sealed class ArgsTaskItem : ITaskItem
    {
        // This list is taken from https://github.com/dotnet/msbuild/blob/291a8108761ed347562228f2f8f25477996a5a93/src/Shared/Modifiers.cs#L36-L70
        private static readonly string[] WellKnownItemSpecMetadataNames =
        [
            "FullPath",
            "RootDir",
            "Filename",
            "Extension",
            "RelativeDir",
            "Directory",
            "RecursiveDir",
            "Identity",
            "ModifiedTime",
            "CreatedTime",
            "AccessedTime",
            "DefiningProjectFullPath",
            "DefiningProjectDirectory",
            "DefiningProjectName",
            "DefiningProjectExtension",
        ];

        private readonly Dictionary<string, string> _metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public ArgsTaskItem(string itemSpec)
        {
            ItemSpec = itemSpec;
        }

        public string ItemSpec { get; set; }

        // Implementation notes that we should include the built-in metadata names as well as our custom ones.
        public ICollection MetadataNames => _metadata.Keys.Concat(WellKnownItemSpecMetadataNames).ToImmutableArray();

        // Implementation notes that we should include the built-in metadata names as well as our custom ones.
        public int MetadataCount => _metadata.Count + WellKnownItemSpecMetadataNames.Length;

        public IDictionary CloneCustomMetadata()
        {
            return _metadata.ToImmutableDictionary();
        }

        public void CopyMetadataTo(ITaskItem destinationItem)
        {
            // Implementation notes that we should not overwrite existing metadata on the destination.
            var destinationMetadataNames = destinationItem.MetadataNames.OfType<string>();
            var metadataNamesToCopy = _metadata.Keys.Except(destinationMetadataNames, StringComparer.OrdinalIgnoreCase).ToArray();

            foreach (var metadataName in metadataNamesToCopy)
            {
                var metadataValue = _metadata[metadataName];
                destinationItem.SetMetadata(metadataName, metadataValue);
            }
        }

        public string GetMetadata(string metadataName)
        {
            return _metadata[metadataName];
        }

        public void RemoveMetadata(string metadataName)
        {
            _metadata.Remove(metadataName);
        }

        public void SetMetadata(string metadataName, string metadataValue)
        {
            _metadata[metadataName] = metadataValue;
        }
    }
}

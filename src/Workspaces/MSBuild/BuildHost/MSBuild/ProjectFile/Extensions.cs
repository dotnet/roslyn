// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild;

internal static class Extensions
{
    extension(MSB.Execution.ProjectInstance executedProject)
    {
        public IEnumerable<MSB.Framework.ITaskItem> GetAdditionalFiles()
        => executedProject.GetItems(ItemNames.AdditionalFiles);

        public IEnumerable<MSB.Framework.ITaskItem> GetAnalyzers()
            => executedProject.GetItems(ItemNames.Analyzer);

        public IEnumerable<MSB.Framework.ITaskItem> GetDocuments()
            => executedProject.GetItems(ItemNames.Compile);

        public IEnumerable<MSB.Framework.ITaskItem> GetEditorConfigFiles()
            => executedProject.GetItems(ItemNames.EditorConfigFiles);

        public IEnumerable<MSB.Framework.ITaskItem> GetMetadataReferences()
            => executedProject.GetItems(ItemNames.ReferencePath);

        public IEnumerable<ProjectFileReference> GetProjectReferences()
            => executedProject
                .GetItems(ItemNames.ProjectReference)
                .Select(CreateProjectFileReference);

        public ImmutableArray<PackageReference> GetPackageReferences()
        {
            var packageReferenceItems = executedProject.GetItems(ItemNames.PackageReference);
            using var _ = PooledHashSet<PackageReference>.GetInstance(out var references);

            foreach (var item in packageReferenceItems)
            {
                var name = item.EvaluatedInclude;
                var versionRangeValue = item.GetMetadataValue(MetadataNames.Version);
                var packageReference = new PackageReference(name, versionRangeValue);
                references.Add(packageReference);
            }

            return [.. references];
        }

        public string? ReadPropertyString(string propertyName)
            => executedProject.GetProperty(propertyName)?.EvaluatedValue;

        public bool ReadPropertyBool(string propertyName)
            => Conversions.ToBool(executedProject.ReadPropertyString(propertyName));

        public int ReadPropertyInt(string propertyName)
            => Conversions.ToInt(executedProject.ReadPropertyString(propertyName));

        public ulong ReadPropertyULong(string propertyName)
            => Conversions.ToULong(executedProject.ReadPropertyString(propertyName));

        public TEnum? ReadPropertyEnum<TEnum>(string propertyName, bool ignoreCase)
            where TEnum : struct
            => Conversions.ToEnum<TEnum>(executedProject.ReadPropertyString(propertyName), ignoreCase);

        public string ReadItemsAsString(string itemType)
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;

            foreach (var item in executedProject.GetItems(itemType))
            {
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(item.EvaluatedInclude);
            }

            return pooledBuilder.ToStringAndFree();
        }

        public IEnumerable<MSB.Framework.ITaskItem> GetTaskItems(string itemType)
            => executedProject.GetItems(itemType);
    }

    /// <summary>
    /// Create a <see cref="ProjectFileReference"/> from a ProjectReference node in the MSBuild file.
    /// </summary>
    private static ProjectFileReference CreateProjectFileReference(MSB.Execution.ProjectItemInstance reference)
        => new(reference.EvaluatedInclude, reference.GetAliases(), reference.ReferenceOutputAssemblyIsTrue());

    extension(MSB.Framework.ITaskItem item)
    {
        public ImmutableArray<string> GetAliases()
        {
            var aliasesText = item.GetMetadata(MetadataNames.Aliases);

            return !string.IsNullOrWhiteSpace(aliasesText)
                ? [.. aliasesText.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim())]
                : [];
        }

        public bool ReferenceOutputAssemblyIsTrue()
        {
            var referenceOutputAssemblyText = item.GetMetadata(MetadataNames.ReferenceOutputAssembly);

            return string.IsNullOrWhiteSpace(referenceOutputAssemblyText) ||
                !string.Equals(referenceOutputAssemblyText, bool.FalseString, StringComparison.OrdinalIgnoreCase);
        }
    }
}

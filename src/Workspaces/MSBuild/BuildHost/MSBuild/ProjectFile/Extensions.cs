// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal static class Extensions
    {
        public static IEnumerable<MSB.Framework.ITaskItem> GetAdditionalFiles(this MSB.Execution.ProjectInstance executedProject)
            => executedProject.GetItems(ItemNames.AdditionalFiles);

        public static IEnumerable<MSB.Framework.ITaskItem> GetAnalyzers(this MSB.Execution.ProjectInstance executedProject)
            => executedProject.GetItems(ItemNames.Analyzer);

        public static IEnumerable<MSB.Framework.ITaskItem> GetDocuments(this MSB.Execution.ProjectInstance executedProject)
            => executedProject.GetItems(ItemNames.Compile);

        public static IEnumerable<MSB.Framework.ITaskItem> GetEditorConfigFiles(this MSB.Execution.ProjectInstance executedProject)
            => executedProject.GetItems(ItemNames.EditorConfigFiles);

        public static IEnumerable<MSB.Framework.ITaskItem> GetMetadataReferences(this MSB.Execution.ProjectInstance executedProject)
            => executedProject.GetItems(ItemNames.ReferencePath);

        public static IEnumerable<ProjectFileReference> GetProjectReferences(this MSB.Execution.ProjectInstance executedProject)
            => executedProject
                .GetItems(ItemNames.ProjectReference)
                .Where(i => i.ReferenceOutputAssemblyIsTrue())
                .Select(CreateProjectFileReference);

        public static ImmutableArray<PackageReference> GetPackageReferences(this MSB.Execution.ProjectInstance executedProject)
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

            return references.ToImmutableArray();
        }

        /// <summary>
        /// Create a <see cref="ProjectFileReference"/> from a ProjectReference node in the MSBuild file.
        /// </summary>
        private static ProjectFileReference CreateProjectFileReference(MSB.Execution.ProjectItemInstance reference)
            => new(reference.EvaluatedInclude, reference.GetAliases());

        public static ImmutableArray<string> GetAliases(this MSB.Framework.ITaskItem item)
        {
            var aliasesText = item.GetMetadata(MetadataNames.Aliases);

            return !RoslynString.IsNullOrWhiteSpace(aliasesText)
                ? ImmutableArray.CreateRange(aliasesText.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()))
                : [];
        }

        public static bool ReferenceOutputAssemblyIsTrue(this MSB.Framework.ITaskItem item)
        {
            var referenceOutputAssemblyText = item.GetMetadata(MetadataNames.ReferenceOutputAssembly);

            return RoslynString.IsNullOrWhiteSpace(referenceOutputAssemblyText) ||
                !string.Equals(referenceOutputAssemblyText, bool.FalseString, StringComparison.OrdinalIgnoreCase);
        }

        public static string? ReadPropertyString(this MSB.Execution.ProjectInstance executedProject, string propertyName)
            => executedProject.GetProperty(propertyName)?.EvaluatedValue;

        public static bool ReadPropertyBool(this MSB.Execution.ProjectInstance executedProject, string propertyName)
            => Conversions.ToBool(executedProject.ReadPropertyString(propertyName));

        public static int ReadPropertyInt(this MSB.Execution.ProjectInstance executedProject, string propertyName)
            => Conversions.ToInt(executedProject.ReadPropertyString(propertyName));

        public static ulong ReadPropertyULong(this MSB.Execution.ProjectInstance executedProject, string propertyName)
            => Conversions.ToULong(executedProject.ReadPropertyString(propertyName));

        public static TEnum? ReadPropertyEnum<TEnum>(this MSB.Execution.ProjectInstance executedProject, string propertyName, bool ignoreCase)
            where TEnum : struct
            => Conversions.ToEnum<TEnum>(executedProject.ReadPropertyString(propertyName), ignoreCase);

        public static string ReadItemsAsString(this MSB.Execution.ProjectInstance executedProject, string itemType)
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

        public static IEnumerable<MSB.Framework.ITaskItem> GetTaskItems(this MSB.Execution.ProjectInstance executedProject, string itemType)
            => executedProject.GetItems(itemType);
    }
}

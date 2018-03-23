// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
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

        public static IEnumerable<MSB.Framework.ITaskItem> GetMetadataReferences(this MSB.Execution.ProjectInstance executedProject)
            => executedProject.GetItems(ItemNames.ReferencePath);

        public static IEnumerable<ProjectFileReference> GetProjectReferences(this MSB.Execution.ProjectInstance executedProject)
            => executedProject
                .GetItems(ItemNames.ProjectReference)
                .Where(i => !i.IsReferenceOutputAssembly())
                .Select(CreateProjectFileReference);

        /// <summary>
        /// Create a <see cref="ProjectFileReference"/> from a ProjectReference node in the MSBuild file.
        /// </summary>
        private static ProjectFileReference CreateProjectFileReference(MSB.Execution.ProjectItemInstance reference)
            => new ProjectFileReference(reference.EvaluatedInclude, reference.GetAliases());

        public static bool IsReferenceOutputAssembly(this MSB.Framework.ITaskItem item)
            => string.Equals(item.GetMetadata(MetadataNames.ReferenceOutputAssembly), bool.TrueString, StringComparison.OrdinalIgnoreCase);

        public static ImmutableArray<string> GetAliases(this MSB.Framework.ITaskItem item)
        {
            var aliasesText = item.GetMetadata(MetadataNames.Aliases);

            return !string.IsNullOrWhiteSpace(aliasesText)
                ? ImmutableArray.CreateRange(aliasesText.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
                : ImmutableArray<string>.Empty;
        }

        public static string ReadPropertyString(this MSB.Execution.ProjectInstance executedProject, string propertyName)
            => executedProject.GetProperty(propertyName)?.EvaluatedValue;

        public static bool ReadPropertyBool(this MSB.Execution.ProjectInstance executedProject, string propertyName)
            => ConvertToBool(executedProject.ReadPropertyString(propertyName));

        private static bool ConvertToBool(string value)
            => value != null
                && (string.Equals(bool.TrueString, value, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals("On", value, StringComparison.OrdinalIgnoreCase));

        public static int ReadPropertyInt(this MSB.Execution.ProjectInstance executedProject, string propertyName)
            => ConvertToInt(executedProject.ReadPropertyString(propertyName));

        private static int ConvertToInt(string value)
        {
            if (value == null)
            {
                return 0;
            }
            else
            {
                int.TryParse(value, out var result);
                return result;
            }
        }

        public static ulong ReadPropertyULong(this MSB.Execution.ProjectInstance executedProject, string propertyName)
            => ConvertToULong(executedProject.ReadPropertyString(propertyName));

        private static ulong ConvertToULong(string value)
        {
            if (value == null)
            {
                return 0;
            }
            else
            {
                ulong.TryParse(value, out var result);
                return result;
            }
        }

        public static TEnum? ReadPropertyEnum<TEnum>(this MSB.Execution.ProjectInstance executedProject, string propertyName)
            where TEnum : struct
            => ConvertToEnum<TEnum>(executedProject.ReadPropertyString(propertyName));

        private static TEnum? ConvertToEnum<TEnum>(string value)
            where TEnum : struct
        {
            if (value == null)
            {
                return null;
            }
            else
            {
                if (Enum.TryParse<TEnum>(value, out var result))
                {
                    return result;
                }
                else
                {
                    return null;
                }
            }
        }

        public static string ReadItemsAsString(this MSB.Execution.ProjectInstance executedProject, string itemType)
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;

            foreach (var item in executedProject.GetItems(itemType))
            {
                if (builder.Length > 0)
                {
                    builder.Append(" ");
                }

                builder.Append(item.EvaluatedInclude);
            }

            return pooledBuilder.ToStringAndFree();
        }

        public static IEnumerable<MSB.Framework.ITaskItem> GetTaskItems(this MSB.Execution.ProjectInstance executedProject, string itemType)
            => executedProject.GetItems(itemType);
    }
}

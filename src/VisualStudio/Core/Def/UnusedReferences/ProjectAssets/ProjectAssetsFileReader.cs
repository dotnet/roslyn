// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.UnusedReferences.ProjectAssets
{
    internal static partial class ProjectAssetsFileReader
    {
        /// <summary>
        /// Enhances references with the assemblies they bring into the compilation and their dependency hierarchy.
        /// </summary>
        public static async Task<ImmutableArray<ReferenceInfo>> ReadReferencesAsync(
            ImmutableArray<ReferenceInfo> projectReferences,
            string projectAssetsFilePath)
        {
            var doesProjectAssetsFileExist = IOUtilities.PerformIO(() => File.Exists(projectAssetsFilePath));
            if (!doesProjectAssetsFileExist)
            {
                return ImmutableArray<ReferenceInfo>.Empty;
            }

            var projectAssetsFileContents = await IOUtilities.PerformIOAsync(async () =>
            {
                using var fileStream = File.OpenRead(projectAssetsFilePath);
                using var reader = new StreamReader(fileStream);
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);

            if (projectAssetsFileContents is null)
            {
                return ImmutableArray<ReferenceInfo>.Empty;
            }

            try
            {
                var projectAssets = JsonConvert.DeserializeObject<ProjectAssetsFile>(projectAssetsFileContents);
                return ProjectAssetsReader.AddDependencyHierarchies(projectReferences, projectAssets);
            }
            catch
            {
                return ImmutableArray<ReferenceInfo>.Empty;
            }
        }
    }
}

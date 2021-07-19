// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.CodeAnalysis.UnusedReferences.ProjectAssets;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences.ProjectAssets
{
    [ExportWorkspaceService(typeof(IProjectAssetsReaderService)), Shared]
    internal sealed class ProjectAssetsReaderService : IProjectAssetsReaderService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ProjectAssetsReaderService()
        {
        }

        /// <summary>
        /// Enhances references with the assemblies they bring into the compilation and their dependency hierarchy.
        /// </summary>
        public ImmutableArray<ReferenceInfo> ReadReferences(
            ImmutableArray<ReferenceInfo> projectReferences,
            string projectAssetsFilePath)
        {
            if (!File.Exists(projectAssetsFilePath))
            {
                return ImmutableArray<ReferenceInfo>.Empty;
            }

            var projectAssetsFileContents = File.ReadAllText(projectAssetsFilePath);
            ProjectAssetsFile projectAssets;

            try
            {
                projectAssets = JsonConvert.DeserializeObject<ProjectAssetsFile>(projectAssetsFileContents);
            }
            catch
            {
                return ImmutableArray<ReferenceInfo>.Empty;
            }

            return ProjectAssetsReader.EnhanceReferences(projectReferences, projectAssets);
        }
    }
}

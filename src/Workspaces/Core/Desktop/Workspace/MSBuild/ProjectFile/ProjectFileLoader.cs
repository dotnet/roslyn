﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal abstract class ProjectFileLoader : IProjectFileLoader
    {
        public abstract string Language { get; }

        protected abstract ProjectFile CreateProjectFile(LoadedProjectInfo info);

        public async Task<IProjectFile> LoadProjectFileAsync(string path, IDictionary<string, string> globalProperties, CancellationToken cancellationToken)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            // load project file async
            var loadedProjectInfo = await LoadProjectAsync(path, globalProperties, cancellationToken).ConfigureAwait(false);

            return this.CreateProjectFile(loadedProjectInfo);
        }

        private static readonly XmlReaderSettings s_xmlSettings = new XmlReaderSettings()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };

        public struct LoadedProjectInfo
        {
            public readonly MSB.Evaluation.Project Project;
            public readonly string ErrorMessage;

            public LoadedProjectInfo(MSB.Evaluation.Project project, string errorMessage)
            {
                this.Project = project;
                this.ErrorMessage = errorMessage;
            }
        }

        private static async Task<LoadedProjectInfo> LoadProjectAsync(string path, IDictionary<string, string> globalProperties, CancellationToken cancellationToken)
        {
            var properties = new Dictionary<string, string>(globalProperties ?? ImmutableDictionary<string, string>.Empty);
            properties["DesignTimeBuild"] = "true"; // this will tell msbuild to not build the dependent projects
            properties["BuildingInsideVisualStudio"] = "true"; // this will force CoreCompile task to execute even if all inputs and outputs are up to date

            try
            {
                var xmlReader = XmlReader.Create(await ReadFileAsync(path, cancellationToken).ConfigureAwait(false), s_xmlSettings);
                var collection = new MSB.Evaluation.ProjectCollection();
                var xml = MSB.Construction.ProjectRootElement.Create(xmlReader, collection);

                // When constructing a project from an XmlReader, MSBuild cannot determine the project file path.  Setting the
                // path explicitly is necessary so that the reserved properties like $(MSBuildProjectDirectory) will work.
                xml.FullPath = path;

                return new LoadedProjectInfo(
                    new MSB.Evaluation.Project(xml, properties, toolsVersion: null, projectCollection: collection),
                    errorMessage: null);
            }
            catch (Exception e)
            {
                return new LoadedProjectInfo(project: null, errorMessage: e.Message);
            }
        }

        public static async Task<string> GetOutputFilePathAsync(string path, IDictionary<string, string> globalProperties, CancellationToken cancellationToken)
        {
            var info = await LoadProjectAsync(path, globalProperties, cancellationToken).ConfigureAwait(false);
            return info.Project?.GetPropertyValue("TargetPath") ?? string.Empty;
        }

        private static async Task<MemoryStream> ReadFileAsync(string path, CancellationToken cancellationToken)
        {
            MemoryStream memoryStream = new MemoryStream();
            var buffer = new byte[1024];
            using (var stream = FileUtilities.OpenAsyncRead(path))
            {
                int bytesRead = 0;
                do
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                    memoryStream.Write(buffer, 0, bytesRead);
                }
                while (bytesRead > 0);
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

        public static IProjectFileLoader GetLoaderForProjectTypeGuid(Workspace workspace, Guid guid)
        {
            return workspace.Services.FindLanguageServices<IProjectFileLoader>(
                d => d.GetEnumerableMetadata<string>("ProjectTypeGuid").Any(g => guid == new Guid(g)))
                .FirstOrDefault();
        }

        public static IProjectFileLoader GetLoaderForProjectFileExtension(Workspace workspace, string extension)
        {
            return workspace.Services.FindLanguageServices<IProjectFileLoader>(
                d => d.GetEnumerableMetadata<string>("ProjectFileExtension").Any(e => string.Equals(e, extension, StringComparison.OrdinalIgnoreCase)))
                .FirstOrDefault();
        }
    }
}

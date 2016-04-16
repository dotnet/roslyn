// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Versions;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Versions
{
    /// <summary>
    /// this service tracks semantic version changes as solution changes and provide a way to get back to initial project/semantic version
    /// pairs at the solution load 
    /// </summary>
    [ExportWorkspaceService(typeof(ISemanticVersionTrackingService), ServiceLayer.Host), Shared]
    internal class SemanticVersionTrackingService : ISemanticVersionTrackingService
    {
        private const int SerializationFormat = 1;
        private const string SemanticVersion = nameof(SemanticVersion);
        private const string DependentSemanticVersion = nameof(DependentSemanticVersion);

        private static readonly ConditionalWeakTable<ProjectId, Versions> s_initialSemanticVersions = new ConditionalWeakTable<ProjectId, Versions>();
        private static readonly ConditionalWeakTable<ProjectId, Versions> s_initialDependentSemanticVersions = new ConditionalWeakTable<ProjectId, Versions>();

        public VersionStamp GetInitialProjectVersionFromSemanticVersion(Project project, VersionStamp semanticVersion)
        {
            Versions versions;
            if (!TryGetInitialVersions(s_initialSemanticVersions, project, SemanticVersion, out versions))
            {
                return VersionStamp.Default;
            }

            if (!VersionStamp.CanReusePersistedVersion(semanticVersion, versions.SemanticVersion))
            {
                return VersionStamp.Default;
            }

            return versions.ProjectVersion;
        }

        public VersionStamp GetInitialDependentProjectVersionFromDependentSemanticVersion(Project project, VersionStamp dependentSemanticVersion)
        {
            Versions versions;
            if (!TryGetInitialVersions(s_initialDependentSemanticVersions, project, DependentSemanticVersion, out versions))
            {
                return VersionStamp.Default;
            }

            if (!VersionStamp.CanReusePersistedVersion(dependentSemanticVersion, versions.SemanticVersion))
            {
                return VersionStamp.Default;
            }

            return versions.ProjectVersion;
        }

        private bool TryGetInitialVersions(ConditionalWeakTable<ProjectId, Versions> initialVersionMap, Project project, string keyName, out Versions versions)
        {
            // if we already loaded this, return it.
            if (initialVersionMap.TryGetValue(project.Id, out versions))
            {
                return true;
            }

            // otherwise, load it
            return TryLoadInitialVersions(initialVersionMap, project, keyName, out versions);
        }

        public void LoadInitialSemanticVersions(Solution solution)
        {
            foreach (var project in solution.Projects)
            {
                LoadInitialSemanticVersions(project);
            }
        }

        public void LoadInitialSemanticVersions(Project project)
        {
            Versions unused;
            if (!s_initialSemanticVersions.TryGetValue(project.Id, out unused))
            {
                PersistedVersionStampLogger.LogProject();

                if (TryLoadInitialVersions(s_initialSemanticVersions, project, SemanticVersion, out unused))
                {
                    PersistedVersionStampLogger.LogInitialSemanticVersion();
                }
            }

            if (!s_initialDependentSemanticVersions.TryGetValue(project.Id, out unused) &&
                TryLoadInitialVersions(s_initialDependentSemanticVersions, project, DependentSemanticVersion, out unused))
            {
                PersistedVersionStampLogger.LogInitialDependentSemanticVersion();
            }
        }

        private bool TryLoadInitialVersions(ConditionalWeakTable<ProjectId, Versions> initialVersionMap, Project project, string keyName, out Versions versions)
        {
            var result = TryReadFrom(project, keyName, out versions);
            if (result)
            {
                Versions save = versions;
                initialVersionMap.GetValue(project.Id, _ => save);
                return true;
            }

            initialVersionMap.GetValue(project.Id, _ => Versions.Default);
            return false;
        }

        private static bool TryReadFrom(Project project, string keyName, out Versions versions)
        {
            versions = default(Versions);

            var service = project.Solution.Workspace.Services.GetService<IPersistentStorageService>();
            if (service == null)
            {
                return false;
            }

            using (var storage = service.GetStorage(project.Solution))
            using (var stream = storage.ReadStreamAsync(keyName, CancellationToken.None).WaitAndGetResult(CancellationToken.None))
            {
                if (stream == null)
                {
                    return false;
                }

                try
                {
                    using (var reader = new ObjectReader(stream))
                    {
                        var formatVersion = reader.ReadInt32();
                        if (formatVersion != SerializationFormat)
                        {
                            return false;
                        }

                        var persistedProjectVersion = VersionStamp.ReadFrom(reader);
                        var persistedSemanticVersion = VersionStamp.ReadFrom(reader);

                        versions = new Versions(persistedProjectVersion, persistedSemanticVersion);
                        return true;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public async Task RecordSemanticVersionsAsync(Project project, CancellationToken cancellationToken)
        {
            var service = project.Solution.Workspace.Services.GetService<IPersistentStorageService>();
            if (service == null)
            {
                return;
            }

            using (var storage = service.GetStorage(project.Solution))
            {
                // first update semantic and dependent semantic version of this project
                await WriteToSemanticVersionAsync(storage, project, cancellationToken).ConfigureAwait(false);
                await WriteToDependentSemanticVersionAsync(storage, project, cancellationToken).ConfigureAwait(false);

                // next update dependent semantic version fo all projects that depend on this project.
                var projectIds = project.Solution.GetProjectDependencyGraph().GetProjectsThatTransitivelyDependOnThisProject(project.Id);
                foreach (var projectId in projectIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var currentProject = project.Solution.GetProject(projectId);
                    if (currentProject == null)
                    {
                        continue;
                    }

                    await WriteToDependentSemanticVersionAsync(storage, currentProject, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static async Task WriteToSemanticVersionAsync(IPersistentStorage storage, Project project, CancellationToken cancellationToken)
        {
            var projectVersion = await project.GetVersionAsync(cancellationToken).ConfigureAwait(false);
            var semanticVersion = await project.GetSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

            await WriteToVersionAsync(storage, SemanticVersion, projectVersion, semanticVersion, cancellationToken).ConfigureAwait(false);
        }

        private static async Task WriteToDependentSemanticVersionAsync(IPersistentStorage storage, Project project, CancellationToken cancellationToken)
        {
            var projectVersion = await project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);
            var semanticVersion = await project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

            await WriteToVersionAsync(storage, DependentSemanticVersion, projectVersion, semanticVersion, cancellationToken).ConfigureAwait(false);
        }

        private static async Task WriteToVersionAsync(
            IPersistentStorage storage, string keyName, VersionStamp projectVersion, VersionStamp semanticVersion, CancellationToken cancellationToken)
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream, cancellationToken: cancellationToken))
            {
                writer.WriteInt32(SerializationFormat);
                projectVersion.WriteTo(writer);
                semanticVersion.WriteTo(writer);

                stream.Position = 0;
                await storage.WriteStreamAsync(keyName, stream, cancellationToken).ConfigureAwait(false);
            }
        }

        private class Versions
        {
            public static readonly Versions Default = new Versions(VersionStamp.Default, VersionStamp.Default);

            public readonly VersionStamp ProjectVersion;
            public readonly VersionStamp SemanticVersion;

            public Versions(VersionStamp projectVersion, VersionStamp semanticVersion)
            {
                this.ProjectVersion = projectVersion;
                this.SemanticVersion = semanticVersion;
            }
        }
    }
}

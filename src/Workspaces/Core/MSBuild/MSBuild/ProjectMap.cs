﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// A map of projects that can be optionally used with <see cref="MSBuildProjectLoader.LoadProjectInfoAsync"/> when loading a
    /// project into a custom <see cref="Workspace"/>. To use, pass <see cref="Workspace.CurrentSolution"/> to <see cref="Create(Solution)"/>.
    /// </summary>
    public class ProjectMap
    {
        /// <summary>
        /// A map of project path to <see cref="ProjectId"/>s. Note that there can be multiple <see cref="ProjectId"/>s per project path
        /// if the project is multi-targeted -- one for each target framework.
        /// </summary>
        private readonly Dictionary<string, HashSet<ProjectId>> _projectPathToProjectIdsMap;

        /// <summary>
        /// A map of project path to <see cref="ProjectInfo"/>s. Note that there can be multiple <see cref="ProjectId"/>s per project path
        /// if the project is multi-targeted -- one for each target framework.
        /// </summary>
        private readonly Dictionary<string, ImmutableArray<ProjectInfo>> _projectPathToProjectInfosMap;

        /// <summary>
        /// A map of <see cref="ProjectId"/> to the output file of the project (if any).
        /// </summary>
        private readonly Dictionary<ProjectId, string> _projectIdToOutputFilePathMap;

        /// <summary>
        /// A map of <see cref="ProjectId"/> to the output ref file of the project (if any).
        /// </summary>
        private readonly Dictionary<ProjectId, string> _projectIdToOutputRefFilePathMap;

        private ProjectMap()
        {
            _projectPathToProjectIdsMap = new Dictionary<string, HashSet<ProjectId>>(PathUtilities.Comparer);
            _projectPathToProjectInfosMap = new Dictionary<string, ImmutableArray<ProjectInfo>>(PathUtilities.Comparer);
            _projectIdToOutputFilePathMap = new Dictionary<ProjectId, string>();
            _projectIdToOutputRefFilePathMap = new Dictionary<ProjectId, string>();
        }

        /// <summary>
        /// Create an empty <see cref="ProjectMap"/>.
        /// </summary>
        public static ProjectMap Create() => new ProjectMap();

        /// <summary>
        /// Create a <see cref="ProjectMap"/> populated with the given <see cref="Solution"/>.
        /// </summary>
        /// <param name="solution">The <see cref="Solution"/> to populate the new <see cref="ProjectMap"/> with.</param>
        /// <returns></returns>
        public static ProjectMap Create(Solution solution)
        {
            var projectMap = new ProjectMap();

            foreach (var project in solution.Projects)
            {
                projectMap.Add(project);
            }

            return projectMap;
        }

        /// <summary>
        /// Add a <see cref="Project"/> to this <see cref="ProjectMap"/>.
        /// </summary>
        /// <param name="project">The <see cref="Project"/> to add to this <see cref="ProjectMap"/>.</param>
        public void Add(Project project)
        {
            Add(project.Id, project.FilePath, project.OutputFilePath, project.OutputRefFilePath);
            AddProjectInfo(project.State.ProjectInfo);
        }

        private void Add(ProjectId projectId, string projectPath, string outputFilePath, string outputRefFilePath)
        {
            _projectPathToProjectIdsMap.MultiAdd(projectPath, projectId);

            if (!string.IsNullOrEmpty(outputFilePath))
            {
                _projectIdToOutputFilePathMap.Add(projectId, outputFilePath);
            }

            if (!string.IsNullOrEmpty(outputRefFilePath))
            {
                _projectIdToOutputRefFilePathMap.Add(projectId, outputRefFilePath);
            }
        }

        internal void AddProjectInfo(ProjectInfo projectInfo)
        {
            if (!_projectPathToProjectInfosMap.TryGetValue(projectInfo.FilePath, out var projectInfos))
            {
                projectInfos = ImmutableArray<ProjectInfo>.Empty;
            }

            if (projectInfos.Contains(pi => pi.Id == projectInfo.Id))
            {
                throw new ArgumentException("Project already added.");
            }

            projectInfos = projectInfos.Add(projectInfo);

            _projectPathToProjectInfosMap[projectInfo.FilePath] = projectInfos;
        }

        private ProjectId CreateProjectId(string projectPath, string outputFilePath, string outputRefFilePath)
        {
            var newProjectId = ProjectId.CreateNewId(debugName: projectPath);
            Add(newProjectId, projectPath, outputFilePath, outputRefFilePath);
            return newProjectId;
        }

        internal ProjectId GetOrCreateProjectId(string projectPath)
        {
            if (!_projectPathToProjectIdsMap.TryGetValue(projectPath, out var projectIds))
            {
                projectIds = new HashSet<ProjectId>();
                _projectPathToProjectIdsMap.Add(projectPath, projectIds);
            }

            return projectIds.Count == 1
                ? projectIds.Single()
                : CreateProjectId(projectPath, outputFilePath: null, outputRefFilePath: null);
        }

        internal ProjectId GetOrCreateProjectId(ProjectFileInfo projectFileInfo)
        {
            var projectPath = projectFileInfo.FilePath;
            var outputFilePath = projectFileInfo.OutputFilePath;
            var outputRefFilePath = projectFileInfo.OutputRefFilePath;

            if (TryGetIdsByProjectPath(projectPath, out var projectIds))
            {
                if (TryFindOutputFileRefPathInProjectIdSet(outputRefFilePath, projectIds, out var projectId) ||
                    TryFindOutputFilePathInProjectIdSet(outputFilePath, projectIds, out projectId))
                {
                    return projectId;
                }
            }

            return CreateProjectId(projectPath, outputFilePath, outputRefFilePath);
        }

        private bool TryFindOutputFileRefPathInProjectIdSet(string outputRefFilePath, HashSet<ProjectId> set, out ProjectId result)
            => TryFindPathInProjectIdSet(outputRefFilePath, GetOutputRefFilePathById, set, out result);

        private bool TryFindOutputFilePathInProjectIdSet(string outputFilePath, HashSet<ProjectId> set, out ProjectId result)
            => TryFindPathInProjectIdSet(outputFilePath, GetOutputFilePathById, set, out result);

        private bool TryFindPathInProjectIdSet(string path, Func<ProjectId, string> getPathById, HashSet<ProjectId> set, out ProjectId result)
        {
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var id in set)
                {
                    var p = getPathById(id);

                    if (PathUtilities.Comparer.Equals(p, path))
                    {
                        result = id;
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }

        internal string GetOutputRefFilePathById(ProjectId projectId)
            => TryGetOutputRefFilePathById(projectId, out var path)
                ? path
                : null;

        internal string GetOutputFilePathById(ProjectId projectId)
            => TryGetOutputFilePathById(projectId, out var path)
                ? path
                : null;

        internal bool TryGetIdsByProjectPath(string projectPath, out HashSet<ProjectId> ids)
            => _projectPathToProjectIdsMap.TryGetValue(projectPath, out ids);

        internal bool TryGetOutputFilePathById(ProjectId id, out string outputFilePath)
            => _projectIdToOutputFilePathMap.TryGetValue(id, out outputFilePath);

        internal bool TryGetOutputRefFilePathById(ProjectId id, out string outputRefFilePath)
            => _projectIdToOutputRefFilePathMap.TryGetValue(id, out outputRefFilePath);

        internal bool TryGetProjectInfosByProjectPath(string projectPath, out ImmutableArray<ProjectInfo> projectInfos)
            => _projectPathToProjectInfosMap.TryGetValue(projectPath, out projectInfos);
    }
}

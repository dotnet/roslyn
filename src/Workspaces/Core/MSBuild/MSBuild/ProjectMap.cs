// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    public class ProjectMap
    {
        private readonly Dictionary<string, HashSet<ProjectId>> _projectPathToProjectIdsMap;
        private readonly Dictionary<ProjectId, string> _projectIdToOutputFilePathMap;

        private ProjectMap()
        {
            _projectPathToProjectIdsMap = new Dictionary<string, HashSet<ProjectId>>(PathUtilities.Comparer);
            _projectIdToOutputFilePathMap = new Dictionary<ProjectId, string>();
        }

        public static ProjectMap Create() => new ProjectMap();

        public static ProjectMap Create(Solution solution)
        {
            var projectMap = new ProjectMap();

            foreach (var project in solution.Projects)
            {
                projectMap.Add(project);
            }

            return projectMap;
        }

        public void Add(Project project)
        {
            Add(project.Id, project.FilePath, project.OutputFilePath);
        }

        private void Add(ProjectId id, string projectPath, string outputFilePath)
        {
            if (!_projectPathToProjectIdsMap.TryGetValue(projectPath, out var projectPathIdSet))
            {
                projectPathIdSet = new HashSet<ProjectId>();
                _projectPathToProjectIdsMap.Add(projectPath, projectPathIdSet);
            }

            projectPathIdSet.Add(id);

            if (!string.IsNullOrEmpty(outputFilePath))
            {
                _projectIdToOutputFilePathMap.Add(id, outputFilePath);
            }
        }

        internal ProjectId GetOrCreateProjectId(string projectPath)
        {
            if (!_projectPathToProjectIdsMap.TryGetValue(projectPath, out var ids))
            {
                ids = new HashSet<ProjectId>();
                _projectPathToProjectIdsMap.Add(projectPath, ids);
            }

            if (ids.Count == 1)
            {
                return ids.Single();
            }
            else
            {
                var id = ProjectId.CreateNewId(debugName: projectPath);
                Add(id, projectPath, outputFilePath: null);
                return id;
            }
        }

        internal ProjectId GetOrCreateProjectId(ProjectFileInfo projectFileInfo)
        {
            ProjectId result = null;
            var projectPath = projectFileInfo.FilePath;
            var outputFilePath = projectFileInfo.OutputFilePath;

            if (TryGetIdsByProjectPath(projectPath, out var ids))
            {
                if (ids.Count == 1)
                {
                    var id = ids.Single();

                    if (string.IsNullOrWhiteSpace(outputFilePath) ||
                        (TryGetOutputFilePathById(id, out var path) && PathUtilities.Comparer.Equals(path, outputFilePath)))
                    {
                        result = id;
                    }
                }

                if (result == null && !string.IsNullOrEmpty(outputFilePath))
                {
                    foreach (var id in ids)
                    {
                        if (TryGetOutputFilePathById(id, out var path) && PathUtilities.Comparer.Equals(path, outputFilePath))
                        {
                            result = id;
                            break;
                        }
                    }
                }
            }

            if (result == null)
            {
                result = ProjectId.CreateNewId(debugName: projectPath);
                Add(result, projectPath, outputFilePath);
            }

            return result;
        }

        internal bool TryGetIdsByProjectPath(string projectPath, out HashSet<ProjectId> ids)
            => _projectPathToProjectIdsMap.TryGetValue(projectPath, out ids);

        internal bool TryGetOutputFilePathById(ProjectId id, out string outputFilePath)
            => _projectIdToOutputFilePathMap.TryGetValue(id, out outputFilePath);
    }
}

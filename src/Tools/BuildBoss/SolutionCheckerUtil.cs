// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace BuildBoss
{
    internal sealed class SolutionCheckerUtil : ICheckerUtil
    {
        private struct SolutionProjectData
        {
            internal ProjectEntry ProjectEntry;
            internal ProjectData ProjectData;

            internal SolutionProjectData(ProjectEntry entry, ProjectData data)
            {
                ProjectEntry = entry;
                ProjectData = data;
            }
        }

        internal string SolutionFilePath { get; }
        internal string SolutionPath { get; }
        internal bool IsPrimarySolution { get; }

        internal SolutionCheckerUtil(string solutionFilePath, bool isPrimarySolution)
        {
            SolutionFilePath = solutionFilePath;
            IsPrimarySolution = isPrimarySolution;
            SolutionPath = Path.GetDirectoryName(SolutionFilePath);
        }

        public bool Check(TextWriter textWriter)
        {
            var allGood = true;

            allGood &= CheckDuplicate(textWriter, out var map);
            allGood &= CheckProjects(textWriter, map);
            allGood &= CheckProjectSystemGuid(textWriter, map.Values);
            allGood &= CheckSameDirectory(textWriter, map);

            return allGood;
        }

        private bool CheckProjects(TextWriter textWriter, Dictionary<ProjectKey, SolutionProjectData> map)
        {
            var solutionMap = new Dictionary<ProjectKey, ProjectData>();
            foreach (var pair in map)
            {
                solutionMap.Add(pair.Key, pair.Value.ProjectData);
            }

            var allGood = true;
            var count = 0;
            foreach (var data in map.Values.OrderBy(x => x.ProjectEntry.Name))
            {
                var projectWriter = new StringWriter();
                var projectData = data.ProjectData;
                projectWriter.WriteLine($"Processing {projectData.Key.FileName}");
                var util = new ProjectCheckerUtil(projectData, solutionMap, IsPrimarySolution);
                if (!util.Check(projectWriter))
                {
                    allGood = false;
                    textWriter.WriteLine(projectWriter.ToString());
                }
                count++;
            }

            textWriter.WriteLine($"Processed {count} projects");
            return allGood;
        }

        private bool CheckDuplicate(TextWriter textWriter, out Dictionary<ProjectKey, SolutionProjectData> map)
        {
            map = new Dictionary<ProjectKey, SolutionProjectData>();
            var allGood = true;
            foreach (var projectEntry in SolutionUtil.ParseProjects(SolutionFilePath))
            {
                if (projectEntry.IsFolder)
                {
                    continue;
                }

                var projectFilePath = Path.Combine(SolutionPath, projectEntry.RelativeFilePath);
                var projectData = new ProjectData(projectFilePath);
                if (map.ContainsKey(projectData.Key))
                {
                    textWriter.WriteLine($"Duplicate project detected {projectData.FileName}");
                    allGood = false;
                }
                else
                {
                    map.Add(projectData.Key, new SolutionProjectData(projectEntry, projectData));
                }
            }

            return allGood;
        }

        /// <summary>
        /// Ensure solution files have the proper project system GUID.
        /// </summary>
        private bool CheckProjectSystemGuid(TextWriter textWriter, IEnumerable<SolutionProjectData> dataList)
        {
            Guid getExpectedGuid(ProjectData data)
            {
                var util = data.ProjectUtil;
                switch (ProjectEntryUtil.GetProjectFileType(data.FilePath))
                {
                    case ProjectFileType.CSharp: return ProjectEntryUtil.ManagedProjectSystemCSharp;
                    case ProjectFileType.Basic: return ProjectEntryUtil.ManagedProjectSystemVisualBasic;
                    case ProjectFileType.Shared: return ProjectEntryUtil.SharedProject;
                    default: throw new Exception($"Invalid file path {data.FilePath}");
                }
            }

            var allGood = true;
            foreach (var data in dataList.Where(x => x.ProjectEntry.ProjectType != ProjectFileType.Tool))
            {
                var guid = getExpectedGuid(data.ProjectData);
                if (guid != data.ProjectEntry.TypeGuid)
                {
                    var name = data.ProjectData.FileName;
                    textWriter.WriteLine($"Project {name} should have GUID {guid} but has {data.ProjectEntry.TypeGuid}");
                    allGood = false;
                }
            }

            return allGood;
        }

        /// <summary>
        /// The .NET SDK does not support multiple projects in the same directory. It creates race conditions 
        /// during restore.
        /// 
        /// https://github.com/dotnet/sdk/issues/28763
        /// </summary>
        private bool CheckSameDirectory(TextWriter textWriter, Dictionary<ProjectKey, SolutionProjectData> map)
        {
            var allGood = true;
            var set = new HashSet<string>();
            foreach (var projectKey in map.Keys.OrderBy(x => x.FileName))
            {
                if (Path.GetExtension(projectKey.FileName) == ".shproj")
                {
                    continue;
                }

                var directory = Path.GetDirectoryName(projectKey.FilePath);
                if (!set.Add(directory))
                {
                    textWriter.WriteLine($"Multiple projects in the same directory {directory}");
                    textWriter.WriteLine($"See this issue for context: https://github.com/dotnet/sdk/issues/28763");
                    allGood = false;
                }
            }

            return allGood;
        }
    }
}

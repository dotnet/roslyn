using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildBoss
{
    internal sealed class SolutionCheckerUtil : ICheckerUtil
    {
        private readonly string _solutionFilePath;

        internal SolutionCheckerUtil(string solutionFilePath)
        {
            _solutionFilePath = solutionFilePath;
        }

        public bool Check(TextWriter textWriter)
        {
            var solutionPath = Path.GetDirectoryName(_solutionFilePath);
            var projectDataList = SolutionUtil.ParseProjects(_solutionFilePath);
            var map = new Dictionary<ProjectKey, ProjectData>();
            var allGood = true;
            foreach (var projectEntry in projectDataList)
            {
                if (projectEntry.IsFolder)
                {
                    continue;
                }

                // TODO: temporary work around util a cross cutting change can be sync'd up.  
                if (Path.GetFileName(projectEntry.RelativeFilePath) == "CompilerPerfTest.vbproj")
                {
                    continue;
                }

                var projectFilePath = Path.Combine(solutionPath, projectEntry.RelativeFilePath);
                var projectData = new ProjectData(projectFilePath);
                if (map.ContainsKey(projectData.Key))
                {
                    textWriter.WriteLine($"Duplicate project detected {projectData.FileName}");
                    allGood = false;
                }
                else
                {
                    map.Add(projectData.Key, projectData);
                }
            }

            var count = 0;
            foreach (var projectData in map.Values.OrderBy(x => x.FileName))
            {
                var projectWriter = new StringWriter();
                projectWriter.WriteLine($"Processing {projectData.Key.FileName}");
                var util = new ProjectCheckerUtil(projectData, map);
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
    }
}

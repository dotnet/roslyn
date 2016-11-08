using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BuildBoss
{
    internal sealed class ProjectCheckerUtil
    {
        private readonly ProjectData _data;
        private readonly ProjectUtil _projectUtil;
        private readonly Dictionary<ProjectKey, ProjectData> _solutionMap;

        internal ProjectFileType ProjectType => _data.ProjectFileType;
        internal string ProjectFilePath => _data.FilePath;

        internal ProjectCheckerUtil(ProjectData data, Dictionary<ProjectKey, ProjectData> solutionMap)
        {
            _data = data;
            _projectUtil = data.ProjectUtil;
            _solutionMap = solutionMap;
        }

        internal bool CheckAll(TextWriter textWriter)
        {
            var allGood = true;
            if (ProjectType == ProjectFileType.CSharp || ProjectType == ProjectFileType.Basic)
            {
                allGood &= CheckForProperty(textWriter, "RestorePackages");
                allGood &= CheckForProperty(textWriter, "SolutionDir");
                allGood &= CheckForProperty(textWriter, "FileAlignment");
                allGood &= CheckForProperty(textWriter, "FileUpgradeFlags");
                allGood &= CheckForProperty(textWriter, "UpgradeBackupLocation");
                allGood &= CheckForProperty(textWriter, "OldToolsVersion");
                allGood &= CheckForProperty(textWriter, "SchemaVersion");
                allGood &= CheckForProperty(textWriter, "Configuration");
                allGood &= CheckForProperty(textWriter, "CheckForOverflowUnderflow");
                allGood &= CheckForProperty(textWriter, "RemoveIntegerChecks");
                allGood &= CheckForProperty(textWriter, "Deterministic");
                allGood &= CheckForProperty(textWriter, "HighEntropyVA");
                allGood &= CheckRoslynProjectType(textWriter);
                allGood &= CheckProjectReferences(textWriter);
            }

            allGood &= CheckTestDeploymentProjects(textWriter);

            return allGood;
        }

        private bool CheckForProperty(TextWriter textWriter, string propertyName)
        {
            foreach (var element in _projectUtil.GetAllPropertyGroupElements())
            {
                if (element.Name.LocalName == propertyName)
                {
                    textWriter.WriteLine($"\tDo not use {propertyName}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validate the content of RoslynProjectType is one of the supported values.
        /// </summary>
        private bool CheckRoslynProjectType(TextWriter textWriter)
        {
            RoslynProjectData data;
            if (!ParseRoslynProjectData(textWriter, out data))
            {
                return false;
            }

            var allGood = true;
            allGood &= IsVsixCorrectlySpecified(textWriter, data);
            allGood &= IsUnitTestCorrectlySpecified(textWriter, data);

            return allGood;
        }

        private bool ParseRoslynProjectData(TextWriter textWriter, out RoslynProjectData data)
        {
            try
            {
                data = _projectUtil.GetRoslynProjectData();
                return true;
            }
            catch (Exception ex)
            {
                data = default(RoslynProjectData);
                textWriter.WriteLine(ex.Message);
                return false;
            }
        }

        private bool IsVsixCorrectlySpecified(TextWriter textWriter, RoslynProjectData data)
        {
            var element = _projectUtil.FindSingleProperty("ProjectTypeGuids");
            if (element == null)
            {
                return true;
            }

            foreach (var rawValue in element.Value.Split(';'))
            {
                var value = rawValue.Trim();
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                var guid = Guid.Parse(value);
                if (guid == ProjectEntryUtil.VsixProjectType && data.EffectiveKind != RoslynProjectKind.Vsix)
                {
                    textWriter.WriteLine("Vsix projects must specify <RoslynProjectType>Vsix</RoslynProjectType>");
                    return false;
                }
            }

            return true;
        }

        private bool CheckProjectReferences(TextWriter textWriter)
        {
            var allGood = true;

            var declaredList = _projectUtil.GetDeclaredProjectReferences();
            allGood &= CheckProjectReferencesComplete(textWriter, declaredList);
            allGood &= CheckUnitTestReferenceRestriction(textWriter, declaredList);
            allGood &= CheckTransitiveReferences(textWriter, declaredList);

            return allGood;
        }

        /// <summary>
        /// It's important that every reference be included in the solution.  MSBuild does not necessarily
        /// apply all configuration entries to projects which are compiled via referenes but not included
        /// in the solution.
        /// </summary>
        private bool CheckProjectReferencesComplete(TextWriter textWriter, IEnumerable<ProjectKey> declaredReferences)
        {
            var allGood = true;
            foreach (var key in declaredReferences)
            {
                if (!_solutionMap.ContainsKey(key))
                {
                    textWriter.WriteLine($"Project reference {key.FileName} is not included in the solution");
                    allGood = false;
                }
            }
            return allGood;
        }

        /// <summary>
        /// Unit test projects should not reference each other.  In order for unit tests to be run / F5 they must be 
        /// modeled as deployment projects.  Having Unit Tests reference each other hurts that because it ends up 
        /// putting two copies of the unit test DLL into the UnitTest folder:
        ///
        ///     1. UnitTests\Current\TheUnitTest\TheUnitTest.dll
        ///     2. UnitTests\Current\TheOtherTests\
        ///             TheUnitTests.dll
        ///             TheOtherTests.dll
        ///
        /// This is problematic as all of our tools do directory based searches for unit test DLLs.  Hence they end up
        /// getting counted twice. 
        ///
        /// Consideration was given to fixing up all of the tools but it felt like fighting against the grain.  Pretty
        /// much every repo has this practice.
        /// </summary>
        private bool CheckUnitTestReferenceRestriction(TextWriter textWriter, IEnumerable<ProjectKey> declaredReferences)
        {
            var data = _projectUtil.TryGetRoslynProjectData();
            if (!data.HasValue || !data.Value.IsAnyUnitTest)
            {
                return true;
            }

            var allGood = true;
            foreach (var key in declaredReferences)
            {
                ProjectData projectData;
                if (!_solutionMap.TryGetValue(key, out projectData))
                {
                    continue;
                }

                var refData = projectData.ProjectUtil.TryGetRoslynProjectData();
                if (refData.HasValue && refData.Value.IsAnyUnitTest)
                {
                    textWriter.WriteLine($"Cannot reference {key.FileName} as it is another unit test project");
                    allGood = false;
                }
            }

            return allGood;
        }

        /// <summary>
        /// In order to ensure all dependencies are properly copied on deployment projects, the declared reference
        /// set much match the transitive dependency set.  When there is a difference it represents dependencies that
        /// MSBuild won't deploy on build.
        /// </summary>
        private bool CheckTransitiveReferences(TextWriter textWriter, IEnumerable<ProjectKey> declaredReferences)
        {
            var data = _projectUtil.TryGetRoslynProjectData();
            if (!data.HasValue || !data.Value.IsDeploymentProject)
            {
                return true;
            }

            var list = GetProjectReferencesTransitive(declaredReferences);
            var set = new HashSet<ProjectKey>(declaredReferences);
            var allGood = true;
            foreach (var key in list)
            {
                if (!set.Contains(key))
                {
                    textWriter.WriteLine($"Missing project reference {key.FileName}");
                    allGood = false;
                }
            }

            return allGood;
        }

        private List<ProjectKey> GetProjectReferencesTransitive(IEnumerable<ProjectKey> declaredReferences)
        {
            var list = new List<ProjectKey>();
            var toVisit = new Queue<ProjectKey>(declaredReferences);
            var seen = new HashSet<ProjectKey>();

            while (toVisit.Count > 0)
            {
                var current = toVisit.Dequeue();
                if (!seen.Add(current))
                {
                    continue;
                }

                ProjectData data;
                if (!_solutionMap.TryGetValue(current, out data))
                {
                    continue;
                }

                list.Add(current);
                foreach (var dep in data.ProjectUtil.GetDeclaredProjectReferences())
                {
                    toVisit.Enqueue(dep);
                }
            }

            list.Sort((x, y) => x.FileName.CompareTo(y.FileName));
            return list;
        }

        private bool IsUnitTestCorrectlySpecified(TextWriter textWriter, RoslynProjectData data)
        {
            if (ProjectType != ProjectFileType.CSharp && ProjectType != ProjectFileType.Basic)
            {
                return true;
            }

            if (data.EffectiveKind == RoslynProjectKind.Depedency)
            {
                return true;
            }

            var element = _projectUtil.FindSingleProperty("AssemblyName");
            if (element == null)
            {
                textWriter.WriteLine($"Need to specify AssemblyName");
                return false;
            }

            var name = element.Value.Trim();
            if (Regex.IsMatch(name, @"(UnitTests|IntegrationTests)$", RegexOptions.IgnoreCase) && !data.IsAnyUnitTest)
            {
                textWriter.WriteLine($"Assembly named {name} is not marked as a unit test");
                return false;
            }

            if (data.IsAnyUnitTest && !Regex.IsMatch(name, @".*(UnitTests|IntegrationTests)$", RegexOptions.IgnoreCase))
            {
                textWriter.WriteLine($"Assembly {name} is a unit test that doesn't end with UnitTests.dll");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verify our test deployment projects properly reference everything which is labeled as a portable
        /// unit test.  This ensurse they are properly deployed during build and test.
        /// </summary>
        private bool CheckTestDeploymentProjects(TextWriter textWriter)
        {
            var fileName = Path.GetFileNameWithoutExtension(_data.FileName);
            var isDesktop = fileName == "DeployDesktopTestRuntime";
            var isCoreClr = fileName == "DeployCoreClrTestRuntime";
            if (!isDesktop && !isCoreClr)
            {
                return true;
            }

            var allGood = true;
            var data = _projectUtil.GetRoslynProjectData();
            if (data.DeclaredKind != RoslynProjectKind.DeploymentTest)
            {
                textWriter.WriteLine("Test deployment project must be marked as <RoslynProjectKind>DeploymentTest</RoslynProjectKind>");
                allGood = false;
            }

            var set = new HashSet<ProjectKey>(_projectUtil.GetDeclaredProjectReferences());
            foreach (var projectData in _solutionMap.Values)
            {
                var rosData = projectData.ProjectUtil.TryGetRoslynProjectData();
                if (rosData == null)
                {
                    continue;
                }

                var kind = rosData.Value.DeclaredKind;
                bool include;
                switch (kind)
                {
                    case RoslynProjectKind.UnitTestPortable:
                        include = true;
                        break;
                    case RoslynProjectKind.UnitTestDesktop:
                        include = isDesktop;
                        break;
                    default:
                        include = false;
                        break;
                }

                if (include && !set.Contains(projectData.Key))
                {
                    textWriter.WriteLine($"Portable unit test {projectData.FileName} must be referenced");
                    allGood = false;
                }
            }

            return allGood;
        }
    }
}

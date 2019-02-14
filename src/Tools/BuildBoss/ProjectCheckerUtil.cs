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
    internal sealed class ProjectCheckerUtil : ICheckerUtil
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

        public bool Check(TextWriter textWriter)
        {
            var allGood = true;
            if (ProjectType == ProjectFileType.CSharp || ProjectType == ProjectFileType.Basic)
            {
                // Properties that aren't related to build but instead artifacts of Visual Studio.
                allGood &= CheckForProperty(textWriter, "RestorePackages");
                allGood &= CheckForProperty(textWriter, "SolutionDir");
                allGood &= CheckForProperty(textWriter, "FileAlignment");
                allGood &= CheckForProperty(textWriter, "FileUpgradeFlags");
                allGood &= CheckForProperty(textWriter, "UpgradeBackupLocation");
                allGood &= CheckForProperty(textWriter, "OldToolsVersion");
                allGood &= CheckForProperty(textWriter, "SchemaVersion");

                // Centrally controlled properties
                allGood &= CheckForProperty(textWriter, "Configuration");
                allGood &= CheckForProperty(textWriter, "CheckForOverflowUnderflow");
                allGood &= CheckForProperty(textWriter, "RemoveIntegerChecks");
                allGood &= CheckForProperty(textWriter, "Deterministic");
                allGood &= CheckForProperty(textWriter, "HighEntropyVA");
                allGood &= CheckForProperty(textWriter, "DocumentationFile");

                // Items which are not necessary anymore in the new SDK
                if (_projectUtil.IsNewSdk)
                {
                    allGood &= CheckForProperty(textWriter, "ProjectGuid");
                    allGood &= CheckForProperty(textWriter, "ProjectTypeGuids");
                    allGood &= CheckForProperty(textWriter, "TargetFrameworkProfile");
                }

                allGood &= CheckRoslynProjectType(textWriter);
                allGood &= CheckProjectReferences(textWriter);
                allGood &= CheckPackageReferences(textWriter);
                allGood &= CheckDeploymentSettings(textWriter);
            }

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
            if (!ParseRoslynProjectData(textWriter, out var data))
            {
                return false;
            }

            var allGood = true;
            allGood &= IsVsixCorrectlySpecified(textWriter, data);
            allGood &= IsUnitTestNameCorrectlySpecified(textWriter, data);
            allGood &= IsUnitTestPortableCorrectlySpecified(textWriter, data);
            allGood &= CheckTargetFrameworks(textWriter, data);

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
                textWriter.WriteLine("Unable to parse Roslyn project properties");
                textWriter.WriteLine(ex.Message);
                data = default;
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

            var declaredEntryList = _projectUtil.GetDeclaredProjectReferences();
            var declaredList = declaredEntryList.Select(x => x.ProjectKey).ToList();
            allGood &= CheckProjectReferencesComplete(textWriter, declaredList);
            allGood &= CheckUnitTestReferenceRestriction(textWriter, declaredList);
            allGood &= CheckTransitiveReferences(textWriter, declaredList);
            allGood &= CheckProjectReferencesHaveCorrectGuid(textWriter, declaredEntryList);
            allGood &= CheckNewSdkSimplified(textWriter, declaredEntryList);

            return allGood;
        }

        /// <summary>
        /// Ensure that all ProjectReference entries in the file have the correct GUID when
        /// the Project child element is present.
        /// </summary>
        private bool CheckProjectReferencesHaveCorrectGuid(TextWriter textWriter, List<ProjectReferenceEntry> entryList)
        {
            if (_projectUtil.IsNewSdk)
            {
                return true;
            }

            var allGood = true;
            foreach (var entry in entryList)
            {
                if (!_solutionMap.TryGetValue(entry.ProjectKey, out var data))
                {
                    textWriter.WriteLine($"Project not recognized {entry.FileName}");
                    allGood = false;
                    continue;
                }

                var dataGuid = data.ProjectUtil.GetProjectGuid();
                if (dataGuid != entry.Project)
                {
                    textWriter.WriteLine($"Project Reference GUID for {entry.ProjectKey.FileName} doesn't match ProjectGuid");
                    textWriter.WriteLine($"\tProject Guid {dataGuid}");
                    textWriter.WriteLine($"\tReference Guid {entry.Project}");
                    allGood = false;
                }
            }

            return allGood;
        }

        private bool CheckNewSdkSimplified(TextWriter textWriter, List<ProjectReferenceEntry> entryList)
        {
            if (!_projectUtil.IsNewSdk)
            {
                return true;
            }

            var allGood = true;
            foreach (var entry in entryList)
            {
                if (entry.Project != null)
                {
                    textWriter.WriteLine($"Project reference for {entry.ProjectKey.FileName} should not have a GUID");
                    allGood = false;
                }
            }

            return allGood;
        }

        private bool CheckPackageReferences(TextWriter textWriter)
        {
            var allGood = true;
            foreach (var packageRef in _projectUtil.GetPackageReferences())
            {
                var name = packageRef.Name.Replace(".", "").Replace("-", "");
                var floatingName = $"$({name}Version)";
                var fixedName = $"$({name}FixedVersion)";
                if (packageRef.Version != floatingName && packageRef.Version != fixedName)
                {
                    textWriter.WriteLine($"PackageReference {packageRef.Name} has incorrect version {packageRef.Version}");
                    textWriter.WriteLine($"Allowed values are {floatingName} or {fixedName}");
                    allGood = false;
                }
            }

            return allGood;
        }

        private bool CheckDeploymentSettings(TextWriter textWriter)
        {
            var data = _projectUtil.TryGetRoslynProjectData();
            if (data?.EffectiveKind == RoslynProjectKind.Custom)
            {
                return true;
            }

            var allGood = CheckForProperty(textWriter, "CopyNuGetImplementations");
            allGood &= CheckForProperty(textWriter, "UseCommonOutputDirectory");
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
                if (!_solutionMap.TryGetValue(key, out var projectData))
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

                if (!_solutionMap.TryGetValue(current, out var data))
                {
                    continue;
                }

                list.Add(current);
                foreach (var dep in data.ProjectUtil.GetDeclaredProjectReferences())
                {
                    toVisit.Enqueue(dep.ProjectKey);
                }
            }

            list.Sort((x, y) => x.FileName.CompareTo(y.FileName));
            return list;
        }

        /// <summary>
        /// Our infrastructure depends on test assembly names having a very specific set of 
        /// suffixes: UnitTest and IntegrationTests. This check will verify that both test assemblies
        /// are properly named and non-test assemblies are not incorrectly named.
        /// </summary>
        private bool IsUnitTestNameCorrectlySpecified(TextWriter textWriter, RoslynProjectData data)
        {
            if (ProjectType != ProjectFileType.CSharp && ProjectType != ProjectFileType.Basic)
            {
                return true;
            }

            if (data.EffectiveKind == RoslynProjectKind.Depedency)
            {
                return true;
            }

            string name = null;
            var element = _projectUtil.FindSingleProperty("AssemblyName");
            if (element != null)
            {
                name = element.Value.Trim();
            }
            else if (_projectUtil.IsNewSdk)
            {
                name = Path.GetFileNameWithoutExtension(_data.FileName);
            }
            else
            {
                textWriter.WriteLine($"Need to specify AssemblyName");
                return false;
            }

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

        private bool IsUnitTestPortableCorrectlySpecified(TextWriter textWriter, RoslynProjectData data)
        {
            if (!data.IsAnyUnitTest)
            {
                return true;
            }

            if (data.EffectiveKind == RoslynProjectKind.UnitTest && _projectUtil.GetTargetFrameworks() != null)
            {
                textWriter.WriteLine($"UnitTestPortable needs to be specified when using TargetFrameworks on a unit test project");
                return false;
            }

            return true;
        }

        private bool CheckTargetFrameworks(TextWriter textWriter, RoslynProjectData data)
        {
            if (!data.IsAnyUnitTest)
            {
                return true;
            }

            var allGood = true;
            foreach (var targetFramework in _projectUtil.GetAllTargetFrameworks())
            {
                // TODO: Code Style projects need to be moved over to 4.7.2 and netstandard2.0
                // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/712825 
                if (ProjectFilePath.Contains("CodeStyle"))
                {

                    switch (targetFramework)
                    {
                        case "net46":
                        case "netstandard1.3":
                            continue;
                    }
                }
                else
                {
                    switch (targetFramework)
                    {
                        case "net20":
                        case "net472":
                        case "netcoreapp1.1":
                        case "netcoreapp2.1":
                        case "$(RoslynPortableTargetFrameworks)":
                            continue;
                    }
                }

                textWriter.WriteLine($"TargetFramework {targetFramework} is not supported in this build");
                allGood = false;
            }

            return allGood;
        }
    }
}

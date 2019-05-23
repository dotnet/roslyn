using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BuildBoss
{
    internal sealed class ProjectCheckerUtil : ICheckerUtil
    {
        private readonly ProjectData _data;
        private readonly ProjectUtil _projectUtil;
        private readonly Dictionary<ProjectKey, ProjectData> _solutionMap;
        private readonly bool _isPrimarySolution;

        internal ProjectFileType ProjectType => _data.ProjectFileType;
        internal string ProjectFilePath => _data.FilePath;

        internal ProjectCheckerUtil(ProjectData data, Dictionary<ProjectKey, ProjectData> solutionMap, bool isPrimarySolution)
        {
            _data = data;
            _projectUtil = data.ProjectUtil;
            _solutionMap = solutionMap;
            _isPrimarySolution = isPrimarySolution;
        }

        public bool Check(TextWriter textWriter)
        {
            var allGood = true;
            if (ProjectType == ProjectFileType.CSharp || ProjectType == ProjectFileType.Basic)
            {
                if (!_projectUtil.IsNewSdk)
                {
                    textWriter.WriteLine($"Project must new .NET SDK based");
                    allGood = false;
                }

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
                allGood &= CheckForProperty(textWriter, "ProjectGuid");
                allGood &= CheckForProperty(textWriter, "ProjectTypeGuids");
                allGood &= CheckForProperty(textWriter, "TargetFrameworkProfile");

                allGood &= CheckTargetFrameworks(textWriter);
                allGood &= CheckProjectReferences(textWriter);
                allGood &= CheckPackageReferences(textWriter);

                if (_isPrimarySolution)
                {
                    allGood &= CheckInternalsVisibleTo(textWriter);
                }

                allGood &= CheckDeploymentSettings(textWriter);
            }
            else if (ProjectType == ProjectFileType.Tool)
            {
                allGood &= CheckPackageReferences(textWriter);
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

        private bool CheckProjectReferences(TextWriter textWriter)
        {
            var allGood = true;

            var declaredEntryList = _projectUtil.GetDeclaredProjectReferences();
            var declaredList = declaredEntryList.Select(x => x.ProjectKey).ToList();
            allGood &= CheckProjectReferencesComplete(textWriter, declaredList);
            allGood &= CheckUnitTestReferenceRestriction(textWriter, declaredList);
            allGood &= CheckTransitiveReferences(textWriter, declaredList);
            allGood &= CheckNoGuidsOnProjectReferences(textWriter, declaredEntryList);

            return allGood;
        }

        private bool CheckNoGuidsOnProjectReferences(TextWriter textWriter, List<ProjectReferenceEntry> entryList)
        {
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

        private bool CheckInternalsVisibleTo(TextWriter textWriter)
        {
            var allGood = true;
            foreach (var internalsVisibleTo in _projectUtil.GetInternalsVisibleTo())
            {
                if (string.Equals(internalsVisibleTo.LoadsWithinVisualStudio, "false", StringComparison.OrdinalIgnoreCase))
                {
                    // IVTs explicitly declared with LoadsWithinVisualStudio="false" are allowed
                    continue;
                }

                if (_projectUtil.Key.FileName.StartsWith("Microsoft.CodeAnalysis.ExternalAccess."))
                {
                    // External access layer may have external IVTs
                    continue;
                }

                if (!string.IsNullOrEmpty(internalsVisibleTo.WorkItem))
                {
                    if (!Uri.TryCreate(internalsVisibleTo.WorkItem, UriKind.Absolute, out _))
                    {
                        textWriter.WriteLine($"InternalsVisibleTo for external assembly '{internalsVisibleTo.TargetAssembly}' does not have a valid URI specified for {nameof(InternalsVisibleTo.WorkItem)}.");
                        allGood = false;
                    }

                    // A work item is tracking elimination of this IVT
                    continue;
                }

                var builtByThisRepository = _solutionMap.Values.Any(projectData => GetAssemblyName(projectData) == internalsVisibleTo.TargetAssembly);
                if (!builtByThisRepository)
                {
                    textWriter.WriteLine($"InternalsVisibleTo not allowed for external assembly '{internalsVisibleTo.TargetAssembly}' that may load within Visual Studio.");
                    allGood = false;
                }
            }

            return allGood;

            // Local functions
            static string GetAssemblyName(ProjectData projectData)
            {
                return projectData.ProjectUtil.FindSingleProperty("AssemblyName")?.Value.Trim()
                    ?? Path.GetFileNameWithoutExtension(projectData.FileName);
            }
        }

        private bool CheckDeploymentSettings(TextWriter textWriter)
        {
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
            if (!_data.IsTestProject)
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

                if (projectData.ProjectUtil.IsTestProject)
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
            if (!_projectUtil.IsDeploymentProject)
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

        private bool CheckTargetFrameworks(TextWriter textWriter)
        {
            if (!_data.IsUnitTestProject)
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
                        case "netcoreapp3.0":
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

using System;
using System.Collections.Generic;
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
        private readonly ProjectUtil _fileUtil;
        private readonly Dictionary<ProjectKey, ProjectData> _solutionMap;

        internal ProjectFileType ProjectType => _data.ProjectFileType;
        internal string ProjectFilePath => _data.FilePath;

        internal ProjectCheckerUtil(ProjectData data, Dictionary<ProjectKey, ProjectData> solutionMap)
        {
            _data = data;
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
                allGood &= CheckRoslynProjectType(textWriter);
                allGood &= CheckProjectReferences(textWriter);
            }

            return allGood;
        }

        private bool CheckForProperty(TextWriter textWriter, string propertyName)
        {
            foreach (var element in ojectFileUtil. GetAllPropertyGroupElements())
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
            data = default(RoslynProjectData);

            var typeElement = FindSingleProperty("RoslynProjectType");
            if (typeElement != null)
            {
                var value = typeElement.Value.Trim();
                var kind = RoslynProjectData.GetRoslynProjectKind(value);
                if (kind == null)
                {
                    textWriter.WriteLine($"Unrecognized RoslynProjectKnid value {value}");
                    return false;
                }

                data = new RoslynProjectData(kind.Value, kind.Value, value);
                return true;
            }
            else
            { 
                var outputType = FindSingleProperty("OutputType");
                switch (outputType?.Value.Trim())
                {
                    case "Exe":
                    case "WinExe":
                        data = new RoslynProjectData(RoslynProjectKind.Exe);
                        return true;
                    case "Library":
                        data = new RoslynProjectData(RoslynProjectKind.Dll);
                        return true;
                    default:
                        textWriter.WriteLine($"Unrecognized OutputType value {outputType?.Value.Trim()}");
                        return false;
                }
            }
        }

        private bool IsVsixCorrectlySpecified(TextWriter textWriter, RoslynProjectData data)
        {
            var element = FindSingleProperty("ProjectTypeGuids");
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

            var declaredList = GetDeclaredProjectReferences(_data);
            allGood &= CheckProjectReferencesComplete(textWriter, declaredList);
            allGood &= CheckUnitTestReferenceRestriction(textWriter, declaredList);

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
        private bool CheckUnitTestReferenceRestriction(TextWriter textWrite, IEnumerable<ProjectKey> declaredReferences)
        {
            var allGood = true;
            foreach (var key in declaredReferences)
            {

            }
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

            var element = FindSingleProperty("AssemblyName");
            if (element == null)
            {
                textWriter.WriteLine($"Need to specify AssemblyName");
                return false;
            }

            var name = element.Value.Trim();
            if (Regex.IsMatch(name, @"UnitTest(s?)\.dll", RegexOptions.IgnoreCase))
            {
                switch (data.EffectiveKind)
                {
                    case RoslynProjectKind.UnitTest:
                    case RoslynProjectKind.UnitTestNext:
                        // This is correct
                        break;
                    default:
                        textWriter.WriteLine($"Assembly named {name} is not marked as a unit test");
                        return false;
                }
            }

            return true;
        }

    }
}

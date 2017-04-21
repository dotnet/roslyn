// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class WorkspaceTestBase : TestBase
    {
        protected readonly TempDirectory SolutionDirectory;

        protected static readonly TimeSpan AsyncEventTimeout = TimeSpan.FromMinutes(5);

        public WorkspaceTestBase()
        {
            this.SolutionDirectory = Temp.CreateDirectory();
        }

        /// <summary>
        /// Gets an absolute file name for a file relative to the tests solution directory.
        /// </summary>
        public string GetSolutionFileName(string relativeFileName)
        {
            return Path.Combine(this.SolutionDirectory.Path, relativeFileName);
        }

        protected void CreateFiles(params string[] fileNames)
        {
            var dictionary = fileNames.ToDictionary(id => id, fileName => (object)GetResourceText(fileName));
            CreateFiles(new FileSet(dictionary));
        }

        protected void CreateFiles(IEnumerable<KeyValuePair<string, object>> fileNameAndContentPairs)
        {
            foreach (var pair in fileNameAndContentPairs)
            {
                Debug.Assert(pair.Value is string || pair.Value is byte[]);

                var subdirectory = Path.GetDirectoryName(pair.Key);
                var fileName = Path.GetFileName(pair.Key);

                var dir = SolutionDirectory;

                if (!string.IsNullOrEmpty(subdirectory))
                {
                    dir = dir.CreateDirectory(subdirectory);
                }

                // workspace uses File APIs that don't work with "delete on close" files:
                var file = dir.CreateFile(fileName);

                if (pair.Value is string)
                {
                    file.WriteAllText((string)pair.Value);
                }
                else
                {
                    file.WriteAllBytes((byte[])pair.Value);
                }
            }
        }

        protected void CreateCSharpFilesWith(string propertyName, string value)
        {
            CreateFiles(GetSimpleCSharpSolutionFiles()
                .WithFile(@"CSharpProject\CSharpProject.csproj", GetResourceText(@"CSharpProject_CSharpProject_AllOptions.csproj"))
                .ReplaceFileElement(@"CSharpProject\CSharpProject.csproj", propertyName, value));
        }

        protected void CreateVBFilesWith(string propertyName, string value)
        {
            CreateFiles(GetMultiProjectSolutionFiles()
                .ReplaceFileElement(@"VisualBasicProject\VisualBasicProject.vbproj", propertyName, value));
        }

        protected void CreateCSharpFiles()
        {
            CreateFiles(GetSimpleCSharpSolutionFiles());
        }

        protected FileSet GetSimpleCSharpSolutionFiles()
        {
            return new FileSet(new Dictionary<string, object>
            {
                { @"TestSolution.sln", GetResourceText("TestSolution_CSharp.sln") },
                { @"CSharpProject\CSharpProject.csproj", GetResourceText("CSharpProject_CSharpProject.csproj") },
                { @"CSharpProject\CSharpClass.cs", GetResourceText("CSharpProject_CSharpClass.cs") },
                { @"CSharpProject\Properties\AssemblyInfo.cs", GetResourceText("CSharpProject_AssemblyInfo.cs") }
            });
        }

        protected FileSet GetMultiProjectSolutionFiles()
        {
            return new FileSet(new Dictionary<string, object>
            {
                { @"TestSolution.sln", GetResourceText("TestSolution_VB_and_CSharp.sln") },
                { @"CSharpProject\CSharpProject.csproj", GetResourceText("CSharpProject_CSharpProject.csproj") },
                { @"CSharpProject\CSharpClass.cs", GetResourceText("CSharpProject_CSharpClass.cs") },
                { @"CSharpProject\Properties\AssemblyInfo.cs", GetResourceText("CSharpProject_AssemblyInfo.cs") },
                { @"VisualBasicProject\VisualBasicProject.vbproj", GetResourceText("VisualBasicProject_VisualBasicProject.vbproj") },
                { @"VisualBasicProject\VisualBasicClass.vb", GetResourceText("VisualBasicProject_VisualBasicClass.vb") },
                { @"VisualBasicProject\My Project\Application.Designer.vb", GetResourceText("VisualBasicProject_Application.Designer.vb") },
                { @"VisualBasicProject\My Project\Application.myapp", GetResourceText("VisualBasicProject_Application.myapp") },
                { @"VisualBasicProject\My Project\AssemblyInfo.vb", GetResourceText("VisualBasicProject_AssemblyInfo.vb") },
                { @"VisualBasicProject\My Project\Resources.Designer.vb", GetResourceText("VisualBasicProject_Resources.Designer.vb") },
                { @"VisualBasicProject\My Project\Resources.resx", GetResourceText("VisualBasicProject_Resources.resx_") },
                { @"VisualBasicProject\My Project\Settings.Designer.vb", GetResourceText("VisualBasicProject_Settings.Designer.vb") },
                { @"VisualBasicProject\My Project\Settings.settings", GetResourceText("VisualBasicProject_Settings.settings") },
            });
        }

        protected FileSet GetProjectReferenceSolutionFiles()
        {
            return new FileSet(new Dictionary<string, object>
            {
                { @"CSharpProjectReference.sln", GetResourceText("TestSolution_CSharpProjectReference.sln") },
                { @"CSharpProject\CSharpProject.csproj", GetResourceText("CSharpProject_CSharpProject.csproj") },
                { @"CSharpProject\CSharpClass.cs", GetResourceText("CSharpProject_CSharpClass.cs") },
                { @"CSharpProject\Properties\AssemblyInfo.cs", GetResourceText("CSharpProject_AssemblyInfo.cs") },
                { @"CSharpProject\CSharpProject_ProjectReference.csproj", GetResourceText("CSharpProject_CSharpProject_ProjectReference.csproj") },
                { @"CSharpProject\CSharpConsole.cs", GetResourceText("CSharpProject_CSharpConsole.cs") },
            });
        }

        protected FileSet GetAnalyzerReferenceSolutionFiles()
        {
            return new FileSet(new Dictionary<string, object>
            {
                { @"AnalyzerReference.sln", GetResourceText("TestSolution_AnalyzerReference.sln") },
                { @"AnalyzerSolution\CSharpProject.dll", GetResourceText("CSharpProject.dll") },
                { @"AnalyzerSolution\CSharpProject_AnalyzerReference.csproj", GetResourceText("CSharpProject_CSharpProject_AnalyzerReference.csproj") },
                { @"AnalyzerSolution\CSharpClass.cs", GetResourceText("CSharpProject_CSharpClass.cs") },
                { @"AnalyzerSolution\XamlFile.xaml", GetResourceText("CSharpProject_MainWindow.xaml") },
                { @"AnalyzerSolution\VisualBasicProject_AnalyzerReference.vbproj", GetResourceText("VisualBasicProject_VisualBasicProject_AnalyzerReference.vbproj") },
                { @"AnalyzerSolution\VisualBasicClass.vb", GetResourceText("VisualBasicProject_VisualBasicClass.vb") },
                { @"AnalyzerSolution\My Project\Application.Designer.vb", GetResourceText("VisualBasicProject_Application.Designer.vb") },
                { @"AnalyzerSolution\My Project\Application.myapp", GetResourceText("VisualBasicProject_Application.myapp") },
                { @"AnalyzerSolution\My Project\AssemblyInfo.vb", GetResourceText("VisualBasicProject_AssemblyInfo.vb") },
                { @"AnalyzerSolution\My Project\Resources.Designer.vb", GetResourceText("VisualBasicProject_Resources.Designer.vb") },
                { @"AnalyzerSolution\My Project\Resources.resx", GetResourceText("VisualBasicProject_Resources.resx_") },
                { @"AnalyzerSolution\My Project\Settings.Designer.vb", GetResourceText("VisualBasicProject_Settings.Designer.vb") },
                { @"AnalyzerSolution\My Project\Settings.settings", GetResourceText("VisualBasicProject_Settings.settings") },
            });
        }

        protected FileSet GetSolutionWithDuplicatedGuidFiles()
        {
            return new FileSet(new Dictionary<string, object>
            {
                { @"DuplicatedGuids.sln", GetResourceText("TestSolution_DuplicatedGuids.sln") },
                { @"ReferenceTest\ReferenceTest.csproj", GetResourceText("CSharpProject_DuplicatedGuidReferenceTest.csproj") },
                { @"Library1\Library1.csproj", GetResourceText("CSharpProject_DuplicatedGuidLibrary1.csproj") },
                { @"Library2\Library2.csproj", GetResourceText("CSharpProject_DuplicatedGuidLibrary2.csproj") }
            });
        }

        protected FileSet GetSolutionWithCircularProjectReferences()
        {
            return new FileSet(new Dictionary<string, object>
            {
                { @"CircularSolution.sln", GetResourceText("CircularProjectReferences.CircularSolution.sln") },
                { @"CircularCSharpProject1.csproj", GetResourceText("CircularProjectReferences.CircularCSharpProject1.csproj") },
                { @"CircularCSharpProject2.csproj", GetResourceText("CircularProjectReferences.CircularCSharpProject2.csproj") },
            });
        }

        public static byte[] GetResourceBytes(string fileName)
        {
            var fullName = @"Microsoft.CodeAnalysis.UnitTests.TestFiles." + fileName;
            var resourceStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(fullName);
            if (resourceStream != null)
            {
                using (resourceStream)
                {
                    var bytes = new byte[resourceStream.Length];
                    resourceStream.Read(bytes, 0, (int)resourceStream.Length);
                    return bytes;
                }
            }

            return null;
        }

        public static string GetResourceText(string fileName)
        {
            var fullName = @"Microsoft.CodeAnalysis.UnitTests.TestFiles." + fileName;
            var resourceStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(fullName);
            if (resourceStream != null)
            {
                using (var streamReader = new StreamReader(resourceStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
            else
            {
                throw new InvalidOperationException(string.Format("Cannot find resource named: '{0}'", fullName));
            }
        }

        protected static string GetParentDirOfParentDirOfContainingDir(string fileName)
        {
            string containingDir = Directory.GetParent(fileName).FullName;
            string parentOfContainingDir = Directory.GetParent(containingDir).FullName;
            return Directory.GetParent(parentOfContainingDir).FullName;
        }

        protected static void AssertThrows<TException>(Action action, Action<TException> checker = null)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                var agg = e as AggregateException;
                if (agg != null && agg.InnerExceptions.Count == 1)
                {
                    e = agg.InnerExceptions[0];
                }

                Assert.Equal(typeof(TException), e.GetType());
                checker?.Invoke((TException)e);
            }
        }

        protected int GetMethodInsertionPoint(VB.Syntax.ClassBlockSyntax cb)
        {
            if (cb.Implements.Count > 0)
            {
                return cb.Implements[cb.Implements.Count - 1].FullSpan.End;
            }
            else if (cb.Inherits.Count > 0)
            {
                return cb.Inherits[cb.Inherits.Count - 1].FullSpan.End;
            }
            else
            {
                return cb.BlockStatement.FullSpan.End;
            }
        }

        protected Document AssertSemanticVersionChanged(Document document, SourceText newText)
        {
            var docVersion = document.GetTopLevelChangeTextVersionAsync().Result;
            var projVersion = document.Project.GetSemanticVersionAsync().Result;

            var text = document.GetTextAsync().Result;
            var newDoc = document.WithText(newText);

            var newDocVersion = newDoc.GetTopLevelChangeTextVersionAsync().Result;
            var newProjVersion = newDoc.Project.GetSemanticVersionAsync().Result;

            Assert.NotEqual(docVersion, newDocVersion);
            Assert.NotEqual(projVersion, newProjVersion);

            return newDoc;
        }

        protected Document AssertSemanticVersionUnchanged(Document document, SourceText newText)
        {
            var docVersion = document.GetTopLevelChangeTextVersionAsync().Result;
            var projVersion = document.Project.GetSemanticVersionAsync().Result;

            var text = document.GetTextAsync().Result;
            var newDoc = document.WithText(newText);

            var newDocVersion = newDoc.GetTopLevelChangeTextVersionAsync().Result;
            var newProjVersion = newDoc.Project.GetSemanticVersionAsync().Result;

            Assert.Equal(docVersion, newDocVersion);
            Assert.Equal(projVersion, newProjVersion);

            return newDoc;
        }
    }
}

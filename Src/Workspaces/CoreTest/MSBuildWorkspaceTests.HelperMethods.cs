// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class MSBuildWorkspaceTests
    {
        private readonly TempDirectory solutionDirectory;

        private static readonly TimeSpan asyncEventTimeout = TimeSpan.FromMinutes(5);

        public MSBuildWorkspaceTests()
        {
            solutionDirectory = Temp.CreateDirectory();
        }

        public string GetSolutionFileName(string relativeFileName)
        {
            return Path.Combine(this.solutionDirectory.Path, relativeFileName);
        }

        private void CreateFiles(params string[] fileNames)
        {
            var dictionary = fileNames.ToDictionary(id => id, fileName => (object)GetResourceText(fileName));
            CreateFiles(new FileSet(dictionary));
        }

        private void CreateFiles(IEnumerable<KeyValuePair<string, object>> fileNameAndContentPairs)
        {
            foreach (var pair in fileNameAndContentPairs)
            {
                Debug.Assert(pair.Value is string || pair.Value is byte[]);

                var subdirectory = Path.GetDirectoryName(pair.Key);
                var fileName = Path.GetFileName(pair.Key);

                var dir = solutionDirectory;

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

        private void CreateCSharpFilesWith(string propertyName, string value)
        {
            CreateFiles(GetSimpleCSharpSolutionFiles()
                .WithFile(@"CSharpProject\CSharpProject.csproj", GetResourceText(@"CSharpProject_CSharpProject_AllOptions.csproj"))
                .ReplaceFileElement(@"CSharpProject\CSharpProject.csproj", propertyName, value));
        }

        private void CreateVBFilesWith(string propertyName, string value)
        {
            CreateFiles(GetMultiProjectSolutionFiles()
                .ReplaceFileElement(@"VisualBasicProject\VisualBasicProject.vbproj", propertyName, value));
        }

        private void CreateCSharpFiles()
        {
            CreateFiles(GetSimpleCSharpSolutionFiles());
        }

        private void AssertOptions<T>(T expected, Func<Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions, T> actual)
        {
            var options = LoadCSharpCompilationOptions();
            Assert.Equal(expected, actual(options));
        }

        private void AssertOptions<T>(T expected, Func<Microsoft.CodeAnalysis.CSharp.CSharpParseOptions, T> actual)
        {
            var options = LoadCSharpParseOptions();
            Assert.Equal(expected, actual(options));
        }

        private void AssertVBOptions<T>(T expected, Func<Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationOptions, T> actual)
        {
            var options = LoadVisualBasicCompilationOptions();
            Assert.Equal(expected, actual(options));
        }

        private void AssertVBOptions<T>(T expected, Func<Microsoft.CodeAnalysis.VisualBasic.VisualBasicParseOptions, T> actual)
        {
            var options = LoadVisualBasicParseOptions();
            Assert.Equal(expected, actual(options));
        }

        private Solution LoadSolution(string solutionFilePath, IDictionary<string, string> properties = null)
        {
            var ws = MSBuildWorkspace.Create(properties ?? ImmutableDictionary<string, string>.Empty);
            return ws.OpenSolutionAsync(solutionFilePath).Result;
        }

        private Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions LoadCSharpCompilationOptions()
        {
            var sol = LoadSolution(GetSolutionFileName("TestSolution.sln"));
            var project = sol.Projects.First();
            var options = (Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions)project.CompilationOptions;
            return options;
        }

        private Microsoft.CodeAnalysis.CSharp.CSharpParseOptions LoadCSharpParseOptions()
        {
            var sol = LoadSolution(GetSolutionFileName("TestSolution.sln"));
            var project = sol.Projects.First();
            var options = (Microsoft.CodeAnalysis.CSharp.CSharpParseOptions)project.ParseOptions;
            return options;
        }

        private Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationOptions LoadVisualBasicCompilationOptions()
        {
            var sol = LoadSolution(GetSolutionFileName("TestSolution.sln"));
            var project = sol.GetProjectsByName("VisualBasicProject").FirstOrDefault();
            var options = (Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationOptions)project.CompilationOptions;
            return options;
        }

        private Microsoft.CodeAnalysis.VisualBasic.VisualBasicParseOptions LoadVisualBasicParseOptions()
        {
            var sol = LoadSolution(GetSolutionFileName("TestSolution.sln"));
            var project = sol.GetProjectsByName("VisualBasicProject").FirstOrDefault();
            var options = (Microsoft.CodeAnalysis.VisualBasic.VisualBasicParseOptions)project.ParseOptions;
            return options;
        }

        private FileSet GetSimpleCSharpSolutionFiles()
        {
            return new FileSet(new Dictionary<string, object>
            {
                { @"TestSolution.sln", GetResourceText("TestSolution_CSharp.sln") },
                { @"CSharpProject\CSharpProject.csproj", GetResourceText("CSharpProject_CSharpProject.csproj") },
                { @"CSharpProject\CSharpClass.cs", GetResourceText("CSharpProject_CSharpClass.cs") },
                { @"CSharpProject\Properties\AssemblyInfo.cs", GetResourceText("CSharpProject_AssemblyInfo.cs") }
            });
        }

        private FileSet GetMultiProjectSolutionFiles()
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

        private FileSet GetProjectReferenceSolutionFiles()
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

        private FileSet GetAnalyzerReferenceSolutionFiles()
        {
            return new FileSet(new Dictionary<string, object>
            {
                { @"AnalyzerReference.sln", GetResourceText("TestSolution_AnalyzerReference.sln") },
                { @"AnalyzerSolution\CSharpProject_AnalyzerReference.csproj", GetResourceText("CSharpProject_CSharpProject_AnalyzerReference.csproj") },
                { @"AnalyzerSolution\CSharpClass.cs", GetResourceText("CSharpProject_CSharpClass.cs") },
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

        private FileSet GetSolutionWithDuplicatedGuidFiles()
        {
            return new FileSet(new Dictionary<string, object>
            {
                { @"DuplicatedGuids.sln", GetResourceText("TestSolution_DuplicatedGuids.sln") },
                { @"ReferenceTest\ReferenceTest.csproj", GetResourceText("CSharpProject_DuplicatedGuidReferenceTest.csproj") },
                { @"Library1\Library1.csproj", GetResourceText("CSharpProject_DuplicatedGuidLibrary1.csproj") },
                { @"Library2\Library2.csproj", GetResourceText("CSharpProject_DuplicatedGuidLibrary2.csproj") }
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

        private void PrepareCrossLanguageProjectWithEmittedMetadata()
        {
            // Now try variant of CSharpProject that has an emitted assembly 
            CreateFiles(GetMultiProjectSolutionFiles()
                .WithFile(@"CSharpProject\CSharpProject.csproj", GetResourceText("CSharpProject_CSharpProject_ForEmittedOutput.csproj")));

            var sol = LoadSolution(GetSolutionFileName("TestSolution.sln"));
            var p1 = sol.Projects.First(p => p.Language == LanguageNames.CSharp);
            var p2 = sol.Projects.First(p => p.Language == LanguageNames.VisualBasic);

            Assert.NotNull(p1.OutputFilePath);
            Assert.Equal("EmittedCSharpProject.dll", Path.GetFileName(p1.OutputFilePath));

            // if the assembly doesn't already exist, emit it now
            if (!File.Exists(p1.OutputFilePath))
            {
                var c1 = p1.GetCompilationAsync().Result;
                var result = c1.Emit(p1.OutputFilePath);
                Assert.Equal(true, result.Success);
            }
        }

        private static string GetParentDirOfParentDirOfContainingDir(string fileName)
        {
            string containingDir = Directory.GetParent(fileName).FullName;
            string parentOfContainingDir = Directory.GetParent(containingDir).FullName;
            return Directory.GetParent(parentOfContainingDir).FullName;
        }

        private static void AssertThrows<TException>(Action action)
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
            }
        }

        private static ImmutableArray<byte> EmitToArray(Compilation compilation)
        {
            var stream = new MemoryStream();
            var result = compilation.Emit(stream);
            stream.Position = 0;
            return stream.ToImmutable();
        }

        private int GetMethodInsertionPoint(VB.Syntax.ClassBlockSyntax cb)
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
                return cb.Begin.FullSpan.End;
            }
        }

        private Document AssertSemanticVersionChanged(Document document, SourceText newText)
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

        private Document AssertSemanticVersionUnchanged(Document document, SourceText newText)
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

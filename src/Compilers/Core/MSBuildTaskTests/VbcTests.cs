// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.BuildTasks;
using Microsoft.CodeAnalysis.BuildTasks.UnitTests.TestUtilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public sealed class VbcTests
    {
        public ITestOutputHelper TestOutputHelper { get; }

        public VbcTests(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelper = testOutputHelper;
        }

        [Fact]
        public void SingleSource()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            Assert.Equal("/optionstrict:custom /out:test.exe test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void MultipleSourceFiles()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test1.vb", "test2.vb");
            Assert.Equal("/optionstrict:custom /out:test1.exe test1.vb test2.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void PathMapOption()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.PathMap = "K1=V1,K2=V2";
            Assert.Equal("/optionstrict:custom /pathmap:\"K1=V1,K2=V2\" /out:test.exe test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            Assert.Equal("/optionstrict:custom /out:test.exe test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void DeterministicFlag()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.Deterministic = true;
            Assert.Equal("/optionstrict:custom /out:test.exe /deterministic+ test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.Deterministic = false;
            Assert.Equal("/optionstrict:custom /out:test.exe /deterministic- test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            Assert.Equal("/optionstrict:custom /out:test.exe test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void PublicSignFlag()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.PublicSign = true;
            Assert.Equal("/optionstrict:custom /out:test.exe /publicsign+ test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.PublicSign = false;
            Assert.Equal("/optionstrict:custom /out:test.exe /publicsign- test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            Assert.Equal("/optionstrict:custom /out:test.exe test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void RuntimeMetadataVersionFlag()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.RuntimeMetadataVersion = "v1234";
            Assert.Equal("/optionstrict:custom /out:test.exe /runtimemetadataversion:v1234 test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.RuntimeMetadataVersion = null;
            Assert.Equal("/optionstrict:custom /out:test.exe test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void LangVersionFlag()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.LangVersion = "15.3";
            Assert.Equal("/optionstrict:custom /out:test.exe /langversion:15.3 test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void ChecksumAlgorithmOption()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.ChecksumAlgorithm = "sha256";
            Assert.Equal("/optionstrict:custom /out:test.exe /checksumalgorithm:sha256 test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.ChecksumAlgorithm = null;
            Assert.Equal("/optionstrict:custom /out:test.exe test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.ChecksumAlgorithm = "";
            Assert.Equal("/optionstrict:custom /out:test.exe /checksumalgorithm: test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void InstrumentTestNamesFlag()
        {
            var vbc = new Vbc();
            vbc.Instrument = null;
            Assert.Equal("/optionstrict:custom", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Instrument = "TestCoverage";
            Assert.Equal("/optionstrict:custom /instrument:TestCoverage", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Instrument = "TestCoverage,Mumble";
            Assert.Equal("/optionstrict:custom /instrument:TestCoverage,Mumble", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Instrument = "TestCoverage,Mumble;Stumble";
            Assert.Equal("/optionstrict:custom /instrument:TestCoverage,Mumble,Stumble", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void TargetTypeDll()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.TargetType = "library";
            Assert.Equal("/optionstrict:custom /out:test.dll /target:library test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void TargetTypeBad()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.TargetType = "bad";
            Assert.Equal("/optionstrict:custom /out:test.exe /target:bad test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void OutputAssembly()
        {
            var vbc = new Vbc();
            vbc.OutputAssembly = MSBuildUtil.CreateTaskItem("x.exe");
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            Assert.Equal("/optionstrict:custom /out:x.exe test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void DefineConstantsSimple()
        {
            Action<string> test = (s) =>
            {
                var vbc = new Vbc();
                vbc.DefineConstants = s;
                vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
                Assert.Equal($@"/optionstrict:custom /define:""{s}"" /out:test.exe test.vb", vbc.GenerateResponseFileContents());
            };

            test("D1;D2");
            test("D1,D2");
            test("D1 D2");
        }

        [Fact]
        public void Features()
        {
            Action<string> test = (s) =>
            {
                var vbc = new Vbc();
                vbc.Features = s;
                vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
                Assert.Equal(@"/optionstrict:custom /out:test.exe /features:a /features:b test.vb", vbc.GenerateResponseFileContents());
            };

            test("a;b");
            test("a,b");
            test("a b");
            test(",a;b ");
            test(";a;;b;");
            test(",a,,b,");
        }

        [Fact]
        public void FeaturesEmpty()
        {
            foreach (var cur in new[] { "", null })
            {
                var vbc = new Vbc();
                vbc.Features = cur;
                vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
                Assert.Equal(@"/optionstrict:custom /out:test.exe test.vb", vbc.GenerateResponseFileContents());
            }
        }

        [Fact]
        public void DebugType()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.DebugType = "full";
            Assert.Equal("/optionstrict:custom /debug:full /out:test.exe test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.DebugType = "pdbonly";
            Assert.Equal("/optionstrict:custom /debug:pdbonly /out:test.exe test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.DebugType = "portable";
            Assert.Equal("/optionstrict:custom /debug:portable /out:test.exe test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.DebugType = "embedded";
            Assert.Equal("/optionstrict:custom /debug:embedded /out:test.exe test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.DebugType = null;
            Assert.Equal("/optionstrict:custom /out:test.exe test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.DebugType = "";
            Assert.Equal("/optionstrict:custom /debug: /out:test.exe test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void SourceLink()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.DebugType = "portable";
            vbc.SourceLink = @"C:\x y\z.json";
            Assert.Equal(@"/optionstrict:custom /debug:portable /out:test.exe /sourcelink:""C:\x y\z.json"" test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.DebugType = "portable";
            vbc.SourceLink = null;
            Assert.Equal(@"/optionstrict:custom /debug:portable /out:test.exe test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.DebugType = "portable";
            vbc.SourceLink = "";
            Assert.Equal(@"/optionstrict:custom /debug:portable /out:test.exe /sourcelink: test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void Embed()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.DebugType = "portable";
            vbc.EmbeddedFiles = MSBuildUtil.CreateTaskItems(@"test.vb", @"test.txt");
            Assert.Equal(@"/optionstrict:custom /debug:portable /out:test.exe /embed:test.vb /embed:test.txt test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.DebugType = "portable";
            vbc.EmbeddedFiles = MSBuildUtil.CreateTaskItems(@"C:\x y\z.json");
            Assert.Equal(@"/optionstrict:custom /debug:portable /out:test.exe /embed:""C:\x y\z.json"" test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.DebugType = "portable";
            vbc.EmbeddedFiles = null;
            Assert.Equal(@"/optionstrict:custom /debug:portable /out:test.exe test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.DebugType = "full";
            vbc.EmbeddedFiles = MSBuildUtil.CreateTaskItems();
            Assert.Equal(@"/optionstrict:custom /debug:full /out:test.exe test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("a;b.vb");
            vbc.DebugType = "full";
            vbc.EmbeddedFiles = MSBuildUtil.CreateTaskItems("a;b.vb");
            Assert.Equal(@"/optionstrict:custom /debug:full /out:""a;b.exe"" /embed:""a;b.vb"" ""a;b.vb""", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("a, b.vb");
            vbc.DebugType = "full";
            vbc.EmbeddedFiles = MSBuildUtil.CreateTaskItems("a, b.vb");
            Assert.Equal(@"/optionstrict:custom /debug:full /out:""a, b.exe"" /embed:""a, b.vb"" ""a, b.vb""", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void EmbedAllSources()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.EmbeddedFiles = MSBuildUtil.CreateTaskItems(@"test.vb", @"test.txt");
            vbc.EmbedAllSources = true;

            Assert.Equal(@"/optionstrict:custom /out:test.exe /embed /embed:test.vb /embed:test.txt test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.EmbedAllSources = true;

            Assert.Equal(@"/optionstrict:custom /out:test.exe /embed test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void RefOut()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.OutputRefAssembly = MSBuildUtil.CreateTaskItem("ref\\test.dll");
            Assert.Equal("/optionstrict:custom /out:test.exe /refout:ref\\test.dll test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void RefOnly()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.RefOnly = true;
            Assert.Equal("/optionstrict:custom /out:test.exe /refonly test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void SharedCompilationId()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.UseSharedCompilation = true;
            vbc.SharedCompilationId = "testPipeName";
            Assert.Equal("/optionstrict:custom /out:test.exe test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.UseSharedCompilation = false;
            vbc.SharedCompilationId = "testPipeName";
            Assert.Equal("/optionstrict:custom /out:test.exe test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.SharedCompilationId = "testPipeName";
            Assert.Equal("/optionstrict:custom /out:test.exe test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        [WorkItem(21371, "https://github.com/dotnet/roslyn/issues/21371")]
        public void GenerateDocumentationFalse()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.GenerateDocumentation = false;
            vbc.DocumentationFile = "test.xml";
            Assert.Equal("/doc- /optionstrict:custom /out:test.exe test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        [WorkItem(21371, "https://github.com/dotnet/roslyn/issues/21371")]
        public void GenerateDocumentationTrue()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.GenerateDocumentation = true;
            vbc.DocumentationFile = "test.xml";
            Assert.Equal("/doc+ /optionstrict:custom /doc:test.xml /out:test.exe test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        [WorkItem(21371, "https://github.com/dotnet/roslyn/issues/21371")]
        public void GenerateDocumentationTrueWithoutFile()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.GenerateDocumentation = true;
            Assert.Equal("/doc+ /optionstrict:custom /out:test.exe test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        [WorkItem(21371, "https://github.com/dotnet/roslyn/issues/21371")]
        public void GenerateDocumentationUnspecified()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.DocumentationFile = "test.xml";
            Assert.Equal("/optionstrict:custom /doc:test.xml /out:test.exe test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        [WorkItem(29252, "https://github.com/dotnet/roslyn/issues/29252")]
        public void SdkPath()
        {
            var vbc = new Vbc();
            vbc.SdkPath = @"path\to\sdk";
            Assert.Equal(@"/optionstrict:custom /sdkpath:path\to\sdk", vbc.GenerateResponseFileContents());
        }

        [Fact]
        [WorkItem(29252, "https://github.com/dotnet/roslyn/issues/29252")]
        public void DisableSdkPath()
        {
            var vbc = new Vbc();
            vbc.DisableSdkPath = true;
            Assert.Equal(@"/optionstrict:custom /nosdkpath", vbc.GenerateResponseFileContents());
        }

        /// <summary>
        /// Setting ToolExe to "vbc.exe" should use the built-in compiler regardless of apphost being used or not.
        /// </summary>
        [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2615118")]
        public void BuiltInToolExe(bool useAppHost, bool setToolExe)
        {
            var vbc = new Vbc();
            vbc.UseAppHost_TestOnly = useAppHost;
            if (setToolExe)
            {
                vbc.ToolExe = $"vbc{PlatformInformation.ExeExtension}";
            }
            if (useAppHost)
            {
                AssertEx.Equal(vbc.PathToBuiltInTool, vbc.GeneratePathToTool());
                AssertEx.Equal("", vbc.GenerateCommandLineContents());
            }
            else
            {
                AssertEx.Equal(RuntimeHostInfo.GetDotNetPathOrDefault(), vbc.GeneratePathToTool());
                AssertEx.Equal(RuntimeHostInfo.GetDotNetExecCommandLine(vbc.PathToBuiltInTool, ""), vbc.GenerateCommandLineContents());
            }
        }

        [Fact]
        public void EditorConfig()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.AnalyzerConfigFiles = MSBuildUtil.CreateTaskItems(".editorconfig");
            Assert.Equal(@"/optionstrict:custom /out:test.exe /analyzerconfig:.editorconfig test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb", "subdir\\test.vb");
            vbc.AnalyzerConfigFiles = MSBuildUtil.CreateTaskItems(".editorconfig", "subdir\\.editorconfig");
            Assert.Equal(@$"/optionstrict:custom /out:test.exe /analyzerconfig:.editorconfig /analyzerconfig:subdir\.editorconfig test.vb subdir{Path.DirectorySeparatorChar}test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.AnalyzerConfigFiles = MSBuildUtil.CreateTaskItems("..\\.editorconfig", "sub dir\\.editorconfig");
            Assert.Equal(@"/optionstrict:custom /out:test.exe /analyzerconfig:..\.editorconfig /analyzerconfig:""sub dir\.editorconfig"" test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        [WorkItem(40926, "https://github.com/dotnet/roslyn/issues/40926")]
        public void SkipAnalyzersFlag()
        {
            var vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.SkipAnalyzers = true;
            Assert.Equal("/optionstrict:custom /out:test.exe /skipanalyzers+ test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.SkipAnalyzers = false;
            Assert.Equal("/optionstrict:custom /out:test.exe /skipanalyzers- test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            Assert.Equal("/optionstrict:custom /out:test.exe test.vb", vbc.GenerateResponseFileContents());
        }

        [Fact]
        [WorkItem(47790, "https://github.com/dotnet/roslyn/issues/47790")]
        public void CheckErrorAndWarningParsing()
        {
            var vbc = new Vbc();
            var engine = new ErrorLoggingEngine();
            vbc.BuildEngine = engine;
            // Use reflection to set protected property UsedCommandLineTool to true so Vbc.LogEventsFromTextOutput will pass the messages along to ParseVBErrorOrWarning.
            typeof(Vbc).GetProperty("UsedCommandLineTool", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(vbc, true);
            // Errors and warnings were generated by compiling the code used in the test at
            // Microsoft.CodeAnalysis.VisualBasic.CommandLine.UnitTests.CommandLineTests.LogErrorsWithColumnNumbers()
            vbc.LogCompilerOutput(@"
C:\Test Path (123)\hellovb.vb(6) : warning BC40008: 'Public Property x As Integer' is obsolete.

        x = 3.5
        ~      
C:\Test Path (123)\hellovb.vb(6) : error BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Integer'.

        x = 3.5
            ~~~
C:\Test Path (123)\hellovb.vb(7) : error BC30451: 'asdf' is not declared. It may be inaccessible due to its protection level.

        asdf
        ~~~~
", Build.Framework.MessageImportance.High);
            // Check that the column number is being added at the correct place.
            Assert.Contains(@"C:\Test Path (123)\hellovb.vb(6,9): warning BC40008: 'Public Property x As Integer' is obsolete.", engine.Log);
            Assert.Contains(@"C:\Test Path (123)\hellovb.vb(6,13): error BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Integer'.", engine.Log);
            Assert.Contains(@"C:\Test Path (123)\hellovb.vb(7,9): error BC30451: 'asdf' is not declared. It may be inaccessible due to its protection level.", engine.Log);
        }

        [Fact]
        [WorkItem(52467, "https://github.com/dotnet/roslyn/issues/52467")]
        public void UnexpectedExceptionLogsMessage()
        {
            var engine = new MockEngine();
            var vbc = new Vbc()
            {
                BuildEngine = engine,
            };

            vbc.ExecuteTool(@"q:\path\vbc.exe", "", "", new TestableCompilerServerLogger()
            {
                LogFunc = delegate { throw new Exception(""); }
            });
            Assert.False(string.IsNullOrEmpty(engine.Log));
        }

        [Fact]
        public void ReportIVTsSwitch()
        {
            var vbc = new Vbc();
            vbc.ReportIVTs = true;
            AssertEx.Equal("/optionstrict:custom /reportivts", vbc.GenerateResponseFileContents());
        }

        [Fact]
        public void CommandLineArgs1()
        {
            var engine = new MockEngine(TestOutputHelper);
            var vbc = new Vbc()
            {
                BuildEngine = engine,
                Sources = MSBuildUtil.CreateTaskItems("test.vb"),
            };

            TaskTestUtil.AssertCommandLine(vbc, engine, "/optionstrict:custom", "/out:test.exe", "test.vb");
        }

        [Fact]
        public void CommandLineArgs2()
        {
            var engine = new MockEngine(TestOutputHelper);
            var vbc = new Vbc()
            {
                BuildEngine = engine,
                Sources = MSBuildUtil.CreateTaskItems("test.vb", "blah.vb"),
                TargetType = "library"
            };

            TaskTestUtil.AssertCommandLine(vbc, engine, "/optionstrict:custom", "/out:test.dll", "/target:library", "test.vb", "blah.vb");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79907")]
        public void StdLib()
        {
            var vbc = new Vbc
            {
                Sources = MSBuildUtil.CreateTaskItems("test.vb"),
            };

            AssertEx.Equal("/optionstrict:custom /out:test.exe test.vb", vbc.GenerateResponseFileContents());
        }

        [ConditionalFact(typeof(UnixLikeOnly)), WorkItem("https://github.com/dotnet/roslyn/issues/80865")]
        public void SourceFileInRootDirectoryOnUnix()
        {
            // On Unix, a source file path starting with "/" without another "/" 
            // should be prefixed with "./" to avoid being misinterpreted as a command-line switch
            var vbc = new Vbc
            {
                Sources = MSBuildUtil.CreateTaskItems("/Program.vb"),
            };

            var responseFileContents = vbc.GenerateResponseFileContents();
            Assert.Contains("/./Program.vb", responseFileContents);
            Assert.DoesNotContain(" /Program.vb", responseFileContents);
        }

        [ConditionalFact(typeof(UnixLikeOnly)), WorkItem("https://github.com/dotnet/roslyn/issues/80865")]
        public void MultipleSourceFilesWithRootDirectoryOnUnix()
        {
            // Test multiple files where some are in root and some are not
            // Also test that /dir/file.vb is NOT transformed (has second '/')
            var vbc = new Vbc
            {
                Sources = MSBuildUtil.CreateTaskItems("/Program.vb", "src/Test.vb", "/App.vb", "/dir/File.vb"),
            };

            var responseFileContents = vbc.GenerateResponseFileContents();
            Assert.Contains("/./Program.vb", responseFileContents);
            Assert.Contains("/./App.vb", responseFileContents);
            Assert.Contains(" src/Test.vb", responseFileContents);
            // /dir/File.vb should NOT be transformed (contains second '/')
            Assert.Contains(" /dir/File.vb", responseFileContents);
            Assert.DoesNotContain("/./dir/File.vb", responseFileContents);
        }

        [ConditionalFact(typeof(WindowsOnly)), WorkItem("https://github.com/dotnet/roslyn/issues/80865")]
        public void SourceFilePathsOnWindows()
        {
            // On Windows, paths should not be transformed even if they start with "/"
            var vbc = new Vbc
            {
                Sources = MSBuildUtil.CreateTaskItems("test.vb", "/test.vb"),
            };

            var responseFileContents = vbc.GenerateResponseFileContents();
            Assert.Contains(" test.vb", responseFileContents);
            // On Windows, /test.vb should NOT be transformed
            Assert.Contains(" /test.vb", responseFileContents);
            Assert.DoesNotContain("/./test.vb", responseFileContents);
        }
    }
}

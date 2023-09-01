// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.BuildTasks;
using Xunit;
using Moq;
using System.IO;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.BuildTasks.UnitTests.TestUtilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public sealed class CscTests
    {
        public ITestOutputHelper TestOutputHelper { get; }

        public CscTests(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelper = testOutputHelper;
        }

        [Fact]
        public void SingleSource()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            Assert.Equal("/out:test.exe test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void MultipleSourceFiles()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test1.cs", "test2.cs");
            Assert.Equal("/out:test1.exe test1.cs test2.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void PathMapOption()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.PathMap = "K1=V1,K2=V2";
            Assert.Equal("/pathmap:\"K1=V1,K2=V2\" /out:test.exe test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            Assert.Equal("/out:test.exe test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void DeterministicFlag()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.Deterministic = true;
            Assert.Equal("/out:test.exe /deterministic+ test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.Deterministic = false;
            Assert.Equal("/out:test.exe /deterministic- test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            Assert.Equal("/out:test.exe test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void PublicSignFlag()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.PublicSign = true;
            Assert.Equal("/out:test.exe /publicsign+ test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.PublicSign = false;
            Assert.Equal("/out:test.exe /publicsign- test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            Assert.Equal("/out:test.exe test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void RuntimeMetadataVersionFlag()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.RuntimeMetadataVersion = "v1234";
            Assert.Equal("/out:test.exe /runtimemetadataversion:v1234 test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.RuntimeMetadataVersion = null;
            Assert.Equal("/out:test.exe test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void LangVersionFlag()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.LangVersion = "iso-1";
            Assert.Equal("/out:test.exe /langversion:iso-1 test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void ChecksumAlgorithmOption()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.ChecksumAlgorithm = "sha256";
            Assert.Equal("/out:test.exe /checksumalgorithm:sha256 test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.ChecksumAlgorithm = null;
            Assert.Equal("/out:test.exe test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.ChecksumAlgorithm = "";
            Assert.Equal("/out:test.exe /checksumalgorithm: test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void InstrumentTestNamesFlag()
        {
            var csc = new Csc();
            csc.Instrument = null;
            Assert.Equal(string.Empty, csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Instrument = "TestCoverage";
            Assert.Equal("/instrument:TestCoverage", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Instrument = "TestCoverage,Mumble";
            Assert.Equal("/instrument:TestCoverage,Mumble", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Instrument = "TestCoverage,Mumble;Stumble";
            Assert.Equal("/instrument:TestCoverage,Mumble,Stumble", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void TargetTypeDll()
        {
            var csc = new Csc();
            csc.TargetType = "library";
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            Assert.Equal("/out:test.dll /target:library test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void TargetTypeBad()
        {
            var csc = new Csc();
            csc.TargetType = "bad";
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            Assert.Equal("/out:test.exe /target:bad test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void OutputAssembly()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.OutputAssembly = MSBuildUtil.CreateTaskItem("x.exe");
            Assert.Equal("/out:x.exe test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void DefineConstantsSimple()
        {
            Action<string> test = (s) =>
            {
                var csc = new Csc();
                csc.DefineConstants = s;
                csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
                Assert.Equal("/define:D1;D2 /out:test.exe test.cs", csc.GenerateResponseFileContents());
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
                var csc = new Csc();
                csc.Features = s;
                csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
                Assert.Equal("/out:test.exe /features:a /features:b test.cs", csc.GenerateResponseFileContents());
            };

            test("a;b");
            test("a,b");
            test("a b");
            test(",a;b ");
            test(";a;;b;");
            test(",a,,b,");
        }

        [Fact]
        public void FeaturesInterceptorsPreview()
        {
            var csc = new Csc();
            csc.InterceptorsPreviewNamespaces = "NS1.NS2;NS3.NS4";
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            AssertEx.Equal("""/features:"InterceptorsPreviewNamespaces=NS1.NS2;NS3.NS4" /out:test.exe test.cs""", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void FeaturesEmpty()
        {
            foreach (var cur in new[] { "", null })
            {
                var csc = new Csc();
                csc.Features = cur;
                csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
                Assert.Equal("/out:test.exe test.cs", csc.GenerateResponseFileContents());
            }
        }

        [Fact]
        public void DebugType()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.DebugType = "full";
            Assert.Equal("/debug:full /out:test.exe test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.DebugType = "pdbonly";
            Assert.Equal("/debug:pdbonly /out:test.exe test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.DebugType = "portable";
            Assert.Equal("/debug:portable /out:test.exe test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.DebugType = "embedded";
            Assert.Equal("/debug:embedded /out:test.exe test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.DebugType = null;
            Assert.Equal("/out:test.exe test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.DebugType = "";
            Assert.Equal("/debug: /out:test.exe test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void SourceLink()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.DebugType = "portable";
            csc.SourceLink = @"C:\x y\z.json";
            Assert.Equal(@"/debug:portable /out:test.exe /sourcelink:""C:\x y\z.json"" test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.DebugType = "portable";
            csc.SourceLink = null;
            Assert.Equal(@"/debug:portable /out:test.exe test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.DebugType = "portable";
            csc.SourceLink = "";
            Assert.Equal(@"/debug:portable /out:test.exe /sourcelink: test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void Embed()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.DebugType = "portable";
            csc.EmbeddedFiles = MSBuildUtil.CreateTaskItems(@"test.cs", @"test.txt");
            Assert.Equal(@"/debug:portable /out:test.exe /embed:test.cs /embed:test.txt test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.DebugType = "portable";
            csc.EmbeddedFiles = MSBuildUtil.CreateTaskItems(@"C:\x y\z.json");
            Assert.Equal(@"/debug:portable /out:test.exe /embed:""C:\x y\z.json"" test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.DebugType = "portable";
            csc.EmbeddedFiles = null;
            Assert.Equal(@"/debug:portable /out:test.exe test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.DebugType = "full";
            csc.EmbeddedFiles = MSBuildUtil.CreateTaskItems();
            Assert.Equal(@"/debug:full /out:test.exe test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void EmbedAllSources()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.EmbeddedFiles = MSBuildUtil.CreateTaskItems(@"test.cs", @"test.txt");
            csc.EmbedAllSources = true;

            Assert.Equal(@"/out:test.exe /embed /embed:test.cs /embed:test.txt test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.EmbedAllSources = true;

            Assert.Equal(@"/out:test.exe /embed test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void RefOut()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.OutputRefAssembly = MSBuildUtil.CreateTaskItem("ref\\test.dll");
            Assert.Equal("/out:test.exe /refout:ref\\test.dll test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void RefOnly()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.RefOnly = true;
            Assert.Equal("/out:test.exe /refonly test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void NullableReferenceTypes_Enabled()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.Nullable = "enable";
            Assert.Equal("/nullable:enable /out:test.exe test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void NullableReferenceTypes_Disabled()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.Nullable = "disable";
            Assert.Equal("/nullable:disable /out:test.exe test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void NullableReferenceTypes_Safeonly()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.Nullable = "safeonly";
            Assert.Equal("/nullable:safeonly /out:test.exe test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void NullableReferenceTypes_Warnings()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.Nullable = "warnings";
            Assert.Equal("/nullable:warnings /out:test.exe test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void NullableReferenceTypes_Safeonlywarnings()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.Nullable = "safeonlywarnings";
            Assert.Equal("/nullable:safeonlywarnings /out:test.exe test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void NullableReferenceTypes_Default_01()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.Nullable = null;
            Assert.Equal("/out:test.exe test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void NullableReferenceTypes_Default_02()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            Assert.Equal("/out:test.exe test.cs", csc.GenerateResponseFileContents());
        }

        [Fact, WorkItem(29252, "https://github.com/dotnet/roslyn/issues/29252")]
        public void DisableSdkPath()
        {
            var csc = new Csc();
            csc.DisableSdkPath = true;
            Assert.Equal(@"/nosdkpath", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void SharedCompilationId()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.UseSharedCompilation = true;
            csc.SharedCompilationId = "testPipeName";
            Assert.Equal("/out:test.exe test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.UseSharedCompilation = false;
            csc.SharedCompilationId = "testPipeName";
            Assert.Equal("/out:test.exe test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.SharedCompilationId = "testPipeName";
            Assert.Equal("/out:test.exe test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void EmptyCscToolPath()
        {
            var csc = new Csc();
            csc.ToolPath = "";
            csc.ToolExe = Path.Combine("path", "to", "custom_csc");
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            Assert.Equal("", csc.GenerateCommandLineContents());
            Assert.Equal(Path.Combine("path", "to", "custom_csc"), csc.GeneratePathToTool());

            csc = new Csc();
            csc.ToolExe = Path.Combine("path", "to", "custom_csc");
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            Assert.Equal("", csc.GenerateCommandLineContents());
            Assert.Equal(Path.Combine("path", "to", "custom_csc"), csc.GeneratePathToTool());
        }

        [Fact]
        public void EmptyCscToolExe()
        {
            var csc = new Csc();
            csc.ToolPath = Path.Combine("path", "to", "custom_csc");
            csc.ToolExe = "";
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            Assert.Equal("", csc.GenerateCommandLineContents());
            // StartsWith because it can be csc.exe or csc.dll
            Assert.StartsWith(Path.Combine("path", "to", "custom_csc", "csc."), csc.GeneratePathToTool());

            csc = new Csc();
            csc.ToolPath = Path.Combine("path", "to", "custom_csc");
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            Assert.Equal("", csc.GenerateCommandLineContents());
            Assert.StartsWith(Path.Combine("path", "to", "custom_csc", "csc."), csc.GeneratePathToTool());
        }

        [Fact]
        public void EditorConfig()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.AnalyzerConfigFiles = MSBuildUtil.CreateTaskItems(".editorconfig");
            Assert.Equal(@"/out:test.exe /analyzerconfig:.editorconfig test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs", "subdir\\test.cs");
            csc.AnalyzerConfigFiles = MSBuildUtil.CreateTaskItems(".editorconfig", "subdir\\.editorconfig");
            Assert.Equal($@"/out:test.exe /analyzerconfig:.editorconfig /analyzerconfig:subdir\.editorconfig test.cs subdir{Path.DirectorySeparatorChar}test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.AnalyzerConfigFiles = MSBuildUtil.CreateTaskItems("..\\.editorconfig", "sub dir\\.editorconfig");
            Assert.Equal(@"/out:test.exe /analyzerconfig:..\.editorconfig /analyzerconfig:""sub dir\.editorconfig"" test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        [WorkItem(40926, "https://github.com/dotnet/roslyn/issues/40926")]
        public void SkipAnalyzersFlag()
        {
            var csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.SkipAnalyzers = true;
            Assert.Equal("/out:test.exe /skipanalyzers+ test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            csc.SkipAnalyzers = false;
            Assert.Equal("/out:test.exe /skipanalyzers- test.cs", csc.GenerateResponseFileContents());

            csc = new Csc();
            csc.Sources = MSBuildUtil.CreateTaskItems("test.cs");
            Assert.Equal("/out:test.exe test.cs", csc.GenerateResponseFileContents());
        }

        [Fact]
        [WorkItem(52467, "https://github.com/dotnet/roslyn/issues/52467")]
        public void UnexpectedExceptionLogsMessage()
        {
            var engine = new MockEngine();
            var csc = new Csc()
            {
                BuildEngine = engine,
            };

            csc.ExecuteTool(@"q:\path\csc.exe", "", "", new TestableCompilerServerLogger()
            {
                LogFunc = delegate { throw new Exception(""); }
            });
            Assert.False(string.IsNullOrEmpty(engine.Log));
        }

        [Fact]
        public void ReportIVTsSwitch()
        {
            var csc = new Csc();
            csc.ReportIVTs = true;
            AssertEx.Equal("/reportivts", csc.GenerateResponseFileContents());
        }

        [Fact]
        public void CommandLineArgs1()
        {
            var engine = new MockEngine(TestOutputHelper);
            var csc = new Csc()
            {
                BuildEngine = engine,
                Sources = MSBuildUtil.CreateTaskItems("test.cs"),
            };

            TaskTestUtil.AssertCommandLine(csc, engine, "/out:test.exe", "test.cs");
        }

        [Fact]
        public void CommandLineArgs2()
        {
            var engine = new MockEngine(TestOutputHelper);
            var csc = new Csc()
            {
                BuildEngine = engine,
                Sources = MSBuildUtil.CreateTaskItems("test.cs", "blah.cs"),
                TargetType = "library"
            };

            TaskTestUtil.AssertCommandLine(csc, engine, "/out:test.dll", "/target:library", "test.cs", "blah.cs");
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.BuildTasks;
using Xunit;
using Moq;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public sealed class CscTests
    {
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
            csc.DebugType = "portable";
            csc.EmbeddedFiles = MSBuildUtil.CreateTaskItems();
            Assert.Equal(@"/debug:portable /out:test.exe test.cs", csc.GenerateResponseFileContents());
        }
    }
}

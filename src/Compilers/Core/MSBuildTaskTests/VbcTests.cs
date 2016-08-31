// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.BuildTasks;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public sealed class VbcTests
    {
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
            vbc.DebugType = "portable";
            vbc.EmbeddedFiles = MSBuildUtil.CreateTaskItems();
            Assert.Equal(@"/optionstrict:custom /debug:portable /out:test.exe test.vb", vbc.GenerateResponseFileContents());
        }
    }
}

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
            Assert.Equal("/optionstrict:custom /deterministic+ /out:test.exe test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.Deterministic = false;
            Assert.Equal("/optionstrict:custom /deterministic- /out:test.exe test.vb", vbc.GenerateResponseFileContents());

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
            Assert.Equal("/optionstrict:custom /publicsign+ /out:test.exe test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            vbc.PublicSign = false;
            Assert.Equal("/optionstrict:custom /publicsign- /out:test.exe test.vb", vbc.GenerateResponseFileContents());

            vbc = new Vbc();
            vbc.Sources = MSBuildUtil.CreateTaskItems("test.vb");
            Assert.Equal("/optionstrict:custom /out:test.exe test.vb", vbc.GenerateResponseFileContents());
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
    }
}

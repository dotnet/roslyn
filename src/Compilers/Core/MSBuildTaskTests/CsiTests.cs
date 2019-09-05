// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public sealed class CsiTests
    {
        [Fact]
        public void SingleSource()
        {
            var csi = new Csi();
            csi.Source = MSBuildUtil.CreateTaskItem("test.csx");
            Assert.Equal("/i- test.csx", csi.GenerateResponseFileContents());
        }

        [Fact]
        public void Features()
        {
            Action<string> test = (s) =>
            {
                var csi = new Csi();
                csi.Features = s;
                csi.Source = MSBuildUtil.CreateTaskItem("test.csx");
                Assert.Equal("/i- /features:a /features:b test.csx", csi.GenerateResponseFileContents());
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
                var csi = new Csi();
                csi.Features = cur;
                csi.Source = MSBuildUtil.CreateTaskItem("test.csx");
                Assert.Equal("/i- test.csx", csi.GenerateResponseFileContents());
            }
        }

        [Fact]
        public void ScriptArguments()
        {
            var csi = new Csi();
            csi.Source = MSBuildUtil.CreateTaskItem("test.csx");
            csi.ScriptArguments = new[] { "-Arg1", "-Arg2" };
            Assert.Equal("/i- test.csx -Arg1 -Arg2", csi.GenerateResponseFileContents());
        }

        [Fact]
        public void ScriptArgumentsNeedQuotes()
        {
            var csi = new Csi();
            csi.Source = MSBuildUtil.CreateTaskItem("test.csx");
            csi.ScriptArguments = new[] { @"C:\Some Path\Some File.ini", @"C:\Some Path\Some Other File.bak" };
            Assert.Equal(@"/i- test.csx ""C:\Some Path\Some File.ini"" ""C:\Some Path\Some Other File.bak""", csi.GenerateResponseFileContents());
        }

        [Fact]
        public void QuotedScriptArguments()
        {
            var csi = new Csi();
            csi.Source = MSBuildUtil.CreateTaskItem("test.csx");
            csi.ScriptArguments = new[] { @"""C:\Some Path\Some File.ini""", @"""C:\Some Path\Some Other File.bak""" };
            Assert.Equal(@"/i- test.csx ""\""C:\Some Path\Some File.ini\"""" ""\""C:\Some Path\Some Other File.bak\""""", csi.GenerateResponseFileContents());
        }

        [Fact]
        public void NoScriptArguments()
        {
            var csi = new Csi();
            csi.Source = MSBuildUtil.CreateTaskItem("test.csx");
            csi.ScriptArguments = null;
            Assert.Equal(@"/i- test.csx", csi.GenerateResponseFileContents());
        }

        [Fact]
        public void EmptyScriptArguments()
        {
            var csi = new Csi();
            csi.Source = MSBuildUtil.CreateTaskItem("test.csx");
            csi.ScriptArguments = new string[0];
            Assert.Equal(@"/i- test.csx", csi.GenerateResponseFileContents());
        }
    }
}

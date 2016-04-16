// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class ProjectInfoTests
    {
        [Fact]
        public void Create()
        {
            var pid = ProjectId.CreateNewId();
            Assert.Throws<ArgumentNullException>(() => ProjectInfo.Create(id: null, version: VersionStamp.Default, name: "Foo", assemblyName: "Bar", language: "C#"));
            Assert.Throws<ArgumentNullException>(() => ProjectInfo.Create(pid, VersionStamp.Default, name: null, assemblyName: "Bar", language: "C#"));
            Assert.Throws<ArgumentNullException>(() => ProjectInfo.Create(pid, VersionStamp.Default, name: "Foo", assemblyName: null, language: "C#"));
            Assert.Throws<ArgumentNullException>(() => ProjectInfo.Create(pid, VersionStamp.Default, name: "Foo", assemblyName: "Bar", language: null));
        }

        [Fact]
        public void DebuggerDisplayHasProjectNameAndFilePath()
        {
            var projectInfo = ProjectInfo.Create(name: "Foo", filePath: @"C:\", id: ProjectId.CreateNewId(), version: VersionStamp.Default, assemblyName: "Bar", language: "C#");
            Assert.Equal(@"ProjectInfo Foo C:\", projectInfo.GetDebuggerDisplay());
        }

        [Fact]
        public void DebuggerDisplayHasOnlyProjectNameWhenFilePathNotSpecified()
        {
            var projectInfo = ProjectInfo.Create(name: "Foo", id: ProjectId.CreateNewId(), version: VersionStamp.Default, assemblyName: "Bar", language: "C#");
            Assert.Equal(@"ProjectInfo Foo", projectInfo.GetDebuggerDisplay());
        }
    }
}

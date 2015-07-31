// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using Roslyn.Test.Utilities;
using Xunit;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class ModuleMetadataTests : TestBase
    {
        [Fact]
        public void CreateFromFile()
        {
            Assert.Throws<ArgumentNullException>(() => ModuleMetadata.CreateFromFile((string)null));
            Assert.Throws<ArgumentException>(() => ModuleMetadata.CreateFromFile(""));
            Assert.Throws<ArgumentException>(() => ModuleMetadata.CreateFromFile(@"http://foo.bar"));
            Assert.Throws<ArgumentException>(() => ModuleMetadata.CreateFromFile(@"c:\*"));
            Assert.Throws<ArgumentException>(() => ModuleMetadata.CreateFromFile(@"\\.\COM1"));

            Assert.Throws<FileNotFoundException>(() => ModuleMetadata.CreateFromFile(@"C:\file_that_does_not_exists.dll"));
            Assert.Throws<FileNotFoundException>(() => ModuleMetadata.CreateFromFile(@"C:\directory_that_does_not_exists\file_that_does_not_exists.dll"));
        }
    }
}

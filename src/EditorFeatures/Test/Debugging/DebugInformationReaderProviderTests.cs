// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Debugging.UnitTests
{
    public class DebugInformationReaderProviderTests
    {
        [Fact]
        public void CreateFrom_Errors()
        {
            Assert.Throws<ArgumentException>(() => DebugInformationReaderProvider.CreateFromStream(new TestStream(canRead: false, canSeek: true, canWrite: true)));
            Assert.Throws<ArgumentException>(() => DebugInformationReaderProvider.CreateFromStream(new TestStream(canRead: true, canSeek: false, canWrite: true)));
            Assert.Throws<ArgumentNullException>(() => DebugInformationReaderProvider.CreateFromStream(null));
            Assert.Throws<ArgumentNullException>(() => DebugInformationReaderProvider.CreateFromMetadataReader(null));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Debugging.UnitTests;

public sealed class DebugInformationReaderProviderTests
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

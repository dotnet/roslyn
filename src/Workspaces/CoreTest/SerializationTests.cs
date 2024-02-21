// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public partial class SerializationTests : TestBase
    {
        [Fact]
        public void VersionStamp_RoundTripText()
        {
            var versionStamp = VersionStamp.Create();

            using var writerStream = new MemoryStream();

            using (var writer = new ObjectWriter(writerStream, leaveOpen: true))
            {
                versionStamp.WriteTo(writer);
            }

            using var readerStream = new MemoryStream(writerStream.ToArray());
            using var reader = ObjectReader.TryGetReader(readerStream);
            var deserializedVersionStamp = VersionStamp.ReadFrom(reader);

            Assert.Equal(versionStamp, deserializedVersionStamp);
        }
    }
}

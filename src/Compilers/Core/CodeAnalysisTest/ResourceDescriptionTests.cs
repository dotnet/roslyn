// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class ResourceDescriptionTests
    {
        [Fact]
        public void ResourceDescriptionCtors()
        {
            Func<Stream> data = () => null;

            // this is ok:
            new ResourceDescription("res", "file", data, isPublic: true);
            new ResourceDescription("re/s", "file", data, isPublic: true);
            new ResourceDescription("re\\s", "file", data, isPublic: true);
            new ResourceDescription("re\\s", "fil*<>|e", data, isPublic: true);

            // null:
            Assert.Throws<ArgumentNullException>(() => new ResourceDescription(null, "file", data, isPublic: true));
            Assert.Throws<ArgumentNullException>(() => new ResourceDescription("res", null, data, isPublic: true));
            Assert.Throws<ArgumentNullException>(() => new ResourceDescription("res", "file", null, isPublic: true));
            Assert.Throws<ArgumentNullException>(() => new ResourceDescription(null, data, isPublic: true));
            Assert.Throws<ArgumentNullException>(() => new ResourceDescription("res", null, isPublic: true));

            // empty:
            Assert.Throws<ArgumentException>(() => new ResourceDescription("", "file", data, isPublic: true));
            Assert.Throws<ArgumentException>(() => new ResourceDescription("res", "", data, isPublic: true));
            Assert.Throws<ArgumentException>(() => new ResourceDescription("res", "", data, isPublic: true));
            Assert.Throws<ArgumentException>(() => new ResourceDescription("", "file", data, isPublic: true));

            // invalid chars:
            Assert.Throws<ArgumentException>(() => new ResourceDescription("x", "x/x", data, isPublic: true));
            Assert.Throws<ArgumentException>(() => new ResourceDescription("x", "x\\x", data, isPublic: true));
            Assert.Throws<ArgumentException>(() => new ResourceDescription("x", "x:x", data, isPublic: true));
            Assert.Throws<ArgumentException>(() => new ResourceDescription("x", "\0", data, isPublic: true));
            Assert.Throws<ArgumentException>(() => new ResourceDescription("", "x", data, isPublic: true));
            Assert.Throws<ArgumentException>(() => new ResourceDescription("xxx\0xxxx", "", data, isPublic: true));
            Assert.Throws<ArgumentException>(() => new ResourceDescription("xxx\uD800asdas", "", data, isPublic: true));
            Assert.Throws<ArgumentException>(() => new ResourceDescription("xxx", "xxx\uD800asdas", data, isPublic: true));

            // Now checked during emit.
            new ResourceDescription(new string('e', 1024), data, true);
            new ResourceDescription("x", new string('e', 260), data, true);
        }
    }
}

// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            // Now checked during emit.
            Assert.DoesNotThrow(() => new ResourceDescription(new String('e', 1024), data, true));
            Assert.DoesNotThrow(() => new ResourceDescription("x", new String('e', 260), data, true));
        }
    }
}

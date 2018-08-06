// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.EditAndContinue;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EditAndContinue
{
    public sealed class DebuggeeModuleMetadataCacheTests
    {
        [Fact]
        public void GetOrAddNull()
        {
            var cache = new DebuggeeModuleMetadataCache();
            var mvid = Guid.NewGuid();

            Assert.Null(cache.GetOrAdd(mvid, m => { Assert.Equal(mvid, m); return default; }));
        }

        [Fact]
        public void GetOrAdd()
        {
            var cache = new DebuggeeModuleMetadataCache();

            var metadata1 = ModuleMetadata.CreateFromImage((IntPtr)1, 1);
            var metadata2 = ModuleMetadata.CreateFromImage((IntPtr)2, 1);

            var mvid1 = Guid.NewGuid();
            var mvid2 = Guid.NewGuid();
            var mvid3 = Guid.NewGuid();

            Assert.Same(metadata1, cache.GetOrAdd(mvid1, _ => metadata1));
            Assert.Same(metadata2, cache.GetOrAdd(mvid2, _ => metadata2));

            Assert.Same(metadata1, cache.GetOrAdd(mvid1, _ => throw null));
            Assert.Same(metadata2, cache.GetOrAdd(mvid2, _ => throw null));
        }

        [Fact]
        public void Remove()
        {
            var cache = new DebuggeeModuleMetadataCache();
            Assert.False(cache.Remove(Guid.NewGuid()));

            var mvid1 = Guid.NewGuid();

            cache.GetOrAdd(mvid1, _ => ModuleMetadata.CreateFromImage((IntPtr)1, 1));

            Assert.True(cache.Remove(mvid1));
            Assert.False(cache.Remove(mvid1));
            Assert.Null(cache.GetOrAdd(mvid1, _ => default));
        }

        [Fact]
        public void RemoveAdd()
        {
            var cache = new DebuggeeModuleMetadataCache();
            Assert.False(cache.Remove(Guid.NewGuid()));
            
            var mvid1 = Guid.NewGuid();

            var metadata1 = ModuleMetadata.CreateFromImage((IntPtr)1, 1);
            var metadata2 = ModuleMetadata.CreateFromImage((IntPtr)2, 1);

            Assert.Same(metadata1, cache.GetOrAdd(mvid1, _ => metadata1));

            Assert.True(cache.Remove(mvid1));
            Assert.Null(cache.GetOrAdd(mvid1, _ => default));

            Assert.Same(metadata2, cache.GetOrAdd(mvid1, _ => metadata2));
        }
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    public sealed class DebuggeeModuleMetadataCacheTests
    {
        [Fact]
        public void GetOrAddNull()
        {
            var cache = new DebuggeeModuleInfoCache();
            var mvid = Guid.NewGuid();

            Assert.Null(cache.GetOrAdd(mvid, m =>
            {
                Assert.Equal(mvid, m);
                return null;
            }));
        }

        [Fact]
        public void GetOrAdd()
        {
            var cache = new DebuggeeModuleInfoCache();

            var metadata1 = ModuleMetadata.CreateFromImage((IntPtr)1, 1);
            var symReader1 = NotImplementedSymUnmanagedReader.Instance;
            var metadata2 = ModuleMetadata.CreateFromImage((IntPtr)2, 1);
            var symReader2 = NotImplementedSymUnmanagedReader.Instance;

            var mvid1 = Guid.NewGuid();
            var mvid2 = Guid.NewGuid();
            var mvid3 = Guid.NewGuid();

            Assert.Same(metadata1, cache.GetOrAdd(mvid1, _ => new DebuggeeModuleInfo(metadata1, symReader1)).Metadata);
            Assert.Same(metadata2, cache.GetOrAdd(mvid2, _ => new DebuggeeModuleInfo(metadata2, symReader2)).Metadata);

            Assert.Same(metadata1, cache.GetOrAdd(mvid1, _ => throw null).Metadata);
            Assert.Same(metadata2, cache.GetOrAdd(mvid2, _ => throw null).Metadata);
        }

        [Fact]
        public void Remove()
        {
            var cache = new DebuggeeModuleInfoCache();
            Assert.False(cache.Remove(Guid.NewGuid()));

            var mvid1 = Guid.NewGuid();

            cache.GetOrAdd(mvid1, _ => new DebuggeeModuleInfo(ModuleMetadata.CreateFromImage((IntPtr)1, 1), NotImplementedSymUnmanagedReader.Instance));

            Assert.True(cache.Remove(mvid1));
            Assert.False(cache.Remove(mvid1));
            Assert.Null(cache.GetOrAdd(mvid1, _ => null));
        }

        [Fact]
        public void RemoveAdd()
        {
            var cache = new DebuggeeModuleInfoCache();
            Assert.False(cache.Remove(Guid.NewGuid()));

            var mvid1 = Guid.NewGuid();

            var metadata1 = ModuleMetadata.CreateFromImage((IntPtr)1, 1);
            var symReader1 = NotImplementedSymUnmanagedReader.Instance;
            var metadata2 = ModuleMetadata.CreateFromImage((IntPtr)2, 1);
            var symReader2 = NotImplementedSymUnmanagedReader.Instance;

            Assert.Same(metadata1, cache.GetOrAdd(mvid1, _ => new DebuggeeModuleInfo(metadata1, symReader1)).Metadata);

            Assert.True(cache.Remove(mvid1));
            Assert.Null(cache.GetOrAdd(mvid1, _ => null));

            Assert.Same(metadata2, cache.GetOrAdd(mvid1, _ => new DebuggeeModuleInfo(metadata2, symReader2)).Metadata);
        }
    }
}

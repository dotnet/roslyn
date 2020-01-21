// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public class CachingFactoryTests
    {
        private sealed class CacheKey
        {
            public CacheKey(int value) { this.Value = value; }
            public readonly int Value;
            public static int GetHashCode(CacheKey key) { return key.Value; }
        }

        private sealed class CacheValue
        {
            public CacheValue(int value) { this.Value = value; }
            public readonly int Value;
        }

        [WorkItem(620704, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/620704")]
        [Fact]
        public void ZeroHash()
        {
            var cache = new CachingFactory<CacheKey, CacheValue>(512,
                k => new CacheValue(k.Value + 1),
                k => k.Value,
                (k, v) => k.Value == v.Value);

            var key = new CacheKey(0);
            Assert.Equal(0, CacheKey.GetHashCode(key));

            CacheValue value;
            bool found = cache.TryGetValue(key, out value);
            Assert.False(found);
        }
    }
}

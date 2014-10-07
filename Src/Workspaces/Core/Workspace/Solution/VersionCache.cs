using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal class VersionCache<TValue> 
        where TValue : class
    {
        private readonly int maxVersions;
        private readonly NonReentrantLock gate = new NonReentrantLock();
        private readonly Dictionary<VersionStamp, TValue> map = new Dictionary<VersionStamp, TValue>();
        private readonly List<VersionStamp> list = new List<VersionStamp>();

        public VersionCache()
            : this(2)
        {
        }

        public VersionCache(int maxVersions)
        {
            this.maxVersions = maxVersions;
        }

        public bool TryGetValue(VersionStamp version, out TValue value)
        {
            using (this.gate.DisposableWait())
            {
                return this.map.TryGetValue(version, out value);
            }
        }

        public TValue GetValue(VersionStamp version, Func<VersionStamp, TValue> createValue)
        {
            TValue value;
            if (!this.TryGetValue(version, out value))
            {
                var newValue = createValue(version);

                using (this.gate.DisposableWait())
                {
                    if (!this.map.TryGetValue(version, out value))
                    {
                        // evict old versions
                        while (list.Count >= maxVersions)
                        {
                            var oldVersion = list[0];

                            // Abort if the newly computed data is for on even older version.
                            // This will allow the logically newer versions to stay in the cache.
                            if (oldVersion.IsNewerThan(version))
                            {
                                break;
                            }

                            list.RemoveAt(0);
                            this.map.Remove(oldVersion);
                        }

                        // add new version
                        value = newValue;
                        this.map.Add(version, value);
                        this.list.Add(version);
                    }
                }
            }

            return value;
        }
    }
}
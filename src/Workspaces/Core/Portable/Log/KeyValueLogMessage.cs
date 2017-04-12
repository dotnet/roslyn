// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// LogMessage that creates key value map lazily
    /// </summary>
    internal sealed class KeyValueLogMessage : LogMessage
    {
        private static readonly ObjectPool<KeyValueLogMessage> s_pool = new ObjectPool<KeyValueLogMessage>(() => new KeyValueLogMessage(), 20);

        public static readonly KeyValueLogMessage NoProperty = new KeyValueLogMessage();

        public static KeyValueLogMessage Create(Action<Dictionary<string, object>> propertySetter)
        {
            var logMessage = s_pool.Allocate();
            logMessage.Construct(propertySetter);

            return logMessage;
        }

        private Dictionary<string, object> _map;
        private Action<Dictionary<string, object>> _propertySetter;

        private KeyValueLogMessage()
        {
            // prevent it from being created directly
        }

        private void Construct(Action<Dictionary<string, object>> propertySetter)
        {
            _propertySetter = propertySetter;
        }

        public bool ContainsProperty
        {
            get
            {
                EnsureMap();
                return _map?.Count > 0;
            }
        }

        public IEnumerable<KeyValuePair<string, object>> Properties
        {
            get
            {
                EnsureMap();
                return _map;
            }
        }

        protected override string CreateMessage()
        {
            EnsureMap();
            return string.Join("|", _map.Select(kv => string.Format("{0}={1}", kv.Key, kv.Value)));
        }

        protected override void FreeCore()
        {
            if (_map != null)
            {
                SharedPools.Default<Dictionary<string, object>>().ClearAndFree(_map);
                _map = null;
            }

            if (_propertySetter != null)
            {
                _propertySetter = null;
                s_pool.Free(this);
            }
        }

        private void EnsureMap()
        {
            if (_map == null && _propertySetter != null)
            {
                _map = SharedPools.Default<Dictionary<string, object>>().AllocateAndClear();
                _propertySetter(_map);
            }
        }
    }
}

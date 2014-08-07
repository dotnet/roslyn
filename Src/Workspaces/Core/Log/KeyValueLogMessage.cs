// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private static readonly ObjectPool<KeyValueLogMessage> Pool = new ObjectPool<KeyValueLogMessage>(() => new KeyValueLogMessage(), 20);

        public static KeyValueLogMessage Create(Action<Dictionary<string, string>> propertySetter)
        {
            var logMessage = Pool.Allocate();
            logMessage.Constrcut(propertySetter);

            return logMessage;
        }

        private Dictionary<string, string> map = null;
        private Action<Dictionary<string, string>> propertySetter = null;

        private KeyValueLogMessage()
        {
            // prevent it from being created directly
        }

        private void Constrcut(Action<Dictionary<string, string>> propertySetter)
        {
            this.propertySetter = propertySetter;
        }

        public bool ContainsProperty
        {
            get
            {
                EnsureMap();
                return this.map.Count > 0;
            }
        }

        public IEnumerable<KeyValuePair<string, string>> Properties
        {
            get
            {
                EnsureMap();
                return this.map;
            }
        }

        protected override string CreateMessage()
        {
            EnsureMap();
            return string.Join("|", map.Select(kv => string.Format("{0}={1}", kv.Key, kv.Value)));
        }

        public override void Free()
        {
            if (this.map != null)
            {
                SharedPools.Default<Dictionary<string, string>>().ClearAndFree(this.map);
                this.map = null;
            }

            if (this.propertySetter != null)
            {
                this.propertySetter = null;
                Pool.Free(this);
            }
        }

        private void EnsureMap()
        {
            if (this.map == null && this.propertySetter != null)
            {
                this.map = SharedPools.Default<Dictionary<string, string>>().AllocateAndClear();
                this.propertySetter(this.map);
            }
        }
    }
}
